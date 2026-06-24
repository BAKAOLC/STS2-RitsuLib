using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Maps dev-console pile argument tokens to localized display titles.
    /// </summary>
    public static class DevConsolePileNameAutocompleteCatalog
    {
        private static readonly Lock Sync = new();
        private static Dictionary<string, string>? _titlesByToken;
        private static string? _builtForLanguage;

        /// <summary>
        ///     Returns the localized title for <paramref name="token" />, or null when unknown or empty.
        /// </summary>
        public static string? TryGetLocalizedTitle(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            EnsureBuilt();
            return _titlesByToken!.GetValueOrDefault(token.Trim());
        }

        /// <summary>
        ///     Returns whether <paramref name="partial" /> matches the localized title of <paramref name="token" />.
        /// </summary>
        public static bool MatchesLocalizedTitle(string token, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var title = TryGetLocalizedTitle(token);
            return !string.IsNullOrWhiteSpace(title) &&
                   title.Contains(partial.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Appends registered mod pile ids that are not already present in <paramref name="candidates" />.
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

        private static void EnsureBuilt()
        {
            var language = I18N.ResolveCurrentLanguageCode();
            lock (Sync)
            {
                if (_titlesByToken != null &&
                    string.Equals(_builtForLanguage, language, StringComparison.OrdinalIgnoreCase))
                    return;

                _titlesByToken = BuildTitles();
                _builtForLanguage = language;
            }
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
