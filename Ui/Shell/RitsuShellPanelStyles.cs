using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Shared <see cref="StyleBoxFlat" /> factories for framed panels and side cards. Use for mod settings, run-time
    ///     overlays, and any in-game surface that should match the Ritsu shell look.
    ///     共享 <see cref="StyleBoxFlat" /> 工厂，用于带框面板和侧边卡片。用于 mod 设置、运行时
    ///     覆盖层，以及任何应匹配 Ritsu shell 外观的游戏内表面。
    /// </summary>
    public static class RitsuShellPanelStyles
    {
        /// <summary>
        ///     Primary framed panel (large panes, content wells).
        ///     主带框面板 (大窗格, 内容井)。
        /// </summary>
        /// <param name="background">
        ///     Background fill color.
        ///     背景填充颜色。
        /// </param>
        /// <param name="cornerRadius">
        ///     Corner radius.
        ///     圆角半径。
        /// </param>
        /// <returns>
        ///     A new framed surface stylebox.
        ///     A 新的带框表面 stylebox。
        /// </returns>
        public static StyleBoxFlat CreateFramedSurface(Color background, int cornerRadius)
        {
            var t = RitsuShellTheme.Current;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.framedSurface.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.framedSurface.layout.padding", 0);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.framedSurface.layout.cornerRadius",
                    cornerRadius);
            return new()
            {
                BgColor = background,
                BorderColor = t.Surface.Framed.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
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
        ///     嵌套卡片 侧边栏内 (mod 分组 chrome)。
        /// </summary>
        /// <param name="cornerRadius">
        ///     Corner radius.
        ///     圆角半径。
        /// </param>
        /// <param name="selected">
        ///     Whether the card is in its selected state.
        ///     卡片是否处于选中状态。
        /// </param>
        /// <returns>
        ///     A new sidebar mod card stylebox.
        ///     A new 侧边栏 mod 卡片 stylebox。
        /// </returns>
        public static StyleBoxFlat CreateSidebarModCard(int cornerRadius, bool selected)
        {
            var t = RitsuShellTheme.Current;
            var state = selected ? t.Component.SidebarCard.Selected : t.Component.SidebarCard.Default;
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebarCard.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebarCard.layout.padding", 10);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.sidebarCard.layout.cornerRadius",
                    cornerRadius);
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
        ///     同 <see cref="CreateSidebarModCard" /> but 与 更紧凑的内部 padding (紧凑导航)。
        /// </summary>
        /// <param name="cornerRadius">
        ///     Corner radius.
        ///     圆角半径。
        /// </param>
        /// <param name="selected">
        ///     Whether the card is in its selected state.
        ///     卡片是否处于选中状态。
        /// </param>
        /// <param name="innerMargin">
        ///     Inner padding override applied to all four edges.
        ///     内部 padding 覆盖值 应用 to 四条边。
        /// </param>
        /// <returns>
        ///     A new compact sidebar mod card stylebox.
        ///     A new compact 侧边栏 mod 卡片 stylebox。
        /// </returns>
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
