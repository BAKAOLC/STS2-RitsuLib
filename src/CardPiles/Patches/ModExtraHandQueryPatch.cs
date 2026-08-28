using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.CardPiles.Patches
{
    internal sealed class ModExtraHandQueryPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_queries";
        public static string Description => "Include play-enabled extra hands in core hand-state queries";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PlayerCombatState), nameof(PlayerCombatState.HasCardsToPlay), []),
                new(typeof(NEndTurnButton), "HasPlayableCard", []),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            const string operation = "[ExtraHand] include play-enabled extra hands in vanilla hand queries";
            var handGetter = HarmonyIl.RequireMethod(
                AccessTools.PropertyGetter(typeof(PlayerCombatState), nameof(PlayerCombatState.Hand)), operation);
            var cardsGetter = HarmonyIl.RequireMethod(
                AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.Cards)), operation);
            var queryHand = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandHandView), nameof(ModExtraHandHandView.GetQueryHand)), operation);
            var queryCards = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandHandView), nameof(ModExtraHandHandView.GetCardsForQueries)),
                operation);
            var isEndTurnUiQuery = __originalMethod.DeclaringType == typeof(NEndTurnButton);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = rewriter.RedirectCalls(
                $"{operation} in {__originalMethod.DeclaringType?.FullName}.{__originalMethod.Name}",
                called => called == handGetter
                    ? queryHand
                    : isEndTurnUiQuery && called == cardsGetter
                        ? queryCards
                        : null,
                code => code.Any(HarmonyIl.IsCall(queryHand))
                        || code.Any(HarmonyIl.IsCall(queryCards)));
            report.RequireSucceeded();
            return rewriter.InstructionsChecked(report.Operation);
        }
    }

    internal sealed class ModExtraHandPlayableFlashPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_playable_flash";
        public static string Description => "Flash playable extra-hand cards with the base-game hand";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPlayerHand), nameof(NPlayerHand.FlashPlayableHolders), [])];
        }

        public static void Postfix()
        {
            foreach (var hand in ModCardPileButtonRegistry.GetExtraHands())
                hand.FlashPlayableHolders();
        }
    }
}
