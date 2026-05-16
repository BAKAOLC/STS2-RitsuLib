using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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
                foreach (var card in ModelDb.AllCards)
                    TryAddTitle(titles, card.Id.Entry, card.TitleLocString);

                foreach (var potion in ModelDb.AllPotions)
                    TryAddTitle(titles, potion.Id.Entry, potion.Title);

                foreach (var relic in ModelDb.AllRelics)
                    TryAddTitle(titles, relic.Id.Entry, relic.Title);

                foreach (var encounter in ModelDb.AllEncounters)
                    TryAddTitle(titles, encounter.Id.Entry, encounter.Title);

                foreach (var affliction in ModelDb.DebugAfflictions)
                    TryAddTitle(titles, affliction.Id.Entry, affliction.Title);

                foreach (var enchantment in ModelDb.DebugEnchantments)
                    TryAddTitle(titles, enchantment.Id.Entry, enchantment.Title);

                foreach (var ancient in ModelDb.AllAncients)
                    TryAddTitle(titles, ancient.Id.Entry, ancient.Title);

                foreach (var evt in ModelDb.AllEvents)
                    TryAddTitle(titles, evt.Id.Entry, evt.Title);
            }
            catch
            {
                // ModelDb may be unavailable before content init.
            }

            return titles;
        }

        private static void TryAddTitle(Dictionary<string, string> titles, string entryId, LocString locString)
        {
            var formatted = TryFormatLocString(locString);
            if (formatted == null)
                return;

            titles.TryAdd(entryId, formatted);
        }

        private static void TryAddTitle(Dictionary<string, string> titles, string entryId, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            titles.TryAdd(entryId, title.Trim());
        }

        private static string? TryFormatLocString(LocString locString)
        {
            try
            {
                var text = locString.GetFormattedText()?.Trim();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch
            {
                return null;
            }
        }
    }
}
