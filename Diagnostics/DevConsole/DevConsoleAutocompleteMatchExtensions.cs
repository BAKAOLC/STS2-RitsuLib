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

        /// <summary>
        ///     Chains <paramref name="inner" /> with localized pile-title matching.
        /// </summary>
        public static Func<string, string, bool> WithLocalizedPileTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsolePileNameAutocompleteCatalog.MatchesLocalizedTitle(token, partial);
            };
        }

        /// <summary>
        ///     Chains <paramref name="inner" /> with localized secondary-resource title and unique local-id matching.
        /// </summary>
        public static Func<string, string, bool> WithSecondaryResourceLocalizedTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var resourceId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsoleSecondaryResourceAutocompleteCatalog.MatchesResourceIdOrTitle(resourceId, partial);
            };
        }

        /// <summary>
        ///     Chains <paramref name="inner" /> with localized ancient-choice matching for a resolved ancient id.
        /// </summary>
        public static Func<string, string, bool> WithAncientChoiceLocalizedMatch(
            Func<string, string, bool>? inner,
            string? ancientEntryId)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                if (string.IsNullOrWhiteSpace(ancientEntryId))
                    return false;

                var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsoleAncientChoiceAutocompleteCatalog.MatchesLocalizedTitle(
                    ancientEntryId,
                    token,
                    partial);
            };
        }

        /// <summary>
        ///     Decorates <c>ancient</c> choice candidates with localized option/relic titles.
        /// </summary>
        public static void ApplyAncientChoiceDisplayLabels(
            ref CompletionResult result,
            string ancientEntryId)
        {
            if (result.Candidates.Count == 0)
                return;

            var tokens = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates = tokens
                .Select(token => DevConsoleAutocompleteDisplay.FormatAncientChoiceCandidate(ancientEntryId, token))
                .ToList();

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(tokens, result.CommandPrefix);
        }

        /// <summary>
        ///     Decorates pile-argument candidates with localized suffix labels.
        /// </summary>
        public static void ApplyPileDisplayLabels(ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var tokens = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates = tokens
                .Select(DevConsoleAutocompleteDisplay.FormatPileCandidate)
                .ToList();

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(tokens, result.CommandPrefix);
        }

        /// <summary>
        ///     Decorates secondary-resource id candidates with localized suffix labels.
        /// </summary>
        public static void ApplySecondaryResourceDisplayLabels(ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var resourceIds = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates = resourceIds
                .Select(resourceId => DevConsoleAutocompleteDisplay.FormatCandidate(
                    resourceId,
                    DevConsoleSecondaryResourceAutocompleteCatalog.TryGetLocalizedTitle(resourceId)))
                .ToList();

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(resourceIds, result.CommandPrefix);
        }

        private static bool DefaultPrefixMatch(string candidate, string partial)
        {
            return candidate.StartsWith(partial, StringComparison.OrdinalIgnoreCase);
        }
    }
}
