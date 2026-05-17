using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Resolves ancient event option tokens to localized display titles for dev-console autocomplete.
    /// </summary>
    internal static class DevConsoleAncientChoiceAutocompleteCatalog
    {
        /// <summary>
        ///     Returns a display title for <paramref name="choiceToken" /> under <paramref name="ancientEntryId" />.
        /// </summary>
        public static string? TryGetDisplayTitle(string ancientEntryId, string choiceToken)
        {
            var option = TryFindOption(ancientEntryId, choiceToken);
            if (option == null)
                return null;

            var title = option.Title.GetFormattedText()?.Trim();
            return !string.IsNullOrWhiteSpace(title) ? title : option.Relic?.Title.GetFormattedText()?.Trim();
        }

        /// <summary>
        ///     Returns whether <paramref name="partial" /> matches the option title or linked relic id/title.
        /// </summary>
        public static bool MatchesLocalizedTitle(string ancientEntryId, string choiceToken, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var option = TryFindOption(ancientEntryId, choiceToken);
            if (option == null)
                return false;

            var trimmed = partial.Trim();

            var title = option.Title.GetFormattedText()?.Trim();
            if (!string.IsNullOrWhiteSpace(title) &&
                title.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                return true;

            if (option.Relic == null)
                return false;

            var relic = option.Relic;
            if (relic.Id.Entry.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                return true;

            var relicTitle = relic.Title.GetFormattedText()?.Trim();
            return !string.IsNullOrWhiteSpace(relicTitle) &&
                   relicTitle.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
        }

        private static EventOption? TryFindOption(string ancientEntryId, string choiceToken)
        {
            if (string.IsNullOrWhiteSpace(ancientEntryId) || string.IsNullOrWhiteSpace(choiceToken))
                return null;

            if (TryGetAncient(ancientEntryId) is not { } ancient)
                return null;

            return ancient.AllPossibleOptions.FirstOrDefault(option =>
                option.TextKey.Split('.').Last().Equals(choiceToken, StringComparison.OrdinalIgnoreCase) ||
                option.TextKey.Contains(choiceToken, StringComparison.OrdinalIgnoreCase));
        }

        private static AncientEventModel? TryGetAncient(string ancientEntryId)
        {
            var id = new ModelId(ModelDb.GetCategory(typeof(EventModel)), ancientEntryId.ToUpperInvariant());
            return ModelDb.GetByIdOrNull<EventModel>(id) as AncientEventModel;
        }
    }
}
