using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies mod-provided refinement mappings before <see cref="TouchOfOrobas.RefinementUpgrades" /> and the base
    ///         game's fallback.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="TouchOfOrobas.RefinementUpgrades" /> 和游戏本体的回退逻辑之前应用模组提供的精炼映射。
    ///     </para>
    /// </summary>
    internal sealed class TouchOfOrobasGetUpgradedStarterRelicPatch : IPatchMethod
    {
        public static string PatchId => "touch_of_orobas_refinement_mod";

        public static string Description => "Apply RitsuLib-registered TouchOfOrobas starter relic upgrade mappings";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic),
                    [typeof(RelicModel)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(RelicModel starterRelic, ref RelicModel __result)
        {
            if (!OrobasAncientUpgradeRegistry.TryGetRefinementUpgrade(starterRelic.Id, out var template))
                return true;

            __result = template;
            return false;
        }
    }
}
