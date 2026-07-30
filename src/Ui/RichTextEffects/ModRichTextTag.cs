using System.Globalization;
using Godot;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     <para xml:lang="en">Builds BBCode tags for mod rich-text effects.</para>
    ///     <para xml:lang="zh-CN">为 mod 富文本特效构建 BBCode 标签。</para>
    /// </summary>
    public static class ModRichTextTag
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a rich-text tag parameter. Its name is validated when the tag is built.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建富文本标签参数。参数名会在构建标签时进行验证。
        ///     </para>
        /// </summary>
        public static ModRichTextTagParameter Param(string name, object? value)
        {
            return new(name, value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="text" /> in a BBCode tag with the supplied parameters.
        ///         Parameter values are formatted with culture-invariant syntax; <see langword="null" /> values
        ///         are omitted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用带有指定参数的 BBCode 标签包裹 <paramref name="text" />。
        ///         参数值使用与区域性无关的格式；值为 <see langword="null" /> 的参数会被省略。
        ///     </para>
        /// </summary>
        public static string Wrap(string bbcode, string text, params ModRichTextTagParameter[] parameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bbcode);
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(parameters);

            var tag = NormalizeName(bbcode, "BBCode tag");
            var opening = BuildOpeningTag(tag, parameters);
            return $"{opening}{text}[/{tag}]";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps <paramref name="text" /> in a BBCode tag with parameters from an enumerable sequence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用带有可枚举参数序列的 BBCode 标签包裹 <paramref name="text" />。
        ///     </para>
        /// </summary>
        public static string Wrap(string bbcode, string text, IEnumerable<ModRichTextTagParameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            return Wrap(bbcode, text, [.. parameters]);
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
