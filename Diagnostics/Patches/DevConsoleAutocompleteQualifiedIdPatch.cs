using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Diagnostics.Patches
{
    /// <summary>
    ///     Enables shorthand autocomplete only for IDs owned by ritsulib-registered content.
    /// </summary>
    public sealed class DevConsoleAutocompleteQualifiedIdPatch : IPatchMethod
    {
        private static readonly Lazy<OwnedIdCatalog> OwnedCatalog = new(BuildOwnedIdCatalog);

        /// <inheritdoc />
        public static string PatchId => "dev_console_autocomplete_qualified_id";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static string Description =>
            "DevConsole autocomplete: shorthand match for ritsulib-owned ids only";

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AbstractConsoleCmd), nameof(AbstractConsoleCmd.CompleteArgument), true)];
        }

        /// <summary>
        ///     Installs a matcher only when no other patch already provided one.
        /// </summary>
        public static void Prefix(ref Func<string, string, bool>? matchPredicate)
        {
            if (matchPredicate != null)
                return;

            matchPredicate = TailAwareOwnedIdMatch;
        }

        /// <summary>
        ///     De-duplicates completion candidates while preserving existing order from upstream patches.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref CompletionResult __result, string partialArg)
        {
            __result.Candidates = __result.Candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TailAwareOwnedIdMatch(string candidate, string partial)
        {
            if (string.IsNullOrWhiteSpace(partial))
                return true;

            var token = partial.Trim();
            if (candidate.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!TryGetOwnedTail(candidate, out var tail))
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

            var modPrefix = GetNormalizedModStem(ownerModId) + "_";
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

            var normalizedId = id.Trim();
            owned.TryAdd(normalizedId, modId);
        }

        private static string GetNormalizedModStem(string modId)
        {
            return ModContentRegistry.NormalizePublicStem(modId);
        }

        private sealed record OwnedIdCatalog(Dictionary<string, string> IdOwnerMap);
    }
}
