#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
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
    public class RewardHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "reward_hook_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish reward, potion, and gold gain lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterGoldGained), [typeof(IRunState), typeof(Player)]),
                new(typeof(Hook), nameof(Hook.AfterPotionProcured),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel)]),
                new(typeof(Hook), nameof(Hook.AfterPotionDiscarded),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel)]),
                new(typeof(Hook), nameof(Hook.AfterRewardTaken), [typeof(IRunState), typeof(Player), typeof(Reward)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: after matching hook methods complete, publishes the corresponding reward/economy lifecycle
        ///     event on the continuation of the original task.
        ///     Harmony postfix：在匹配的 hook 方法完成后，在原始任务的延续中发布对应的奖励/经济生命周期事件。
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            __result = __originalMethod.Name switch
            {
                nameof(Hook.AfterGoldGained) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new GoldGainedEvent((IRunState)__args[0], (Player)__args[1], ((Player)__args[1]).Gold,
                            DateTimeOffset.UtcNow), nameof(GoldGainedEvent))),
                nameof(Hook.AfterPotionProcured) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new PotionProcuredEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                            (PotionModel)__args[2],
                            DateTimeOffset.UtcNow), nameof(PotionProcuredEvent))),
                nameof(Hook.AfterPotionDiscarded) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new PotionDiscardedEvent((IRunState)__args[0], (CombatStateCompat?)__args[1],
                            (PotionModel)__args[2],
                            DateTimeOffset.UtcNow), nameof(PotionDiscardedEvent))),
                nameof(Hook.AfterRewardTaken) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new RewardTakenEvent((IRunState)__args[0], (Player)__args[1], (Reward)__args[2],
                            DateTimeOffset.UtcNow), nameof(RewardTakenEvent))),
                _ => __result,
            };
        }
    }

    /// <summary>
    ///     Publishes a lifecycle event when the player loses gold through <see cref="PlayerCmd.LoseGold" />.
    ///     当玩家通过 <see cref="PlayerCmd.LoseGold" /> 失去金币时发布生命周期事件。
    /// </summary>
    public class GoldLossLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "gold_loss_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish gold loss lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold),
                    [typeof(decimal), typeof(Player), typeof(GoldLossType)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: publishes <see cref="GoldLostEvent" /> with the updated gold total.
        ///     Harmony postfix：发布带有更新后金币总数的 <see cref="GoldLostEvent" />。
        /// </summary>
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
    public class RelicObtainedLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "relic_obtained_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish relic obtain lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: when obtain completes, publishes <see cref="RelicObtainedEvent" /> for the resolved relic.
        ///     Harmony postfix：获得流程完成后，为解析出的遗物发布 <see cref="RelicObtainedEvent" />。
        /// </summary>
        // ReSharper disable once InconsistentNaming
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
    public class RelicRemovedLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "relic_removed_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish relic removal lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicCmd), nameof(RelicCmd.Remove), [typeof(RelicModel)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: after removal completes, publishes <see cref="RelicRemovedEvent" /> using the relic owner.
        ///     Harmony postfix：移除完成后，使用遗物拥有者发布 <see cref="RelicRemovedEvent" />。
        /// </summary>
        // ReSharper disable once InconsistentNaming
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
