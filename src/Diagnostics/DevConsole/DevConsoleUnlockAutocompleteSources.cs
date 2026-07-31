using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides candidates for discovery-ID arguments of the <c>unlock</c> command.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 <c>unlock</c> 命令的发现 ID 参数候选项。
    ///     </para>
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
