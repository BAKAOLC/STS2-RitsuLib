using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A themed text action button with selectable visual tones and selected-state styling.</para>
    ///     <para xml:lang="zh-CN">支持视觉色调和选中状态样式的主题化文本动作按钮。</para>
    /// </summary>
    public partial class ModSettingsTextButton : ModSettingsGamepadCompatibleButton
    {
        private Action? _action;
        private bool _pressedHandlerAttached;
        private bool _selected;
        private string? _text;
        private ModSettingsButtonTone _tone;

        /// <summary>
        ///     <para xml:lang="en">Creates a themed text button with an optional pressed action.</para>
        ///     <para xml:lang="zh-CN">创建带可选按下动作的主题化文本按钮。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="tone">
        ///     <para xml:lang="en">The visual tone used for the button's foreground and surfaces.</para>
        ///     <para xml:lang="zh-CN">用于按钮前景和表面样式的视觉色调。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The optional action invoked when the button is pressed.</para>
        ///     <para xml:lang="zh-CN">按钮按下时调用的可选动作。</para>
        /// </param>
        public ModSettingsTextButton(string text, ModSettingsButtonTone tone, Action? action)
        {
            Configure(text, tone, action);
            EnsurePressedHandlerAttached();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured text button for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的文本按钮。</para>
        /// </summary>
        public ModSettingsTextButton()
        {
            EnsurePressedHandlerAttached();
        }

        internal void Configure(string text, ModSettingsButtonTone tone, Action? action)
        {
            _text = text;
            _tone = tone;
            _action = action;
            Text = text;
            Alignment = HorizontalAlignment.Center;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.textButton.layout.minSize",
                new(RitsuShellTheme.Current.Metric.Entry.ValueMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            AddThemeColorOverride("font_color", ResolveToneTextForeground(tone));
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(ResolveToneTextForeground(tone)));
            ModSettingsUiControlTheming.EnableAdaptiveButtonText(
                this,
                11,
                RitsuShellTheme.Current.Metric.FontSize.Button);
            ApplyVisualState();
        }

        internal void ClearAction()
        {
            _action = null;
            _selected = false;
            this.ReleaseFocusIfInsideTree();
            Disabled = false;
            ProcessMode = ProcessModeEnum.Inherit;
            Modulate = Colors.White;
            ApplyVisualState();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            Text = _text ?? string.Empty;
            Alignment = HorizontalAlignment.Center;
            ApplyVisualState();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the selected styling used by segmented groups and previews without invoking the pressed
        ///         action.
        ///     </para>
        ///     <para xml:lang="zh-CN">更新分段组和预览使用的选中样式，但不调用按下动作。</para>
        /// </summary>
        /// <param name="selected">
        ///     <para xml:lang="en">Whether the button should render as selected.</para>
        ///     <para xml:lang="zh-CN">按钮是否应呈现为选中状态。</para>
        /// </param>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        private void EnsurePressedHandlerAttached()
        {
            if (_pressedHandlerAttached)
                return;
            Pressed += InvokeAction;
            _pressedHandlerAttached = true;
        }

        private void InvokeAction()
        {
            _action?.Invoke();
        }

        private void ApplyVisualState()
        {
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _tone));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _tone));
            AddThemeStyleboxOverride("focus", CreateFocusStyle(_selected, _tone));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, false, _tone));
        }

        private static Color ResolveToneForeground(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };
        }

        private static Color ResolveToneTextForeground(ModSettingsButtonTone tone)
        {
            return ResolveToneForeground(tone).Lerp(RitsuShellTheme.Current.Text.LabelPrimary, 0.5f);
        }

        private static StyleBoxFlat CreateStyle(bool selected, bool hovered, ModSettingsButtonTone tone)
        {
            var key = $"settings.textButton.{tone}.{selected}.{hovered}";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildStyle(selected, hovered, tone));
        }

        private static StyleBoxFlat CreateFocusStyle(bool selected, ModSettingsButtonTone tone)
        {
            var key = $"settings.textButton.{tone}.{selected}.focus";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildFocusStyle(selected, tone));
        }

        private static StyleBoxFlat BuildFocusStyle(bool selected, ModSettingsButtonTone tone)
        {
            var style = (StyleBoxFlat)BuildStyle(selected, true, tone).Duplicate();
            var border = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.textButton.layout.borderWidthFocus", 3);
            var focusColor = ResolveToneForeground(tone);
            style.BorderColor = focusColor;
            style.BorderWidthLeft = border.Left;
            style.BorderWidthTop = border.Top;
            style.BorderWidthRight = border.Right;
            style.BorderWidthBottom = border.Bottom;
            style.ShadowColor = new(focusColor.R, focusColor.G, focusColor.B, 0.48f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.textButton.layout.focusShadowSize", 8);
            return style;
        }

        private static StyleBoxFlat BuildStyle(bool selected, bool hovered, ModSettingsButtonTone tone)
        {
            var borderColor = tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };

            var backgroundColor = tone switch
            {
                ModSettingsButtonTone.Accent => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Accent.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Accent.Bg,
                ModSettingsButtonTone.Danger => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Danger.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Danger.Bg,
                _ => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Neutral.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Neutral.Bg,
            };

            var shadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                hovered ? "components.textButton.layout.shadowSizeHover" : "components.textButton.layout.shadowSize",
                hovered ? 7 : 2);
            var shadowColor = hovered
                ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                : RitsuShellTheme.Current.Color.Shadow.Ambient;
            var normalBorder =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.textButton.layout.borderWidth", 1);
            var hoverBorder = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.textButton.layout.borderWidthHover",
                normalBorder.Left + 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.textButton.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.bottom", 8));
            var border = hovered ? hoverBorder : normalBorder;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.textButton.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);

            return new()
            {
                BgColor = backgroundColor,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = shadowColor,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
