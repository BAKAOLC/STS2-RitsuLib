using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Potions;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     Connects right-click dispatch to potion nodes.
    ///     将右键分发接入药水节点。
    /// </summary>
    internal sealed class ModRightClickPotionPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_right_click_potion";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to potions";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPotion), nameof(NPotion._Ready))];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NPotion __instance)
        {
            __instance.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
        }

        private static void OnGuiInput(NPotion potionNode, InputEvent inputEvent)
        {
            var viewport = potionNode.GetViewport();
            if (viewport.IsInputHandled())
                return;

            if (!TryGetTrigger(potionNode, inputEvent, out var trigger) ||
                NTargetManager.Instance.IsInSelection)
                return;

            var potion = potionNode.Model;
            var player = LocalContext.GetMe(potion.Owner.RunState);
            if (player == null)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, potion, trigger)))
                viewport.SetInputAsHandled();
        }

        private static bool TryGetTrigger(Control node, InputEvent inputEvent, out ModRightClickTrigger trigger)
        {
            switch (inputEvent)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton when
                    mouseButton.IsReleased():
                    trigger = new(false);
                    return true;
                case InputEventAction { Action: var action } actionEvent when
                    action == MegaInput.cancel &&
                    actionEvent.IsPressed() &&
                    node.HasFocus():
                    trigger = new(true);
                    return true;
                default:
                    trigger = default;
                    return false;
            }
        }
    }
}
