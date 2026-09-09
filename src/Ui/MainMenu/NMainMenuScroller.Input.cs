using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Ui.MainMenu
{
    internal sealed partial class NMainMenuScroller
    {
        private const float HeightTweenDuration = 0.2f;
        private static readonly AccessTools.FieldRef<NClickableControl, bool> Pressed =
            AccessTools.FieldRefAccess<NClickableControl, bool>("_isPressed");
        private static readonly AccessTools.FieldRef<NClickableControl, bool> Hovered =
            AccessTools.FieldRefAccess<NClickableControl, bool>("_isHovered");
        private static readonly AccessTools.FieldRef<NClickableControl, bool> ControllerFocused =
            AccessTools.FieldRefAccess<NClickableControl, bool>("_isControllerFocused");
        private static readonly Action<NClickableControl> RefreshClickableFocus =
            AccessTools.MethodDelegate<Action<NClickableControl>>(
                AccessTools.DeclaredMethod(typeof(NClickableControl), "RefreshFocus"));

        private NMainMenuTextButton? _lastFocused;
        private NMainMenuTextButton? _pressedButton;
        private int _lastFocusedIndex;
        private bool _wasActive;
        private bool _restorePending;
        private bool _keyboardNavigating;
        private NControllerManager? _controllerManager;
        private Tween? _visualScrollTween;
        private float _visualScroll;
        private int _heightStepIndex;

        private static bool IsDirectional => Sts2InputCompat.IsUsingDirectionalNavigation;

        private bool FollowsFocus => _keyboardNavigating || IsDirectional;

        private bool IsActive => Initialized && IsVisibleInTree() &&
                                 ActiveScreenContext.Instance.IsCurrent(_mainMenu) &&
                                 NGame.Instance?.Transition?.InTransition != true;

        private void ConnectInputEvents()
        {
            GetViewport().GuiFocusChanged += OnGuiFocusChanged;
            GetWindow().FocusExited += OnWindowFocusExited;
            ActiveScreenContext.Instance.Updated += OnScreenChanged;
            _controllerManager = NControllerManager.Instance;
            if (_controllerManager == null)
                return;
            _controllerManager.MouseDetected += OnMouseDetected;
            _controllerManager.ControllerDetected += OnControllerDetected;
        }

        private void DisconnectInputEvents()
        {
            CancelPress();
            GetViewport().GuiFocusChanged -= OnGuiFocusChanged;
            GetWindow().FocusExited -= OnWindowFocusExited;
            ActiveScreenContext.Instance.Updated -= OnScreenChanged;
            if (_controllerManager == null || !IsInstanceValid(_controllerManager))
                return;
            _controllerManager.MouseDetected -= OnMouseDetected;
            _controllerManager.ControllerDetected -= OnControllerDetected;
        }

        private static bool IsNavigable(NMainMenuTextButton? button)
        {
            return button != null && IsInstanceValid(button) && !button.IsQueuedForDeletion() &&
                   button.IsInsideTree() && button.IsVisibleInTree() &&
                   button is { IsEnabled: true, FocusMode: FocusModeEnum.All };
        }

        private void OnWindowFocusExited()
        {
            CancelPress();
        }

        internal Control? GetDefaultFocus()
        {
            if (!Initialized)
                return null;
            if (_layoutDirty)
                RefreshLayout();
            if (IsNavigable(_lastFocused) && _lastFocused!.GetParent() == this)
                return _lastFocused;
            return _buttons.Skip(Math.Min(_lastFocusedIndex, _buttons.Count)).FirstOrDefault(IsNavigable) ??
                   _buttons.Take(Math.Min(_lastFocusedIndex, _buttons.Count)).LastOrDefault(IsNavigable);
        }

        private void RebuildNavigation()
        {
            var navigable = _buttons.Where(IsNavigable).ToList();
            for (var i = 0; i < navigable.Count; i++)
            {
                var button = navigable[i];
                button.FocusNeighborTop = i == 0 ? new("") : button.GetPathTo(navigable[i - 1]);
                button.FocusNeighborBottom = button.GetPathTo(navigable[Math.Min(navigable.Count - 1, i + 1)]);
                button.FocusNeighborLeft = new(".");
                button.FocusNeighborRight = new(".");
                button.FocusPrevious = button.FocusNeighborTop;
                button.FocusNext = button.FocusNeighborBottom;
            }
        }

        private void OnGuiFocusChanged(Control control)
        {
            if (_pressedButton != control)
                CancelPress();
            if (!IsActive || control.GetParent() != this || control is not NMainMenuTextButton button)
                return;
            if (_layoutDirty)
                RefreshLayout();
            if (!FollowsFocus)
                return;
            AdoptExclusiveFocus(button, mouse: false);
        }

        private void OnButtonFocused(NClickableControl control)
        {
            if (control is not NMainMenuTextButton button || !IsActive)
                return;
            _lastFocused = button;
            _lastFocusedIndex = Math.Max(0, _buttons.IndexOf(button));
            if (_layoutDirty)
                RefreshLayout();
            if (button.HasFocus())
                AdoptExclusiveFocus(button, mouse: false);
            else
                AdoptExclusiveFocus(button, mouse: true);
        }

        private void OnButtonUnfocused(NClickableControl control)
        {
            if (control is not NMainMenuTextButton button)
                return;
            CancelPress(button);
            HideReticles(button);
        }

        private void AdoptExclusiveFocus(NMainMenuTextButton button, bool mouse)
        {
            if (mouse)
            {
                _keyboardNavigating = false;
                ClearGodotFocus();
                ShowReticles(button);
                return;
            }

            _keyboardNavigating = true;
            ClearHoverFocus();
            if (!button.HasFocus())
                button.GrabFocus();
            EnsureVisible(button);
            ShowReticles(button);
        }

        private void ClearHoverFocus()
        {
            foreach (var button in _buttons)
            {
                if (!IsInstanceValid(button) || !Hovered(button))
                    continue;
                Hovered(button) = false;
                RefreshClickableFocus(button);
            }
        }

        private void ClearGodotFocus()
        {
            foreach (var button in _buttons)
            {
                if (!IsInstanceValid(button))
                    continue;
                if (button.HasFocus())
                    button.ReleaseFocus();
                if (!ControllerFocused(button))
                    continue;
                ControllerFocused(button) = false;
                RefreshClickableFocus(button);
            }
        }

        private void OnScreenChanged()
        {
            CancelPress();
            _restorePending = IsDirectional;
            if (!IsActive)
                HideDecorations();
        }

        private void OnVisibilityChanged()
        {
            RequestLayout();
            OnScreenChanged();
        }

        private void OnMouseDetected()
        {
            CancelPress();
            _restorePending = false;
            _keyboardNavigating = false;
            ClearGodotFocus();
            var pos = GetGlobalMousePosition();
            if (_buttons.LastOrDefault(button =>
                    IsNavigable(button) && button.GetGlobalRect().HasPoint(pos)) is { } hovered)
                ShowReticles(hovered);
        }

        private void OnControllerDetected()
        {
            CancelPress();
            _restorePending = true;
            ClearHoverFocus();
            RestoreFocusIfMissing();
        }

        private void RefreshInteraction()
        {
            var active = IsActive;
            if (!active)
            {
                if (_wasActive)
                {
                    CancelPress();
                    HideDecorations();
                }
            }
            else if (!_wasActive || _restorePending ||
                     (FollowsFocus && GetViewport().GuiGetFocusOwner() == null) ||
                     (_lastFocused != null && !IsNavigable(_lastFocused)))
            {
                RestoreFocusIfMissing();
            }

            _wasActive = active;
        }

        private void RestoreFocusIfMissing()
        {
            if (!IsActive || !IsDirectional)
                return;
            _restorePending = false;
            var current = GetViewport().GuiGetFocusOwner();
            if (current != null && (current.GetParent() != this ||
                                    current is NMainMenuTextButton button && IsNavigable(button)))
            {
                if (current.GetParent() == this && current is NMainMenuTextButton focusedButton)
                    AdoptExclusiveFocus(focusedButton, mouse: false);

                return;
            }

            if (GetDefaultFocus() is not NMainMenuTextButton target)
                return;
            AdoptExclusiveFocus(target, mouse: false);
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (!IsActive || !ScrollEngaged)
                return;
            if (inputEvent is InputEventMouseButton
                {
                    Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown,
                }
                or InputEventPanGesture)
            {
                CancelPress();
                SetScroll(_scroll - ScrollHelper.GetDragForScrollEvent(inputEvent));
                AcceptEvent();
            }
        }

        public override void _UnhandledInput(InputEvent inputEvent)
        {
            if (!IsActive ||
                !inputEvent.IsActionPressed(MegaInput.up) && !inputEvent.IsActionPressed(MegaInput.down))
                return;
            if (GetViewport().GuiGetFocusOwner() is Control focused && focused.GetParent() == this)
                return;

            _keyboardNavigating = true;
            RebuildNavigation();
            var navigable = _buttons.Where(IsNavigable).ToList();
            if (navigable.Count == 0)
                return;
            var from = navigable.FirstOrDefault(button => Hovered(button)) ??
                       (IsNavigable(_lastFocused) ? _lastFocused : null) ??
                       navigable[0];
            var index = Math.Max(0, navigable.IndexOf(from));
            var target = inputEvent.IsActionPressed(MegaInput.down)
                ? navigable[Math.Min(navigable.Count - 1, index + 1)]
                : navigable[Math.Max(0, index - 1)];
            AdoptExclusiveFocus(target, mouse: false);
            GetViewport().SetInputAsHandled();
        }

        internal bool FilterButtonInput(NMainMenuTextButton button, InputEvent inputEvent)
        {
            if (_layoutDirty)
                RefreshLayout();
            if (inputEvent is InputEventMouseButton
                {
                    Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown,
                }
                or InputEventPanGesture)
            {
                if (!IsActive || !ScrollEngaged)
                    return false;
                CancelPress();
                SetScroll(_scroll - ScrollHelper.GetDragForScrollEvent(inputEvent));
                GetViewport().SetInputAsHandled();
                return false;
            }

            if (inputEvent.IsActionPressed(MegaInput.up) || inputEvent.IsActionPressed(MegaInput.down) ||
                inputEvent.IsActionPressed(MegaInput.left) || inputEvent.IsActionPressed(MegaInput.right))
            {
                CancelPress();
                if (!IsActive)
                    return false;
                _keyboardNavigating = true;
                ClearHoverFocus();
                RebuildNavigation();
                return true;
            }

            var mouse = inputEvent as InputEventMouseButton;
            var isMouseClick = mouse?.ButtonIndex == MouseButton.Left;
            var isSelect = inputEvent.IsAction(MegaInput.select);
            if (!isMouseClick && !isSelect)
                return IsActive;

            var valid = IsActive && !button.IsQueuedForDeletion() && button.IsVisibleInTree() &&
                        button.IsEnabled && button.GetParent() == this;
            if (valid && isSelect && FollowsFocus)
                EnsureVisible(button);
            valid &= isMouseClick
                ? (!ScrollEngaged || GetGlobalRect().HasPoint(GetGlobalMousePosition())) &&
                  button.GetGlobalRect().HasPoint(GetGlobalMousePosition())
                : FollowsFocus && IsNavigable(button) && button.HasFocus() && IsRowFullyVisible(button);
            if (!valid)
            {
                CancelPress();
                GetViewport().SetInputAsHandled();
                return false;
            }

            if (inputEvent.IsPressed())
            {
                if (inputEvent.IsEcho() || _pressedButton != null)
                {
                    GetViewport().SetInputAsHandled();
                    return false;
                }

                _pressedButton = button;
                return true;
            }

            if (_pressedButton != button)
            {
                CancelPress(button);
                GetViewport().SetInputAsHandled();
                return false;
            }

            _pressedButton = null;
            return true;
        }

        private void CancelPress(NMainMenuTextButton? button = null)
        {
            if (button != null && button != _pressedButton)
            {
                if (IsInstanceValid(button))
                    Pressed(button) = false;
                return;
            }

            if (_pressedButton != null && IsInstanceValid(_pressedButton))
                Pressed(_pressedButton) = false;
            _pressedButton = null;
        }

        private bool IsRowFullyVisible(Control control)
        {
            return !ScrollEngaged ||
                   (control.Position.Y >= 0f && control.Position.Y + control.Size.Y <= Size.Y + 0.5f);
        }

        private void SyncHeightSteps(bool instant)
        {
            var next = 0;
            foreach (var item in _measured)
            {
                if (_scroll + 0.001f < _rowTops[item] + _measurements[item].Y * 0.5f)
                    break;
                next++;
            }

            var target = _scroll + 0.001f >= _scrollLimit
                ? _scrollLimit
                : Mathf.Min(ScrollPixelsForStep(next), _scrollLimit);
            if (instant)
            {
                _visualScrollTween?.Kill();
                _visualScrollTween = null;
                _heightStepIndex = next;
                _visualScroll = target;
                return;
            }

            if (Mathf.IsEqualApprox(_visualScroll, target) && next == _heightStepIndex)
                return;
            _heightStepIndex = next;
            _visualScrollTween?.Kill();
            _visualScrollTween = CreateTween();
            _visualScrollTween.TweenMethod(Callable.From<float>(value =>
                {
                    _visualScroll = value;
                    PositionItems();
                }), _visualScroll, target, HeightTweenDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        private float ScrollPixelsForStep(int step)
        {
            if (step <= 0 || _measured.Count == 0)
                return 0f;
            var last = _measured[Math.Min(step, _measured.Count) - 1];
            return _rowTops[last] + _measurements[last].Y;
        }
    }
}
