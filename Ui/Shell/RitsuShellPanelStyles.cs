using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Shared <see cref="StyleBoxFlat" /> factories for framed panels and side cards. Use for mod settings, run-time
    ///     overlays, and any in-game surface that should match the Ritsu shell look.
    /// </summary>
    public static class RitsuShellPanelStyles
    {
        /// <summary>
        ///     Primary framed panel (large panes, content wells).
        /// </summary>
        /// <param name="background">Background fill color.</param>
        /// <param name="cornerRadius">Corner radius.</param>
        /// <returns>A new framed surface stylebox.</returns>
        public static StyleBoxFlat CreateFramedSurface(Color background, int cornerRadius)
        {
            var t = RitsuShellTheme.Current;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.framedSurface.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.framedSurface.layout.padding", 0);
            var radius = RitsuShellThemeLayoutResolver.ResolveInt("components.framedSurface.layout.cornerRadius",
                cornerRadius);
            return new()
            {
                BgColor = background,
                BorderColor = t.Surface.Framed.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = radius,
                CornerRadiusTopRight = radius,
                CornerRadiusBottomRight = radius,
                CornerRadiusBottomLeft = radius,
                ShadowColor = t.Surface.Framed.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.framedSurface.layout.shadowSize", 12),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Nested card inside a sidebar (mod group chrome).
        /// </summary>
        /// <param name="cornerRadius">Corner radius.</param>
        /// <param name="selected">Whether the card is in its selected state.</param>
        /// <returns>A new sidebar mod card stylebox.</returns>
        public static StyleBoxFlat CreateSidebarModCard(int cornerRadius, bool selected)
        {
            var t = RitsuShellTheme.Current;
            var state = selected ? t.Component.SidebarCard.Selected : t.Component.SidebarCard.Default;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebarCard.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebarCard.layout.padding", 10);
            var radius = RitsuShellThemeLayoutResolver.ResolveInt("components.sidebarCard.layout.cornerRadius",
                cornerRadius);
            return new()
            {
                BgColor = state.Bg,
                BorderColor = state.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = radius,
                CornerRadiusTopRight = radius,
                CornerRadiusBottomRight = radius,
                CornerRadiusBottomLeft = radius,
                ShadowColor = t.Component.SidebarCard.Shadow,
                ShadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.sidebarCard.layout.shadowSize", 4),
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Same as <see cref="CreateSidebarModCard" /> but with tighter inner padding (compact nav).
        /// </summary>
        /// <param name="cornerRadius">Corner radius.</param>
        /// <param name="selected">Whether the card is in its selected state.</param>
        /// <param name="innerMargin">Inner padding override applied to all four edges.</param>
        /// <returns>A new compact sidebar mod card stylebox.</returns>
        public static StyleBoxFlat CreateSidebarModCardCompact(int cornerRadius, bool selected, int innerMargin = 6)
        {
            var b = CreateSidebarModCard(cornerRadius, selected);
            b.ContentMarginLeft = innerMargin;
            b.ContentMarginTop = innerMargin;
            b.ContentMarginRight = innerMargin;
            b.ContentMarginBottom = innerMargin;
            return b;
        }
    }
}
