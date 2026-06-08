#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes gold gain, potion procure/discard, and reward-taken lifecycle events via <see cref="Hook" />.
    ///     通过 <see cref="Hook" /> 发布金币获得、药水获取/丢弃以及奖励领取生命周期事件。
    /// </summary>
    internal sealed class AfterGoldGainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "reward_hook_lifecycle_after_gold_gained";
        public static string Description => "Publish gold gained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterGoldGained), [typeof(IRunState), typeof(Player)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, Player __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new GoldGainedEvent(__0, __1, __1.Gold, DateTimeOffset.UtcNow),
                    nameof(GoldGainedEvent)));
        }
    }

    internal sealed class AfterPotionProcuredLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "reward_hook_lifecycle_after_potion_procured";
        public static string Description => "Publish potion procured lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterPotionProcured),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, PotionModel __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new PotionProcuredEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(PotionProcuredEvent)));
        }
    }

    internal sealed class AfterPotionDiscardedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "reward_hook_lifecycle_after_potion_discarded";
        public static string Description => "Publish potion discarded lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterPotionDiscarded),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, PotionModel __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new PotionDiscardedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(PotionDiscardedEvent)));
        }
    }

    internal sealed class AfterRewardTakenLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "reward_hook_lifecycle_after_reward_taken";
        public static string Description => "Publish reward taken lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterRewardTaken), [typeof(IRunState), typeof(Player), typeof(Reward)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, Player __1, Reward __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RewardTakenEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(RewardTakenEvent)));
        }
    }

    /// <summary>
    ///     Publishes a lifecycle event when the player loses gold through <see cref="PlayerCmd.LoseGold" />.
    ///     当玩家通过 <see cref="PlayerCmd.LoseGold" /> 失去金币时发布生命周期事件。
    /// </summary>
    internal class GoldLossLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "gold_loss_lifecycle";
        public static string Description => "Publish gold loss lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold),
                    [typeof(decimal), typeof(Player), typeof(GoldLossType)]),
            ];
        }

        public static void Postfix(decimal amount, Player player, GoldLossType goldLossType)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new GoldLostEvent(player, amount, goldLossType, player.Gold, DateTimeOffset.UtcNow),
                nameof(GoldLostEvent)
            );
        }
    }

    /// <summary>
    ///     Publishes a lifecycle event when a relic is obtained via <see cref="RelicCmd.Obtain" />.
    ///     当通过 <see cref="RelicCmd.Obtain" /> 获得遗物时发布生命周期事件。
    /// </summary>
    internal class RelicObtainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "relic_obtained_lifecycle";
        public static string Description => "Publish relic obtain lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)]),
            ];
        }

        public static void Postfix(Player player, ref Task<RelicModel> __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, relic =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RelicObtainedEvent(player, relic, DateTimeOffset.UtcNow),
                    nameof(RelicObtainedEvent)
                ));
        }
    }

    /// <summary>
    ///     Publishes a lifecycle event when a relic is removed via <see cref="RelicCmd.Remove" />.
    ///     当通过 <see cref="RelicCmd.Remove" /> 移除遗物时发布生命周期事件。
    /// </summary>
    internal class RelicRemovedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "relic_removed_lifecycle";
        public static string Description => "Publish relic removal lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicCmd), nameof(RelicCmd.Remove), [typeof(RelicModel)]),
            ];
        }

        public static void Postfix(RelicModel relic, ref Task __result)
        {
            var owner = relic.Owner;
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RelicRemovedEvent(owner, relic, DateTimeOffset.UtcNow),
                    nameof(RelicRemovedEvent)
                ));
        }
    }
}
