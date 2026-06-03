using System.Text;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Helpers for converting ANSI terminal text into plain or segmented styled text.
    /// </summary>
    public static class RitsuAnsiText
    {
        private static readonly string[] NormalPalette =
        [
            "#2f343f",
            "#c83d3d",
            "#1b8554",
            "#a66a00",
            "#266dcc",
            "#9b59b6",
            "#087a79",
            "#d7dee8",
        ];

        private static readonly string[] BrightPalette =
        [
            "#687587",
            "#ff7777",
            "#7cda95",
            "#edba60",
            "#75aaff",
            "#c792ea",
            "#62d3cb",
            "#ffffff",
        ];

        /// <summary>
        ///     Removes ANSI CSI/OSC escape sequences from <paramref name="text" />.
        /// </summary>
        public static string StripControlSequences(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var escape = text.IndexOf('\u001b');
            if (escape < 0)
                return text;

            var result = new StringBuilder(text.Length);
            var index = 0;
            while (index < text.Length)
            {
                if (text[index] != '\u001b')
                {
                    result.Append(text[index]);
                    index++;
                    continue;
                }

                index = SkipEscapeSequence(text, index);
            }

            return result.ToString();
        }

        /// <summary>
        ///     Parses ANSI SGR foreground colors and text attributes into styled text segments.
        /// </summary>
        public static IReadOnlyList<RitsuTextSegment> ParseSegments(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (!text.Contains('\u001b'))
                return string.IsNullOrEmpty(text) ? [] : [new() { Text = text }];

            var segments = new List<RitsuTextSegment>();
            var state = new StyleState();
            var index = 0;
            var start = 0;

            while (index < text.Length)
            {
                if (text[index] != '\u001b')
                {
                    index++;
                    continue;
                }

                AppendSegment(segments, text[start..index], state);

                var next = index + 1;
                if (next >= text.Length)
                {
                    index++;
                    start = index;
                    continue;
                }

                if (text[next] != '[')
                {
                    index = SkipEscapeSequence(text, index);
                    start = index;
                    continue;
                }

                var finalIndex = FindAnsiFinalByte(text, next + 1);
                if (finalIndex < 0)
                {
                    index++;
                    start = index;
                    continue;
                }

                if (text[finalIndex] == 'm')
                    ApplySgrValues(text[(next + 1)..finalIndex], ref state);

                index = finalIndex + 1;
                start = index;
            }

            AppendSegment(segments, text[start..], state);
            return segments.Count == 0 ? [] : segments;
        }

        private static void AppendSegment(List<RitsuTextSegment> segments, string text, StyleState state)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var previous = segments.Count == 0 ? null : segments[^1];
            if (previous != null &&
                previous.Color == state.Color &&
                previous.Bold == state.Bold &&
                previous.Dim == state.Dim &&
                previous.Kind == null)
            {
                segments[^1] = previous with { Text = previous.Text + text };
                return;
            }

            segments.Add(new()
            {
                Text = text,
                Color = state.Color,
                Bold = state.Bold,
                Dim = state.Dim,
            });
        }

        private static int SkipEscapeSequence(string text, int escapeIndex)
        {
            var next = escapeIndex + 1;
            if (next >= text.Length)
                return next;

            return text[next] switch
            {
                '[' => SkipAnsiCsi(text, next + 1),
                ']' => SkipAnsiOsc(text, next + 1),
                _ => next + 1,
            };
        }

        private static int SkipAnsiCsi(string text, int index)
        {
            while (index < text.Length)
            {
                var ch = text[index++];
                if (ch is >= '@' and <= '~')
                    break;
            }

            return index;
        }

        private static int SkipAnsiOsc(string text, int index)
        {
            while (index < text.Length)
            {
                var ch = text[index++];
                if (ch == '\a')
                    break;

                if (ch != '\u001b' || index >= text.Length || text[index] != '\\')
                    continue;

                index++;
                break;
            }

            return index;
        }

        private static int FindAnsiFinalByte(string text, int index)
        {
            while (index < text.Length)
            {
                var code = text[index];
                if (code is >= '@' and <= '~')
                    return index;

                index++;
            }

            return -1;
        }

        private static void ApplySgrValues(string payload, ref StyleState state)
        {
            var values = payload.Split(';').Select(static value => value.Length == 0 ? 0 : ParseSgrNumber(value))
                .ToArray();
            if (values.Length == 0 || values.Any(static value => value < 0))
                values = [0];

            for (var index = 0; index < values.Length; index++)
            {
                var code = values[index];
                switch (code)
                {
                    case 0:
                        state = new();
                        break;
                    case 1:
                        state.Bold = true;
                        break;
                    case 2:
                        state.Dim = true;
                        break;
                    case 22:
                        state.Bold = false;
                        state.Dim = false;
                        break;
                    case 39:
                        state.Color = null;
                        break;
                    case >= 30 and <= 37:
                        state.Color = NormalPalette[code - 30];
                        break;
                    case >= 90 and <= 97:
                        state.Color = BrightPalette[code - 90];
                        break;
                    case 38 when index + 4 < values.Length && values[index + 1] == 2:
                        state.Color =
                            $"rgb({ClampRgb(values[index + 2])}, {ClampRgb(values[index + 3])}, {ClampRgb(values[index + 4])})";
                        index += 4;
                        break;
                }
            }
        }

        private static int ParseSgrNumber(string value)
        {
            return int.TryParse(value, out var number) ? number : -1;
        }

        private static int ClampRgb(int value)
        {
            return Math.Clamp(value, 0, 255);
        }

        private struct StyleState
        {
            public string? Color { get; set; }

            public bool Bold { get; set; }

            public bool Dim { get; set; }
        }
    }
}
