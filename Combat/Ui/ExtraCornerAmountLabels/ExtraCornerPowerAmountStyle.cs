using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Visuals for extra badges on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" />, matched to
    ///     <c>%AmountLabel</c> (see sts-2-source <c>scenes/combat/power.tscn</c>).
    /// </summary>
    internal static class ExtraCornerPowerAmountStyle
    {
        private static readonly StringName MegaLabelThemeType = new("MegaLabel");
        private static readonly StringName LabelThemeType = new("Label");
        private static readonly StringName ShadowOffsetX = new("shadow_offset_x");
        private static readonly StringName ShadowOffsetY = new("shadow_offset_y");
        private static readonly StringName ShadowOutlineSize = new("shadow_outline_size");

        internal static void Apply(MegaLabel target, MegaLabel? amountOnPower)
        {
            if (amountOnPower == null)
            {
                ExtraCornerCombatFallbackFonts.Apply(target);
                return;
            }

            target.MinFontSize = amountOnPower.MinFontSize;
            target.MaxFontSize = amountOnPower.MaxFontSize;

            var font = amountOnPower.GetThemeFont(ThemeConstants.Label.Font, LabelThemeType)
                       ?? amountOnPower.GetThemeFont(ThemeConstants.Label.Font, MegaLabelThemeType);
            if (font != null)
                target.AddThemeFontOverride(ThemeConstants.Label.Font, font);

            var size = amountOnPower.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType);
            if (size <= 0)
                size = amountOnPower.GetThemeFontSize(ThemeConstants.Label.FontSize, MegaLabelThemeType);
            if (size > 0)
                target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, size);

            var color = amountOnPower.GetThemeColor(ThemeConstants.Label.FontColor, LabelThemeType);
            if (color.A <= 0f)
                color = amountOnPower.GetThemeColor(ThemeConstants.Label.FontColor, MegaLabelThemeType);
            if (color.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.Label.FontColor, color);

            CopyOutlineAndShadowFromLabelTheme(target, amountOnPower);
            target.AutoSizeEnabled = true;
        }

        private static void CopyOutlineAndShadowFromLabelTheme(MegaLabel target, MegaLabel source)
        {
            var outlineColor = source.GetThemeColor(ThemeConstants.Label.FontOutlineColor, LabelThemeType);
            if (outlineColor.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);

            var shadowColor = source.GetThemeColor(ThemeConstants.Label.FontShadowColor, LabelThemeType);
            if (shadowColor.A > 0f)
                target.AddThemeColorOverride(ThemeConstants.Label.FontShadowColor, shadowColor);

            var outlineSize = source.GetThemeConstant(ThemeConstants.Label.OutlineSize, LabelThemeType);
            if (outlineSize > 0)
                target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, outlineSize);

            var sx = source.GetThemeConstant(ShadowOffsetX, LabelThemeType);
            if (sx != 0)
                target.AddThemeConstantOverride(ShadowOffsetX, sx);

            var sy = source.GetThemeConstant(ShadowOffsetY, LabelThemeType);
            if (sy != 0)
                target.AddThemeConstantOverride(ShadowOffsetY, sy);

            var sos = source.GetThemeConstant(ShadowOutlineSize, LabelThemeType);
            if (sos != 0)
                target.AddThemeConstantOverride(ShadowOutlineSize, sos);
        }
    }
}
