using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    internal sealed class BaseLibCombatRoomRewardRestorePatch : IPatchMethod
    {
        public static string PatchId => "baselib_combat_room_reward_restore";
        public static string Description => "Use RitsuLib reward restoration instead of BaseLib's legacy restoration";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            var patchType = Type.GetType("BaseLib.Patches.Fixes.CombatRoomFromSerializableRewardExtPatch, BaseLib");
            return patchType == null ? [] : [new(patchType, "Prefix", [typeof(SerializableRoom)])];
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}
