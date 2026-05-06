using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Maps shell tokens onto Godot native tooltip theme types (<c>TooltipPanel</c>, <c>TooltipLabel</c>) via a
    ///     <see cref="Control.Theme" /> attached to an ancestor of hovered controls.
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
        ///     Applies tooltip panel styling and typography from <see cref="RitsuShellTheme.Current" /> to
        ///     <paramref name="root" /> so descendant controls resolve tooltip theme items consistently.
        /// </summary>
        /// <param name="root">Sub-tree root (typically the mod settings submenu control).</param>
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
