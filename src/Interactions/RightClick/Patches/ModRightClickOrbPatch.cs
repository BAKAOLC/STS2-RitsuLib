using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    /// <summary>
    ///     Connects right-click dispatch to active local-player orb nodes.
    ///     将右键分发接入本地玩家的活动充能球节点。
    /// </summary>
    internal sealed class ModRightClickOrbPatch : IPatchMethod
    {
        private const string ConnectedMeta = "ritsulib_right_click_orb_connected";

        public static string PatchId => "ritsulib_right_click_orb";
        public static bool IsCritical => false;
        public static string Description => "Connect RitsuLib model right-click dispatch to active orbs";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NOrb), nameof(NOrb._Ready))];
        }

        public static void Postfix(NOrb __instance)
        {
            if (__instance.HasMeta(ConnectedMeta))
                return;

            __instance.SetMeta(ConnectedMeta, true);
            __instance.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
        }

        private static void OnGuiInput(NOrb orbNode, InputEvent inputEvent)
        {
            var viewport = orbNode.GetViewport();
            if (viewport.IsInputHandled() ||
                !TryGetTrigger(orbNode, inputEvent, out var trigger))
                return;

            var hand = NPlayerHand.Instance;
            if (hand == null ||
                hand.InCardPlay ||
                NTargetManager.Instance.IsInSelection ||
                !CombatManager.Instance.IsInProgress ||
                CombatManager.Instance.IsOverOrEnding)
                return;

            var orb = orbNode.Model;
            if (orb == null || orb.HasBeenRemovedFromState)
                return;

            var player = LocalContext.GetMe(orb.CombatState);
            if (player == null ||
                orb.Owner != player ||
                player.PlayerCombatState?.OrbQueue.Orbs.Contains(orb) != true)
                return;

            if (ModRightClickRegistry.TryDispatch(new(player, orb, trigger)))
                viewport.SetInputAsHandled();
        }

        private static bool TryGetTrigger(
            Control node,
            InputEvent inputEvent,
            out ModRightClickTrigger trigger)
        {
            switch (inputEvent)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton when
                    mouseButton.IsReleased():
                    trigger = new(false, null, ModRightClickSource.Orb);
                    return true;
                case InputEventAction { Action: var action } actionEvent when
                    action == MegaInput.cancel &&
                    actionEvent.IsPressed() &&
                    !actionEvent.IsEcho() &&
                    node.HasFocus():
                    trigger = new(true, null, ModRightClickSource.Orb);
                    return true;
                default:
                    trigger = default;
                    return false;
            }
        }
    }
}
