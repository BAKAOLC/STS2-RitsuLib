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
        private static readonly Lazy<OwnedIdCatalog> OwnedCatalog = new(BuildOwnedIdCatalog);

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

            var ownership = OwnedCatalog.Value.IdOwnerMap;
            if (!ownership.TryGetValue(candidate.Trim(), out var ownerModId))
                return false;

            var modPrefix = ModContentRegistry.NormalizePublicStem(ownerModId) + "_";
            if (!candidate.StartsWith(modPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (candidate.Length <= modPrefix.Length)
                return false;

            tail = candidate[modPrefix.Length..];
            return tail.Contains('_');
        }

        private static OwnedIdCatalog BuildOwnedIdCatalog()
        {
            var owned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in ModContentRegistry.GetRegisteredTypeSnapshots())
            {
                if (s.ModelDbId != null)
                    TryAddOwned(owned, s.ModelDbId.Entry, s.ModId);

                if (!string.IsNullOrWhiteSpace(s.ExpectedPublicEntry))
                    TryAddOwned(owned, s.ExpectedPublicEntry, s.ModId);
            }

            foreach (var d in ModKeywordRegistry.GetDefinitionsSnapshot())
                TryAddOwned(owned, d.Id, d.ModId);

            foreach (var d in ModCardTagRegistry.GetDefinitionsSnapshot())
                TryAddOwned(owned, d.Id, d.ModId);

            foreach (var d in ModCardPileRegistry.GetDefinitionsSnapshot())
                TryAddOwned(owned, d.Id, d.ModId);

            foreach (var d in ModTopBarButtonRegistry.GetDefinitionsSnapshot())
                TryAddOwned(owned, d.Id, d.ModId);

            return new(owned);
        }

        private static void TryAddOwned(Dictionary<string, string> owned, string? id, string? modId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(modId))
                return;

            owned.TryAdd(id.Trim(), modId);
        }

        private sealed record OwnedIdCatalog(Dictionary<string, string> IdOwnerMap);
    }
}
