using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Tail-aware autocomplete matching for ritsulib-registered public entry ids.
    /// </summary>
    public static class DevConsoleAutocompleteOwnedIdMatch
    {
        /// <summary>
        ///     Matches full id prefix or the mod-stem tail segment for owned ids.
        /// </summary>
        public static bool Match(string candidate, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var token = partial.Trim();
            var entryId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);

            if (entryId.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!TryGetOwnedTail(entryId, out var tail))
                return false;

            return tail.StartsWith(token, StringComparison.OrdinalIgnoreCase) ||
                   tail.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetOwnedTail(string candidate, out string tail)
        {
            tail = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            if (!TryGetOwnerModId(candidate.Trim(), out var ownerModId))
                return false;

            var modPrefix = ModContentRegistry.NormalizePublicStem(ownerModId) + "_";
            if (!candidate.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (candidate.Length <= modPrefix.Length)
                return false;

            tail = candidate[modPrefix.Length..];
            return tail.Contains('_');
        }

        private static bool TryGetOwnerModId(string candidate, out string ownerModId)
        {
            foreach (var s in ModContentRegistry.GetRegisteredTypeSnapshots())
            {
                if (string.Equals(s.ModelDbId?.Entry, candidate, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.ExpectedPublicEntry, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    ownerModId = s.ModId;
                    return true;
                }
            }

            if (ModKeywordRegistry.TryGetOwnerModId(candidate, out ownerModId) ||
                ModCardTagRegistry.TryGetOwnerModId(candidate, out ownerModId) ||
                ModCardPileRegistry.TryGetOwnerModId(candidate, out ownerModId))
                return true;

            if (ModTopBarButtonRegistry.TryGet(candidate, out var topBarButton))
            {
                ownerModId = topBarButton.ModId;
                return true;
            }

            ownerModId = string.Empty;
            return false;
        }
    }
}
