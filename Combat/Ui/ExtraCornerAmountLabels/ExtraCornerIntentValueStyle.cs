using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Visuals for extra badges on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" />, matched to intent
    ///     <c>%Value</c> (MegaRichTextLabel; see sts-2-source <c>scenes/combat/intent.tscn</c>).
    /// </summary>
    internal static class ExtraCornerIntentValueStyle
    {
        private static readonly StringName RichTextThemeType = new("RichTextLabel");

        internal static void Apply(MegaLabel target, RichTextLabel? valueOnIntent)
        {
            if (valueOnIntent == null)
            {
                ExtraCornerCombatFallbackFonts.Apply(target);
                return;
            }

            var font = valueOnIntent.GetThemeFont(ThemeConstants.RichTextLabel.NormalFont, RichTextThemeType);
            if (font != null)
                target.AddThemeFontOverride(ThemeConstants.Label.Font, font);

            var size = valueOnIntent.GetThemeFontSize(ThemeConstants.RichTextLabel.NormalFontSize, RichTextThemeType);
            if (size > 0)
                target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, size);

            var color = valueOnIntent.GetThemeColor(ThemeConstants.RichTextLabel.DefaultColor, RichTextThemeType);
            if (color.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.Label.FontColor, color);

            var outlineColor =
                valueOnIntent.GetThemeColor(ThemeConstants.RichTextLabel.FontOutlineColor, RichTextThemeType);
            if (outlineColor.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);

            var outlineSize = valueOnIntent.GetThemeConstant(ThemeConstants.Label.OutlineSize, RichTextThemeType);
            if (outlineSize > 0)
                target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, outlineSize);

            target.AutoSizeEnabled = true;
        }
    }
}
