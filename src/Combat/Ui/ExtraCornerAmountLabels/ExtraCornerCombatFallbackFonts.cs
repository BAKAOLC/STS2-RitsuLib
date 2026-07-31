using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">Applies a fallback font when the host's base-game reference label is unavailable.</para>
    ///     <para xml:lang="zh-CN">当宿主的游戏原有参考标签不可用时，应用回退字体。</para>
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

            target.AddThemeFontOverride(ThemeConstants.Label.Font,
                RitsuShellThemeValueCoerce.AsFont(null));
            target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 22);
            target.AutoSizeEnabled = true;
        }

        internal static void Apply(MegaRichTextLabel target)
        {
            var vanilla = NCombatRoom.Instance?.Ui?.DrawPile?.GetNodeOrNull<MegaLabel>("CountContainer/Count");
            var font = vanilla?.GetThemeFont(ThemeConstants.Label.Font, MegaLabelThemeType);
            if (font != null)
            {
                target.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont, font);
                if (vanilla != null)
                    target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize,
                        vanilla.GetThemeFontSize(ThemeConstants.Label.FontSize, MegaLabelThemeType));
                target.AutoSizeEnabled = true;
                return;
            }

            target.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont,
                RitsuShellThemeValueCoerce.AsFont(null));
            target.AddThemeFontSizeOverride(ThemeConstants.RichTextLabel.NormalFontSize, 22);
            target.AutoSizeEnabled = true;
        }
    }
}
