using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Relics.Patches
{
    /// <summary>
    ///     Applies mod refinement mappings before vanilla <see cref="TouchOfOrobas.RefinementUpgrades" /> and fallback.
    ///     在原版 <see cref="TouchOfOrobas.RefinementUpgrades" /> 和回退逻辑之前应用 mod 精炼映射。
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
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(RelicModel starterRelic, ref RelicModel __result)
        {
            if (!OrobasAncientUpgradeRegistry.TryGetRefinementUpgrade(starterRelic.Id, out var template))
                return true;

            __result = template;
            return false;
        }
    }
}
