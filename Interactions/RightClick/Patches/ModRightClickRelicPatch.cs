using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     Connects right-click dispatch to relic nodes.
    ///     将右键分发接入遗物节点。
    /// </summary>
    internal sealed class ModRightClickRelicPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_right_click_relic";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelic), nameof(NRelic._Ready))];
        }

        public static void Postfix(NRelic __instance)
        {
            __instance.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
        }

        private static void OnGuiInput(NRelic relicNode, InputEvent inputEvent)
        {
            var viewport = relicNode.GetViewport();
            if (viewport.IsInputHandled())
                return;

            if (!TryGetTrigger(relicNode, inputEvent, out var trigger) ||
                NTargetManager.Instance.IsInSelection)
                return;

            var relic = relicNode.Model;
            var player = LocalContext.GetMe(relic.Owner.RunState);
            if (player == null)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, relic, trigger)))
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
