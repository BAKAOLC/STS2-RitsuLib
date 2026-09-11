using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Platform;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Ui.Files
{
    public sealed partial class RitsuFileDialog
    {
        private enum PickerInputMode
        {
            Keyboard,
            Controller,
            Touch,
        }

        private PickerInputMode _inputMode;
        private Label _inputHint = null!;
        private Vector2 _touchPosition;

        private void InitializeInputMode()
        {
            SetInputMode(Sts2InputCompat.IsUsingController
                ? PickerInputMode.Controller
                : DisplayServer.IsTouchscreenAvailable()
                    ? PickerInputMode.Touch
                    : PickerInputMode.Keyboard);
        }

        private void SetInputMode(PickerInputMode mode)
        {
            _inputMode = mode;
            _inputHint.Text = Text(mode switch
            {
                PickerInputMode.Controller => "controllerHint",
                PickerInputMode.Touch => "touchHint",
                _ => "keyboardHint",
            });
        }

        private void UpdateInputMode(InputEvent input)
        {
            switch (input)
            {
                case InputEventScreenTouch touch:
                    _touchPosition = touch.Position;
                    SetInputMode(PickerInputMode.Touch);
                    break;
                case InputEventScreenDrag drag:
                    _touchPosition = drag.Position;
                    SetInputMode(PickerInputMode.Touch);
                    break;
                case InputEventJoypadButton { Pressed: true }:
                case InputEventJoypadMotion motion when Math.Abs(motion.AxisValue) > 0.5f:
                case InputEventAction { Pressed: true }
                    when Sts2InputCompat.IsUsingController:
                    SetInputMode(PickerInputMode.Controller);
                    break;
                case InputEventKey { Pressed: true }:
                case InputEventMouseButton { Device: >= 0, Pressed: true }:
                case InputEventMouseMotion { Device: >= 0 } mouse
                    when mouse.Relative.LengthSquared() is > 1 and < 250000:
                    SetInputMode(PickerInputMode.Keyboard);
                    break;
            }
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            if (ActiveDialog != this || @event.IsEcho())
                return;
            UpdateInputMode(@event);
            if (_prompt == null)
            {
                foreach (var list in new[] { _files, _favorites, _recent })
                {
                    if (!list.HandleTouchInput(@event) && !(@event is InputEventMouse { Device: < 0 } mouse &&
                                                            list.GetGlobalRect().HasPoint(mouse.Position)))
                        continue;
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }

            var focus = GetViewport().GuiGetFocusOwner();
            if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape } ||
                @event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack))
            {
                if (_keyboardOpen)
                    CloseKeyboard();
                else if (!CloseTransientPopups())
                {
                    if (_prompt != null)
                        ClosePrompt();
                    else
                        Cancel();
                }

                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventJoypadButton or InputEventJoypadMotion)
            {
                if (focus is RitsuFileList fileList && fileList.HandleControllerInput(@event))
                {
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (@event is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.X })
                {
                    ShowFocusedActions(focus);
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (focus is LineEdit && (@event.IsActionPressed(MegaInput.select) ||
                                          @event.IsActionPressed(Sts2InputCompat.ConfirmAction)))
                {
                    PlatformUtil.OpenVirtualKeyboard();
                    _keyboardOpen = true;
                    GetViewport().SetInputAsHandled();
                }
                else if (@event.IsActionPressed(MegaInput.viewDeckAndTabLeft) ||
                         @event.IsActionPressed(MegaInput.viewExhaustPileAndTabRight))
                {
                    CycleRegion(@event.IsActionPressed(MegaInput.viewDeckAndTabLeft) ? -1 : 1);
                    GetViewport().SetInputAsHandled();
                }

                return;
            }

            if (@event is not InputEventKey { Pressed: true } key)
                return;
            if (key.Keycode == Key.Tab)
            {
                CycleFocus(key.ShiftPressed ? -1 : 1);
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_prompt != null)
                return;
            switch (key)
            {
                case { CtrlPressed: true, Keycode: Key.L }:
                    _path.GrabFocus();
                    _path.SelectAll();
                    break;
                case { CtrlPressed: true, Keycode: Key.H }:
                    ToggleHidden();
                    break;
                case { CtrlPressed: true, Keycode: Key.D }:
                    ToggleFavorite();
                    break;
                case { Keycode: Key.Menu }:
                    ShowFocusedActions(focus);
                    break;
                case { CtrlPressed: true, Keycode: Key.F }:
                    if (!_nameFilterRow.Visible)
                        ToggleNameFilter();
                    _nameFilter.GrabFocus();
                    break;
                case { Keycode: Key.F5 }:
                    Navigate(_directory, _historyPosition);
                    break;
                case { Keycode: Key.Backspace } when focus is not LineEdit:
                    GoUp();
                    break;
                case { AltPressed: true, Keycode: Key.Left or Key.Right }:
                    TraverseHistory(key.Keycode == Key.Left ? -1 : 1);
                    break;
                default:
                    return;
            }

            GetViewport().SetInputAsHandled();
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (ActiveDialog == this)
                GetViewport().SetInputAsHandled();
        }

        private void CloseKeyboard()
        {
            if (!_keyboardOpen)
                return;
            _keyboardOpen = false;
            PlatformUtil.CloseVirtualKeyboard();
        }

        private void ShowFocusedActions(Control? focus)
        {
            if (_prompt != null)
                return;
            if (focus == _favorites)
                ShowFavoriteActions(_favorites.Cursor);
            else if (focus == _files)
                ShowFileActions(_files.Cursor);
        }

        private bool CloseTransientPopups()
        {
            var open = Descendants(this).OfType<Control>().Where(control =>
                    control is IModSettingsTransientPopupOwner &&
                    Descendants(control).OfType<Control>().Any(child => child.TopLevel && child.IsVisibleInTree()))
                .Cast<IModSettingsTransientPopupOwner>().ToArray();
            foreach (var owner in open)
                owner.ForceCloseTransientUi();
            return open.Length > 0;
        }

        private void CycleRegion(int direction)
        {
            if (_prompt != null)
            {
                CycleFocus(direction);
                return;
            }

            CloseTransientPopups();
            var regions = _regions.Where(control => IsInstanceValid(control) && control.IsVisibleInTree() &&
                                                    control is not BaseButton { Disabled: true }).ToArray();
            if (regions.Length == 0)
                return;
            var focus = GetViewport().GuiGetFocusOwner();
            var index = Array.FindIndex(regions, control => control == focus ||
                                                            focus != null && control.IsAncestorOf(focus));
            var target = regions[(index + direction + regions.Length) % regions.Length];
            var child = target.FocusMode == FocusModeEnum.All ? target : FocusableControls(target).FirstOrDefault();
            child?.GrabFocus();
        }

        private void CycleFocus(int direction)
        {
            var controls = FocusableControls(_prompt ?? _window).ToArray();
            if (controls.Length == 0)
                return;
            var index = Array.IndexOf(controls, GetViewport().GuiGetFocusOwner());
            controls[(index + direction + controls.Length) % controls.Length].GrabFocus();
        }

        private void RebuildFocusGraph()
        {
            if (_closing || !IsInsideTree())
                return;
            var controls = FocusableControls(_prompt ?? _window).ToArray();
            for (var i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                control.FocusNext = control.GetPathTo(controls[(i + 1) % controls.Length]);
                control.FocusPrevious = control.GetPathTo(controls[(i + controls.Length - 1) % controls.Length]);
                foreach (var side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom })
                {
                    var direction = side switch
                    {
                        Side.Left => Vector2.Left,
                        Side.Top => Vector2.Up,
                        Side.Right => Vector2.Right,
                        _ => Vector2.Down,
                    };
                    var origin = control.GetGlobalRect().GetCenter();
                    var next = controls.Where(candidate => candidate != control &&
                                                           (candidate.GetGlobalRect().GetCenter() - origin).Dot(
                                                               direction) > 1)
                        .MinBy(candidate =>
                        {
                            var offset = candidate.GetGlobalRect().GetCenter() - origin;
                            return Math.Abs(offset.Cross(direction)) * 4 + offset.Length();
                        }) ?? control;
                    control.SetFocusNeighbor(side, control.GetPathTo(next));
                }
            }
        }

        private static IEnumerable<Control> FocusableControls(Node root) =>
            Descendants(root).OfType<Control>().Where(control => control.IsVisibleInTree() &&
                                                                 control.FocusMode == FocusModeEnum.All &&
                                                                 control is not BaseButton { Disabled: true });

        private static IEnumerable<Node> Descendants(Node root)
        {
            foreach (var child in root.GetChildren())
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }
    }
}
