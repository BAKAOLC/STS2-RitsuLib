using MegaCrit.Sts2.Core.DevConsole;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Applies registered dev-console autocomplete enhancements to matchers and results.
    /// </summary>
    public static class DevConsoleAutocompleteEnhancer
    {
        /// <summary>
        ///     Builds a match predicate chain for <paramref name="enhancements" />.
        /// </summary>
        public static Func<string, string, bool>? BuildMatchPredicate(
            DevConsoleAutocompleteEnhancements enhancements,
            Func<string, string, bool>? inner = null,
            IReadOnlyList<string>? completedArgs = null)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return inner;

            var predicate = inner;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.RitsuLibOwnedIdShorthandMatch) &&
                predicate == null)
                predicate = DevConsoleAutocompleteOwnedIdMatch.Match;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch(predicate);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.PileNameLocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithLocalizedPileTitleMatch(predicate);

            if (!enhancements.HasFlag(DevConsoleAutocompleteEnhancements.AncientChoiceLocalizedTitleMatch))
                return predicate;
            var ancientEntryId = completedArgs is { Count: > 0 } ? completedArgs[0] : null;
            predicate = DevConsoleAutocompleteMatchExtensions.WithAncientChoiceLocalizedMatch(
                predicate,
                ancientEntryId);

            return predicate;
        }

        /// <summary>
        ///     Applies result-side enhancements such as localized labels and de-duplication.
        /// </summary>
        public static void ApplyToResult(
            ref CompletionResult result,
            DevConsoleAutocompleteEnhancements enhancements,
            IReadOnlyList<string>? completedArgs = null)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.IncludeModPileCandidates))
                DevConsolePileNameAutocompleteCatalog.AppendModPileCandidates(result.Candidates);

            if (result.Candidates.Count == 0)
                return;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.AncientChoiceDisplayLabels) &&
                completedArgs is { Count: > 0 })
                DevConsoleAutocompleteMatchExtensions.ApplyAncientChoiceDisplayLabels(ref result, completedArgs[0]);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.PileNameDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplyPileDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.DeduplicateCandidates))
                result.Candidates = result.Candidates
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
    }
}
