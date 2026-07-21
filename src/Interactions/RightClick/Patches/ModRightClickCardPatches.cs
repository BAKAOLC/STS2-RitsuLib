using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     Connects right-click dispatch to hand-card holders.
    ///     将右键分发接入手牌 holder。
    /// </summary>
    internal sealed class ModRightClickCardHolderPatch : IPatchMethod
    {
        private const string AddCardHolderMethodName = "AddCardHolder";

        public static string PatchId => "ritsulib_right_click_card_holder";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to hand cards";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPlayerHand), AddCardHolderMethodName, [typeof(NHandCardHolder), typeof(int)])];
        }

        public static void Postfix(NHandCardHolder holder)
        {
            holder.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnHolderGuiInput(holder, inputEvent)));
            holder.Hitbox.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnHitboxGuiInput(holder, inputEvent)));
        }

        private static void OnHolderGuiInput(NCardHolder holder, InputEvent inputEvent)
        {
            var triggeredByController =
                inputEvent is InputEventAction { Action: var action } actionEvent &&
                action == MegaInput.cancel &&
                actionEvent.IsPressed() &&
                holder.HasFocus();

            if (triggeredByController)
                TryHandle(holder, new(true));
        }

        private static void OnHitboxGuiInput(NCardHolder holder, InputEvent inputEvent)
        {
            var triggeredByMouse =
                inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick &&
                rightClick.IsPressed();

            if (triggeredByMouse)
                TryHandle(holder, new(false));
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
    ///     Connects right-click dispatch to cards shown in combat pile screens.
    ///     将右键分发接入战斗牌堆 screen 中显示的卡牌。
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

            var hand = NPlayerHand.Instance;
            if (hand == null || hand.InCardPlay || NTargetManager.Instance.IsInSelection)
                return true;

            var card = holder.CardModel;
            if (card == null)
                return true;

            var player = LocalContext.GetMe(card.CombatState);
            if (player == null)
                return true;

            var trigger = new ModRightClickTrigger(NControllerManager.Instance?.IsUsingController == true);
            if (!ModRightClickRegistry.TryDispatch(new(player, card, trigger)))
                return true;

            holder.GetViewport().SetInputAsHandled();
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
