using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes cards entering an extra hand through the vanilla hand-holder animation.
    ///     </para>
    ///     <para xml:lang="zh-CN">使进入额外手牌的卡牌使用原版手牌容器动画。</para>
    /// </summary>
    internal sealed class ModExtraHandCardEntryPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_entry";

        public static string Description =>
            "Use vanilla hand-holder motion when cards enter an extra hand";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_110_0
                new(typeof(CardPileCmd), "GetTweenForCardsChangingPiles",
                    [typeof(IEnumerable<(NCard, PileType?)>)]),
#else
                new(typeof(CardPileCmd), nameof(CardPileCmd.Add),
                [
                    typeof(IEnumerable<MegaCrit.Sts2.Core.Models.CardModel>),
                    typeof(CardPile),
                    typeof(CardPilePosition),
                    typeof(MegaCrit.Sts2.Core.Models.AbstractModel),
                    typeof(bool),
#if STS2_AT_LEAST_0_109_0
                    typeof(bool),
#endif
                ], MethodType.Async),
#endif
            ];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation = "[ExtraHand] route visible extra-hand entries through the vanilla hand branch";
            var pileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.Type)), operation);
            var visualPileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandCardEntryPatch), nameof(GetVisualPileType)), operation);
            var rewriter = HarmonyIlRewriter.From(instructions);
#if STS2_AT_LEAST_0_110_0
            var report = rewriter.RedirectCalls(
                operation,
                called => called == pileTypeGetter ? visualPileTypeGetter : null,
                code => code.Any(HarmonyIl.IsCall(visualPileTypeGetter)));
            return rewriter.InstructionsChecked([report]);
#else
            var findOnTableMethod = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(NCard), nameof(NCard.FindOnTable)), operation);
            var afterCardChangedPilesMethod = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(MegaCrit.Sts2.Core.Hooks.Hook),
                    nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterCardChangedPiles)), operation);
            var code = rewriter.Instructions();
            var startIndexes = code
                .Select((instruction, index) => (instruction, index))
                .Where(entry => HarmonyIl.IsCallTo(entry.instruction, findOnTableMethod))
                .Select(entry => entry.index)
                .ToArray();
            var hookIndexes = code
                .Select((instruction, index) => (instruction, index))
                .Where(entry => HarmonyIl.IsCallTo(entry.instruction, afterCardChangedPilesMethod))
                .Select(entry => entry.index)
                .ToArray();
            if (startIndexes.Length != 1 || hookIndexes.Length != 1)
                throw new InvalidOperationException(
                    $"{operation} expected one visual-start and one hook boundary, but found "
                    + $"{startIndexes.Length} and {hookIndexes.Length}.");

            var hookPileTypeIndex = Enumerable.Range(startIndexes[0] + 1, hookIndexes[0] - startIndexes[0] - 1)
                .Last(index => HarmonyIl.IsCallTo(code[index], pileTypeGetter));
            var replaced = 0;
            for (var index = startIndexes[0] + 1; index < hookPileTypeIndex; index++)
            {
                if (!HarmonyIl.IsCallTo(code[index], pileTypeGetter))
                    continue;

                code[index].opcode = System.Reflection.Emit.OpCodes.Call;
                code[index].operand = visualPileTypeGetter;
                replaced++;
            }

#if STS2_AT_LEAST_0_109_0
            const int expectedSites = 17;
#else
            const int expectedSites = 16;
#endif
            if (replaced != expectedSites)
                throw new InvalidOperationException(
                    $"{operation} replaced {replaced} visual pile-type reads, expected {expectedSites}.");

            return rewriter.InstructionsChecked(operation);
#endif
        }

        internal static PileType GetVisualPileType(CardPile pile)
        {
            return ModCardPileButtonRegistry.TryGetExtraHand(pile) != null
                ? PileType.Hand
                : pile.Type;
        }
    }

#if STS2_AT_LEAST_0_110_0
    internal sealed class ModExtraHandCardEntryPreparationPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_entry_preparation";

        public static string Description => "Prepare extra-hand destinations through the vanilla hand visual flow";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPileCmd), "GetTweenForCardsChangingPiles",
#if STS2_AT_LEAST_0_111_0
                    [typeof(IEnumerable<CardPileAddResult>), typeof(bool)]),
#else
                    [typeof(IEnumerable<CardPileAddResult>)]),
#endif
            ];
        }

        [HarmonyAfter(Const.BaseLibHarmonyId)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation = "[ExtraHand] prepare extra-hand destinations as vanilla hand visuals";
            var pileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.Type)), operation);
            var visualPileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandCardEntryPatch),
                    nameof(ModExtraHandCardEntryPatch.GetVisualPileType)), operation);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = rewriter.TryReplaceFirst(
                operation,
                HarmonyIlPattern.Sequence(HarmonyIl.IsCall(pileTypeGetter)),
                [HarmonyIl.Call(visualPileTypeGetter)],
                code => code.Any(HarmonyIl.IsCall(visualPileTypeGetter)));
            report.RequireSucceeded();
            report.RequireExactly(1);
            return rewriter.InstructionsChecked(operation);
        }
    }
#endif

    internal sealed class ModExtraHandCardAddPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_add";
        public static string Description => "Add extra-hand card nodes through the vanilla hand-holder flow";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NPlayerHand), nameof(NPlayerHand.Add), [typeof(NCard), typeof(int)]),
            ];
        }

        public static bool Prefix(NCard card, int index, ref NHandCardHolder __result)
        {
            if (card.Model?.Pile is not { } pile
                || ModCardPileButtonRegistry.TryGetExtraHand(pile) is not { } extraHand
                || extraHand.AddFromVanillaHandFlow(card, index) is not { } holder)
                return true;

            __result = holder;
            return false;
        }
    }

    internal sealed class ModExtraHandCardMovePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_move";
        public static string Description => "Move card nodes out of extra hands through the vanilla hand flow";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPileCmd), "MoveCardNodeToNewPileBeforeTween",
                    [typeof(NCard), typeof(PileType)]),
            ];
        }

        public static bool Prefix(NCard cardNode, PileType newPileType)
        {
            return ModCardPileButtonRegistry.TryGetExtraHandContaining(cardNode)
                ?.MoveCardNodeToNewPileBeforeTween(cardNode, newPileType) != true;
        }
    }
}
