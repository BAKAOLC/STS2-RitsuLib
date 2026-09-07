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
        private static readonly AccessTools.FieldRef<NClickableControl, bool> Pressed =
            AccessTools.FieldRefAccess<NClickableControl, bool>("_isPressed");

        private static readonly AccessTools.FieldRef<NScrollbar, bool> ScrollBarDragging =
            AccessTools.FieldRefAccess<NScrollbar, bool>("_isDragging");

        private NMainMenuTextButton? _lastFocused;
        private NMainMenuTextButton? _pressedButton;
        private int _lastFocusedIndex;
        private bool _wasActive;
        private bool _restorePending;
        private NControllerManager? _controllerManager;

        private static bool IsDirectional => Sts2InputCompat.IsUsingDirectionalNavigation;

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
            ScrollBarDragging(_scrollBar) = false;
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
            if (!IsActive || !IsDirectional || control.GetParent() != this)
                return;
            if (_layoutDirty)
                RefreshLayout();
            EnsureVisible(control);
        }

        private void OnButtonFocused(NClickableControl control)
        {
            if (control is not NMainMenuTextButton button || !IsActive)
                return;
            _lastFocused = button;
            _lastFocusedIndex = Math.Max(0, _buttons.IndexOf(button));
            if (_layoutDirty)
                RefreshLayout();
            if (IsDirectional)
                EnsureVisible(button);
            ShowReticles(button);
        }

        private void OnButtonUnfocused(NClickableControl control)
        {
            if (control is not NMainMenuTextButton button)
                return;
            CancelPress(button);
            HideReticles(button);
        }

        private void OnScreenChanged()
        {
            CancelPress();
            ScrollBarDragging(_scrollBar) = false;
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
        }

        private void OnControllerDetected()
        {
            CancelPress();
            ScrollBarDragging(_scrollBar) = false;
            _restorePending = true;
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
                     (IsDirectional && GetViewport().GuiGetFocusOwner() == null) ||
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
                if (current.GetParent() == this)
                {
                    EnsureVisible(current);
                    if (current is NMainMenuTextButton focusedButton && _reticleButton != focusedButton)
                        ShowReticles(focusedButton);
                }

                return;
            }

            if (GetDefaultFocus() is not { } target)
                return;
            EnsureVisible(target);
            target.GrabFocus();
            if (target is NMainMenuTextButton textButton)
                ShowReticles(textButton);
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (!IsActive || ScrollLimit <= 0f)
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

        internal bool FilterButtonInput(NMainMenuTextButton button, InputEvent inputEvent)
        {
            if (_layoutDirty)
                RefreshLayout();
            if (inputEvent.IsActionPressed(MegaInput.up) || inputEvent.IsActionPressed(MegaInput.down) ||
                inputEvent.IsActionPressed(MegaInput.left) || inputEvent.IsActionPressed(MegaInput.right))
            {
                CancelPress();
                if (!IsActive)
                    return false;
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
            if (valid && isSelect && IsDirectional)
                EnsureVisible(button);
            valid &= isMouseClick
                ? GetGlobalRect().HasPoint(GetGlobalMousePosition()) &&
                  button.GetGlobalRect().HasPoint(GetGlobalMousePosition())
                : IsDirectional && IsNavigable(button) && button.HasFocus() && IsRowFullyVisible(button);
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
            return control.Position.Y >= 0f && control.Position.Y + control.Size.Y <= Size.Y + 0.5f;
        }
    }
}
