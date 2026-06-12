#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.SecondaryResources.Patches
{
    internal sealed class CardModelCanPlaySecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_can_play";
        public static string Description => "Check secondary-resource affordability in CardModel.CanPlay";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.CanPlay),
                    [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]),
            ];
        }

        public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason)
        {
            if (!__result ||
                !ModSecondaryResourceRegistry.HasAny ||
                !__instance.HasMaterialSecondaryCosts())
                return;

            var isFree = FreePlayBindingRegistry.IsCardFreeForUpcomingPlay(__instance);
            var plan = SecondaryResourcePaymentResolver.Plan(__instance, isFree);
            if (plan.IsAffordable)
                return;

            reason |= UnplayableReason.BlockedByCardLogic;
            __result = false;
        }
    }

    internal sealed class CardModelSpendResourcesSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_spend_resources";

        public static string Description =>
            "Capture and commit secondary-resource payments alongside CardModel.SpendResources";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.SpendResources))];
        }

        public static void Prefix(CardModel __instance, out SecondaryResourcePaymentPlan? __state)
        {
            __state = null;
            if (!ModSecondaryResourceRegistry.HasAny ||
                !__instance.HasMaterialSecondaryCosts())
                return;

            var isFree = FreePlayBindingRegistry.IsCardFreeForUpcomingPlay(__instance);
            var plan = SecondaryResourcePaymentResolver.Plan(__instance, isFree);
            if (plan.HasLines)
                __state = plan;
        }

        public static void Postfix(
            CardModel __instance,
            SecondaryResourcePaymentPlan? __state,
            ref Task<(int, int)> __result)
        {
            if (__state is { HasLines: true })
                __result = After(__instance, __state, __result);
        }

        private static async Task<(int, int)> After(
            CardModel card,
            SecondaryResourcePaymentPlan plan,
            Task<(int, int)> original)
        {
            var resources = await original;
            await SecondaryResourcePaymentResolver.Commit(plan, card);
            return resources;
        }
    }

    internal sealed class CardModelOnPlayWrapperSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_on_play_wrapper";
        public static string Description => "Prepare auto-play secondary-resource ledgers and clear until-played costs";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.OnPlayWrapper),
                [
                    typeof(PlayerChoiceContext),
                    typeof(Creature),
                    typeof(bool),
                    typeof(ResourceInfo),
                    typeof(bool),
                ]),
            ];
        }

        public static void Prefix(CardModel __instance, bool isAutoPlay)
        {
            if (!isAutoPlay ||
                !ModSecondaryResourceRegistry.HasAny ||
                !__instance.HasMaterialSecondaryCosts())
                return;

            var plan = SecondaryResourcePaymentResolver.Plan(__instance, true);
            if (plan.HasLines)
                SecondaryResourcePaymentResolver.CommitFree(plan);
        }

        public static void Postfix(CardModel __instance, ref Task __result)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                return;

            __result = LifecyclePatchTaskBridge.After(__result, () => { __instance.ClearSecondaryCostsUntilPlayed(); });
        }
    }

    internal sealed class HookBeforeCardPlayedSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_bind_play_ledger";
        public static string Description => "Bind pending secondary-resource ledgers before card-play hooks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeCardPlayed), [typeof(CombatStateLike), typeof(CardPlay)])];
        }

        public static void Prefix(CardPlay cardPlay)
        {
            if (ModSecondaryResourceRegistry.HasAny)
                SecondaryResourcePlayLedgerRuntime.TryBindPending(cardPlay);
        }
    }

    internal sealed class CardModelEndOfTurnSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_card_end_turn_cleanup";
        public static string Description => "Clear this-turn secondary-resource card costs";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.EndOfTurnCleanup))];
        }

        public static void Postfix(CardModel __instance)
        {
            if (ModSecondaryResourceRegistry.HasAny)
                __instance.ClearSecondaryCostsThisTurn();
        }
    }

    internal sealed class HookAfterSideTurnStartSecondaryResourcesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_secondary_resource_turn_start";
        public static string Description => "Apply secondary-resource turn-start policies";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_106_0
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart),
                    [typeof(CombatStateLike), typeof(CombatSide), typeof(IReadOnlyList<Creature>)]),
#else
                new(typeof(Hook), nameof(Hook.AfterSideTurnStart), [typeof(CombatStateLike), typeof(CombatSide)]),
#endif
            ];
        }

#if STS2_AT_LEAST_0_106_0
        public static void Postfix(
            CombatSide side,
            IReadOnlyList<Creature> participants,
            ref Task __result)
        {
            if (!ModSecondaryResourceRegistry.HasAny ||
                side != CombatSide.Player)
                return;

            var players = participants
                .Where(static creature => creature is { IsPlayer: true, Player: not null })
                .Select(static creature => creature.Player!)
                .Distinct()
                .ToArray();
            __result = After(__result, players);
        }
#else
        public static void Postfix(
            CombatStateLike combatState,
            CombatSide side,
            ref Task __result)
        {
            if (!ModSecondaryResourceRegistry.HasAny ||
                side != CombatSide.Player)
                return;

            __result = After(__result, combatState.Players.Distinct().ToArray());
        }
#endif

        private static async Task After(Task original, IReadOnlyList<Player> players)
        {
            await original;
            foreach (var player in players)
                await SecondaryResourceCmd.ApplyTurnStartPolicies(player);
        }
    }
}
