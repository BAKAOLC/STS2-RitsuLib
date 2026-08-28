using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    internal sealed class ModExtraHandCardPileSemanticPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_pile_semantics";
        public static string Description => "Expose hand identity while evaluating playable extra-hand cards";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_Pile", Type.EmptyTypes)];
        }

        public static bool Prefix(CardModel __instance, ref CardPile? __result)
        {
            if (!ModExtraHandSemanticContext.IsActive(__instance))
                return true;

            __result = __instance.Owner.PlayerCombatState?.Hand;
            return __result == null;
        }
    }

    internal sealed class ModExtraHandCanPlaySemanticPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_can_play_semantics";
        public static string Description => "Evaluate playable extra-hand cards with hand-consistent semantics";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.CanPlay),
                [
                    typeof(UnplayableReason).MakeByRefType(),
                    typeof(AbstractModel).MakeByRefType(),
                ]),
            ];
        }

        public static void Prefix(CardModel __instance, out IDisposable? __state)
        {
            __state = ModExtraHandSemanticContext.EnterPlayEvaluation(__instance);
        }

        public static void Finalizer(IDisposable? __state)
        {
            __state?.Dispose();
        }
    }

    internal sealed class ModExtraHandCardVisualSemanticPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_visual_semantics";
        public static string Description => "Render playable extra-hand cards with hand-consistent costs";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard), [])];
        }

        public static void Prefix(NHandCardHolder __instance, out IDisposable? __state)
        {
            __state = __instance.CardModel is { } card
                ? ModExtraHandSemanticContext.EnterPlayEvaluation(card)
                : null;
        }

        public static void Finalizer(IDisposable? __state)
        {
            __state?.Dispose();
        }
    }
}
