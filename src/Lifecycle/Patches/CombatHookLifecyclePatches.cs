#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal static class CombatHookLifecycleEvents
    {
#if !STS2_AT_LEAST_0_105_0
        internal static void PublishLegacyCardRetained(CombatStateCompat combatState, CardModel card)
        {
#pragma warning disable CS0618
            RitsuLibFramework.PublishLifecycleEvent(
                new CardRetainedEvent(combatState, card, DateTimeOffset.UtcNow),
                nameof(CardRetainedEvent));
#pragma warning restore CS0618
        }
#else
        internal static void PublishCardsFlushed(
            CombatStateCompat combatState,
            Player player,
            IReadOnlyCollection<CardModel> flushedCards,
            IReadOnlyCollection<CardModel> retainedCards)
        {
            var occurredAtUtc = DateTimeOffset.UtcNow;
            RitsuLibFramework.PublishLifecycleEvent(
                new CardsFlushedEvent(combatState, player, flushedCards, retainedCards, occurredAtUtc),
                nameof(CardsFlushedEvent));
#pragma warning disable CS0618
            foreach (var card in retainedCards)
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardRetainedEvent(combatState, card, occurredAtUtc),
                    nameof(CardRetainedEvent));
#pragma warning restore CS0618
        }
#endif
    }

    internal sealed class BeforeCombatStartLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_before_combat_start";
        public static string Description => "Publish combat starting lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeCombatStart), [typeof(IRunState), typeof(CombatStateCompat)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(IRunState __0, CombatStateCompat __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new CombatStartingEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(CombatStartingEvent));
        }
    }

    internal sealed class AfterCombatEndLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_combat_end";
        public static string Description => "Publish combat ended lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCombatEnd),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(CombatRoom)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, CombatRoom __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CombatEndedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(CombatEndedEvent)));
        }
    }

    internal sealed class AfterCombatVictoryLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_combat_victory";
        public static string Description => "Publish combat victory lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCombatVictory),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(CombatRoom)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, CombatRoom __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CombatVictoryEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(CombatVictoryEvent)));
        }
    }

    internal sealed class BeforeSideTurnStartLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_before_side_turn_start";
        public static string Description => "Publish side turn starting lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.BeforeSideTurnStart),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IReadOnlyList<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.BeforeSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, CombatSide __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new SideTurnStartingEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(SideTurnStartingEvent));
        }
    }

    internal sealed class AfterSideTurnStartLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_side_turn_start";
        public static string Description => "Publish side turn started lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IReadOnlyList<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart), [typeof(CombatStateCompat), typeof(CombatSide)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CombatSide __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new SideTurnStartedEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(SideTurnStartedEvent)));
        }
    }

    internal sealed class BeforeCardPlayedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_before_card_played";
        public static string Description => "Publish card playing lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeCardPlayed), [typeof(CombatStateCompat), typeof(CardPlay)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, CardPlay __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new CardPlayingEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(CardPlayingEvent));
        }
    }

    internal sealed class AfterCardPlayedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_played";
        public static string Description => "Publish card played lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardPlayed),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardPlay)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardPlay __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardPlayedEvent(__0, __2, DateTimeOffset.UtcNow),
                    nameof(CardPlayedEvent)));
        }
    }

    internal sealed class AfterCardChangedPilesLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_changed_piles";
        public static string Description => "Publish card moved between piles lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardChangedPiles),
                [
                    typeof(IRunState), typeof(CombatStateCompat), typeof(CardModel), typeof(PileType),
                    typeof(AbstractModel),
                ]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(
            IRunState __0,
            CombatStateCompat __1,
            CardModel __2,
            PileType __3,
            AbstractModel __4,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardMovedBetweenPilesEvent(__0, __1, __2, __3, __4, DateTimeOffset.UtcNow),
                    nameof(CardMovedBetweenPilesEvent)));
        }
    }

    internal sealed class AfterCardDrawnLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_drawn";
        public static string Description => "Publish card drawn lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardDrawn),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __2, bool __3, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardDrawnEvent(__0, __2, __3, DateTimeOffset.UtcNow),
                    nameof(CardDrawnEvent)));
        }
    }

    internal sealed class AfterCardDiscardedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_discarded";
        public static string Description => "Publish card discarded lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardDiscarded),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardDiscardedEvent(__0, __2, DateTimeOffset.UtcNow),
                    nameof(CardDiscardedEvent)));
        }
    }

    internal sealed class AfterCardExhaustedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_exhausted";
        public static string Description => "Publish card exhausted lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCardExhausted),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __2, bool __3, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CardExhaustedEvent(__0, __2, __3, DateTimeOffset.UtcNow),
                    nameof(CardExhaustedEvent)));
        }
    }

    internal sealed class BeforeFlushLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_before_flush";
        public static string Description => "Publish before flush lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeFlush), [typeof(CombatStateCompat), typeof(Player)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, Player __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new BeforeFlushEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(BeforeFlushEvent));
        }
    }

#if !STS2_AT_LEAST_0_105_0
    internal sealed class AfterCardRetainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_card_retained";
        public static string Description => "Publish legacy card retained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterCardRetained), [typeof(CombatStateCompat), typeof(CardModel)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                CombatHookLifecycleEvents.PublishLegacyCardRetained(__0, __1));
        }
    }
#else
    internal sealed class AfterFlushLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_flush";
        public static string Description => "Publish cards flushed lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterFlush),
                [
                    typeof(CombatStateCompat), typeof(Player), typeof(PlayerChoiceContext),
                    typeof(IReadOnlyCollection<CardModel>), typeof(IReadOnlyCollection<CardModel>),
                ]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(
            CombatStateCompat __0,
            Player __1,
            IReadOnlyCollection<CardModel> __3,
            IReadOnlyCollection<CardModel> __4,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                CombatHookLifecycleEvents.PublishCardsFlushed(__0, __1, __3, __4));
        }
    }
#endif

    internal sealed class BeforeDeathLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_before_death";
        public static string Description => "Publish creature dying lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(IRunState __0, CombatStateCompat __1, Creature __2)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new CreatureDyingEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                nameof(CreatureDyingEvent));
        }
    }

    internal sealed class AfterDeathLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "combat_hook_lifecycle_after_death";
        public static string Description => "Publish creature died lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterDeath),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature), typeof(bool), typeof(float)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(
            IRunState __0,
            CombatStateCompat __1,
            Creature __2,
            bool __3,
            float __4,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CreatureDiedEvent(__0, __1, __2, __3, __4, DateTimeOffset.UtcNow),
                    nameof(CreatureDiedEvent)));
        }
    }
}
