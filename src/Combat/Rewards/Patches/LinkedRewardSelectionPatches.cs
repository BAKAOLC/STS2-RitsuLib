using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    internal static class LinkedRewardSelectionCodec
    {
        private const int LinkedChoiceOffset = 128;

        internal static int Encode(RewardsSet rewardsSet, Reward selectedReward, out LinkedRewardSet linkedRewardSet)
        {
            if (rewardsSet.Rewards.Count > LinkedChoiceOffset)
                throw new InvalidOperationException(
                    "Linked reward synchronization supports at most 128 top-level rewards in one reward set.");

            var ordinal = 0;
            foreach (var reward in rewardsSet.Rewards)
            {
                if (reward is not LinkedRewardSet candidate ||
                    candidate.GetType() != typeof(LinkedRewardSet))
                    continue;

                foreach (var child in candidate.Rewards)
                {
                    if (ordinal >= LinkedRewardSets.MaximumEncodedChildren)
                        throw new InvalidOperationException(
                            $"A reward set cannot contain more than {LinkedRewardSets.MaximumEncodedChildren} " +
                            "linked child rewards.");

                    if (ReferenceEquals(child, selectedReward))
                    {
                        linkedRewardSet = candidate;
                        return LinkedChoiceOffset + ordinal;
                    }

                    ordinal++;
                }
            }

            throw new InvalidOperationException(
                "The selected linked reward is not a direct child of the active reward set.");
        }

        internal static bool TryDecode(
            RewardsSet rewardsSet,
            int encodedChoice,
            out LinkedRewardSet? linkedRewardSet,
            out Reward? selectedReward)
        {
            linkedRewardSet = null;
            selectedReward = null;
            if (encodedChoice < LinkedChoiceOffset || encodedChoice < rewardsSet.Rewards.Count)
                return false;

            var targetOrdinal = encodedChoice - LinkedChoiceOffset;
            var ordinal = 0;
            foreach (var reward in rewardsSet.Rewards)
            {
                if (reward is not LinkedRewardSet candidate ||
                    candidate.GetType() != typeof(LinkedRewardSet))
                    continue;

                foreach (var child in candidate.Rewards)
                {
                    if (ordinal == targetOrdinal)
                    {
                        linkedRewardSet = candidate;
                        selectedReward = child;
                        return true;
                    }

                    ordinal++;
                }
            }

            return false;
        }
    }

    internal sealed class LinkedRewardSetOnSelectPatch : IPatchMethod
    {
        public static string PatchId => "linked_reward_set_on_select";
        public static string Description => "Resolve the pending child choice through the base-game linked reward set";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LinkedRewardSet), "OnSelect", Type.EmptyTypes)];
        }

        public static bool Prefix(LinkedRewardSet __instance, ref Task<bool> __result)
        {
            if (!LinkedRewardSetRuntime.HasPendingSelection(__instance))
                return true;

            __result = LinkedRewardSetRuntime.ResolveSelection(__instance);
            return false;
        }
    }

    internal sealed class LinkedRewardSetSelectLocalRewardPatch : IPatchMethod
    {
        public static string PatchId => "linked_reward_set_select_local_reward";
        public static string Description => "Synchronize a linked child through its top-level base-game reward";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.SelectLocalReward),
                    [typeof(Reward)]),
            ];
        }

        public static bool Prefix(
            RewardsSetSynchronizer __instance,
            Reward reward,
            ref Task<bool> __result)
        {
            if (reward.ParentRewardSet is not { } parentRewardSet ||
                parentRewardSet.GetType() != typeof(LinkedRewardSet))
                return true;
            if (!ReferenceEquals(reward.Player, __instance.LocalPlayer))
                throw new InvalidOperationException(
                    $"SelectLocalReward called for linked reward {reward} with a non-local player.");

            var rewardState = __instance.GetRewardStateForPlayer(__instance.LocalPlayer);
            if (rewardState.rewardsStack.Count <= 0)
                throw new InvalidOperationException(
                    "Tried to synchronize a linked reward while no reward set is active.");

            var rewardsSetState = rewardState.rewardsStack.Last();
            var encodedChoice = LinkedRewardSelectionCodec.Encode(
                rewardsSetState.set,
                reward,
                out var linkedRewardSet);
            if (!LinkedRewardSetRuntime.TryPrepareSelection(linkedRewardSet, reward))
            {
                __result = Task.FromResult(false);
                return false;
            }

            __instance._netService.SendMessage(new RewardSelectedMessage
            {
                location = __instance._messageBuffer.CurrentLocation,
                setId = rewardsSetState.set.Id,
                rewardIndex = encodedChoice,
            });
            __result = __instance.SelectRewardForPlayer(rewardsSetState, linkedRewardSet);
            return false;
        }
    }

    internal sealed class LinkedRewardSetHandleSelectedMessagePatch : IPatchMethod
    {
        public static string PatchId => "linked_reward_set_handle_selected_message";
        public static string Description => "Decode synchronized linked child selections";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.HandleRewardSelectedMessage),
                    [typeof(RewardSelectedMessage), typeof(ulong)]),
            ];
        }

        public static bool Prefix(
            RewardsSetSynchronizer __instance,
            RewardSelectedMessage message,
            ulong senderId)
        {
            if (message.rewardIndex < 128)
                return true;

            var player = __instance._playerCollection.GetPlayer(senderId);
            if (player == null)
                return false;

            var rewardState = __instance.GetRewardStateForPlayer(player);
            if (rewardState.nextId <= message.setId || rewardState.rewardsStack.Count <= 0)
                return true;

            var rewardsSetState = rewardState.rewardsStack.Last();
            if (!LinkedRewardSelectionCodec.TryDecode(
                    rewardsSetState.set,
                    message.rewardIndex,
                    out var linkedRewardSet,
                    out var selectedReward))
                return true;
            if (linkedRewardSet == null || selectedReward == null ||
                !LinkedRewardSetRuntime.TryPrepareSelection(linkedRewardSet, selectedReward))
                return false;

            TaskHelper.RunSafely(__instance.SelectRewardForPlayer(rewardsSetState, linkedRewardSet));
            return false;
        }
    }
}
