#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal sealed class AfterCreatureAddedToCombatLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_creature_added_to_combat";
        public static string Description => "Publish creature added to combat lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCreatureAddedToCombat),
                    [typeof(CombatStateCompat), typeof(Creature)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, Creature __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CreatureAddedToCombatEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(CreatureAddedToCombatEvent)));
        }
    }

    internal sealed class AfterCurrentHpChangedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_current_hp_changed";
        public static string Description => "Publish current HP changed lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterCurrentHpChanged),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(Creature), typeof(decimal)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, Creature __2, decimal __3, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new CurrentHpChangedEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                    nameof(CurrentHpChangedEvent)));
        }
    }

    internal sealed class AfterEnergyGainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_energy_gained";
        public static string Description => "Publish energy gained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayerCombatState), nameof(PlayerCombatState.GainEnergy), [typeof(decimal)])];
        }

        public static void Prefix(PlayerCombatState __instance, out int __state)
        {
            __state = __instance.Energy;
        }

        public static void Postfix(PlayerCombatState __instance, Player ____player, int __state)
        {
            var amount = __instance.Energy - __state;
            if (amount <= 0 || ____player.Creature?.CombatState is not { } combatState)
                return;

            RitsuLibFramework.PublishLifecycleEvent(
                new EnergyGainedEvent(combatState, amount, ____player, DateTimeOffset.UtcNow),
                nameof(EnergyGainedEvent));
        }
    }

    internal sealed class AfterEnergyResetLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_energy_reset";
        public static string Description => "Publish energy reset lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterEnergyReset), [typeof(CombatStateCompat), typeof(Player)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, Player __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new EnergyResetEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(EnergyResetEvent)));
        }
    }

    internal sealed class AfterEnergySpentLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_energy_spent";
        public static string Description => "Publish energy spent lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterEnergySpent),
                    [typeof(CombatStateCompat), typeof(CardModel), typeof(int)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, CardModel __1, int __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new EnergySpentEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(EnergySpentEvent)));
        }
    }

    internal sealed class BeforeHandDrawLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_hand_draw";
        public static string Description => "Publish hand drawing lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeHandDraw),
                    [typeof(CombatStateCompat), typeof(Player), typeof(PlayerChoiceContext)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(CombatStateCompat __0, Player __1, PlayerChoiceContext __2)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new HandDrawingEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                nameof(HandDrawingEvent));
        }
    }

    internal sealed class AfterHandEmptiedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_hand_emptied";
        public static string Description => "Publish hand emptied lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterHandEmptied),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, PlayerChoiceContext __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new HandEmptiedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(HandEmptiedEvent)));
        }
    }

    internal sealed class AfterItemPurchasedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_item_purchased";
        public static string Description => "Publish item purchased lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterItemPurchased),
                    [typeof(IRunState), typeof(Player), typeof(MerchantEntry), typeof(int)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, Player __1, MerchantEntry __2, int __3, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new ItemPurchasedEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                    nameof(ItemPurchasedEvent)));
        }
    }

    internal sealed class AfterMapGeneratedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_map_generated";
        public static string Description => "Publish map generated lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
                [new(typeof(Hook), nameof(Hook.AfterMapGenerated), [typeof(IRunState), typeof(ActMap), typeof(int)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, ActMap __1, int __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new MapGeneratedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(MapGeneratedEvent)));
        }
    }

    internal sealed class AfterPlayerTurnStartLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_player_turn_start";
        public static string Description => "Publish player turn started lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterPlayerTurnStart),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, PlayerChoiceContext __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new PlayerTurnStartedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(PlayerTurnStartedEvent)));
        }
    }

    internal sealed class BeforePotionUsedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_potion_used";
        public static string Description => "Publish potion using lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforePotionUsed),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel), typeof(Creature)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(IRunState __0, CombatStateCompat __1, PotionModel __2, Creature? __3)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new PotionUsingEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                nameof(PotionUsingEvent));
        }
    }

    internal sealed class AfterPotionUsedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_potion_used";
        public static string Description => "Publish potion used lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterPotionUsed),
                    [typeof(IRunState), typeof(CombatStateCompat), typeof(PotionModel), typeof(Creature)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, CombatStateCompat __1, PotionModel __2, Creature? __3,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new PotionUsedEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                    nameof(PotionUsedEvent)));
        }
    }

    internal sealed class AfterRestSiteHealLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_rest_site_heal";
        public static string Description => "Publish rest site healed lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterRestSiteHeal), [typeof(IRunState), typeof(Player), typeof(bool)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, Player __1, bool __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RestSiteHealedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(RestSiteHealedEvent)));
        }
    }

    internal sealed class AfterRestSiteSmithLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_rest_site_smith";
        public static string Description => "Publish rest site smithed lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterRestSiteSmith), [typeof(IRunState), typeof(Player)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, Player __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RestSiteSmithedEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(RestSiteSmithedEvent)));
        }
    }

    internal sealed class AfterShuffleLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_shuffle";
        public static string Description => "Publish shuffled lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterShuffle),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, PlayerChoiceContext __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new ShuffledEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(ShuffledEvent)));
        }
    }

    internal sealed class AfterStarsGainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_stars_gained";
        public static string Description => "Publish stars gained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterStarsGained),
                    [typeof(CombatStateCompat), typeof(int), typeof(Player)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, int __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new StarsGainedEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(StarsGainedEvent)));
        }
    }

    internal sealed class AfterStarsSpentLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_stars_spent";
        public static string Description => "Publish stars spent lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterStarsSpent),
                    [typeof(CombatStateCompat), typeof(int), typeof(Player)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, int __1, Player __2, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new StarsSpentEvent(__0, __1, __2, DateTimeOffset.UtcNow),
                    nameof(StarsSpentEvent)));
        }
    }

    internal sealed class AfterSummonLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_summon";
        public static string Description => "Publish summoned lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterSummon),
                    [typeof(CombatStateCompat), typeof(PlayerChoiceContext), typeof(Player), typeof(decimal)]),
            ];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(
            CombatStateCompat __0,
            PlayerChoiceContext __1,
            Player __2,
            decimal __3,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new SummonedEvent(__0, __1, __2, __3, DateTimeOffset.UtcNow),
                    nameof(SummonedEvent)));
        }
    }

    internal sealed class AfterTakingExtraTurnLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_taking_extra_turn";
        public static string Description => "Publish extra turn taken lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterTakingExtraTurn), [typeof(CombatStateCompat), typeof(Player)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CombatStateCompat __0, Player __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new ExtraTurnTakenEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(ExtraTurnTakenEvent)));
        }
    }

    internal sealed class BeforeTurnEndLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_before_turn_end";
        public static string Description => "Publish side turn ending lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if !STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.BeforeTurnEnd), [typeof(CombatStateCompat), typeof(CombatSide)]),
#elif STS2_AT_LEAST_0_108_0
                new(typeof(Hook), nameof(Hook.BeforeSideTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.BeforeTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.First)]
#if !STS2_AT_LEAST_0_106_0
        public static void Prefix(CombatStateCompat __0, CombatSide __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new SideTurnEndingEvent(__0, __1, null, DateTimeOffset.UtcNow),
                nameof(SideTurnEndingEvent));
        }
#else
        public static void Prefix(CombatStateCompat __0, CombatSide __1, IEnumerable<Creature> __2)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new SideTurnEndingEvent(
                    __0,
                    __1,
                    AdditionalHookLifecycleEvents.GetParticipants(__2),
                    DateTimeOffset.UtcNow),
                nameof(SideTurnEndingEvent));
        }
#endif
    }

    internal sealed class AfterTurnEndLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "additional_hook_lifecycle_after_turn_end";
        public static string Description => "Publish side turn ended lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if !STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.AfterTurnEnd), [typeof(CombatStateCompat), typeof(CombatSide)]),
#elif STS2_AT_LEAST_0_108_0
                new(typeof(Hook), nameof(Hook.AfterSideTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.AfterTurnEnd),
                    [typeof(CombatStateCompat), typeof(CombatSide), typeof(IEnumerable<Creature>)]),
#endif
            ];
        }

        [HarmonyPriority(Priority.Last)]
#if !STS2_AT_LEAST_0_106_0
        public static void Postfix(CombatStateCompat __0, CombatSide __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new SideTurnEndedEvent(__0, __1, null, DateTimeOffset.UtcNow),
                    nameof(SideTurnEndedEvent)));
        }
#else
        public static void Postfix(CombatStateCompat __0, CombatSide __1, IEnumerable<Creature> __2, ref Task __result)
        {
            var participants = AdditionalHookLifecycleEvents.GetParticipants(__2);
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new SideTurnEndedEvent(__0, __1, participants, DateTimeOffset.UtcNow),
                    nameof(SideTurnEndedEvent)));
        }
#endif
    }
}
