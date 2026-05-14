using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     StyleBox factories for dense editor/list/toolbar chrome shared across mod settings and modal overlays.
    ///     StyleBox 工厂，用于 mod 设置和模态覆盖层共享的密集编辑器 / 列表 / 工具栏 chrome。
    /// </summary>
    public static class RitsuShellChromeStyles
    {
        /// <summary>
        ///     Builds a rounded flat panel for generic content surfaces (background, border, soft shadow).
        ///     为通用内容表面构建圆角平面面板（背景、边框、柔和阴影）。
        /// </summary>
        public static StyleBoxFlat CreateSurfaceStyle()
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
        ///     Builds a frame around an entry or form field, optionally with a stronger border and shadow.
        ///     围绕条目或表单字段构建边框，可选使用更强的边框和阴影。
        /// </summary>
        /// <param name="emphasized">
        ///     When <see langword="true" />, uses a thicker border and stronger shadow.
        ///     当 <see langword="true" /> 时，使用更粗的边框和更强的阴影。
        /// </param>
        public static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
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
        ///     Builds a tight inset frame around a color swatch preview.
        ///     构建 a 紧凑内嵌边框 around a 颜色样本预览。
        /// </summary>
        public static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
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
        ///     Builds a recessed panel (inset background) for secondary content blocks.
        ///     为次级内容块构建凹陷面板（内嵌背景）。
        /// </summary>
        public static StyleBoxFlat CreateInsetSurfaceStyle()
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
        ///     Builds a compact menu row or popup action item (background and border from chrome tokens).
        ///     构建紧凑菜单行或弹出动作项（背景和边框来自 chrome 令牌）。
        /// </summary>
        /// <param name="highlighted">
        ///     When <see langword="true" />, uses hover chrome colors.
        ///     当 <see langword="true" />, 使用 悬停 chrome 颜色。
        /// </param>
        public static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
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
        ///     Builds the tray behind per-page toolbar controls (search, actions).
        ///     构建每页工具栏控件（搜索、动作）后方的托盘。
        ///     构建每页工具栏控件（搜索、动作）后方的托盘。
        /// </summary>
        public static StyleBoxFlat CreatePageToolbarTrayStyle()
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
        ///     Builds the outer container for scrollable list content (list shell with shadow).
        ///     为可滚动列表内容构建外层容器（带阴影的列表外壳）。
        /// </summary>
        public static StyleBoxFlat CreateListShellStyle()
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
        ///     Builds a card row inside a list (optional accent styling for selection or emphasis).
        ///     在列表内构建卡片行（可选为选中或强调使用 accent 样式）。
        /// </summary>
        /// <param name="accent">
        ///     When <see langword="true" />, uses accent background and border tokens.
        ///     当 <see langword="true" />, 使用 强调背景和边框 token。
        /// </param>
        public static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
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
        ///     Builds the inner editor surface for inline list editing (e.g. path or text rows).
        ///     为内联列表编辑构建内部编辑器表面（例如路径或文本行）。
        /// </summary>
        public static StyleBoxFlat CreateListEditorSurfaceStyle()
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
        ///     Builds a pill-shaped control (tags, compact buttons) with optional hover emphasis.
        ///     构建 a 胶囊形控件 (标签, 紧凑按钮) 与 可选 悬停强调。
        /// </summary>
        /// <param name="highlighted">
        ///     When <see langword="true" />, uses hover background and border colors.
        ///     当 <see langword="true" /> 时，使用悬停背景和边框颜色。
        /// </param>
        public static StyleBoxFlat CreatePillStyle(bool highlighted = false)
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
        ///     Compact panel backing Godot TooltipPanel (<see cref="Control.TooltipText" /> popups), aligned with entry
        ///     chrome tokens.
        ///     Godot TooltipPanel（<see cref="Control.TooltipText" /> 弹窗）的紧凑面板底板，与条目
        ///     chrome 令牌对齐。
        /// </summary>
        public static StyleBoxFlat CreateTooltipPanelStyle()
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
