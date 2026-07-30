using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides shared <see cref="StyleBoxFlat" /> factories for framed panels and sidebar cards used by
    ///         mod settings, runtime overlays, and other in-game UI that follows the Ritsu Shell style.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供带框面板和侧边栏卡片所用的共享 <see cref="StyleBoxFlat" /> 工厂，适用于模组设置界面、
    ///         运行时浮层及其他采用 Ritsu Shell 样式的游戏内界面。
    ///     </para>
    /// </summary>
    public static class RitsuShellPanelStyles
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a primary framed panel for large panes and content areas.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为大型窗格和内容区域创建主要的带框面板。
        ///     </para>
        /// </summary>
        /// <param name="background">
        ///     <para xml:lang="en">The background fill color.</para>
        ///     <para xml:lang="zh-CN">背景填充颜色。</para>
        /// </param>
        /// <param name="cornerRadius">
        ///     <para xml:lang="en">The fallback radius applied to each corner.</para>
        ///     <para xml:lang="zh-CN">应用于各个角的备用圆角半径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new style box for the framed panel.</para>
        ///     <para xml:lang="zh-CN">带框面板的新样式框。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a nested sidebar card used to group a mod's entries.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建用于组织模组条目的嵌套侧边栏卡片。
        ///     </para>
        /// </summary>
        /// <param name="cornerRadius">
        ///     <para xml:lang="en">The fallback radius applied to each corner.</para>
        ///     <para xml:lang="zh-CN">应用于各个角的备用圆角半径。</para>
        /// </param>
        /// <param name="selected">
        ///     <para xml:lang="en"><see langword="true" /> to use the selected-state colors.</para>
        ///     <para xml:lang="zh-CN">若要使用选中状态的颜色，则为 <see langword="true" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new style box for the sidebar card.</para>
        ///     <para xml:lang="zh-CN">新建的侧边栏卡片样式框。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a sidebar card with explicitly controlled inner padding for compact navigation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建可显式控制内边距的侧边栏卡片，用于紧凑导航。
        ///     </para>
        /// </summary>
        /// <param name="cornerRadius">
        ///     <para xml:lang="en">The fallback radius applied to each corner.</para>
        ///     <para xml:lang="zh-CN">应用于各个角的备用圆角半径。</para>
        /// </param>
        /// <param name="selected">
        ///     <para xml:lang="en"><see langword="true" /> to use the selected-state colors.</para>
        ///     <para xml:lang="zh-CN">若要使用选中状态的颜色，则为 <see langword="true" />。</para>
        /// </param>
        /// <param name="innerMargin">
        ///     <para xml:lang="en">The content margin applied to all four edges.</para>
        ///     <para xml:lang="zh-CN">应用于四条边的内容边距。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new style box for the compact sidebar card.</para>
        ///     <para xml:lang="zh-CN">新建的紧凑侧边栏卡片样式框。</para>
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
