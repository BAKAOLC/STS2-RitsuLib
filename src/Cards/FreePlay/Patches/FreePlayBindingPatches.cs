using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.FreePlay.Patches
{
    /// <summary>
    ///     Binds engine-level SetToFree calls into <see cref="FreePlayBindingRegistry" /> markers.
    ///     将引擎级 SetToFree 调用绑定到 <see cref="FreePlayBindingRegistry" /> 标记。
    /// </summary>
    internal sealed class CardModelSetToFreeThisTurnBindingPatch : IPatchMethod
    {
        public static string PatchId => "card_model_set_to_free_this_turn_binding";
        public static string Description => "Bind CardModel.SetToFreeThisTurn calls to FreePlayBindingRegistry markers";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.SetToFreeThisTurn))];
        }

        public static void Postfix(CardModel __instance)
        {
            FreePlayBindingRegistry.MarkCardBaseCostsFreeThisTurn(__instance);
            FreePlayCardVisuals.Refresh(__instance);
        }
    }

    internal sealed class CardModelFreeThisTurnEndCleanupPatch : IPatchMethod
    {
        public static string PatchId => "card_model_free_this_turn_end_cleanup";

        public static string Description =>
            "Clear CardModel.SetToFreeThisTurn bindings during CardModel.EndOfTurnCleanup";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.EndOfTurnCleanup))];
        }

        public static void Postfix(CardModel __instance)
        {
            if (FreePlayBindingRegistry.ClearCardFreeThisTurn(__instance))
                FreePlayCardVisuals.Refresh(__instance);
        }
    }

    internal sealed class CardModelFreeAfterPlayedCleanupPatch : IPatchMethod
    {
        public static string PatchId => "card_model_free_after_played_cleanup";

        public static string Description =>
            "Clear CardModel free-play bindings after CardModel.OnPlayWrapper";

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

        public static void Postfix(CardModel __instance, ref Task __result)
        {
            __result = After(__instance, __result);
        }

        private static async Task After(CardModel card, Task original)
        {
            await original;
            if (FreePlayBindingRegistry.ClearCardFreeAfterPlayed(card))
                FreePlayCardVisuals.Refresh(card);
        }
    }

    internal sealed class CardModelSetToFreeThisCombatBindingPatch : IPatchMethod
    {
        public static string PatchId => "card_model_set_to_free_this_combat_binding";

        public static string Description =>
            "Bind CardModel.SetToFreeThisCombat calls to FreePlayBindingRegistry markers";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.SetToFreeThisCombat))];
        }

        public static void Postfix(CardModel __instance)
        {
            FreePlayBindingRegistry.MarkCardBaseCostsFreeThisCombat(__instance);
            FreePlayCardVisuals.Refresh(__instance);
        }
    }

    internal static class FreePlayCardVisuals
    {
        public static void Refresh(CardModel card)
        {
            if (card.Pile == null)
                return;

            NCard.FindOnTable(card)?.UpdateVisuals(card.Pile.Type, CardPreviewMode.Normal);
        }
    }
}
