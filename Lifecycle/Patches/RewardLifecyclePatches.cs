#if STS2_V_0_103_2
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
