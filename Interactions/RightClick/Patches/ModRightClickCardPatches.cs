using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
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
}
