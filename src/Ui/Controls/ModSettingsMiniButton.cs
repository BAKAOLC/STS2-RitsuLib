using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A compact themed button for dense settings controls such as steppers and binding rows.</para>
    ///     <para xml:lang="zh-CN">供步进控件、绑定行等紧凑设置控件使用的主题化小型按钮。</para>
    /// </summary>
    public sealed partial class ModSettingsMiniButton : ModSettingsGamepadCompatibleButton
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a compact button and attaches its pressed action.</para>
        ///     <para xml:lang="zh-CN">创建紧凑按钮并连接其按下动作。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The action invoked when the button is pressed.</para>
        ///     <para xml:lang="zh-CN">按钮按下时调用的动作。</para>
        /// </param>
        public ModSettingsMiniButton(string text, Action action)
        {
            Text = text;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.MiniButton);
            AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelSecondary));
            AddThemeStyleboxOverride("normal", CreateStyle(false));
            AddThemeStyleboxOverride("hover", CreateStyle(true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true));
            AddThemeStyleboxOverride("focus", CreateFocusStyle());
            AddThemeStyleboxOverride("disabled", CreateStyle(false, true));
            ModSettingsUiControlTheming.EnableAdaptiveButtonText(
                this,
                10,
                RitsuShellTheme.Current.Metric.FontSize.MiniButton);
            Pressed += action;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured button for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的按钮。</para>
        /// </summary>
        public ModSettingsMiniButton()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared mini-button surface for the requested highlight and disabled state.</para>
        ///     <para xml:lang="zh-CN">获取指定高亮与禁用状态对应的共享小型按钮表面样式。</para>
        /// </summary>
        /// <param name="highlighted">
        ///     <para xml:lang="en">Whether to use the highlighted surface colors.</para>
        ///     <para xml:lang="zh-CN">是否使用高亮表面颜色。</para>
        /// </param>
        /// <param name="disabled">
        ///     <para xml:lang="en">Whether to use the disabled surface colors.</para>
        ///     <para xml:lang="zh-CN">是否使用禁用表面颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The cached style instance; callers must not mutate it.</para>
        ///     <para xml:lang="zh-CN">缓存的样式实例；调用方不得修改。</para>
        /// </returns>
        public static StyleBoxFlat CreateStyle(bool highlighted, bool disabled = false)
        {
            var key = disabled
                ? "settings.miniButton.disabled"
                : highlighted
                    ? "settings.miniButton.highlighted"
                    : "settings.miniButton";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildStyle(highlighted, disabled));
        }

        private static StyleBoxFlat BuildStyle(bool highlighted, bool disabled)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.stepper.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = disabled
                    ? RitsuShellTheme.Current.Component.Stepper.Neutral.Bg
                    : highlighted
                        ? RitsuShellTheme.Current.Component.Stepper.Hover.Bg
                        : RitsuShellTheme.Current.Component.Stepper.Default.Bg,
                BorderColor = disabled
                    ? RitsuShellTheme.Current.Component.Stepper.Neutral.Border
                    : highlighted
                        ? RitsuShellTheme.Current.Component.Stepper.Hover.Border
                        : RitsuShellTheme.Current.Component.Stepper.Default.Border,
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

        /// <summary>
        ///     <para xml:lang="en">Gets the shared highlighted surface used for a pressed mini button.</para>
        ///     <para xml:lang="zh-CN">获取小型按钮按下状态使用的共享高亮表面样式。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The cached highlighted style instance; callers must not mutate it.</para>
        ///     <para xml:lang="zh-CN">缓存的高亮样式实例；调用方不得修改。</para>
        /// </returns>
        public static StyleBoxFlat CreatePressedStyle()
        {
            return CreateStyle(true);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared focused mini-button surface.</para>
        ///     <para xml:lang="zh-CN">获取共享的小型按钮焦点表面样式。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The cached focus style instance; callers must not mutate it.</para>
        ///     <para xml:lang="zh-CN">缓存的焦点样式实例；调用方不得修改。</para>
        /// </returns>
        public static StyleBoxFlat CreateFocusStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.miniButton.focus", BuildFocusStyle);
        }

        private static StyleBoxFlat BuildFocusStyle()
        {
            var style = (StyleBoxFlat)CreateStyle(true).Duplicate();
            var border = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.stepper.layout.borderWidthFocus", 3);
            style.BorderWidthLeft = border.Left;
            style.BorderWidthTop = border.Top;
            style.BorderWidthRight = border.Right;
            style.BorderWidthBottom = border.Bottom;
            style.BorderColor = RitsuShellTheme.Current.Text.HoverHighlight;
            style.ShadowColor = new(style.BorderColor.R, style.BorderColor.G, style.BorderColor.B, 0.45f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.stepper.layout.focusShadowSize", 6);
            return style;
        }
    }
}
