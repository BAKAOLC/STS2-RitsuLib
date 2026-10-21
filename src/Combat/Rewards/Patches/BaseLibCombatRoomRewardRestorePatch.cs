using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;
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
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
        }

        public static void Prefix()
        {
            var patchType = ExternalFrameworkRegistry.ResolveType(
                "BaseLib.Patches.Fixes.CombatRoomFromSerializableRewardExtPatch");
            if (patchType == null)
                return;

            var legacyPrefix = AccessTools.Method(patchType, "Prefix", [typeof(SerializableRoom)]) ??
                               throw new MissingMethodException(patchType.FullName, "Prefix");
            var patcher = RitsuLibFramework.GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.Core);
            if (!patcher.ApplyDynamicPatches(
                [
                    new(PatchId + ".legacy", legacyPrefix,
                        prefix: new(typeof(BaseLibCombatRoomRewardRestorePatch), nameof(SkipLegacyRestore)),
                        description: Description),
                ]))
                throw new InvalidOperationException("Failed to coordinate BaseLib reward restoration.");
        }

        private static bool SkipLegacyRestore()
        {
            return false;
        }
    }
}
