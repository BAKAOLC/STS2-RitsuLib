using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Maps model entry IDs to localized display titles for dev-console autocomplete.
    /// </summary>
    internal static class DevConsoleModelIdAutocompleteCatalog
    {
        private static readonly Lock Sync = new();
        private static Dictionary<string, string>? _titlesByEntry;
        private static string? _builtForLanguage;

        /// <summary>
        ///     Returns the localized title for <paramref name="entryId" />, or null when unknown or empty.
        /// </summary>
        public static string? TryGetLocalizedTitle(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
                return null;

            EnsureBuilt();
            return _titlesByEntry!.GetValueOrDefault(entryId.Trim());
        }

        /// <summary>
        ///     Returns whether <paramref name="partial" /> matches the localized title of <paramref name="entryId" />.
        /// </summary>
        public static bool MatchesLocalizedTitle(string entryId, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var title = TryGetLocalizedTitle(entryId);
            return !string.IsNullOrWhiteSpace(title) &&
                   title.Contains(partial.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureBuilt()
        {
            var language = I18N.ResolveCurrentLanguageCode();
            lock (Sync)
            {
                if (_titlesByEntry != null &&
                    string.Equals(_builtForLanguage, language, StringComparison.OrdinalIgnoreCase))
                    return;

                _titlesByEntry = BuildTitles();
                _builtForLanguage = language;
            }
        }

        private static Dictionary<string, string> BuildTitles()
        {
            var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var (entryId, locString) in DevConsoleAutocompleteCandidateSources
                             .EnumerateLocalizedModelTitles())
                    TryAddTitle(titles, entryId, locString);
            }
            catch
            {
                // ModelDb may be unavailable before content init.
            }

            return titles;
        }

        private static void TryAddTitle(Dictionary<string, string> titles, string entryId, LocString locString)
        {
            try
            {
                var text = locString.GetFormattedText()?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                titles.TryAdd(entryId, text);
            }
            catch
            {
                // Loc tables may be unavailable before content init.
            }
        }
    }
}
