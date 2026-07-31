using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Removes shared mod Ancient events from an act when
    ///         <see cref="IModAncientActValidity.IsValidForAct" /> rejects them.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从章节中移除被 <see cref="IModAncientActValidity.IsValidForAct" /> 判定为无效的共享模组先古事件。
    ///     </para>
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
