using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps model entry IDs to localized display titles for developer-console autocomplete.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将模型条目 ID 映射到开发者控制台自动补全所用的本地化显示标题。
    ///     </para>
    /// </summary>
    internal static class DevConsoleModelIdAutocompleteCatalog
    {
        private static readonly Lock Sync = new();
        private static Dictionary<string, string>? _titlesByEntry;
        private static string? _builtForLanguage;

        static DevConsoleModelIdAutocompleteCatalog()
        {
            RitsuLibFramework.SubscribeLifecycle<ModelRegistryInitializedEvent>(_ => Invalidate(), false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the localized title for <paramref name="entryId" />, or <see langword="null" /> when it is
        ///         unknown or empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="entryId" /> 的本地化标题；标题未知或为空时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        public static string? TryGetLocalizedTitle(string entryId)
        {
            return string.IsNullOrWhiteSpace(entryId)
                ? null
                : GetTitlesSnapshot().GetValueOrDefault(entryId.Trim());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="partial" /> occurs in the localized title of
        ///         <paramref name="entryId" />, ignoring case.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="entryId" /> 的本地化标题是否包含 <paramref name="partial" />，忽略大小写。
        ///     </para>
        /// </summary>
        public static bool MatchesLocalizedTitle(string entryId, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var title = GetTitlesSnapshot().GetValueOrDefault(entryId.Trim());
            return !string.IsNullOrWhiteSpace(title) &&
                   title.Contains(partial.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        internal static IReadOnlyDictionary<string, string> GetTitlesSnapshot()
        {
            return EnsureBuilt();
        }

        private static Dictionary<string, string> EnsureBuilt()
        {
            var language = I18N.ResolveCurrentLanguageCode();
            lock (Sync)
            {
                if (_titlesByEntry != null &&
                    string.Equals(_builtForLanguage, language, StringComparison.OrdinalIgnoreCase))
                    return _titlesByEntry;

                _titlesByEntry = BuildTitles();
                _builtForLanguage = language;
                return _titlesByEntry;
            }
        }

        private static void Invalidate()
        {
            lock (Sync)
            {
                _titlesByEntry = null;
                _builtForLanguage = null;
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
