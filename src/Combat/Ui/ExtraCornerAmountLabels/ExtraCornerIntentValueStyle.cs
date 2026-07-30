using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Copies an <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" /> value label's typography to its extra
    ///         badges.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" /> 数值标签的排版样式复制到额外角标。
    ///     </para>
    /// </summary>
    internal static class ExtraCornerIntentValueStyle
    {
        private static readonly StringName RichTextThemeType = new("RichTextLabel");

        internal static void Apply(Control target, RichTextLabel? valueOnIntent)
        {
            switch (target)
            {
                case MegaLabel label:
                    Apply(label, valueOnIntent);
                    break;
                case MegaRichTextLabel rich:
                    Apply(rich, valueOnIntent);
                    break;
            }
        }

        internal static void Apply(MegaLabel target, RichTextLabel? valueOnIntent)
        {
            switch (valueOnIntent)
            {
                case null:
                    ExtraCornerCombatFallbackFonts.Apply(target);
                    return;
                case MegaRichTextLabel richSource:
                    target.MinFontSize = richSource.MinFontSize;
                    target.MaxFontSize = richSource.MaxFontSize;
                    break;
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

        internal static void Apply(MegaRichTextLabel target, RichTextLabel? valueOnIntent)
        {
            switch (valueOnIntent)
            {
                case null:
                    ExtraCornerCombatFallbackFonts.Apply(target);
                    return;
                case MegaRichTextLabel richSource:
                    target.MinFontSize = richSource.MinFontSize;
                    target.MaxFontSize = richSource.MaxFontSize;
                    break;
            }

            var font = valueOnIntent.GetThemeFont(ThemeConstants.RichTextLabel.NormalFont, RichTextThemeType);
            if (font != null)
                target.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont, font);

            var size = valueOnIntent.GetThemeFontSize(ThemeConstants.RichTextLabel.NormalFontSize, RichTextThemeType);
            if (size > 0)
                target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize, size);

            var color = valueOnIntent.GetThemeColor(ThemeConstants.RichTextLabel.DefaultColor, RichTextThemeType);
            if (color.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.RichTextLabel.DefaultColor, color);

            var outlineColor =
                valueOnIntent.GetThemeColor(ThemeConstants.RichTextLabel.FontOutlineColor, RichTextThemeType);
            if (outlineColor.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontOutlineColor, outlineColor);

            var shadowColor =
                valueOnIntent.GetThemeColor(ThemeConstants.RichTextLabel.FontShadowColor, RichTextThemeType);
            if (shadowColor.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontShadowColor, shadowColor);

            var outlineSize = valueOnIntent.GetThemeConstant(ThemeConstants.Label.OutlineSize, RichTextThemeType);
            if (outlineSize > 0)
                target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, outlineSize);

            target.AutoSizeEnabled = true;
        }
    }
}
