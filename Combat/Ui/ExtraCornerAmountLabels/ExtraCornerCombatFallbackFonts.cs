using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Last-resort font when no vanilla reference control is available (e.g. node not ready).
    /// </summary>
    internal static class ExtraCornerCombatFallbackFonts
    {
        private static readonly StringName MegaLabelThemeType = new("MegaLabel");

        internal static void Apply(MegaLabel target)
        {
            var vanilla = NCombatRoom.Instance?.Ui?.DrawPile?.GetNodeOrNull<MegaLabel>("CountContainer/Count");
            var font = vanilla?.GetThemeFont(ThemeConstants.Label.Font, MegaLabelThemeType);
            if (font != null)
            {
                target.AddThemeFontOverride(ThemeConstants.Label.Font, font);
                if (vanilla != null)
                    target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize,
                        vanilla.GetThemeFontSize(ThemeConstants.Label.FontSize, MegaLabelThemeType));
                target.AutoSizeEnabled = true;
                return;
            }

            var fallback = ThemeDB.FallbackFont;
            if (fallback != null)
            {
                target.AddThemeFontOverride(ThemeConstants.Label.Font, fallback);
                target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 22);
                target.AutoSizeEnabled = true;
                return;
            }

            target.AddThemeFontOverride(ThemeConstants.Label.Font, new SystemFont());
            target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 22);
            target.AutoSizeEnabled = true;
        }
    }
}
