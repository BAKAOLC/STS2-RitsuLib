using System.Globalization;
using Godot;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     Helpers for building mod rich-text BBCode tags.
    ///     构建 mod 富文本 BBCode tag 的辅助工具。
    /// </summary>
    public static class ModRichTextTag
    {
        /// <summary>
        ///     Creates a rich-text tag parameter.
        ///     创建富文本 tag 参数。
        /// </summary>
        public static ModRichTextTagParameter Param(string name, object? value)
        {
            return new(name, value);
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> with a BBCode tag.
        ///     用 BBCode tag 包装 <paramref name="text" />。
        /// </summary>
        public static string Wrap(string bbcode, string text, params ModRichTextTagParameter[] parameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bbcode);
            ArgumentNullException.ThrowIfNull(text);

            var tag = NormalizeName(bbcode, "BBCode tag");
            var opening = BuildOpeningTag(tag, parameters);
            return $"{opening}{text}[/{tag}]";
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> with a BBCode tag.
        ///     用 BBCode tag 包装 <paramref name="text" />。
        /// </summary>
        public static string Wrap(string bbcode, string text, IEnumerable<ModRichTextTagParameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            return Wrap(bbcode, text, parameters.ToArray());
        }

        internal static string NormalizeName(string value, string label)
        {
            var trimmed = value.Trim();
            if (trimmed.Length == 0)
                throw new ArgumentException($"{label} must not be empty.", nameof(value));

            foreach (var ch in trimmed.Where(ch => !char.IsLetterOrDigit(ch) && ch is not ('_' or '-' or ':' or '.')))
                throw new ArgumentException(
                    $"{label} '{trimmed}' contains unsupported character '{ch}'.",
                    nameof(value));

            return trimmed;
        }

        private static string BuildOpeningTag(string tag, IReadOnlyList<ModRichTextTagParameter> parameters)
        {
            if (parameters.Count == 0)
                return $"[{tag}]";

            var parts = new List<string>(parameters.Count);
            foreach (var parameter in parameters)
            {
                if (parameter.Value == null)
                    continue;

                var name = NormalizeName(parameter.Name, "BBCode parameter");
                parts.Add($"{name}={FormatValue(parameter.Value)}");
            }

            return parts.Count == 0
                ? $"[{tag}]"
                : $"[{tag} {string.Join(' ', parts)}]";
        }

        private static string FormatValue(object value)
        {
            return value switch
            {
                bool b => b ? "true" : "false",
                byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                    => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
                Color color => FormatColor(color),
                _ => FormatString(value.ToString() ?? string.Empty),
            };
        }

        private static string FormatColor(Color color)
        {
            var r = ToByte(color.R);
            var g = ToByte(color.G);
            var b = ToByte(color.B);
            var a = ToByte(color.A);
            return a == 255
                ? $"#{r:X2}{g:X2}{b:X2}"
                : $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        private static int ToByte(float value)
        {
            return Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static string FormatString(string value)
        {
            if (value.Length > 0 && value.All(IsBareValueChar))
                return value;

            return "\"" + value
                            .Replace("\\", @"\\", StringComparison.Ordinal)
                            .Replace("\"", "\\\"", StringComparison.Ordinal)
                            .Replace("]", "\\]", StringComparison.Ordinal)
                        + "\"";
        }

        private static bool IsBareValueChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch is '_' or '-' or ':' or '.' or '#' or '+';
        }
    }
}
