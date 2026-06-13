using STS2RitsuLib.Combat.SecondaryResources;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Completion sources and localized labels for registered secondary-resource ids.
    /// </summary>
    public static class DevConsoleSecondaryResourceAutocompleteCatalog
    {
        /// <summary>
        ///     Returns registered secondary-resource ids in deterministic order.
        /// </summary>
        public static string[] GetResourceIds()
        {
            return ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                .Select(static definition => definition.Id)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        ///     Appends registered secondary-resource ids to <paramref name="candidates" />.
        /// </summary>
        public static void AppendResourceIdCandidates(ICollection<string> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            foreach (var resourceId in GetResourceIds())
                candidates.Add(resourceId);
        }

        /// <summary>
        ///     Resolves a full resource id or an unambiguous resource-local id.
        /// </summary>
        public static bool TryResolveResource(
            string input,
            out SecondaryResourceDefinition definition)
        {
            definition = null!;
            var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(input).Trim();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (ModSecondaryResourceRegistry.TryGet(token, out definition))
                return true;

            var localMatches = ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                .Where(candidate => string.Equals(candidate.LocalId, token, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (localMatches.Length != 1)
                return false;

            definition = localMatches[0];
            return true;
        }

        /// <summary>
        ///     Returns the localized title for a registered resource id, or null when unavailable.
        /// </summary>
        public static string? TryGetLocalizedTitle(string resourceId)
        {
            return TryResolveResource(resourceId, out var definition)
                ? TryGetLocalizedTitle(definition)
                : null;
        }

        /// <summary>
        ///     Returns the localized title for a registered resource, or null when unavailable.
        /// </summary>
        public static string? TryGetLocalizedTitle(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            try
            {
                return SecondaryResourceText.GetTitle(definition)?.GetFormattedText()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Returns whether <paramref name="partial" /> matches a resource id, unique local id, or localized title.
        /// </summary>
        public static bool MatchesResourceIdOrTitle(string resourceId, string partial)
        {
            if (!TryResolveResource(resourceId, out var definition))
                return false;

            var normalizedPartial = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(partial).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPartial))
                return true;

            var title = TryGetLocalizedTitle(definition);
            return definition.Id.StartsWith(normalizedPartial, StringComparison.OrdinalIgnoreCase) ||
                   definition.LocalId.StartsWith(normalizedPartial, StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(title) &&
                    title.Contains(normalizedPartial, StringComparison.OrdinalIgnoreCase));
        }
    }
}
