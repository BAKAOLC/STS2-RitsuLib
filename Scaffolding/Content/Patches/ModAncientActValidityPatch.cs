using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Removes mod shared ancients from an act's <c>_sharedAncientSubset</c> when
    ///     <see cref="IModAncientActValidity.IsValidForAct" /> returns false for that act.
    /// </summary>
    internal class ModAncientActValidityPatch : IPatchMethod
    {
        public static string PatchId => "mod_ancient_act_validity";

        public static string Description =>
            "Filter mod shared ancients by IModAncientActValidity before act room generation";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), nameof(ActModel.GenerateRooms),
                    [typeof(Rng), typeof(UnlockState), typeof(bool)]),
            ];
        }

        public static void Prefix(ActModel __instance, List<AncientEventModel>? ____sharedAncientSubset)
        {
            if (____sharedAncientSubset is not { Count: > 0 })
                return;

            ____sharedAncientSubset.RemoveAll(ancient =>
                !ModAncientActValidityFilter.IsValidForAct(__instance, ancient));
        }
    }
}
