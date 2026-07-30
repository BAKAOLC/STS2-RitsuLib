using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     JSON Canonicalization Scheme (JCS, RFC 8785) for <see cref="JsonNode" /> DOM.
    ///     https://www.rfc-editor.org/rfc/rfc8785
    ///     JSON Canonicalization Scheme (JCS, RFC 8785) 用于 <see cref="JsonNode" /> DOM.
    ///     https://www.rfc-edit或.或g/rfc/rfc8785
    /// </summary>
    public static class JsonCanonicalizer
    {
        /// <summary>
        ///     Canonicalizes a JSON DOM node into a deterministic UTF-16 string representation.
        ///     将 JSON DOM 节点规范化为确定性的 UTF-16 字符串表示。
        /// </summary>
        public static string Canonicalize(JsonNode? node)
        {
            var output = new StringBuilder();
            WriteCanonical(output, node);
            return output.ToString();
        }

        private static void WriteCanonical(StringBuilder output, JsonNode? node)
        {
            if (node == null)
            {
                output.Append("null");
                return;
            }

            switch (node)
            {
                case JsonObject obj:
                    output.Append('{');
                    var firstProperty = true;
                    foreach (var (name, value) in obj.OrderBy(static p => p.Key, Utf16CodeUnitComparer.Instance))
                    {
                        if (!firstProperty)
                            output.Append(',');
                        firstProperty = false;
                        WriteString(output, name);
                        output.Append(':');
                        WriteCanonical(output, value);
                    }

                    output.Append('}');
                    return;
                case JsonArray arr:
                    output.Append('[');
                    for (var i = 0; i < arr.Count; i++)
                    {
                        if (i > 0)
                            output.Append(',');
                        WriteCanonical(output, arr[i]);
                    }

                    output.Append(']');
                    return;
                default:
                    WritePrimitiveCanonical(output, node);
                    return;
            }
        }

        private static void WritePrimitiveCanonical(StringBuilder output, JsonNode node)
        {
            switch (node.GetValueKind())
            {
                case JsonValueKind.String:
                    WriteString(output, GetStringValue(node));
                    return;
                case JsonValueKind.Number:
                    if (!TryGetFiniteDouble(node, out var number))
                        throw new InvalidOperationException("JCS requires every number to be a finite IEEE 754 binary64 value.");
                    output.Append(FormatFiniteNumber(number));
                    return;
                case JsonValueKind.True:
                    output.Append("true");
                    return;
                case JsonValueKind.False:
                    output.Append("false");
                    return;
                case JsonValueKind.Null:
                    output.Append("null");
                    return;
                default:
                    throw new InvalidOperationException(
                        $"JCS cannot serialize a JSON value of kind {node.GetValueKind()}.");
            }
        }

        private static string GetStringValue(JsonNode node)
        {
            if (node is JsonValue value && value.TryGetValue<string>(out var text))
                return text;

            var element = JsonSerializer.SerializeToElement(node);
            return element.ValueKind == JsonValueKind.String
                ? element.GetString()!
                : throw new InvalidOperationException("The JSON DOM value did not serialize as a string.");
        }

        private static void WriteString(StringBuilder output, string value)
        {
            output.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                switch (ch)
                {
                    case '"':
                        output.Append("\\\"");
                        continue;
                    case '\\':
                        output.Append("\\\\");
                        continue;
                    case '\b':
                        output.Append("\\b");
                        continue;
                    case '\t':
                        output.Append("\\t");
                        continue;
                    case '\n':
                        output.Append("\\n");
                        continue;
                    case '\f':
                        output.Append("\\f");
                        continue;
                    case '\r':
                        output.Append("\\r");
                        continue;
                }

                if (ch <= '\u001f')
                {
                    output.Append("\\u00");
                    output.Append(ToLowerHex(ch >> 4));
                    output.Append(ToLowerHex(ch & 0xf));
                    continue;
                }

                if (char.IsHighSurrogate(ch))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw InvalidUnicode(ch, i);

                    var low = value[++i];
                    var scalar = char.ConvertToUtf32(ch, low);
                    if (IsNoncharacter(scalar))
                        throw InvalidUnicode(scalar, i - 1);

                    output.Append(ch);
                    output.Append(low);
                    continue;
                }

                if (char.IsLowSurrogate(ch) || IsNoncharacter(ch))
                    throw InvalidUnicode(ch, i);

                output.Append(ch);
            }

            output.Append('"');
        }

        private static InvalidOperationException InvalidUnicode(int codePoint, int index)
        {
            return new(
                $"JCS forbids Unicode surrogate and noncharacter code points; found U+{codePoint:X4} at UTF-16 index {index}.");
        }

        private static char ToLowerHex(int value)
        {
            return (char)(value < 10 ? '0' + value : 'a' + value - 10);
        }

        private static bool IsNoncharacter(int codePoint)
        {
            return codePoint is >= 0xfdd0 and <= 0xfdef || (codePoint & 0xffff) is 0xfffe or 0xffff;
        }

        private static bool TryGetFiniteDouble(JsonNode node, out double value)
        {
            value = default;
            if (node is not JsonValue jsonValue)
                return false;

            if (jsonValue.TryGetValue<JsonElement>(out var element))
                return element.ValueKind == JsonValueKind.Number &&
                       element.TryGetDouble(out value) &&
                       double.IsFinite(value);

            if (jsonValue.TryGetValue<double>(out value))
                return double.IsFinite(value);

            if (jsonValue.TryGetValue<float>(out var single))
            {
                value = single;
                return float.IsFinite(single);
            }

            string raw;
            try
            {
                raw = node.ToJsonString();
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }

            return double.TryParse(
                       raw,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   double.IsFinite(value);
        }

        internal static string FormatFiniteNumber(double value)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "The number must be finite.");

            if (value == 0)
                return "0";

            var roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
            var negative = roundTrip[0] == '-';
            var offset = negative ? 1 : 0;
            var exponentMarker = roundTrip.IndexOfAny(['E', 'e'], offset);
            var mantissaEnd = exponentMarker < 0 ? roundTrip.Length : exponentMarker;
            var explicitExponent = exponentMarker < 0
                ? 0
                : int.Parse(
                    roundTrip.AsSpan(exponentMarker + 1),
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture);
            var decimalPoint = roundTrip.IndexOf('.', offset, mantissaEnd - offset);
            var digitsBeforePoint = decimalPoint < 0 ? mantissaEnd - offset : decimalPoint - offset;

            var digits = new StringBuilder(mantissaEnd - offset);
            for (var i = offset; i < mantissaEnd; i++)
                if (roundTrip[i] != '.')
                    digits.Append(roundTrip[i]);

            var leadingZeroCount = 0;
            while (leadingZeroCount < digits.Length - 1 && digits[leadingZeroCount] == '0')
                leadingZeroCount++;
            if (leadingZeroCount > 0)
            {
                digits.Remove(0, leadingZeroCount);
                digitsBeforePoint -= leadingZeroCount;
            }

            while (digits.Length > 1 && digits[^1] == '0')
                digits.Length--;

            var n = digitsBeforePoint + explicitExponent;
            var output = new StringBuilder(digits.Length + 16);
            if (negative)
                output.Append('-');

            if (n is > 0 and <= 21)
            {
                if (digits.Length <= n)
                {
                    output.Append(digits);
                    output.Append('0', n - digits.Length);
                }
                else
                {
                    output.Append(digits, 0, n);
                    output.Append('.');
                    output.Append(digits, n, digits.Length - n);
                }
            }
            else if (n is > -6 and <= 0)
            {
                output.Append("0.");
                output.Append('0', -n);
                output.Append(digits);
            }
            else
            {
                output.Append(digits[0]);
                if (digits.Length > 1)
                {
                    output.Append('.');
                    output.Append(digits, 1, digits.Length - 1);
                }

                var exponent = n - 1;
                output.Append('e');
                if (exponent >= 0)
                    output.Append('+');
                output.Append(exponent.ToString(CultureInfo.InvariantCulture));
            }

            return output.ToString();
        }

        private sealed class Utf16CodeUnitComparer : IComparer<string>
        {
            public static Utf16CodeUnitComparer Instance { get; } = new();

            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y))
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;

                var commonLength = Math.Min(x.Length, y.Length);
                for (var i = 0; i < commonLength; i++)
                {
                    var comparison = x[i].CompareTo(y[i]);
                    if (comparison != 0)
                        return comparison;
                }

                return x.Length.CompareTo(y.Length);
            }
        }
    }
}
