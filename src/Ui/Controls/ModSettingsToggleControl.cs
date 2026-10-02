using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A themed on/off toggle used by settings entries.</para>
    ///     <para xml:lang="zh-CN">设置条目使用的主题开关控件。</para>
    /// </summary>
    public sealed partial class ModSettingsToggleControl : ModSettingsGamepadCompatibleButton
    {
        private bool _initialValue;
        private bool _isOn;
        private bool _switchOnly;
        private bool _applyingVisualState;
        private Action<bool>? _onChanged;

        /// <summary>
        ///     <para xml:lang="en">Creates a toggle with an initial value and an optional user-change callback.</para>
        ///     <para xml:lang="zh-CN">创建带初始值和可选用户变更回调的开关控件。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">Whether the toggle initially displays the on state.</para>
        ///     <para xml:lang="zh-CN">开关初始是否显示为开启状态。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The optional callback invoked after the user toggles the value.</para>
        ///     <para xml:lang="zh-CN">用户切换值后调用的可选回调。</para>
        /// </param>
        public ModSettingsToggleControl(bool initialValue, Action<bool>? onChanged)
        {
            _initialValue = initialValue;
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.toggle.layout.entry.minSize",
                new(RitsuShellTheme.Current.Metric.Entry.ValueMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            Alignment = HorizontalAlignment.Center;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelSecondary));
            ModSettingsUiControlTheming.EnableAdaptiveButtonText(
                this,
                11,
                RitsuShellTheme.Current.Metric.FontSize.Button);
            Pressed += ToggleValue;
            Resized += RefreshLayout;
            MouseEntered += QueueRedraw;
            MouseExited += QueueRedraw;
            FocusEntered += QueueRedraw;
            FocusExited += QueueRedraw;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured toggle for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的开关控件。</para>
        /// </summary>
        public ModSettingsToggleControl()
        {
        }

        internal void BindValue(bool value, Action<bool>? onChanged)
        {
            _initialValue = value;
            _onChanged = onChanged;
            SetValue(value);
        }

        internal void ClearBinding()
        {
            _onChanged = null;
            this.ReleaseFocusIfInsideTree();
            Disabled = false;
            ProcessMode = ProcessModeEnum.Inherit;
            Modulate = Colors.White;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the configured initial value when the control becomes ready.</para>
        ///     <para xml:lang="zh-CN">控件就绪时应用已配置的初始值。</para>
        /// </summary>
        public override void _Ready()
        {
            _isOn = _initialValue;
            ApplyVisualState();
        }

        /// <inheritdoc />
        public override void _Draw()
        {
            var theme = RitsuShellTheme.Current;
            var foregroundFallback = Disabled
                ? ModSettingsUiControlTheming.ResolveDisabledForeground(theme.Text.LabelPrimary)
                : theme.Text.LabelPrimary;
            var state = Disabled ? "disabled" : _isOn ? "on" : "off";
            var foreground = ResolveSwitchColor($"thumb.{state}.bg", foregroundFallback);
            var trackColorFallback = Disabled
                ? theme.Component.Toggle.Disabled.Border
                : _isOn
                    ? theme.Component.Toggle.On.Border
                    : theme.Component.Toggle.Off.Border;
            var trackColor = ResolveSwitchColor($"track.{state}.bg", trackColorFallback);
            var switchSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.toggle.switch.layout.size", new(52f, 26f));
            switchSize = new(Mathf.Max(switchSize.X, 2f), Mathf.Max(switchSize.Y, 2f));
            var inset = Mathf.Clamp(RitsuShellThemeLayoutResolver.ResolveFloat(
                "components.toggle.switch.layout.inset", 3f), 0f, (Mathf.Min(switchSize.X, switchSize.Y) - 1f) * 0.5f);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            var rightPadding = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.toggle.layout.padding.right", padding.Right);
            var trackRect = new Rect2(
                _switchOnly ? (Size.X - switchSize.X) * 0.5f : Size.X - rightPadding - switchSize.X,
                (Size.Y - switchSize.Y) * 0.5f, switchSize.X, switchSize.Y);
            var trackRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.toggle.switch.track.layout.cornerRadius", 0);
            var trackBorder = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.toggle.switch.track.layout.borderWidth", 1);
            var track = new StyleBoxFlat
            {
                BgColor = trackColor,
                BorderColor = _switchOnly && !Disabled && (IsHovered() || HasFocus())
                    ? theme.Text.HoverHighlight
                    : ResolveSwitchColor($"track.{state}.border", theme.Text.LabelSecondary),
                BorderWidthLeft = trackBorder.Left,
                BorderWidthTop = trackBorder.Top,
                BorderWidthRight = trackBorder.Right,
                BorderWidthBottom = trackBorder.Bottom,
                CornerRadiusTopLeft = trackRadii.TopLeft,
                CornerRadiusTopRight = trackRadii.TopRight,
                CornerRadiusBottomLeft = trackRadii.BottomLeft,
                CornerRadiusBottomRight = trackRadii.BottomRight,
            };
            track.Draw(GetCanvasItem(), trackRect);
            var thumbSize = Mathf.Min(switchSize.X, switchSize.Y) - inset * 2f;
            var thumbRect = new Rect2(
                _isOn ? trackRect.End.X - inset - thumbSize : trackRect.Position.X + inset,
                trackRect.Position.Y + (switchSize.Y - thumbSize) * 0.5f, thumbSize, thumbSize);
            var thumbRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.toggle.switch.thumb.layout.cornerRadius", 0);
            var thumbBorder = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.toggle.switch.thumb.layout.borderWidth", 1);
            var thumb = new StyleBoxFlat
            {
                BgColor = foreground,
                BorderColor = ResolveSwitchColor($"thumb.{state}.border", theme.Component.Toggle.Off.Bg),
                BorderWidthLeft = thumbBorder.Left,
                BorderWidthTop = thumbBorder.Top,
                BorderWidthRight = thumbBorder.Right,
                BorderWidthBottom = thumbBorder.Bottom,
                CornerRadiusTopLeft = thumbRadii.TopLeft,
                CornerRadiusTopRight = thumbRadii.TopRight,
                CornerRadiusBottomLeft = thumbRadii.BottomLeft,
                CornerRadiusBottomRight = thumbRadii.BottomRight,
            };
            thumb.Draw(GetCanvasItem(), thumbRect);
        }

        private static Color ResolveSwitchColor(string path, Color fallback)
        {
            return RitsuShellTheme.Current.TryGetColor($"components.toggle.switch.{path}", out var color)
                ? color
                : fallback;
        }

        private static float ResolveSwitchContentWidth()
        {
            var size = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.toggle.switch.layout.size", new(52f, 26f));
            var gap = RitsuShellThemeLayoutResolver.ResolveFloat("components.toggle.switch.layout.textGap", 12f);
            return Mathf.Max(size.X, 2f) + Mathf.Max(gap, 0f);
        }

        private void RefreshLayout()
        {
            if (_applyingVisualState)
                return;

            if (_switchOnly != ShouldUseSwitchOnly())
                ApplyVisualState();
            else
                QueueRedraw();
        }

        private bool ShouldUseSwitchOnly()
        {
            var width = SizeFlagsHorizontal == SizeFlags.ShrinkEnd && CustomMinimumSize.X > 0f
                ? CustomMinimumSize.X
                : Size.X;
            if (width <= 0f)
                return false;

            var font = GetThemeFont("font");
            var fontSize = RitsuShellTheme.Current.Metric.FontSize.Button;
            var onWidth = font.GetStringSize(RitsuModuleLocalization.Get("toggle.on", "On"),
                HorizontalAlignment.Left, -1f, fontSize).X;
            var offWidth = font.GetStringSize(RitsuModuleLocalization.Get("toggle.off", "Off"),
                HorizontalAlignment.Left, -1f, fontSize).X;
            var style = CreateStyle(_isOn, false);
            return width < Mathf.Max(onWidth, offWidth) + style.ContentMarginLeft + style.ContentMarginRight;
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the displayed value without invoking the user-change callback.</para>
        ///     <para xml:lang="zh-CN">更新显示值而不调用用户变更回调。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to display.</para>
        ///     <para xml:lang="zh-CN">要显示的值。</para>
        /// </param>
        public void SetValue(bool value)
        {
            _isOn = value;
            ApplyVisualState();
        }

        private void ToggleValue()
        {
            _isOn = !_isOn;
            ApplyVisualState();
            InvokeOnChanged(_isOn);
        }

        private void InvokeOnChanged(bool value)
        {
            _onChanged?.Invoke(value);
        }

        private void ApplyVisualState()
        {
            if (_applyingVisualState)
                return;

            _applyingVisualState = true;
            BeginBulkThemeOverride();
            try
            {
                _switchOnly = ShouldUseSwitchOnly();
                var stateText = _isOn
                    ? RitsuModuleLocalization.Get("toggle.on", "On")
                    : RitsuModuleLocalization.Get("toggle.off", "Off");
                Text = _switchOnly ? string.Empty : stateText;
                TooltipText = _switchOnly ? stateText : string.Empty;
                if (_switchOnly)
                {
                    var empty = new StyleBoxEmpty();
                    AddThemeStyleboxOverride("normal", empty);
                    AddThemeStyleboxOverride("hover", empty);
                    AddThemeStyleboxOverride("pressed", empty);
                    AddThemeStyleboxOverride("focus", empty);
                    AddThemeStyleboxOverride("disabled", empty);
                }
                else
                {
                    AddThemeStyleboxOverride("normal", CreateStyle(_isOn, false));
                    AddThemeStyleboxOverride("hover", CreateStyle(_isOn, true));
                    AddThemeStyleboxOverride("pressed", CreateStyle(_isOn, true));
                    AddThemeStyleboxOverride("focus", CreateFocusStyle(_isOn));
                    AddThemeStyleboxOverride("disabled", CreateDisabledStyle());
                }

                ModSettingsUiControlTheming.RefreshAdaptiveButtonText(this);
                QueueRedraw();
            }
            finally
            {
                EndBulkThemeOverride();
                _applyingVisualState = false;
            }
        }

        private static StyleBoxFlat CreateFocusStyle(bool on)
        {
            var style = (StyleBoxFlat)CreateStyle(on, true).Duplicate();
            style.DrawCenter = false;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthFocus", 4);
            var focusColor = on
                ? RitsuShellTheme.Current.Component.Toggle.On.Border
                : RitsuShellTheme.Current.Text.HoverHighlight;
            style.BorderColor = focusColor;
            style.BorderWidthLeft = border.Left;
            style.BorderWidthTop = border.Top;
            style.BorderWidthRight = border.Right;
            style.BorderWidthBottom = border.Bottom;
            style.ShadowColor = new(focusColor.R, focusColor.G, focusColor.B, 0.48f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.toggle.layout.shadowSizeFocus", 8);
            return style;
        }

        private static StyleBoxFlat CreateStyle(bool on, bool hovered)
        {
            var borderColor = on
                ? RitsuShellTheme.Current.Component.Toggle.On.Border
                : RitsuShellTheme.Current.Component.Toggle.Off.Border;
            var normalBorder = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidth", 2);
            var hoverBorder =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthHover", 3);
            var border = hovered ? hoverBorder : normalBorder;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.toggle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.bottom", 8));
            var shadowSize = hovered
                ? RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSizeHover", 7)
                : RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSize", 2);
            return new()
            {
                BgColor = on
                    ? RitsuShellTheme.Current.Component.Toggle.On.Bg
                    : RitsuShellTheme.Current.Component.Toggle.Off.Bg,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = hovered
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : RitsuShellTheme.Current.Component.Toggle.Shadow,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right + ResolveSwitchContentWidth(),
                ContentMarginBottom = padding.Bottom,
            };
        }

        internal static StyleBoxFlat CreateDisabledStyle()
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthDisabled", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.toggle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.bottom", 8));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Toggle.Disabled.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Toggle.Disabled.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right + ResolveSwitchContentWidth(),
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
