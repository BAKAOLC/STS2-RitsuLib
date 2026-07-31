using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves ancient-event option tokens and localized titles for developer-console autocomplete.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为开发者控制台自动补全解析先古之民事件选项标记及其本地化标题。
    ///     </para>
    /// </summary>
    internal static class DevConsoleAncientChoiceAutocompleteCatalog
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the display title of <paramref name="choiceToken" /> in the specified ancient event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回指定先古之民事件中 <paramref name="choiceToken" /> 的显示标题。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="partial" /> matches the option title or the linked relic's ID or
        ///         title.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="partial" /> 是否匹配选项标题或关联遗物的 ID、标题。
        ///     </para>
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
