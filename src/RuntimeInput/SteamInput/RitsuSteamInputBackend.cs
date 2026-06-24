using Godot;

namespace STS2RitsuLib.RuntimeInput
{
    internal static class RitsuSteamInputBackend
    {
        private static readonly Dictionary<string, object> ActionHandles = new(StringComparer.Ordinal);
        private static readonly HashSet<string> PressedActions = new(StringComparer.Ordinal);
        private static bool _handleCacheDirty = true;
        private static int _unavailableLogged;

        static RitsuSteamInputBackend()
        {
            RitsuSteamInputActionRegistry.ActionsChanged += () => _handleCacheDirty = true;
        }

        public static void Process()
        {
            if (!RitsuSteamInputManifestInstaller.IsManifestInstalled || !RitsuSteamInputInterop.IsSteamAvailable)
            {
                ReleaseAll();
                return;
            }

            var actions = RitsuSteamInputActionRegistry.GetActions();
            if (actions.Count == 0)
            {
                ReleaseAll();
                return;
            }

            try
            {
                if (!RitsuSteamInputInterop.TryGetFirstController(out var controllerHandle))
                {
                    ReleaseAll();
                    return;
                }

                EnsureActionHandles(actions);
                foreach (var action in actions)
                {
                    if (!ActionHandles.TryGetValue(action.SteamActionId, out var actionHandle))
                        continue;

                    var pressed = RitsuSteamInputInterop.IsDigitalActionPressed(controllerHandle, actionHandle);
                    var wasPressed = PressedActions.Contains(action.SteamActionId);
                    if (pressed == wasPressed)
                        continue;

                    if (pressed)
                        PressedActions.Add(action.SteamActionId);
                    else
                        PressedActions.Remove(action.SteamActionId);

                    EmitAction(action.InputActionName, pressed);
                }
            }
            catch (Exception ex)
            {
                ReleaseAll();
                if (Interlocked.Exchange(ref _unavailableLogged, 1) == 0)
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamInput] Optional backend disabled for this session; falling back to normal input. {ex.Message}");
            }
        }

        private static void EnsureActionHandles(IReadOnlyList<RitsuSteamInputActionDescriptor> actions)
        {
            if (!_handleCacheDirty)
                return;

            ActionHandles.Clear();
            foreach (var action in actions)
                if (RitsuSteamInputInterop.TryGetDigitalActionHandle(action.SteamActionId, out var handle))
                    ActionHandles[action.SteamActionId] = handle;

            _handleCacheDirty = false;
        }

        private static void ReleaseAll()
        {
            foreach (var actionId in PressedActions.ToArray())
            {
                var descriptor = RitsuSteamInputActionRegistry.GetActions()
                    .FirstOrDefault(action => action.SteamActionId == actionId);
                if (descriptor != null)
                    EmitAction(descriptor.InputActionName, false);
            }

            PressedActions.Clear();
        }

        private static void EmitAction(string inputActionName, bool pressed)
        {
            using var inputEvent = new InputEventAction();
            inputEvent.Action = new(inputActionName);
            inputEvent.Pressed = pressed;
            Input.ParseInputEvent(inputEvent);
        }
    }
}
