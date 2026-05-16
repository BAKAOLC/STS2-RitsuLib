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
            Func<string, string, bool>? inner = null)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return inner;

            var predicate = inner;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.RitsuLibOwnedIdShorthandMatch) &&
                predicate == null)
                predicate = DevConsoleAutocompleteOwnedIdMatch.Match;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch(predicate);

            return predicate;
        }

        /// <summary>
        ///     Applies result-side enhancements such as localized labels and de-duplication.
        /// </summary>
        public static void ApplyToResult(
            ref CompletionResult result,
            DevConsoleAutocompleteEnhancements enhancements)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None || result.Candidates.Count == 0)
                return;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.DeduplicateCandidates))
                result.Candidates = result.Candidates
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
    }
}
