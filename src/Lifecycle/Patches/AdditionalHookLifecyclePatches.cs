#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal static class AdditionalHookLifecycleEvents
    {
        internal static IReadOnlyCollection<Creature>? GetParticipants(IEnumerable<Creature>? participants)
        {
            return participants as IReadOnlyCollection<Creature> ?? participants?.ToArray();
        }
    }

    internal sealed class BeforeAttackLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_attack";
        public static string Description => "Publish attack starting lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeAttack), [typeof(CombatStateCompat), typeof(AttackCommand)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, AttackCommand __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new AttackStartingEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(AttackStartingEvent));
        }
    }

    internal sealed class AfterAttackLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_attack";
        public static string Description => "Publish attack ended lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if !STS2_AT_LEAST_0_104_0
                new(typeof(Hook), nameof(Hook.AfterAttack), [typeof(CombatStateCompat), typeof(AttackCommand)]),
#else
                new(typeof(Hook), nameof(Hook.AfterAttack),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(AttackCommand)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.Last)]
#if !STS2_AT_LEAST_0_104_0
        public static void Postfix(CombatStateCompat __0, AttackCommand __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new AttackEndedEvent(__0, null, __1, DateTimeOffset.UtcNow),
                    nameof(AttackEndedEvent)));
        }
#else
        public static void Postfix(CombatStateCompat __0, PlayerChoiceContext __1, AttackCommand __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new AttackEndedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(AttackEndedEvent)));
        }
#endif
    }

    internal sealed class AfterBlockBrokenLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_block_broken";
        public static string Description => "Publish block broken lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterBlockBroken), [typeof(CombatStateCompat), typeof(Creature)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, Creature __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new BlockBrokenEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(BlockBrokenEvent)));
        }
    }

    internal sealed class AfterBlockClearedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_block_cleared";
        public static string Description => "Publish block cleared lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterBlockCleared), [typeof(CombatStateCompat), typeof(Creature)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, Creature __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new BlockClearedEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(BlockClearedEvent)));
        }
    }

    internal sealed class BeforeBlockGainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_block_gained";
        public static string Description => "Publish block gaining lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeBlockGained),
                [
                    typeof(CombatStateCompat), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel),
                ]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, Creature __1, decimal __2, ValueProp __3, CardModel? __4)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new BlockGainingEvent(__0, __1, __2, __3, __4, DateTimeOffset.UtcNow),
                nameof(BlockGainingEvent));
        }
    }

    internal sealed class AfterBlockGainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_block_gained";
        public static string Description => "Publish block gained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterBlockGained),
                [
                    typeof(CombatStateCompat), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel),
                ]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(
            CombatStateCompat __0,
            Creature __1,
            decimal __2,
            ValueProp __3,
            CardModel? __4,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new BlockGainedEvent(__0, __1, __2, __3, __4, DateTimeOffset.UtcNow),
                    nameof(BlockGainedEvent)));
        }
    }

    internal sealed class BeforeCardAutoPlayedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_card_auto_played";
        public static string Description => "Publish card auto playing lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeCardAutoPlayed),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(Creature), typeof(AutoPlayType)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, CardModel __1, Creature? __2, AutoPlayType __3)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new CardAutoPlayingEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                nameof(CardAutoPlayingEvent));
        }
    }

    internal sealed class AfterCardEnteredCombatLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_card_entered_combat";
        public static string Description => "Publish card entered combat lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardEnteredCombat), [typeof(CombatStateCompat), typeof(CardModel)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardEnteredCombatEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(CardEnteredCombatEvent)));
        }
    }

    internal sealed class AfterCardGeneratedForCombatLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_card_generated_for_combat";
        public static string Description => "Publish card generated for combat lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if !STS2_AT_LEAST_0_104_0
                new(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(bool)]),
#else
                new(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(Player)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.Last)]
#if !STS2_AT_LEAST_0_104_0
        public static void Postfix(CombatStateCompat __0, CardModel __1, bool __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardGeneratedForCombatEvent(__0, __1, null, __2, DateTimeOffset.UtcNow),
                    nameof(CardGeneratedForCombatEvent)));
        }
#else
        public static void Postfix(CombatStateCompat __0, CardModel __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardGeneratedForCombatEvent(__0, __1, __2, null, DateTimeOffset.UtcNow),
                    nameof(CardGeneratedForCombatEvent)));
        }
#endif
    }

    internal sealed class BeforeCardRemovedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_card_removed";
        public static string Description => "Publish card removing lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeCardRemoved), [typeof(IRunState), typeof(CardModel)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(IRunState __0, CardModel __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new CardRemovingEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(CardRemovingEvent));
        }
    }
}
