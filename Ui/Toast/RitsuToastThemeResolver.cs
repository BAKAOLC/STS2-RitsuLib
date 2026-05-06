using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Toast
{
    internal static class RitsuToastThemeResolver
    {
        internal static RitsuToastVisualStyle Resolve(RitsuToastLevel level)
        {
            var theme = RitsuShellTheme.Current;
            var surfaceBg = ColorOr("components.toast.surface.bg", theme.Component.OverlayPanel.Bg);
            var surfaceBorder = ColorOr("components.toast.surface.border", theme.Component.OverlayPanel.Border);
            var titleColor = ColorOr("components.toast.text.title", theme.Text.RichTitle);
            var bodyColor = ColorOr("components.toast.text.body", theme.Text.RichBody);
            var interactiveBadgeBackground = ColorOr("components.toast.interactive.bg",
                new(0.26f, 0.39f, 0.56f, 0.75f));
            var interactiveBadgeForeground = ColorOr("components.toast.interactive.fg", theme.Text.HoverHighlight);
            var closeButtonBackground = ColorOr("components.toast.closeButton.bg", interactiveBadgeBackground);
            var closeButtonBackgroundHover =
                ColorOr("components.toast.closeButton.bgHover", closeButtonBackground);
            var closeButtonBorder = ColorOr("components.toast.closeButton.border", surfaceBorder);
            var closeButtonBorderHover = ColorOr("components.toast.closeButton.borderHover", closeButtonBorder);

            var levelKey = level switch
            {
                RitsuToastLevel.Warning => "warning",
                RitsuToastLevel.Error => "error",
                _ => "info",
            };

            var accentFallback = level switch
            {
                RitsuToastLevel.Warning => new Color(0.95f, 0.72f, 0.22f),
                RitsuToastLevel.Error => new Color(0.90f, 0.35f, 0.35f),
                _ => new Color(0.45f, 0.72f, 0.93f),
            };

            var accent = ColorOr($"components.toast.levels.{levelKey}.accent", accentFallback);
            var background = ColorOr($"components.toast.levels.{levelKey}.bg", surfaceBg);
            var border = ColorOr($"components.toast.levels.{levelKey}.border", surfaceBorder);
            var levelTitle = ColorOr($"components.toast.levels.{levelKey}.title", titleColor);
            var levelBody = ColorOr($"components.toast.levels.{levelKey}.body", bodyColor);

            return new(
                background,
                border,
                levelTitle,
                levelBody,
                accent,
                ColorOr("components.toast.surface.shadow", new(0f, 0f, 0f, 0.28f)),
                interactiveBadgeBackground,
                interactiveBadgeForeground,
                closeButtonBackground,
                closeButtonBackgroundHover,
                closeButtonBorder,
                closeButtonBorderHover,
                IntOr("components.toast.layout.borderWidth", theme.Metric.BorderWidth.Overlay),
                IntOr("components.toast.layout.cornerRadius", theme.Metric.Radius.Overlay),
                IntOr("components.toast.layout.titleFontSize", theme.Metric.FontSize.OverlayBody),
                IntOr("components.toast.layout.bodyFontSize", theme.Metric.FontSize.OverlayBody),
                IntOr("components.toast.layout.badgeFontSize", theme.Metric.FontSize.HintSmall),
                IntOr("components.toast.interactive.borderWidth", 1),
                IntOr("components.toast.closeButton.layout.borderWidth", 1),
                FloatOr("components.toast.layout.shadowSize", 8f),
                FloatOr("components.toast.layout.width", 420f),
                FloatOr("components.toast.layout.minHeight", 72f),
                FloatOr("components.toast.layout.padding.horizontal", 14f),
                FloatOr("components.toast.layout.padding.vertical", 12f),
                FloatOr("components.toast.layout.textSpacing", 4f),
                FloatOr("components.toast.layout.rowSpacing", 10f),
                FloatOr("components.toast.layout.imageSize", 44f),
                FloatOr("components.toast.layout.closeButtonSize", 26f),
                FloatOr("components.toast.closeButton.layout.paddingH", 2f),
                FloatOr("components.toast.closeButton.layout.paddingV", 1f),
                FloatOr("components.toast.layout.interactiveBadgeHeight", 24f),
                FloatOr("components.toast.layout.screenMargin", 16f),
                FloatOr("components.toast.animation.enterDuration", 0.22f),
                FloatOr("components.toast.animation.moveDuration", 0.18f),
                FloatOr("components.toast.animation.exitDuration", 0.18f),
                FloatOr("components.toast.animation.enterSlideDistance", 24f),
                FloatOr("components.toast.animation.exitSlideDistance", 18f),
                FloatOr("components.toast.animation.enterScale", 0.92f));
        }

        private static Color ColorOr(string path, Color fallback)
        {
            return RitsuShellTheme.Current.TryGetColor(path, out var value) ? value : fallback;
        }

        private static int IntOr(string path, int fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value)
                ? (int)Math.Round(value, MidpointRounding.AwayFromZero)
                : fallback;
        }

        private static float FloatOr(string path, float fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value) ? (float)value : fallback;
        }
    }
}
