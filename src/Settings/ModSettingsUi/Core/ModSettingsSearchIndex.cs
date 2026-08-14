using STS2RitsuLib.Search;

namespace STS2RitsuLib.Settings
{
    internal sealed record ModSettingsSearchResult(
        string Title,
        string Path,
        string SearchText,
        ModSettingsLocation Location,
        int Order)
    {
        internal RitsuSearchPreparedText PreparedTitle { get; } = new(Title);
        internal RitsuSearchPreparedText PreparedPath { get; } = new(Path);
        internal RitsuSearchPreparedText PreparedSearchText { get; } = new(SearchText);
    }

    internal sealed record ModSettingsSearchBatch(
        IReadOnlyList<ModSettingsSearchResult> Results,
        int ProcessedCount,
        int TotalCount,
        bool IsComplete,
        bool HasMore);

    internal static class ModSettingsSearchIndex
    {
        internal static IReadOnlyList<ModSettingsSearchResult> BuildVisible()
        {
            var results = new List<ModSettingsSearchResult>();
            var pages = ModSettingsRegistry.GetPages();
            var pageMap = pages.ToDictionary(
                static page => CreatePageKey(page.ModId, page.Id),
                StringComparer.OrdinalIgnoreCase);
            var order = 0;

            foreach (var page in pages)
            {
                if (!ModSettingsVisibility.IsPageVisible(page))
                    continue;

                var modTitle = ModSettingsLocalization.ResolveModName(page.ModId, page.ModId);
                var modFallbackTitle = ModSettingsLocalization.ResolveModNameFallback(page.ModId, page.ModId);
                var pageTitles = ResolvePageTitles(page, pageMap, false);
                var pageFallbackTitles = ResolvePageTitles(page, pageMap, true);
                var pageTitle = pageTitles[^1];
                var pagePath = JoinPath([modTitle, .. pageTitles]);
                results.Add(CreateResult(
                    pageTitle,
                    JoinPath([modTitle, .. pageTitles.Take(pageTitles.Count - 1)]),
                    ModSettingsUiContext.ResolvePageDescription(page),
                    new(page.ModId, page.Id),
                    order++,
                    [modFallbackTitle, .. pageFallbackTitles, page.Description?.FallbackText ?? string.Empty],
                    page.ModId,
                    page.Id));

                foreach (var section in page.Sections)
                {
                    if (!ModSettingsVisibility.IsSectionVisible(page, section))
                        continue;

                    var sectionTitle = ResolveSectionTitle(section);
                    if (section.Title != null)
                        results.Add(CreateResult(
                            sectionTitle,
                            pagePath,
                            section.Description?.Resolve(),
                            new(page.ModId, page.Id, section.Id),
                            order++,
                            [
                                modFallbackTitle,
                                .. pageFallbackTitles,
                                section.Title?.FallbackText ?? string.Empty,
                                section.Description?.FallbackText ?? string.Empty,
                            ],
                            page.ModId,
                            page.Id,
                            section.Id));

                    var sectionPath = section.Title == null
                        ? pagePath
                        : JoinPath([modTitle, .. pageTitles, sectionTitle]);
                    foreach (var entry in section.Entries)
                    {
                        if (!ModSettingsVisibility.IsEntryVisible(page, entry))
                            continue;

                        var entryTitle = ModSettingsUiFactory.ResolveEntryLabelDisplay(entry.Label);
                        results.Add(CreateResult(
                            entryTitle,
                            sectionPath,
                            entry.Description?.Resolve(),
                            new(page.ModId, page.Id, section.Id, entry.Id),
                            order++,
                            [
                                modFallbackTitle,
                                .. pageFallbackTitles,
                                section.Title?.FallbackText ?? string.Empty,
                                entry.Label.FallbackText ?? string.Empty,
                                entry.Description?.FallbackText ?? string.Empty,
                            ],
                            page.ModId,
                            page.Id,
                            section.Id,
                            entry.Id));
                    }
                }
            }

            return results.AsReadOnly();
        }

        internal static IReadOnlyList<ModSettingsSearchResult> Search(
            IReadOnlyList<ModSettingsSearchResult> index,
            string query,
            int limit)
        {
            ArgumentNullException.ThrowIfNull(index);
            ArgumentNullException.ThrowIfNull(query);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

            var terms = query.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length == 0)
                return [];

            return
            [
                .. index
                    .Select(result => (Result: result, Score: Score(result, terms)))
                    .Where(static match => match.Score >= 0)
                    .OrderBy(static match => match.Score)
                    .ThenBy(static match => match.Result.Order)
                    .Take(limit)
                    .Select(static match => match.Result),
            ];
        }

        internal static async Task<ModSettingsSearchBatch> SearchAsync(
            IReadOnlyList<ModSettingsSearchResult> index,
            string query,
            int limit,
            CancellationToken cancellationToken = default)
        {
            ModSettingsSearchBatch? completed = null;
            await foreach (var batch in SearchStreamAsync(index, query, limit, cancellationToken)
                               .ConfigureAwait(false))
                completed = batch;
            return completed ?? new([], 0, index.Count, true, false);
        }

        internal static async IAsyncEnumerable<ModSettingsSearchBatch> SearchStreamAsync(
            IReadOnlyList<ModSettingsSearchResult> index,
            string query,
            int limit,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(index);
            ArgumentNullException.ThrowIfNull(query);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

            var terms = query.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length == 0)
            {
                yield return new([], 0, index.Count, true, false);
                yield break;
            }

            const int resultsPerBatch = 12;
            var matches = new List<(ModSettingsSearchResult Result, int Score)>();
            for (var indexPosition = 0; indexPosition < index.Count; indexPosition++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = index[indexPosition];
                var score = await ScoreAsync(result, terms, cancellationToken).ConfigureAwait(false);
                if (score >= 0)
                    matches.Add((result, score));

                var processedCount = indexPosition + 1;
                if (matches.Count > limit)
                {
                    yield return CreateBatch(matches, limit, processedCount, index.Count, true, true);
                    yield break;
                }

                if (processedCount % resultsPerBatch == 0)
                    yield return CreateBatch(matches, limit, processedCount, index.Count, false, false);
            }

            yield return CreateBatch(matches, limit, index.Count, index.Count, true, false);
        }

        private static ModSettingsSearchResult CreateResult(
            string title,
            string path,
            string? description,
            ModSettingsLocation location,
            int order,
            IReadOnlyList<string> alternateTexts,
            params string[] ids)
        {
            var displayTitle = string.IsNullOrWhiteSpace(title) ? ids[^1] : title.Trim();
            string[] searchParts =
                [displayTitle, path, description ?? string.Empty, .. alternateTexts, .. ids];
            var searchText = string.Join('\n',
                searchParts.Where(static value => !string.IsNullOrWhiteSpace(value)));
            return new(displayTitle, path, searchText, location, order);
        }

        private static int Score(ModSettingsSearchResult result, IReadOnlyList<string> terms)
        {
            var score = 0;
            foreach (var term in terms)
            {
                var titleIndex = result.Title.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (titleIndex >= 0)
                {
                    score += titleIndex == 0 ? 0 : 12 + titleIndex;
                    continue;
                }

                var pathIndex = result.Path.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (pathIndex >= 0)
                {
                    score += 40 + pathIndex;
                    continue;
                }

                var searchIndex = result.SearchText.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (searchIndex >= 0)
                {
                    score += 80 + searchIndex;
                    continue;
                }

                var expandedTitleScore = result.PreparedTitle.ScoreExpansion(term);
                if (expandedTitleScore >= 0)
                {
                    score += 100 + expandedTitleScore;
                    continue;
                }

                var expandedPathScore = result.PreparedPath.ScoreExpansion(term);
                if (expandedPathScore >= 0)
                {
                    score += 140 + expandedPathScore;
                    continue;
                }

                var expandedSearchScore = result.PreparedSearchText.ScoreExpansion(term);
                if (expandedSearchScore < 0)
                    return -1;
                score += 180 + expandedSearchScore;
            }

            return score;
        }

        private static async ValueTask<int> ScoreAsync(
            ModSettingsSearchResult result,
            IReadOnlyList<string> terms,
            CancellationToken cancellationToken)
        {
            var score = 0;
            foreach (var term in terms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var titleIndex = result.Title.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (titleIndex >= 0)
                {
                    score += titleIndex == 0 ? 0 : 12 + titleIndex;
                    continue;
                }

                var pathIndex = result.Path.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (pathIndex >= 0)
                {
                    score += 40 + pathIndex;
                    continue;
                }

                var searchIndex = result.SearchText.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (searchIndex >= 0)
                {
                    score += 80 + searchIndex;
                    continue;
                }

                var expandedTitleScore = await result.PreparedTitle.ScoreExpansionAsync(term, cancellationToken)
                    .ConfigureAwait(false);
                if (expandedTitleScore >= 0)
                {
                    score += 100 + expandedTitleScore;
                    continue;
                }

                var expandedPathScore = await result.PreparedPath.ScoreExpansionAsync(term, cancellationToken)
                    .ConfigureAwait(false);
                if (expandedPathScore >= 0)
                {
                    score += 140 + expandedPathScore;
                    continue;
                }

                var expandedSearchScore = await result.PreparedSearchText.ScoreExpansionAsync(term, cancellationToken)
                    .ConfigureAwait(false);
                if (expandedSearchScore < 0)
                    return -1;
                score += 180 + expandedSearchScore;
            }

            return score;
        }

        private static ModSettingsSearchBatch CreateBatch(
            IReadOnlyList<(ModSettingsSearchResult Result, int Score)> matches,
            int limit,
            int processedCount,
            int totalCount,
            bool isComplete,
            bool hasMore)
        {
            IReadOnlyList<ModSettingsSearchResult> results =
            [
                .. matches
                    .OrderBy(static match => match.Score)
                    .ThenBy(static match => match.Result.Order)
                    .Take(limit)
                    .Select(static match => match.Result),
            ];
            return new(results, processedCount, totalCount, isComplete, hasMore);
        }

        private static IReadOnlyList<string> ResolvePageTitles(
            ModSettingsPage page,
            IReadOnlyDictionary<string, ModSettingsPage> pageMap,
            bool useFallbackText)
        {
            var titles = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = page;
            while (current != null)
            {
                var key = CreatePageKey(current.ModId, current.Id);
                if (!visited.Add(key))
                    break;
                titles.Add(useFallbackText
                    ? current.Title?.FallbackText ?? current.Id
                    : ModSettingsLocalization.ResolvePageDisplayName(current));
                current = string.IsNullOrWhiteSpace(current.ParentPageId) ||
                          !pageMap.TryGetValue(CreatePageKey(current.ModId, current.ParentPageId), out var parent)
                    ? null
                    : parent;
            }

            titles.Reverse();
            return titles;
        }

        private static string ResolveSectionTitle(ModSettingsSection section)
        {
            var title = section.Title?.Resolve();
            return string.IsNullOrWhiteSpace(title)
                ? ModSettingsLocalization.Get("section.default", "Section")
                : title;
        }

        private static string JoinPath(IEnumerable<string> parts)
        {
            return string.Join("  ›  ", parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string CreatePageKey(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
