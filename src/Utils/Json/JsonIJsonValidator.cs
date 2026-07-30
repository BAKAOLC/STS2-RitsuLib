using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Validates the DOM-level I-JSON constraints of <see cref="JsonNode" /> values (RFC 7493), including
    ///         Unicode scalar and noncharacter restrictions plus finite IEEE 754 binary64-compatible numbers. See
    ///         <see href="https://www.rfc-editor.org/rfc/rfc7493" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         验证 <see cref="JsonNode" /> 值在 DOM 层面的 I-JSON（RFC 7493）约束，包括 Unicode 标量和非字符限制，
    ///         以及与有限 IEEE 754 binary64 数值的兼容性。参见 <see href="https://www.rfc-editor.org/rfc/rfc7493" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">A DOM cannot reveal the original byte encoding or duplicate object names discarded before construction; validate those properties while parsing the source JSON.</para>
    ///     <para xml:lang="zh-CN">DOM 无法反映原始字节编码，也无法发现构建前已被丢弃的重复对象成员名；这些属性应在解析源 JSON 时验证。</para>
    /// </remarks>
    public static class JsonIJsonValidator
    {
        /// <summary>
        ///     <para xml:lang="en">Attempts to validate the DOM node against the representable I-JSON constraints.</para>
        ///     <para xml:lang="zh-CN">尝试验证 DOM 节点是否符合可在 DOM 中检查的 I-JSON 约束。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> with a null <paramref name="error" /> when validation succeeds; otherwise <see langword="false" /> with a diagnostic.</para>
        ///     <para xml:lang="zh-CN">验证成功时返回 <see langword="true" />，且 <paramref name="error" /> 为 null；否则返回 <see langword="false" /> 并提供诊断。</para>
        /// </returns>
        public static bool TryValidate(JsonNode? node, out string? error)
        {
            error = null;
            try
            {
                return ValidateCore(node, "$", ref error);
            }
            catch (ArgumentException ex)
            {
                error = $"Invalid JSON DOM value: {ex.Message}";
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = $"Invalid JSON DOM value: {ex.Message}";
                return false;
            }
            catch (JsonException ex)
            {
                error = $"Invalid JSON DOM value: {ex.Message}";
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = $"Unsupported JSON DOM value: {ex.Message}";
                return false;
            }
        }

        private static bool ValidateCore(JsonNode? node, string path, ref string? error)
        {
            if (node == null)
                return true;

            switch (node)
            {
                case JsonObject obj:
                    foreach (var (name, value) in obj)
                    {
                        if (!TryValidateUnicode(name, out var invalidCodePoint, out var invalidIndex))
                        {
                            error =
                                $"I-JSON forbids Unicode surrogate and noncharacter code points in property names at {path}; found U+{invalidCodePoint:X4} at UTF-16 index {invalidIndex}.";
                            return false;
                        }

                        if (!ValidateCore(value, path + "/" + EscapePointerSegment(name), ref error))
                            return false;
                    }

                    return true;
                case JsonArray arr:
                    for (var i = 0; i < arr.Count; i++)
                        if (!ValidateCore(arr[i], path + "/" + i, ref error))
                            return false;
                    return true;
                default:
                    var kind = node.GetValueKind();
                    switch (kind)
                    {
                        case JsonValueKind.String:
                            var text = GetStringValue(node);
                            if (TryValidateUnicode(text, out var invalidCodePoint, out var invalidIndex))
                                return true;
                            error =
                                $"I-JSON forbids Unicode surrogate and noncharacter code points in string values at {path}; found U+{invalidCodePoint:X4} at UTF-16 index {invalidIndex}.";
                            return false;
                        case JsonValueKind.Number:
                            return ValidateNumber(node, path, ref error);
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                        case JsonValueKind.Null:
                            return true;
                        default:
                            error = $"Unsupported JSON value kind {kind} at {path}.";
                            return false;
                    }
            }
        }

        private static bool ValidateNumber(JsonNode node, string path, ref string? error)
        {
            if (!TryGetFiniteDouble(node, out _))
            {
                error = $"I-JSON requires a finite IEEE 754 binary64 number at {path}.";
                return false;
            }

            return true;
        }

        private static bool TryGetFiniteDouble(JsonNode node, out double value)
        {
            value = default;

            if (node is not JsonValue jsonValue)
                return false;

            if (jsonValue.TryGetValue<JsonElement>(out var element))
            {
                return element.ValueKind == JsonValueKind.Number &&
                       element.TryGetDouble(out value) &&
                       double.IsFinite(value);
            }

            if (jsonValue.TryGetValue<double>(out value) && !double.IsFinite(value))
                return false;

            if (jsonValue.TryGetValue<float>(out var single) && !float.IsFinite(single))
                return false;

            try
            {
                var raw = node.ToJsonString();
                return double.TryParse(
                           raw,
                           NumberStyles.Float,
                           CultureInfo.InvariantCulture,
                           out value) &&
                       double.IsFinite(value);
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

        private static bool TryValidateUnicode(string value, out int invalidCodePoint, out int invalidIndex)
        {
            invalidCodePoint = default;
            invalidIndex = -1;

            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (char.IsHighSurrogate(ch))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    {
                        invalidCodePoint = ch;
                        invalidIndex = i;
                        return false;
                    }

                    var scalar = char.ConvertToUtf32(ch, value[i + 1]);
                    if (IsNoncharacter(scalar))
                    {
                        invalidCodePoint = scalar;
                        invalidIndex = i;
                        return false;
                    }

                    i++;
                    continue;
                }

                if (char.IsLowSurrogate(ch) || IsNoncharacter(ch))
                {
                    invalidCodePoint = ch;
                    invalidIndex = i;
                    return false;
                }
            }

            return true;
        }

        private static bool IsNoncharacter(int codePoint)
        {
            return codePoint is >= 0xfdd0 and <= 0xfdef || (codePoint & 0xffff) is 0xfffe or 0xffff;
        }

        private static string EscapePointerSegment(string segment)
        {
            return segment.Replace("~", "~0", StringComparison.Ordinal)
                .Replace("/", "~1", StringComparison.Ordinal);
        }
    }
}
