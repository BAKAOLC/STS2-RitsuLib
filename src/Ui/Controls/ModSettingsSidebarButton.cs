using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A themed navigation button for mod, page, section, and utility rows in the settings sidebar.</para>
    ///     <para xml:lang="zh-CN">设置侧栏中模组、页面、节和工具行使用的主题化导航按钮。</para>
    /// </summary>
    public sealed partial class ModSettingsSidebarButton : ModSettingsGamepadCompatibleButton
    {
        private readonly int _indentLevel;
        private readonly ModSettingsSidebarItemKind _kind;
        private readonly string? _prefix;
        private readonly string? _rawText;
        private bool _selected;

        /// <summary>
        ///     <para xml:lang="en">Creates a sidebar row with role-specific styling and an optional pressed action.</para>
        ///     <para xml:lang="zh-CN">创建采用角色特定样式并可带按下动作的侧栏行。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The row label and tooltip text.</para>
        ///     <para xml:lang="zh-CN">行标签和工具提示文本。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The optional action invoked when the row is pressed.</para>
        ///     <para xml:lang="zh-CN">按下该行时调用的可选动作。</para>
        /// </param>
        /// <param name="kind">
        ///     <para xml:lang="en">The semantic role that selects the row's typography and surface styling.</para>
        ///     <para xml:lang="zh-CN">用于选择行字体和表面样式的语义角色。</para>
        /// </param>
        /// <param name="prefix">
        ///     <para xml:lang="en">Optional text displayed before the row label.</para>
        ///     <para xml:lang="zh-CN">显示在行标签之前的可选文本。</para>
        /// </param>
        /// <param name="indentLevel">
        ///     <para xml:lang="en">The non-negative visual nesting level; negative values are treated as zero.</para>
        ///     <para xml:lang="zh-CN">非负的视觉嵌套层级；负值按零处理。</para>
        /// </param>
        public ModSettingsSidebarButton(string text, Action? action,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            _rawText = text;
            _indentLevel = Math.Max(0, indentLevel);
            _kind = kind;
            _prefix = prefix;
            Text = text;
            TooltipText = text;
            var minHeight = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 62f,
                ModSettingsSidebarItemKind.Page => 48f,
                ModSettingsSidebarItemKind.Section => 38f,
                _ => 44f,
            };
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.sidebar.layout.button.minSize",
                new(0f, minHeight));
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            Alignment = HorizontalAlignment.Left;
            IconAlignment = HorizontalAlignment.Left;

            AddThemeFontOverride("font", kind == ModSettingsSidebarItemKind.ModGroup
                ? RitsuShellTheme.Current.Font.BodyBold
                : RitsuShellTheme.Current.Font.Body);
            AddThemeFontSizeOverride("font_size", kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 22,
                ModSettingsSidebarItemKind.Page => 19,
                ModSettingsSidebarItemKind.Section => 16,
                _ => 17,
            });
            AddThemeColorOverride("font_color", kind == ModSettingsSidebarItemKind.Section
                ? RitsuShellTheme.Current.Text.SidebarSection
                : RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelSecondary));

            AddThemeStyleboxOverride("normal", CreateStyle(false, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateFocusStyle(false, _kind, _indentLevel));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());

            Pressed += () =>
            {
                if (action == null)
                    return;
                action();
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured sidebar button for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的侧栏按钮。</para>
        /// </summary>
        public ModSettingsSidebarButton()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            Text = string.IsNullOrWhiteSpace(_prefix) ? _rawText ?? string.Empty : $"{_prefix}  {_rawText}";
            SetSelected(_selected);
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the selected surface styling without invoking the pressed action.</para>
        ///     <para xml:lang="zh-CN">更新选中表面样式，但不调用按下动作。</para>
        /// </summary>
        /// <param name="selected">
        ///     <para xml:lang="en">Whether the row should render as selected.</para>
        ///     <para xml:lang="zh-CN">该行是否应呈现为选中状态。</para>
        /// </param>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateFocusStyle(_selected, _kind, _indentLevel));
        }

        internal static StyleBoxFlat CreateStyle(bool selected, bool hovered,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            int indentLevel = 0)
        {
            var key = $"settings.sidebar.{kind}.{indentLevel}.{selected}.{hovered}";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildStyle(selected, hovered, kind, indentLevel));
        }

        internal static StyleBoxFlat CreateFocusStyle(bool selected,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            int indentLevel = 0)
        {
            var key = $"settings.sidebar.{kind}.{indentLevel}.{selected}.focus";
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildFocusStyle(selected, kind, indentLevel));
        }

        private static StyleBoxFlat BuildFocusStyle(bool selected,
            ModSettingsSidebarItemKind kind,
            int indentLevel)
        {
            var style = (StyleBoxFlat)BuildStyle(selected, true, kind, indentLevel).Duplicate();
            var focusWidth = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.sidebar.layout.button.borderWidth.focus", 3);
            style.BorderWidthLeft = Math.Max(style.BorderWidthLeft, focusWidth + 1);
            style.BorderWidthTop = Math.Max(style.BorderWidthTop, focusWidth);
            style.BorderWidthRight = Math.Max(style.BorderWidthRight, focusWidth);
            style.BorderWidthBottom = Math.Max(style.BorderWidthBottom, focusWidth);
            style.BorderColor = RitsuShellTheme.Current.Text.HoverHighlight;
            style.ShadowColor = new(style.BorderColor.R, style.BorderColor.G, style.BorderColor.B, 0.48f);
            style.ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt(
                "components.sidebar.layout.button.focusShadowSize", 7);
            return style;
        }

        private static StyleBoxFlat BuildStyle(bool selected, bool hovered,
            ModSettingsSidebarItemKind kind,
            int indentLevel)
        {
            var bg = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.SelectedHover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Selected.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.Mod.Bg,
                ModSettingsSidebarItemKind.Section => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.Hover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Default.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.IdleDeep.Bg,
                ModSettingsSidebarItemKind.Utility => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.UtilitySelected.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.IdleDeepHover.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.IdleDeep.Bg,
                _ => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.ModHover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Mod.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.ModDeep.Bg,
            };

            var borderColor = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.SelectedHover.Border
                    : RitsuShellTheme.Current.Component.SidebarBtn.Selected.Border,
                ModSettingsSidebarItemKind.Section => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.Hover.Border
                    : RitsuShellTheme.Current.Component.SidebarBtn.Default.Border,
                _ => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.DeepBorderHover
                    : RitsuShellTheme.Current.Component.SidebarBtn.DeepBorder,
            };

            var leftBorder = selected
                ? kind == ModSettingsSidebarItemKind.Section ? 3 : 4
                : kind == ModSettingsSidebarItemKind.ModGroup
                    ? 2
                    : 1;
            var borderWidths =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.button.borderWidth", 1);
            borderWidths = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.left",
                    leftBorder),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.top",
                    borderWidths.Top),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.right",
                    borderWidths.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.bottom",
                    borderWidths.Bottom));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.sidebar.layout.button.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var shadowSize = kind == ModSettingsSidebarItemKind.ModGroup ? 4 : 2;
            shadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.shadowSize",
                shadowSize);
            var fallbackTopBottom = kind == ModSettingsSidebarItemKind.Section ? 8 : 10;
            var fallbackRight = kind == ModSettingsSidebarItemKind.Section ? 14 : 18;
            var fallbackLeft = (kind == ModSettingsSidebarItemKind.Section ? 14 : 18) + indentLevel * 14;
            BoxEdges padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.left", fallbackLeft),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.top",
                    fallbackTopBottom),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.right",
                    fallbackRight),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.bottom",
                    fallbackTopBottom));

            return new()
            {
                BgColor = bg,
                BorderColor = borderColor,
                BorderWidthLeft = borderWidths.Left,
                BorderWidthTop = borderWidths.Top,
                BorderWidthRight = borderWidths.Right,
                BorderWidthBottom = borderWidths.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = RitsuShellTheme.Current.Component.SidebarBtn.Shadow,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        internal static StyleBoxFlat CreateDisabledStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.sidebar.disabled", BuildDisabledStyle);
        }

        private static StyleBoxFlat BuildDisabledStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.disabled.borderWidth", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.sidebar.layout.disabled.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.disabled.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.bottom", 8));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.SidebarRail.Bg,
                BorderColor = RitsuShellTheme.Current.Component.SidebarRail.Border,
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
