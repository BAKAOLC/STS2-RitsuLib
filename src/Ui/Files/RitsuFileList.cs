using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Files
{
    internal sealed partial class RitsuFileList : ItemList, IModSettingsDirectionalInputClaimant
    {
        private int _cursor;
        private int _direction;
        private double _repeat;
        private bool _grid;
        private int _touchIndex = -1;
        private Vector2 _touchStart;
        private Vector2 _touchLast;
        private bool _touchDragged;
        private int _touchItem = -1;
        private int _lastTapItem = -1;
        private ulong _lastTapTime;

        internal int Cursor => Math.Clamp(_cursor, 0, Math.Max(0, ItemCount - 1));
        internal event Action<int>? ControllerActivated;
        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => HasFocus();

        internal RitsuFileList()
        {
            FocusMode = FocusModeEnum.All;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AllowRmbSelect = true;
            AllowReselect = true;
            ItemSelected += index => _cursor = (int)index;
            MultiSelected += (index, _) => _cursor = (int)index;
            ItemClicked += (index, _, _) => _cursor = (int)index;
            FocusExited += () => _direction = 0;
        }

        public override void _Ready()
        {
            ApplyTheme();
        }

        internal void ApplyTheme()
        {
            var theme = RitsuShellTheme.Current;
            AddThemeFontOverride("font", theme.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", 14);
            AddThemeColorOverride("font_color", theme.Text.LabelPrimary);
            AddThemeColorOverride("font_selected_color", theme.Text.HoverHighlight);
            AddThemeColorOverride("font_hovered_color", theme.Text.HoverHighlight);
            AddThemeColorOverride("font_hovered_selected_color", theme.Text.HoverHighlight);
            AddThemeColorOverride("guide_color", theme.Color.Divider);
            AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());
            var focus = (StyleBoxFlat)RitsuShellChromeStyles.CreateEntryFieldFrameStyle(true).Duplicate();
            focus.DrawCenter = false;
            AddThemeStyleboxOverride("focus", focus);
            AddThemeStyleboxOverride("selected", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("selected_focus", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("hovered", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("hovered_selected", RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("hovered_selected_focus",
                RitsuShellChromeStyles.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("cursor", RitsuShellChromeStyles.CreateEntryFieldFrameStyle(true));
            AddThemeStyleboxOverride("cursor_unfocused", new StyleBoxEmpty());
            AddThemeConstantOverride("h_separation", 8);
            AddThemeConstantOverride("v_separation", 8);
            AddThemeConstantOverride("icon_margin", 8);
            ModSettingsUiControlTheming.ApplySettingsVerticalScrollBarTheme(GetVScrollBar());
        }

        internal void SetGrid(bool grid)
        {
            _grid = grid;
            IconMode = grid ? IconModeEnum.Top : IconModeEnum.Left;
            FixedIconSize = grid ? new(104, 104) : new(36, 36);
            FixedColumnWidth = grid ? 156 : 0;
            MaxColumns = grid ? 0 : 1;
            MaxTextLines = grid ? 2 : 1;
        }

        internal void ResetCursor()
        {
            _cursor = 0;
            _direction = 0;
            _touchIndex = -1;
            _lastTapItem = -1;
        }

        internal bool HandleTouchInput(InputEvent input)
        {
            if (!IsVisibleInTree())
                return false;
            if (input is InputEventScreenDrag drag && drag.Index == _touchIndex)
            {
                if (drag.Position.DistanceTo(_touchStart) > 12)
                    _touchDragged = true;
                if (_touchDragged)
                    GetVScrollBar().Value += _touchLast.Y - drag.Position.Y;
                _touchLast = drag.Position;
                return true;
            }

            if (input is not InputEventScreenTouch touch)
                return false;
            if (touch.Pressed)
            {
                if (!GetGlobalRect().HasPoint(touch.Position))
                    return false;
                if (_touchIndex >= 0)
                    return true;
                _touchIndex = touch.Index;
                _touchStart = _touchLast = touch.Position;
                _touchDragged = false;
                _touchItem = GetItemAtPosition(GetGlobalTransformWithCanvas().AffineInverse() * touch.Position, true);
                GrabFocus();
                return true;
            }

            if (touch.Index != _touchIndex)
                return false;
            _touchIndex = -1;
            if (touch.Canceled || _touchDragged || !GetGlobalRect().HasPoint(touch.Position) ||
                (uint)_touchItem >= ItemCount || IsItemDisabled(_touchItem))
            {
                _lastTapItem = -1;
                return true;
            }

            var index = _touchItem;
            var now = Time.GetTicksMsec();
            var activate = _lastTapItem == index && now - _lastTapTime < 450;
            _lastTapItem = activate ? -1 : index;
            _lastTapTime = now;
            _cursor = index;
            switch (SelectMode)
            {
                case SelectModeEnum.Multi when !activate:
                    var selected = !IsSelected(index);
                    if (selected)
                        Select(index, false);
                    else
                        Deselect(index);
                    EmitSignal(ItemList.SignalName.MultiSelected, index, selected);
                    break;
                case SelectModeEnum.Single:
                    Select(index);
                    EmitSignal(ItemList.SignalName.ItemSelected, index);
                    break;
            }

            if (activate && (uint)index < ItemCount)
                EmitSignal(ItemList.SignalName.ItemActivated, index);
            return true;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (HandleControllerInput(@event))
                AcceptEvent();
        }

        internal bool HandleControllerInput(InputEvent @event)
        {
            if (@event is not (InputEventJoypadButton or InputEventJoypadMotion))
                return false;
            if (@event.IsActionPressed(MegaInput.select) || @event.IsActionPressed(Sts2InputCompat.ConfirmAction))
            {
                if (ItemCount > 0)
                {
                    if (ControllerActivated != null)
                        ControllerActivated(Cursor);
                    else
                        EmitSignal(ItemList.SignalName.ItemActivated, Cursor);
                }

                return true;
            }

            var columns = _grid ? Math.Max(1, (int)((Size.X - 20) / 164)) : 1;
            var direction = @event.IsActionPressed(MegaInput.up) ? -columns :
                @event.IsActionPressed(MegaInput.down) ? columns :
                @event.IsActionPressed(MegaInput.left) ? -1 :
                @event.IsActionPressed(MegaInput.right) ? 1 : 0;
            if (direction == 0)
            {
                _direction = 0;
                return false;
            }

            MoveCursor(direction);
            _direction = direction;
            _repeat = 0.35;
            return true;
        }

        public override void _Process(double delta)
        {
            if (!HasFocus() || _direction == 0 ||
                !(Input.IsActionPressed(MegaInput.up) || Input.IsActionPressed(MegaInput.down) ||
                  Input.IsActionPressed(MegaInput.left) || Input.IsActionPressed(MegaInput.right)))
                return;
            _repeat -= delta;
            if (_repeat > 0)
                return;
            _repeat = 0.09;
            MoveCursor(_direction);
        }

        private void MoveCursor(int direction)
        {
            if (ItemCount == 0)
                return;
            SetItemCustomBgColor(Cursor, Colors.Transparent);
            _cursor = Math.Clamp(Cursor + direction, 0, ItemCount - 1);
            if (SelectMode == SelectModeEnum.Single)
            {
                Select(_cursor);
                EmitSignal(ItemList.SignalName.ItemSelected, _cursor);
            }
            else
                SetItemCustomBgColor(_cursor, RitsuShellTheme.Current.Component.ChromeMenu.Hover.Bg);

            var rect = GetItemRect(_cursor);
            var scroll = GetVScrollBar();
            if (rect.Position.Y < scroll.Value)
                scroll.Value = rect.Position.Y;
            else if (rect.End.Y > scroll.Value + Size.Y - 12)
                scroll.Value = rect.End.Y - Size.Y + 12;
        }
    }
}
