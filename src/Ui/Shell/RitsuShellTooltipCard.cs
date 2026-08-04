using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    // ReSharper disable once Godot.MissingParameterlessConstructor
    internal sealed partial class RitsuShellTooltipCard : PanelContainer
    {
        private const float MinimumContentWidth = 150f;
        private const float MaximumContentWidth = 420f;
        private const float PreferredBodyWidth = 280f;

        private RitsuShellTooltipCard(string title, string? body)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateTooltipPanelStyle());

            var contentWidth = ResolveContentWidth(title, body);
            var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            column.AddThemeConstantOverride("separation", 5);
            AddChild(column);

            var heading = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            heading.AddThemeConstantOverride("separation", 8);
            heading.AddChild(new ColorRect
            {
                Color = RitsuShellTheme.Current.Text.HoverHighlight,
                CustomMinimumSize = new(3f, 0f),
                MouseFilter = MouseFilterEnum.Ignore,
            });

            var titleLabel = CreateLabel(
                title,
                RitsuShellTheme.Current.Font.BodyBold,
                RitsuShellTheme.Current.Metric.FontSize.PopupRow,
                RitsuShellTheme.Current.Text.LabelPrimary,
                contentWidth - 11f);
            heading.AddChild(titleLabel);
            column.AddChild(heading);

            if (string.IsNullOrWhiteSpace(body))
                return;

            var bodyLabel = CreateLabel(
                body,
                RitsuShellTheme.Current.Font.Body,
                ResolveBodyFontSize(),
                RitsuShellTheme.Current.Text.LabelSecondary,
                contentWidth);
            column.AddChild(bodyLabel);
        }

        internal static Control Create(string text)
        {
            var lines = NormalizeLines(text);
            if (lines.Count == 0)
                return null!;

            var title = lines[0];
            var body = lines.Count > 1 ? string.Join('\n', lines.Skip(1)) : null;
            return new RitsuShellTooltipCard(title, body);
        }

        private static Label CreateLabel(string text, Font font, int fontSize, Color color, float width)
        {
            var label = new Label
            {
                Text = text,
                CustomMinimumSize = new(Math.Max(1f, width), 0f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static float ResolveContentWidth(string title, string? body)
        {
            var titleWidth = MeasureWidestLine(
                title,
                RitsuShellTheme.Current.Font.BodyBold,
                RitsuShellTheme.Current.Metric.FontSize.PopupRow) + 11f;
            var bodyWidth = string.IsNullOrWhiteSpace(body)
                ? 0f
                : Math.Min(
                    MaximumContentWidth,
                    Math.Max(
                        PreferredBodyWidth,
                        MeasureWidestLine(body, RitsuShellTheme.Current.Font.Body, ResolveBodyFontSize())));
            return Mathf.Clamp(Math.Max(titleWidth, bodyWidth), MinimumContentWidth, MaximumContentWidth);
        }

        private static float MeasureWidestLine(string text, Font font, int fontSize)
        {
            return NormalizeLines(text)
                .Select(line => font.GetStringSize(line, HorizontalAlignment.Left, -1f, fontSize).X)
                .DefaultIfEmpty(0f)
                .Max();
        }

        private static int ResolveBodyFontSize()
        {
            var fontSize = RitsuShellTheme.Current.Metric.FontSize.Tooltip;
            return fontSize > 0 ? fontSize : RitsuShellTheme.Current.Metric.FontSize.Secondary;
        }

        private static List<string> NormalizeLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            return
            [
                .. text.Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n')
                    .Split('\n')
                    .Select(static line => line.Trim())
                    .Where(static line => line.Length > 0),
            ];
        }
    }

    internal partial class RitsuShellTooltipLabel : Label
    {
        public override Control _MakeCustomTooltip(string forText)
        {
            return RitsuShellTooltipCard.Create(forText);
        }
    }

    internal partial class RitsuShellTooltipPanelContainer : PanelContainer
    {
        public override Control _MakeCustomTooltip(string forText)
        {
            return RitsuShellTooltipCard.Create(forText);
        }
    }
}
