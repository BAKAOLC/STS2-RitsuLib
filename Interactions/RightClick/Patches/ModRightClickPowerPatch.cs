using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     Connects right-click dispatch to power nodes.
    ///     将右键分发接入能力节点。
    /// </summary>
    internal sealed class ModRightClickPowerPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_right_click_power";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to powers";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPower), nameof(NPower._Ready))];
        }

        public static void Postfix(NPower __instance)
        {
            __instance.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
        }

        private static void OnGuiInput(NPower powerNode, InputEvent inputEvent)
        {
            var viewport = powerNode.GetViewport();
            if (viewport.IsInputHandled())
                return;

            if (!TryGetTrigger(powerNode, inputEvent, out var trigger) ||
                NTargetManager.Instance.IsInSelection)
                return;

            var power = powerNode.Model;
            var player = LocalContext.GetMe(power.Owner.CombatState);
            if (player == null)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, power, trigger)))
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
