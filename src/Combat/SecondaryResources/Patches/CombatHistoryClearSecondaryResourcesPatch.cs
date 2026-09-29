using MegaCrit.Sts2.Core.Combat.History;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.SecondaryResources.Patches
{
    internal sealed class CombatHistoryClearSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_history_clear";
        public static string Description => "Clear secondary-resource history alongside CombatHistory.Clear";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatHistory), nameof(CombatHistory.Clear))];
        }

        public static void Prefix(CombatHistory __instance)
        {
            SecondaryResourceHistory.Clear(__instance);
        }
    }
}
