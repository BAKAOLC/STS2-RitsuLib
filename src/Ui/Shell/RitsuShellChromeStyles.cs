using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides style-box factories for compact editors, lists, and toolbars shared by mod settings
    ///         and modal overlays.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供由模组设置界面和模态浮层共享的紧凑编辑器、列表及工具栏样式框工厂。
    ///     </para>
    /// </summary>
    public static class RitsuShellChromeStyles
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a rounded flat panel with a background, border, and soft shadow for general content.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为通用内容创建带背景、边框和柔和阴影的圆角平面面板。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateSurfaceStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.surface", BuildSurfaceStyle);
        }

        private static StyleBoxFlat BuildSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.surface.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.surface.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.surface.layout.padding", 12);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Surface.Entry.Bg,
                BorderColor = t.Surface.Entry.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = t.Surface.Entry.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.shadowSize", 2),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a frame for an entry or form field, optionally with a stronger border and shadow.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为条目或表单字段创建边框，可选择使用更醒目的边框和阴影。
        ///     </para>
        /// </summary>
        /// <param name="emphasized">
        ///     <para xml:lang="en">
        ///         <see langword="true" /> to use a thicker border and stronger shadow.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若要使用更粗的边框和更强的阴影，则为 <see langword="true" />。
        ///     </para>
        /// </param>
        public static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
        {
            return RitsuShellStyleCache.GetOrBuild(emphasized ? "chrome.entryField.emph" : "chrome.entryField",
                () => BuildEntryFieldFrameStyle(emphasized));
        }

        private static StyleBoxFlat BuildEntryFieldFrameStyle(bool emphasized)
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.entryField.layout.cornerRadius",
                t.Metric.Radius.Default);
            var borderColor = t.Surface.Entry.Border;
            var borderW = emphasized ? t.Metric.BorderWidth.Normal : t.Metric.BorderWidth.Thin;
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.entryField.layout.borderWidth", borderW);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.entryField.layout.padding", 12);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Surface.Entry.Bg,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = emphasized
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : t.Surface.Entry.Shadow,
                ShadowSize = emphasized
                    ? RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.shadowSizeHover", 7)
                    : RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.shadowSize", 2),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a compact inset frame around a color-swatch preview.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为颜色样本预览创建紧凑的内嵌边框。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.colorSwatch", BuildColorPickerSwatchFrameStyle);
        }

        private static StyleBoxFlat BuildColorPickerSwatchFrameStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.colorSwatch.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.colorSwatch.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.colorSwatch.layout.padding", 5);
            return new()
            {
                BgColor = t.Surface.Entry.Bg,
                BorderColor = t.Surface.Entry.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.colorSwatch.layout.shadowSize", 0),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a recessed panel for secondary content.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为次级内容创建凹陷面板。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.inset", BuildInsetSurfaceStyle);
        }

        private static StyleBoxFlat BuildInsetSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.insetSurface.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.insetSurface.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.insetSurface.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.insetSurface.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.insetSurface.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.insetSurface.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.insetSurface.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Surface.Inset.Bg,
                BorderColor = t.Surface.Inset.Border,
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
        ///     <para xml:lang="en">
        ///         Creates a compact menu row or pop-up action item using the menu background and border tokens.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用菜单的背景和边框令牌创建紧凑的菜单行或弹出式操作项。
        ///     </para>
        /// </summary>
        /// <param name="highlighted">
        ///     <para xml:lang="en">
        ///         <see langword="true" /> to use the hover-state colors.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若要使用悬停状态的颜色，则为 <see langword="true" />。
        ///     </para>
        /// </param>
        public static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
        {
            return RitsuShellStyleCache.GetOrBuild(highlighted ? "chrome.actionsMenu.hl" : "chrome.actionsMenu",
                () => BuildChromeActionsMenuStyle(highlighted));
        }

        private static StyleBoxFlat BuildChromeActionsMenuStyle(bool highlighted)
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.chromeMenu.layout.cornerRadius",
                t.Metric.Radius.Default);
            var state = highlighted ? t.Component.ChromeMenu.Hover : t.Component.ChromeMenu.Default;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.chromeMenu.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.chromeMenu.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.chromeMenu.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.chromeMenu.layout.padding.top", 6),
                RitsuShellThemeLayoutResolver.ResolveInt("components.chromeMenu.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.chromeMenu.layout.padding.bottom", 6));
            return new()
            {
                BgColor = state.Bg,
                BorderColor = state.Border,
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
        ///     <para xml:lang="en">
        ///         Creates the tray behind page-level toolbar controls, such as search and action controls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建页面级工具栏控件（如搜索及操作控件）后方的托盘。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreatePageToolbarTrayStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.toolbarTray", BuildPageToolbarTrayStyle);
        }

        private static StyleBoxFlat BuildPageToolbarTrayStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.pageToolbarTray.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.pageToolbarTray.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.pageToolbarTray.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbarTray.layout.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbarTray.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbarTray.layout.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbarTray.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Component.PageToolbarTray.Bg,
                BorderColor = t.Component.PageToolbarTray.Border,
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
        ///     <para xml:lang="en">
        ///         Creates the outer container for scrollable list content.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为可滚动的列表内容创建外层容器。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateListShellStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.listShell", BuildListShellStyle);
        }

        private static StyleBoxFlat BuildListShellStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.listShell.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.listShell.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.listShell.layout.padding", 12);
            return new()
            {
                BgColor = t.Component.ListShell.Bg,
                BorderColor = t.Component.ListShell.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = t.Component.ListShell.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.listShell.layout.shadowSize", 3),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a card row within a list, optionally using accent styling for selection or emphasis.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在列表中创建卡片行，可选择使用强调样式表示选中或突出状态。
        ///     </para>
        /// </summary>
        /// <param name="accent">
        ///     <para xml:lang="en">
        ///         <see langword="true" /> to use the accent background and border tokens.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若要使用强调背景及边框令牌，则为 <see langword="true" />。
        ///     </para>
        /// </param>
        public static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            return RitsuShellStyleCache.GetOrBuild(accent ? "chrome.listItem.accent" : "chrome.listItem",
                () => BuildListItemCardStyle(accent));
        }

        private static StyleBoxFlat BuildListItemCardStyle(bool accent)
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.listItem.layout.cornerRadius",
                t.Metric.Radius.Default);
            var state = accent ? t.Component.ListItem.Accent : t.Component.ListItem.Default;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.listItem.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.listItem.layout.padding", 10);
            return new()
            {
                BgColor = state.Bg,
                BorderColor = state.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = t.Component.ListItem.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.shadowSize", 2),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the inner editor panel used to edit list entries inline, such as path or text entries.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建用于内联编辑列表项的内部编辑器面板，例如路径或文本条目。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.listEditor", BuildListEditorSurfaceStyle);
        }

        private static StyleBoxFlat BuildListEditorSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.listEditor.layout.cornerRadius",
                t.Metric.Radius.Default);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.listEditor.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.listEditor.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Component.ListEditor.Bg,
                BorderColor = t.Component.ListEditor.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = t.Component.ListItem.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.shadowSize", 2),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a pill-shaped control for tags or compact buttons, optionally using hover emphasis.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为标签或紧凑按钮创建胶囊形控件，可选择使用悬停强调样式。
        ///     </para>
        /// </summary>
        /// <param name="highlighted">
        ///     <para xml:lang="en">
        ///         <see langword="true" /> to use the hover background and border colors.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若要使用悬停状态的背景及边框颜色，则为 <see langword="true" />。
        ///     </para>
        /// </param>
        public static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            return RitsuShellStyleCache.GetOrBuild(highlighted ? "chrome.pill.hl" : "chrome.pill",
                () => BuildPillStyle(highlighted));
        }

        private static StyleBoxFlat BuildPillStyle(bool highlighted)
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.pill.layout.cornerRadius",
                t.Metric.Radius.Default);
            var state = highlighted ? t.Component.Pill.Hover : t.Component.Pill.Default;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.pill.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.pill.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.pill.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pill.layout.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pill.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.pill.layout.padding.bottom", 5));
            return new()
            {
                BgColor = state.Bg,
                BorderColor = state.Border,
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
        ///     <para xml:lang="en">
        ///         Creates a compact panel for Godot <c>TooltipPanel</c> pop-ups produced from
        ///         <see cref="Control.TooltipText" />, styled consistently with entry controls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see cref="Control.TooltipText" /> 生成的 Godot <c>TooltipPanel</c> 弹出框创建紧凑面板，
        ///         并使其样式与条目控件保持一致。
        ///     </para>
        /// </summary>
        public static StyleBoxFlat CreateTooltipPanelStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("chrome.tooltip", BuildTooltipPanelStyle);
        }

        private static StyleBoxFlat BuildTooltipPanelStyle()
        {
            var t = RitsuShellTheme.Current;
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.tooltip.layout.cornerRadius", 0);
            var borderWidth =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.tooltip.layout.borderWidth",
                    t.Metric.BorderWidth.Thin);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.tooltip.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.tooltip.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.tooltip.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.tooltip.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.tooltip.layout.padding.bottom", 8));
            return new()
            {
                BgColor = t.Surface.Entry.Bg,
                BorderColor = t.Surface.Entry.Border,
                BorderWidthLeft = borderWidth.Left,
                BorderWidthTop = borderWidth.Top,
                BorderWidthRight = borderWidth.Right,
                BorderWidthBottom = borderWidth.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = t.Surface.Entry.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.tooltip.layout.shadowSize", 4),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
