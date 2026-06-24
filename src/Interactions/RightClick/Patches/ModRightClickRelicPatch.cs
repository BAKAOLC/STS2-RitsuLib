using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
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
        private const string ConnectedMeta = "ritsulib_right_click_relic_connected";

        public static string PatchId => "ritsulib_right_click_relic";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to relics";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))];
        }

        public static void Postfix(NRelicInventoryHolder __instance)
        {
            if (__instance.HasMeta(ConnectedMeta))
                return;

            __instance.SetMeta(ConnectedMeta, true);
            __instance.Connect(NClickableControl.SignalName.MouseReleased,
                Callable.From<InputEvent>(inputEvent => OnMouseReleased(__instance, inputEvent)));
            __instance.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
        }

        private static void OnMouseReleased(NRelicInventoryHolder holder, InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick &&
                rightClick.IsReleased())
                TryHandle(holder, new(false));
        }

        private static void OnGuiInput(NRelicInventoryHolder holder, InputEvent inputEvent)
        {
            if (inputEvent is InputEventAction { Action: var action } actionEvent &&
                action == MegaInput.cancel &&
                actionEvent.IsPressed() &&
                !actionEvent.IsEcho() &&
                holder.HasFocus())
                TryHandle(holder, new(true));
        }

        private static void TryHandle(NRelicInventoryHolder holder, ModRightClickTrigger trigger)
        {
            var viewport = holder.GetViewport();
            if (viewport.IsInputHandled())
                return;

            if (NTargetManager.Instance.IsInSelection)
                return;

            var relic = holder.Relic.Model;
            var player = LocalContext.GetMe(relic.Owner.RunState);
            if (player == null)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, relic, trigger)))
                viewport.SetInputAsHandled();
        }
    }
}
