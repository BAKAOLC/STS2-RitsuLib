using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    internal sealed class LinkedRewardSetToSerializablePatch : IPatchMethod
    {
        public static string PatchId => "linked_reward_set_to_serializable";
        public static string Description => "Persist base-game linked reward children and selection mode";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Reward), nameof(Reward.ToSerializable), Type.EmptyTypes)];
        }

        public static void Postfix(Reward __instance, ref SerializableReward __result)
        {
            if (__instance is LinkedRewardSet linkedRewardSet &&
                linkedRewardSet.GetType() == typeof(LinkedRewardSet))
                __result = LinkedRewardSetSerialization.CreateSerializable(linkedRewardSet);
        }
    }
}
