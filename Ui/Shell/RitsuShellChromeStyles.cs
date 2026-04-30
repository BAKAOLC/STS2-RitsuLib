using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     StyleBox factories for dense editor/list/toolbar chrome shared across mod settings and modal overlays.
    /// </summary>
    public static class RitsuShellChromeStyles
    {
        /// <summary>
        ///     Builds a rounded flat panel for generic content surfaces (background, border, soft shadow).
        /// </summary>
        public static StyleBoxFlat CreateSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.surface.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
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
        /// </summary>
        /// <param name="emphasized">When <see langword="true" />, uses a thicker border and stronger shadow.</param>
        public static StyleBoxFlat CreateEntryFieldFrameStyle(bool emphasized)
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.entryField.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
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
        /// </summary>
        public static StyleBoxFlat CreateColorPickerSwatchFrameStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.colorSwatch.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.colorSwatch.layout.shadowSize", 0),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Builds a recessed panel (inset background) for secondary content blocks.
        /// </summary>
        public static StyleBoxFlat CreateInsetSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.insetSurface.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Builds a compact menu row or popup action item (background and border from chrome tokens).
        /// </summary>
        /// <param name="highlighted">When <see langword="true" />, uses hover chrome colors.</param>
        public static StyleBoxFlat CreateChromeActionsMenuStyle(bool highlighted)
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.chromeMenu.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Builds the tray behind per-page toolbar controls (search, actions).
        /// </summary>
        public static StyleBoxFlat CreatePageToolbarTrayStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.pageToolbarTray.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Builds the outer container for scrollable list content (list shell with shadow).
        /// </summary>
        public static StyleBoxFlat CreateListShellStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.listShell.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
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
        /// </summary>
        /// <param name="accent">When <see langword="true" />, uses accent background and border tokens.</param>
        public static StyleBoxFlat CreateListItemCardStyle(bool accent = false)
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
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
        /// </summary>
        public static StyleBoxFlat CreateListEditorSurfaceStyle()
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.listEditor.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
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
        /// </summary>
        /// <param name="highlighted">When <see langword="true" />, uses hover background and border colors.</param>
        public static StyleBoxFlat CreatePillStyle(bool highlighted = false)
        {
            var t = RitsuShellTheme.Current;
            var r = RitsuShellThemeLayoutResolver.ResolveInt("components.pill.layout.cornerRadius",
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
                CornerRadiusTopLeft = r,
                CornerRadiusTopRight = r,
                CornerRadiusBottomRight = r,
                CornerRadiusBottomLeft = r,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
