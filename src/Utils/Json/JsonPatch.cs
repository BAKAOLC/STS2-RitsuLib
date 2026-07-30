using System.Text;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Implements JSON Patch (RFC 6902) operations for <see cref="JsonNode" /> DOM values.
    ///         See <see href="https://www.rfc-editor.org/rfc/rfc6902" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="JsonNode" /> DOM 值实现 JSON 补丁（RFC 6902）操作。
    ///         参见 <see href="https://www.rfc-editor.org/rfc/rfc6902" />。
    ///     </para>
    /// </summary>
    public static class JsonPatch
    {
        /// <summary>
        ///     <para xml:lang="en">Applies a JSON Patch (RFC 6902) document to <paramref name="target" /> and returns the patched result. The document must be an array of operation objects.</para>
        ///     <para xml:lang="zh-CN">将 JSON 补丁（RFC 6902）文档应用于 <paramref name="target" /> 并返回应用后的结果。该文档必须是操作对象数组。</para>
        /// </summary>
        /// <exception cref="JsonPatchException">
        ///     <para xml:lang="en">Thrown when the patch document is malformed or cannot be applied.</para>
        ///     <para xml:lang="zh-CN">当补丁文档格式错误或无法应用时引发。</para>
        /// </exception>
        public static JsonNode? Apply(JsonNode? target, JsonNode? patchDocument)
        {
            if (patchDocument == null)
                throw new JsonPatchException("JSON Patch document must be an array.");

            return patchDocument is not JsonArray arr
                ? throw new JsonPatchException("JSON Patch document must be an array.")
                : Apply(target, ParseOperations(arr));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies a JSON Patch document to <paramref name="target" /> and returns the patched result.</para>
        ///     <para xml:lang="zh-CN">将 JSON Patch 文档应用于 <paramref name="target" /> 并返回应用后的结果。</para>
        /// </summary>
        /// <exception cref="JsonPatchException">
        ///     <para xml:lang="en">Thrown when an operation cannot be applied.</para>
        ///     <para xml:lang="zh-CN">当某项操作无法应用时引发。</para>
        /// </exception>
        public static JsonNode? Apply(JsonNode? target, IEnumerable<JsonPatchOperation> operations)
        {
            ArgumentNullException.ThrowIfNull(operations);

            var root = target?.DeepClone();
            return operations.Aggregate(root, ApplyOne);
        }

        private static JsonNode? ApplyOne(JsonNode? root, JsonPatchOperation op)
        {
            if (op == null)
                throw new JsonPatchException("JSON Patch operations cannot be null.");

            var operation = op.Op
                            ?? throw new JsonPatchException("Missing required member 'op'.");
            var path = op.Path
                       ?? throw new JsonPatchException("Missing required member 'path'.");
            var segments = ParsePointer(path, "path");

            switch (operation)
            {
                case "add":
                    return Add(root, path, segments, op.Value);
                case "remove":
                    return Remove(root, path, segments);
                case "replace":
                    return Replace(root, path, segments, op.Value);
                case "move":
                    return Move(root, path, segments, op.From);
                case "copy":
                    return Copy(root, path, segments, op.From);
                case "test":
                    Test(root, path, segments, op.Value);
                    return root;
                default:
                    throw new JsonPatchException($"Unsupported JSON Patch operation: '{op.Op}'.");
            }
        }

        private static JsonNode? Add(
            JsonNode? root,
            string path,
            IReadOnlyList<string> segments,
            JsonNode? value)
        {
            if (segments.Count == 0)
                return value?.DeepClone();

            var (parent, segment) = ResolveParent(root, path, segments);
            switch (parent)
            {
                case JsonObject obj:
                    obj[segment] = value?.DeepClone();
                    return root;
                case JsonArray arr when segment == "-":
                    arr.Add(value?.DeepClone());
                    return root;
                case JsonArray arr:
                {
                    if (!TryParseArrayIndex(segment, out var idx) || idx > arr.Count)
                        throw new JsonPatchException($"Invalid array index for add: '{segment}'.");

                    arr.Insert(idx, value?.DeepClone());
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot add at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Remove(JsonNode? root, string path, IReadOnlyList<string> segments)
        {
            if (segments.Count == 0)
                return null;

            var (parent, segment) = ResolveParent(root, path, segments);
            switch (parent)
            {
                case JsonObject obj when !obj.Remove(segment):
                    throw new JsonPatchException($"Path not found for remove: '{path}'.");
                case JsonObject:
                    return root;
                case JsonArray arr:
                {
                    if (!TryParseArrayIndex(segment, out var idx) || idx >= arr.Count)
                        throw new JsonPatchException($"Invalid array index for remove: '{segment}'.");

                    arr.RemoveAt(idx);
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot remove at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Replace(
            JsonNode? root,
            string path,
            IReadOnlyList<string> segments,
            JsonNode? value)
        {
            if (segments.Count == 0)
                return value?.DeepClone();

            var (parent, segment) = ResolveParent(root, path, segments);
            switch (parent)
            {
                case JsonObject obj when !obj.ContainsKey(segment):
                    throw new JsonPatchException($"Path not found for replace: '{path}'.");
                case JsonObject obj:
                    obj[segment] = value?.DeepClone();
                    return root;
                case JsonArray arr:
                {
                    if (!TryParseArrayIndex(segment, out var idx) || idx >= arr.Count)
                        throw new JsonPatchException($"Invalid array index for replace: '{segment}'.");

                    arr[idx] = value?.DeepClone();
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot replace at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Move(
            JsonNode? root,
            string path,
            IReadOnlyList<string> pathSegments,
            string? fromRaw)
        {
            if (fromRaw == null)
                throw new JsonPatchException("Missing 'from' for move operation.");

            var fromSegments = ParsePointer(fromRaw, "from");
            if (IsProperPrefix(fromSegments, pathSegments))
                throw new JsonPatchException("The 'path' of a move operation cannot be a child of its 'from' path.");

            var source = GetRequired(root, fromRaw, fromSegments)?.DeepClone();
            root = Remove(root, fromRaw, fromSegments);
            return Add(root, path, pathSegments, source);
        }

        private static JsonNode? Copy(
            JsonNode? root,
            string path,
            IReadOnlyList<string> pathSegments,
            string? fromRaw)
        {
            if (fromRaw == null)
                throw new JsonPatchException("Missing 'from' for copy operation.");

            var fromSegments = ParsePointer(fromRaw, "from");
            var source = GetRequired(root, fromRaw, fromSegments)?.DeepClone();
            return Add(root, path, pathSegments, source);
        }

        private static void Test(
            JsonNode? root,
            string path,
            IReadOnlyList<string> segments,
            JsonNode? expected)
        {
            if (!TryGetAtPath(root, segments, out var actual) || !JsonNode.DeepEquals(actual, expected))
                throw new JsonPatchException($"Test operation failed at '{path}'.");
        }

        private static JsonNode? GetRequired(
            JsonNode? root,
            string path,
            IReadOnlyList<string> segments)
        {
            return TryGetAtPath(root, segments, out var value)
                ? value
                : throw new JsonPatchException($"Path not found: '{path}'.");
        }

        private static (JsonNode parent, string segment) ResolveParent(
            JsonNode? root,
            string path,
            IReadOnlyList<string> segments)
        {
            if (segments.Count == 0)
                throw new JsonPatchException($"Invalid path: '{path}'.");

            var current = root
                          ?? throw new JsonPatchException(
                              $"Cannot traverse path '{path}': encountered a non-container node.");
            for (var i = 0; i < segments.Count - 1; i++)
            {
                var seg = segments[i];

                switch (current)
                {
                    case JsonObject obj when obj.TryGetPropertyValue(seg, out var child):
                    {
                        current = child
                                  ?? throw new JsonPatchException(
                                      $"Cannot traverse path '{path}': encountered a non-container node.");
                        break;
                    }
                    case JsonObject:
                        throw new JsonPatchException($"Path not found: '{path}'.");
                    case JsonArray arr:
                    {
                        if (!TryParseArrayIndex(seg, out var idx) || idx >= arr.Count)
                            throw new JsonPatchException($"Invalid array index: '{seg}'.");

                        current = arr[idx]
                                  ?? throw new JsonPatchException(
                                      $"Cannot traverse path '{path}': encountered a non-container node.");
                        break;
                    }
                    default:
                        throw new JsonPatchException(
                            $"Cannot traverse path '{path}': encountered a non-container node.");
                }
            }

            return (current, segments[^1]);
        }

        private static bool TryGetAtPath(
            JsonNode? root,
            IReadOnlyList<string> segments,
            out JsonNode? value)
        {
            value = root;
            foreach (var segment in segments)
            {
                switch (value)
                {
                    case JsonObject obj when obj.TryGetPropertyValue(segment, out var child):
                        value = child;
                        break;
                    case JsonArray arr
                        when TryParseArrayIndex(segment, out var index) && index < arr.Count:
                        value = arr[index];
                        break;
                    default:
                        value = null;
                        return false;
                }
            }

            return true;
        }

        private static string[] ParsePointer(string pointer, string memberName)
        {
            if (pointer.Length == 0)
                return [];
            if (pointer[0] != '/')
                throw new JsonPatchException(
                    $"Member '{memberName}' must be an RFC 6901 JSON Pointer.");

            return pointer[1..]
                .Split('/')
                .Select(segment => DecodePointerSegment(segment, memberName))
                .ToArray();
        }

        private static string DecodePointerSegment(string segment, string memberName)
        {
            if (!segment.Contains('~'))
                return segment;

            var decoded = new StringBuilder(segment.Length);
            for (var i = 0; i < segment.Length; i++)
            {
                var current = segment[i];
                if (current != '~')
                {
                    decoded.Append(current);
                    continue;
                }

                if (++i >= segment.Length)
                    throw new JsonPatchException(
                        $"Member '{memberName}' contains an invalid JSON Pointer escape.");

                decoded.Append(segment[i] switch
                {
                    '0' => '~',
                    '1' => '/',
                    _ => throw new JsonPatchException(
                        $"Member '{memberName}' contains an invalid JSON Pointer escape."),
                });
            }

            return decoded.ToString();
        }

        private static bool TryParseArrayIndex(string segment, out int index)
        {
            index = 0;
            if (segment.Length == 0 || segment.Length > 1 && segment[0] == '0')
                return false;

            foreach (var character in segment)
            {
                if (character is < '0' or > '9')
                    return false;

                var digit = character - '0';
                if (index > (int.MaxValue - digit) / 10)
                    return false;

                index = index * 10 + digit;
            }

            return true;
        }

        private static bool IsProperPrefix(
            IReadOnlyList<string> candidate,
            IReadOnlyList<string> path)
        {
            if (candidate.Count >= path.Count)
                return false;

            for (var i = 0; i < candidate.Count; i++)
                if (!string.Equals(candidate[i], path[i], StringComparison.Ordinal))
                    return false;

            return true;
        }

        private static IEnumerable<JsonPatchOperation> ParseOperations(JsonArray arr)
        {
            foreach (var node in arr)
            {
                if (node is not JsonObject o)
                    throw new JsonPatchException("JSON Patch array elements must be objects.");

                var op = ReadRequiredString(o, "op");
                var path = ReadRequiredString(o, "path");
                string? from = null;
                JsonNode? value = null;
                switch (op)
                {
                    case "add":
                    case "replace":
                    case "test":
                        if (!o.TryGetPropertyValue("value", out value))
                            throw new JsonPatchException($"Missing required member 'value' for '{op}' operation.");
                        break;
                    case "move":
                    case "copy":
                        from = ReadRequiredString(o, "from");
                        break;
                }

                yield return new(op, path, from, value?.DeepClone());
            }
        }

        private static string ReadRequiredString(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out var n) || n is not JsonValue v)
                throw new JsonPatchException($"Missing required member '{key}'.");

            try
            {
                return v.GetValue<string>();
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                throw new JsonPatchException($"Member '{key}' must be a string.");
            }
        }

    }

    /// <summary>
    ///     <para xml:lang="en">Represents one JSON Patch operation object (RFC 6902).</para>
    ///     <para xml:lang="zh-CN">表示一个 JSON 补丁操作对象（RFC 6902）。</para>
    /// </summary>
    public sealed record JsonPatchOperation(string Op, string Path, string? From = null, JsonNode? Value = null);

    /// <summary>
    ///     <para xml:lang="en">Represents an error raised when a JSON Patch cannot be applied.</para>
    ///     <para xml:lang="zh-CN">表示无法应用 JSON 补丁时引发的错误。</para>
    /// </summary>
    public sealed class JsonPatchException : Exception
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a JSON Patch exception.</para>
        ///     <para xml:lang="zh-CN">创建 JSON Patch 异常。</para>
        /// </summary>
        public JsonPatchException(string message) : base(message)
        {
        }
    }
}
