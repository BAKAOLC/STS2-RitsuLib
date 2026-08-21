using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Connects right-click dispatch to hand-style card holders.</para>
    ///     <para xml:lang="zh-CN">将右键分发接入手牌样式的卡牌容器。</para>
    /// </summary>
    internal sealed class ModRightClickCardHolderPatch : IPatchMethod
    {
        private const string AddCardHolderMethodName = "AddCardHolder";

        public static string PatchId => "ritsulib_right_click_card_holder";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to hand-style cards";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPlayerHand), AddCardHolderMethodName, [typeof(NHandCardHolder), typeof(int)])];
        }

        public static void Postfix(NHandCardHolder holder)
        {
            Connect(holder, ModRightClickSource.HandCard, null);
        }

        internal static void ConnectModPileHolder(NCardHolder holder, PileType pileType)
        {
            Connect(holder, ModRightClickSource.CombatPileCard, pileType);
        }

        private static void Connect(
            NCardHolder holder,
            ModRightClickSource source,
            PileType? expectedCardPile)
        {
            holder.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent =>
                    OnHolderGuiInput(holder, inputEvent, source, expectedCardPile)));
            holder.Hitbox.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent =>
                    OnHitboxGuiInput(holder, inputEvent, source, expectedCardPile)));
        }

        private static void OnHolderGuiInput(
            NCardHolder holder,
            InputEvent inputEvent,
            ModRightClickSource source,
            PileType? expectedCardPile)
        {
            var triggeredByController =
                inputEvent is InputEventAction { Action: var action } actionEvent &&
                action == MegaInput.cancel &&
                actionEvent.IsPressed() &&
                !actionEvent.IsEcho() &&
                holder.HasFocus();

            if (triggeredByController)
                TryHandle(holder, new(true, null, source, expectedCardPile));
        }

        private static void OnHitboxGuiInput(
            NCardHolder holder,
            InputEvent inputEvent,
            ModRightClickSource source,
            PileType? expectedCardPile)
        {
            var triggeredByMouse =
                inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick &&
                rightClick.IsPressed();

            if (triggeredByMouse)
                TryHandle(holder, new(false, null, source, expectedCardPile));
        }

        private static void TryHandle(NCardHolder holder, ModRightClickTrigger trigger)
        {
            var viewport = holder.GetViewport();
            if (viewport.IsInputHandled())
                return;

            var hand = NPlayerHand.Instance;
            if (hand == null || hand.InCardPlay || NTargetManager.Instance.IsInSelection)
                return;

            var card = holder.CardModel;
            if (card == null)
                return;

            var player = LocalContext.GetMe(card.CombatState);
            if (player == null)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, card, trigger)))
                viewport.SetInputAsHandled();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Connects right-click dispatch to cards displayed on combat-pile screens.</para>
    ///     <para xml:lang="zh-CN">将右键分发接入战斗牌堆界面中显示的卡牌。</para>
    /// </summary>
    internal sealed class ModRightClickCardPilePatch : IPatchMethod
    {
        private const string OnHolderAltPressedMethodName = "OnHolderAltPressed";

        public static string PatchId => "ritsulib_right_click_card_pile";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to combat pile cards";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardGrid), OnHolderAltPressedMethodName, [typeof(NCardHolder)])];
        }

        public static bool Prefix(NCardGrid __instance, NCardHolder holder)
        {
            if (!IsPileScreenGrid(__instance))
                return true;

            var viewport = __instance.GetViewport();
            var hand = NPlayerHand.Instance;
            if (hand == null || hand.InCardPlay || NTargetManager.Instance.IsInSelection)
                return true;

            var card = holder.CardModel;
            // ReSharper disable once UseNullPropagation
            if (card == null)
                return true;
            var pileType = card.Pile?.Type;
            if (pileType is not { } expectedPile || !ModRightClickCardPilePolicy.IsSupported(expectedPile))
                return true;

            var player = LocalContext.GetMe(card.CombatState);
            if (player == null)
                return true;

            var trigger = new ModRightClickTrigger(
                Sts2InputCompat.IsUsingController,
                null,
                ModRightClickSource.CombatPileCard,
                expectedPile);
            if (!ModRightClickInputConsumer.TryDispatchAndConsumeInput(
                    () => ModRightClickRegistry.TryDispatch(new(player, card, trigger)),
                    viewport.SetInputAsHandled))
                return true;

            return false;
        }

        private static bool IsPileScreenGrid(NCardGrid grid)
        {
            for (var node = grid.GetParent(); node != null; node = node.GetParent())
                if (node is NCardPileScreen)
                    return true;

            return false;
        }
    }
}
