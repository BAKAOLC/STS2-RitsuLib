using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Cancels active extra-hand targeting before the vanilla hand starts another card play.
    ///     </para>
    ///     <para xml:lang="zh-CN">原版手牌开始另一次出牌前，取消生效中的额外手牌目标选择。</para>
    /// </summary>
    internal sealed class ModExtraHandVanillaCardPlaySwitchPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_vanilla_card_play_switch";
        public static string Description => "Switch from extra-hand targeting to a newly selected vanilla hand card";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPlayerHand), "StartCardPlay", [typeof(NHandCardHolder), typeof(bool)])];
        }

        public static void Prefix(NHandCardHolder holder)
        {
            if (!ModExtraHandPlayCoordinator.IsActiveHolder(holder))
                ModExtraHandPlayCoordinator.CancelActiveTargeting();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes playable extra-hand cards through the vanilla manual-play queue without moving them into the
    ///         backend hand, and returns the same card node to its extra hand when a queued action is canceled.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使可打出的额外手牌无需移入后端手牌即可使用原版手动出牌队列，并在已排队动作取消时将同一卡牌节点退回额外手牌。
    ///     </para>
    /// </summary>
    internal sealed class ModExtraHandCardPlayCancelPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_play_cancel";

        public static string Description =>
            "Play extra-hand cards directly from their source pile and return canceled card nodes";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                PatchTarget.AsyncMethod<PlayCardAction>("ExecuteAction"),
                new(typeof(NCardPlayQueue), nameof(NCardPlayQueue.OnLocalCardPlayed),
                    [typeof(PlayCardAction), typeof(NCardHolder), typeof(CardModel)]),
                new(typeof(NCardPlayQueue), "RemoveCardFromQueueForCancellation",
                    [typeof(int), typeof(bool)]),
                new(typeof(PlayCardAction), "CancelAction", Type.EmptyTypes),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            if (__originalMethod.DeclaringType == typeof(PlayCardAction))
                return instructions;

            const string pileOperation = "[ExtraHand] direct manual-play pile routing";
            var pileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.Type)), pileOperation);
            var compatiblePileTypeGetter = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandPlayCoordinator),
                    nameof(ModExtraHandPlayCoordinator.GetVanillaManualPlayPileType)), pileOperation);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var pileReport = rewriter.RedirectCalls(
                pileOperation,
                called => called == pileTypeGetter ? compatiblePileTypeGetter : null,
                code => code.Any(HarmonyIl.IsCall(compatiblePileTypeGetter)));
            if (__originalMethod.DeclaringType?.DeclaringType == typeof(PlayCardAction))
            {
                const string resourceOperation = "[ExtraHand] evaluate resource payment with hand semantics";
                var spendResources = HarmonyIl.RequireMethod(
                    AccessTools.Method(typeof(CardModel), nameof(CardModel.SpendResources)), resourceOperation);
                var spendResourcesWithHandSemantics = HarmonyIl.RequireMethod(
                    AccessTools.Method(typeof(ModExtraHandPlayCoordinator),
                        nameof(ModExtraHandPlayCoordinator.SpendResourcesWithHandSemantics)), resourceOperation);
                var resourceReport = rewriter.RedirectCalls(
                    resourceOperation,
                    called => called == spendResources ? spendResourcesWithHandSemantics : null,
                    code => code.Any(HarmonyIl.IsCall(spendResourcesWithHandSemantics)));
                return rewriter.InstructionsChecked([pileReport, resourceReport]);
            }

            if (__originalMethod.Name != "RemoveCardFromQueueForCancellation")
                return rewriter.InstructionsChecked([pileReport]);

            const string returnOperation = "[ExtraHand] route canceled queued card to its source extra hand";
            var vanillaHandAdd = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(NPlayerHand), nameof(NPlayerHand.Add), [typeof(NCard), typeof(int)]),
                returnOperation);
            var returnCancelledCard = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandPlayCoordinator),
                    nameof(ModExtraHandPlayCoordinator.ReturnCancelledQueuedCard)), returnOperation);
            var returnReport = rewriter.RedirectCalls(
                returnOperation,
                called => called == vanillaHandAdd ? returnCancelledCard : null,
                code => code.Any(HarmonyIl.IsCall(returnCancelledCard)));
            return rewriter.InstructionsChecked([pileReport, returnReport]);
        }

        public static void Postfix(object __instance)
        {
            if (__instance is PlayCardAction action)
                ModExtraHandPlayCoordinator.RestoreCancelledAction(action);
        }
    }
}
