using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Candidate sources for <c>unlock</c> command discovery-id arguments.
    /// </summary>
    internal static class DevConsoleUnlockAutocompleteSources
    {
        private static readonly Dictionary<string, Func<IEnumerable<string>>> Sources =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["cards"] = DevConsoleAutocompleteCandidateSources.GetCardEntryIds,
                ["potions"] = DevConsoleAutocompleteCandidateSources.GetPotionEntryIds,
                ["relics"] = DevConsoleAutocompleteCandidateSources.GetRelicEntryIds,
                ["monsters"] = DevConsoleAutocompleteCandidateSources.GetMonsterEntryIds,
                ["events"] = () => ModelDb.AllEvents.Select(e => e.Id.Entry),
                ["epochs"] = DevConsoleAutocompleteCandidateSources.GetEpochEntryIds,
            };

        public static bool SupportsDiscoveryIds(string discoveryType)
        {
            return Sources.ContainsKey(discoveryType.Trim());
        }

        public static bool TryGetCandidates(string discoveryType, out IReadOnlyList<string> candidates)
        {
            if (!Sources.TryGetValue(discoveryType.Trim(), out var source))
            {
                candidates = [];
                return false;
            }

            candidates = [.. source()];
            return true;
        }
    }
}
