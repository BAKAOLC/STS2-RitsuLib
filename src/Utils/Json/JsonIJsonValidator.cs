using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     I-JSON (RFC 7493) validator for <see cref="JsonNode" /> DOM.
    ///     I-JSON is a restricted profile of JSON for interoperable exchanges.
    ///     https://www.rfc-editor.org/rfc/rfc7493
    ///     面向 <see cref="JsonNode" /> DOM 的 I-JSON（RFC 7493）验证器。
    ///     I-JSON 是用于互操作交换的受限 JSON 配置文件。
    /// </summary>
    public static class JsonIJsonValidator
    {
        /// <summary>
        ///     Validates that the DOM node conforms to I-JSON constraints.
        ///     验证 DOM 节点是否符合 I-JSON 约束。
        ///     验证 DOM 节点是否符合 I-JSON 约束。
        /// </summary>
        public static bool TryValidate(JsonNode? node, out string? error)
        {
            error = null;
            return ValidateCore(node, "$", ref error);
        }

        private static bool ValidateCore(JsonNode? node, string path, ref string? error)
        {
            if (node == null)
                return true;

            switch (node)
            {
                case JsonObject obj:
                    foreach (var kv in obj)
                        if (!ValidateCore(kv.Value, path + "/" + kv.Key, ref error))
                            return false;
                    return true;
                case JsonArray arr:
                    for (var i = 0; i < arr.Count; i++)
                        if (!ValidateCore(arr[i], path + "/" + i, ref error))
                            return false;
                    return true;
                default:
                    var kind = node.GetValueKind();
                    if (kind != JsonValueKind.Number)
                        return true;

                    // I-JSON: numbers must be IEEE 754 binary64 finite values.
                    try
                    {
                        var el = JsonSerializer.SerializeToElement(node);
                        if (!el.TryGetDouble(out var d))
                        {
                            error = $"I-JSON requires numbers to be IEEE 754 binary64 (double) at {path}.";
                            return false;
                        }

                        if (!double.IsNaN(d) && !double.IsInfinity(d)) return true;
                        error = $"I-JSON forbids NaN/Infinity at {path}.";
                        return false;
                    }
                    catch
                    {
                        error = $"Invalid number at {path}.";
                        return false;
                    }
            }
        }
    }
}
