using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maps shell-theme tokens to Godot's native <c>TooltipPanel</c> and <c>TooltipLabel</c> theme types
    ///         through a <see cref="Control.Theme" /> attached to an ancestor control.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过附加到祖先控件的 <see cref="Control.Theme" />，将外壳主题令牌映射到 Godot 原生的
    ///         <c>TooltipPanel</c> 和 <c>TooltipLabel</c> 主题类型。
    ///     </para>
    /// </summary>
    public static class RitsuShellTooltipTheme
    {
        private static readonly StringName TooltipPanelClass = new("TooltipPanel");

        /// <seealso href="https://docs.godotengine.org/en/stable/classes/class_tooltippanel.html" />
        private static readonly StringName TooltipLabelClass = new("TooltipLabel");

        /// <seealso href="https://docs.godotengine.org/en/stable/classes/class_tooltip.html" />
        private static readonly StringName PanelStyle = new("panel");

        private static readonly StringName FontColor = new("font_color");
        private static readonly StringName NormalFont = new("font");
        private static readonly StringName FontSize = new("font_size");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies the current theme's tooltip panel style and typography to <paramref name="root" /> so
        ///         descendant controls resolve tooltip theme items consistently.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将当前主题的工具提示面板样式及排版应用到 <paramref name="root" />，使其后代控件能够一致地解析
        ///         工具提示主题项。
        ///     </para>
        /// </summary>
        /// <param name="root">
        ///     <para xml:lang="en">
        ///         The root of the subtree, typically the mod-settings submenu control.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         子树的根控件，通常为模组设置子菜单控件。
        ///     </para>
        /// </param>
        public static void ApplyToTreeRoot(Control root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var t = RitsuShellTheme.Current;
            var patch = new Godot.Theme();
            patch.SetStylebox(PanelStyle, TooltipPanelClass, RitsuShellChromeStyles.CreateTooltipPanelStyle());
            patch.SetColor(FontColor, TooltipLabelClass, t.Text.Hint);
            patch.SetFont(NormalFont, TooltipLabelClass, t.Font.Body);
            var fontPx = t.Metric.FontSize.Tooltip;
            if (fontPx <= 0)
                fontPx = t.Metric.FontSize.PopupRow;
            patch.SetFontSize(FontSize, TooltipLabelClass, fontPx);

            Godot.Theme merged;
            if (root.Theme != null)
            {
                merged = (Godot.Theme)root.Theme.Duplicate();
                merged.MergeWith(patch);
            }
            else
            {
                merged = patch;
            }

            root.Theme = merged;
        }
    }
}
