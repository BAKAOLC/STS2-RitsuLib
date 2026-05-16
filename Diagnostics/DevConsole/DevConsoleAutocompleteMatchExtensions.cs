using MegaCrit.Sts2.Core.DevConsole;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Low-level helpers for dev-console autocomplete predicates and display formatting.
    ///     Prefer <see cref="DevConsoleAutocomplete" /> for registration and slot resolution.
    /// </summary>
    public static class DevConsoleAutocompleteMatchExtensions
    {
        /// <summary>
        ///     Chains <paramref name="inner" /> with localized-title matching for model entry IDs.
        /// </summary>
        public static Func<string, string, bool> WithLocalizedModelTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var entryId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsoleModelIdAutocompleteCatalog.MatchesLocalizedTitle(entryId, partial);
            };
        }

        /// <summary>
        ///     Decorates completion candidates with localized suffix labels and fixes
        ///     <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" />.
        /// </summary>
        public static void ApplyLocalizedDisplayLabels(
            ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var entryIds = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates = entryIds
                .Select(DevConsoleAutocompleteDisplay.FormatCandidate)
                .ToList();

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(entryIds, result.CommandPrefix);
        }

        private static bool DefaultPrefixMatch(string candidate, string partial)
        {
            return candidate.StartsWith(partial, StringComparison.OrdinalIgnoreCase);
        }
    }
}
