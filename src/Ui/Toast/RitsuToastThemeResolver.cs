using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Toast
{
    internal static class RitsuToastThemeResolver
    {
        internal static RitsuToastVisualStyle Resolve(RitsuToastLevel level)
        {
            var theme = RitsuShellTheme.Current;
            var surfaceBg = ColorOr(theme, "components.toast.surface.bg", theme.Component.OverlayPanel.Bg);
            var surfaceBorder =
                ColorOr(theme, "components.toast.surface.border", theme.Component.OverlayPanel.Border);
            var titleColor = ColorOr(theme, "components.toast.text.title", theme.Text.RichTitle);
            var bodyColor = ColorOr(theme, "components.toast.text.body", theme.Text.RichBody);
            var interactiveBadgeBackground = ColorOr(theme, "components.toast.interactive.bg",
                new(0.26f, 0.39f, 0.56f, 0.75f));
            var interactiveBadgeForeground =
                ColorOr(theme, "components.toast.interactive.fg", theme.Text.HoverHighlight);
            var closeButtonBackground =
                ColorOr(theme, "components.toast.closeButton.bg", interactiveBadgeBackground);
            var closeButtonBackgroundHover =
                ColorOr(theme, "components.toast.closeButton.bgHover", closeButtonBackground);
            var closeButtonBorder = ColorOr(theme, "components.toast.closeButton.border", surfaceBorder);
            var closeButtonBorderHover =
                ColorOr(theme, "components.toast.closeButton.borderHover", closeButtonBorder);

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

            var accent = ColorOr(theme, $"components.toast.levels.{levelKey}.accent", accentFallback);
            var background = ColorOr(theme, $"components.toast.levels.{levelKey}.bg", surfaceBg);
            var border = ColorOr(theme, $"components.toast.levels.{levelKey}.border", surfaceBorder);
            var levelTitle = ColorOr(theme, $"components.toast.levels.{levelKey}.title", titleColor);
            var levelBody = ColorOr(theme, $"components.toast.levels.{levelKey}.body", bodyColor);
            var progressTrack = ColorOr(theme, "components.toast.progress.track",
                new(accent.R, accent.G, accent.B, 0.18f));
            var progressFill = ColorOr(theme, $"components.toast.levels.{levelKey}.progress",
                ColorOr(theme, "components.toast.progress.fill", accent));

            return new(
                background,
                border,
                levelTitle,
                levelBody,
                accent,
                progressTrack,
                progressFill,
                ColorOr(theme, "components.toast.surface.shadow", new(0f, 0f, 0f, 0.28f)),
                interactiveBadgeBackground,
                interactiveBadgeForeground,
                closeButtonBackground,
                closeButtonBackgroundHover,
                closeButtonBorder,
                closeButtonBorderHover,
                IntOr(theme, "components.toast.layout.borderWidth", theme.Metric.BorderWidth.Overlay),
                IntOr(theme, "components.toast.layout.cornerRadius", theme.Metric.Radius.Overlay),
                IntOr(theme, "components.toast.layout.titleFontSize", theme.Metric.FontSize.OverlayBody),
                IntOr(theme, "components.toast.layout.bodyFontSize", theme.Metric.FontSize.OverlayBody),
                IntOr(theme, "components.toast.layout.badgeFontSize", theme.Metric.FontSize.HintSmall),
                IntOr(theme, "components.toast.interactive.borderWidth", 1),
                IntOr(theme, "components.toast.closeButton.layout.borderWidth", 1),
                FloatOr(theme, "components.toast.layout.shadowSize", 8f),
                FloatOr(theme, "components.toast.layout.width", 420f),
                FloatOr(theme, "components.toast.layout.minHeight", 72f),
                FloatOr(theme, "components.toast.layout.padding.horizontal", 14f),
                FloatOr(theme, "components.toast.layout.padding.vertical", 12f),
                FloatOr(theme, "components.toast.layout.textSpacing", 4f),
                FloatOr(theme, "components.toast.layout.rowSpacing", 10f),
                FloatOr(theme, "components.toast.progress.height", 3f),
                FloatOr(theme, "components.toast.progress.spacing", 8f),
                FloatOr(theme, "components.toast.layout.imageSize", 44f),
                FloatOr(theme, "components.toast.layout.closeButtonSize", 26f),
                FloatOr(theme, "components.toast.closeButton.layout.paddingH", 2f),
                FloatOr(theme, "components.toast.closeButton.layout.paddingV", 1f),
                FloatOr(theme, "components.toast.layout.interactiveBadgeHeight", 24f),
                FloatOr(theme, "components.toast.layout.screenMargin", 16f),
                FloatOr(theme, "components.toast.animation.enterDuration", 0.22f),
                FloatOr(theme, "components.toast.animation.moveDuration", 0.18f),
                FloatOr(theme, "components.toast.animation.exitDuration", 0.18f),
                FloatOr(theme, "components.toast.animation.enterSlideDistance", 24f),
                FloatOr(theme, "components.toast.animation.exitSlideDistance", 18f),
                FloatOr(theme, "components.toast.animation.enterScale", 0.92f));
        }

        private static Color ColorOr(RitsuShellTheme theme, string path, Color fallback)
        {
            return theme.TryGetColor(path, out var value) ? value : fallback;
        }

        private static int IntOr(RitsuShellTheme theme, string path, int fallback)
        {
            if (!theme.TryGetNumber(path, out var value))
                return fallback;

            var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            return rounded is >= int.MinValue and <= int.MaxValue ? (int)rounded : fallback;
        }

        private static float FloatOr(RitsuShellTheme theme, string path, float fallback)
        {
            return theme.TryGetNumber(path, out var value) &&
                   value is >= -float.MaxValue and <= float.MaxValue
                ? (float)value
                : fallback;
        }
    }
}
