using System.Runtime.CompilerServices;
using Godot;

namespace STS2RitsuLib.Settings
{
    public static partial class ModSettingsUiFactory
    {
        private static readonly ConditionalWeakTable<Control, ControlEnablementState> ControlEnablementStates = [];

        internal static Control MaybeWrapDynamicEnabled(ModSettingsUiContext context, Control host,
            Func<bool>? predicate, Func<bool>? canApply = null)
        {
            if (predicate == null)
                return host;

            var gate = new object();
            Apply();
            RegisterRefreshWhenAlive(context, host, Apply, ModSettingsUiRefreshSpec.Always);
            return host;

            void Apply()
            {
                if (canApply != null && !canApply())
                    return;
                if (!GodotObject.IsInstanceValid(host))
                    return;
                ApplyEnabledRecursive(host, gate, ModSettingsPredicate.Evaluate(predicate));
            }
        }

        internal static void AttachHostSurfaceReadOnlySync(ModSettingsUiContext context, Control host,
            ModSettingsHostSurface readOnlyMask, Func<bool>? canApply = null)
        {
            if (readOnlyMask == ModSettingsHostSurface.None)
                return;

            var gate = new object();
            Apply();
            RegisterRefreshWhenAlive(context, host, Apply, ModSettingsUiRefreshSpec.Always);
            return;

            void Apply()
            {
                if (canApply != null && !canApply())
                    return;
                if (!GodotObject.IsInstanceValid(host))
                    return;

                var enabled = !ModSettingsHostSurfaceResolver.IsReadOnlyOnCurrentHost(readOnlyMask);
                ApplyEnabledRecursive(host, gate, enabled);
            }
        }

        internal static void ApplyEnabledRecursive(Node node, object gate, bool enabled)
        {
            if (node is Control control)
                ApplyEnabledToControl(control, gate, enabled);

            foreach (var child in node.GetChildren())
                if (child != null)
                    ApplyEnabledRecursive(child, gate, enabled);
        }

        internal static void ClearEnabledStateRecursive(Node node)
        {
            if (node is Control control && ControlEnablementStates.TryGetValue(control, out var state))
            {
                if (!state.IsEnabledApplied)
                    state.Restore(control);
                ControlEnablementStates.Remove(control);
            }

            foreach (var child in node.GetChildren())
                if (child != null)
                    ClearEnabledStateRecursive(child);
        }

        private static void ApplyEnabledToControl(Control control, object gate, bool enabled)
        {
            if (!GodotObject.IsInstanceValid(control))
                return;

            var state = ControlEnablementStates.GetValue(control, static current => new(current));
            var isEnabled = state.SetGate(gate, enabled);
            if (isEnabled == state.IsEnabledApplied)
                return;
            if (isEnabled)
            {
                state.Restore(control);
                return;
            }

            state.Capture(control);

            if (control is ModSettingsActionsButton actions)
                actions.ForceCloseDropdown();
            if (control is IModSettingsTransientPopupOwner popupOwner)
                popupOwner.ForceCloseTransientUi();
            if (control.HasFocus())
                control.ReleaseFocus();

            control.ProcessMode = Node.ProcessModeEnum.Disabled;
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
            control.FocusMode = Control.FocusModeEnum.None;
            control.Modulate = ResolveDisabledModulate(control, state.Modulate);
            switch (control)
            {
                case BaseButton button:
                    button.Disabled = true;
                    break;
                case LineEdit lineEdit:
                    lineEdit.Editable = false;
                    break;
                case TextEdit textEdit:
                    textEdit.Editable = false;
                    break;
            }

            state.IsEnabledApplied = false;
        }

        private sealed class ControlEnablementState
        {
            private int _disabledGateCount;

            public ControlEnablementState(Control control)
            {
                Capture(control);
            }

            public bool ButtonDisabled { get; private set; }
            public Dictionary<object, bool> Gates { get; } = new(ReferenceEqualityComparer.Instance);
            public bool IsEnabledApplied { get; set; } = true;
            public bool LineEditEditable { get; private set; }
            public Color Modulate { get; private set; }
            public Control.MouseFilterEnum MouseFilter { get; private set; }
            public Node.ProcessModeEnum ProcessMode { get; private set; }
            public Control.FocusModeEnum FocusMode { get; private set; }
            public bool TextEditEditable { get; private set; }

            public void Capture(Control current)
            {
                MouseFilter = current.MouseFilter;
                ProcessMode = current.ProcessMode;
                FocusMode = current.FocusMode;
                Modulate = current.Modulate;
                ButtonDisabled = current is BaseButton { Disabled: true };
                LineEditEditable = current is not LineEdit { Editable: false };
                TextEditEditable = current is not TextEdit { Editable: false };
            }

            public bool SetGate(object gate, bool enabled)
            {
                if (Gates.TryGetValue(gate, out var previous))
                {
                    if (previous == enabled)
                        return _disabledGateCount == 0;
                    _disabledGateCount += enabled ? -1 : 1;
                }
                else if (!enabled)
                {
                    _disabledGateCount++;
                }

                Gates[gate] = enabled;
                return _disabledGateCount == 0;
            }

            public void Restore(Control control)
            {
                control.MouseFilter = MouseFilter;
                control.ProcessMode = ProcessMode;
                control.FocusMode = FocusMode;
                control.Modulate = Modulate;
                switch (control)
                {
                    case BaseButton button:
                        button.Disabled = ButtonDisabled;
                        break;
                    case LineEdit lineEdit:
                        lineEdit.Editable = LineEditEditable;
                        break;
                    case TextEdit textEdit:
                        textEdit.Editable = TextEditEditable;
                        break;
                }

                IsEnabledApplied = true;
            }
        }
    }
}
