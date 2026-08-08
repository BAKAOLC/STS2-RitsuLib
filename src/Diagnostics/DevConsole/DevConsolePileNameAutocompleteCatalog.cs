using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps developer-console pile-argument tokens to localized display titles.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将开发者控制台的牌堆参数令牌映射到本地化显示标题。
    ///     </para>
    /// </summary>
    public static class DevConsolePileNameAutocompleteCatalog
    {
        private static readonly Lock Sync = new();
        private static Dictionary<string, string>? _titlesByToken;
        private static string? _builtForLanguage;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the localized title for <paramref name="token" />, or <see langword="null" /> when it is
        ///         unknown or empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="token" /> 的本地化标题；标题未知或为空时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        public static string? TryGetLocalizedTitle(string token)
        {
            return string.IsNullOrWhiteSpace(token)
                ? null
                : GetTitlesSnapshot().GetValueOrDefault(token.Trim());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="partial" /> occurs in the localized title of
        ///         <paramref name="token" /> or one of its enabled search expansions, ignoring case.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="token" /> 的本地化标题或已启用搜索扩展是否包含
        ///         <paramref name="partial" />，忽略大小写。
        ///     </para>
        /// </summary>
        public static bool MatchesLocalizedTitle(string token, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var title = GetTitlesSnapshot().GetValueOrDefault(token.Trim());
            return DevConsoleAutocompleteMatchExtensions.MatchesLocalizedText(title, partial);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends registered mod pile IDs that are not already present in <paramref name="candidates" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="candidates" /> 中尚不存在的已注册模组牌堆 ID 追加到其中。
        ///     </para>
        /// </summary>
        public static void AppendModPileCandidates(IList<string> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            foreach (var definition in ModCardPileRegistry.GetDefinitionsSnapshot())
            {
                if (candidates.Any(c => c.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;

                candidates.Add(definition.Id);
            }
        }

        private static Dictionary<string, string> EnsureBuilt()
        {
            var language = I18N.ResolveCurrentLanguageCode();
            lock (Sync)
            {
                if (_titlesByToken != null &&
                    string.Equals(_builtForLanguage, language, StringComparison.OrdinalIgnoreCase))
                    return _titlesByToken;

                _titlesByToken = BuildTitles();
                _builtForLanguage = language;
                return _titlesByToken;
            }
        }

        internal static IReadOnlyDictionary<string, string> GetTitlesSnapshot()
        {
            return EnsureBuilt();
        }

        private static Dictionary<string, string> BuildTitles()
        {
            var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in Enum.GetNames<PileType>())
            {
                if (!Enum.TryParse<PileType>(name, out var pileType))
                    continue;

                if (ModCardPileRegistry.TryGetByPileType(pileType, out var modDefinition))
                    TryAddTitle(titles, name, modDefinition.Title);
            }

            foreach (var definition in ModCardPileRegistry.GetDefinitionsSnapshot())
            {
                TryAddTitle(titles, definition.Id, definition.Title);

                if (ModCardPileRegistry.TryGetId(definition.PileType, out var mintedId) &&
                    !mintedId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase))
                    TryAddTitle(titles, mintedId, definition.Title);
            }

            return titles;
        }

        private static void TryAddTitle(Dictionary<string, string> titles, string token, LocString locString)
        {
            try
            {
                var text = locString.GetFormattedText()?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                titles.TryAdd(token, text);
            }
            catch
            {
                // Loc tables may be unavailable before content init.
            }
        }
    }
}
