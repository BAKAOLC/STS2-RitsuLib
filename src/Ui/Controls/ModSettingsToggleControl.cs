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
            Text = _isOn
                ? RitsuModuleLocalization.Get("toggle.on", "On")
                : RitsuModuleLocalization.Get("toggle.off", "Off");
            AddThemeStyleboxOverride("normal", CreateStyle(_isOn, false));
            AddThemeStyleboxOverride("hover", CreateStyle(_isOn, true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateFocusStyle(_isOn));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());
            ModSettingsUiControlTheming.RefreshAdaptiveButtonText(this);
        }

        private static StyleBoxFlat CreateFocusStyle(bool on)
        {
            var style = (StyleBoxFlat)CreateStyle(on, true).Duplicate();
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
                    : hovered
                        ? RitsuShellTheme.Current.Component.Toggle.OffHover.Bg
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
                ContentMarginRight = padding.Right,
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
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
