using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     JSON Patch (RFC 6902) implementation for <see cref="JsonNode" /> DOM.
    ///     https://www.rfc-editor.org/rfc/rfc6902
    /// </summary>
    public static class JsonPatch
    {
        /// <summary>
        ///     Applies a JSON Patch (RFC 6902) document to <paramref name="target" /> and returns the patched result.
        ///     The patch document must be a JSON array of operation objects.
        /// </summary>
        /// <exception cref="JsonPatchException">Thrown when the patch document is malformed or cannot be applied.</exception>
        public static JsonNode? Apply(JsonNode? target, JsonNode? patchDocument)
        {
            if (patchDocument == null)
                return target?.DeepClone();

            return patchDocument is not JsonArray arr
                ? throw new JsonPatchException("JSON Patch document must be an array.")
                : Apply(target, ParseOperations(arr));
        }

        /// <summary>
        ///     Applies a JSON Patch document to <paramref name="target" /> and returns the patched result.
        /// </summary>
        /// <exception cref="JsonPatchException">Thrown when an operation cannot be applied.</exception>
        public static JsonNode? Apply(JsonNode? target, IEnumerable<JsonPatchOperation> operations)
        {
            ArgumentNullException.ThrowIfNull(operations);

            var root = target?.DeepClone();
            return operations.Aggregate(root, ApplyOne);
        }

        private static JsonNode? ApplyOne(JsonNode? root, JsonPatchOperation op)
        {
            var operation = (op.Op ?? "").Trim().ToLowerInvariant();
            var path = JsonPointer.Normalize(op.Path ?? "");

            switch (operation)
            {
                case "add":
                    return Add(root, path, op.Value);
                case "remove":
                    return Remove(root, path);
                case "replace":
                    return Replace(root, path, op.Value);
                case "move":
                    return Move(root, path, op.From);
                case "copy":
                    return Copy(root, path, op.From);
                case "test":
                    Test(root, path, op.Value);
                    return root;
                default:
                    throw new JsonPatchException($"Unsupported JSON Patch operation: '{op.Op}'.");
            }
        }

        private static JsonNode? Add(JsonNode? root, string path, JsonNode? value)
        {
            if (JsonPointer.IsRoot(path))
                return value?.DeepClone();

            var (parent, segment) = ResolveParent(root, path, true);
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
                    if (!int.TryParse(segment, out var idx) || idx < 0 || idx > arr.Count)
                        throw new JsonPatchException($"Invalid array index for add: '{segment}'.");

                    arr.Insert(idx, value?.DeepClone());
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot add at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Remove(JsonNode? root, string path)
        {
            if (JsonPointer.IsRoot(path))
                return null;

            var (parent, segment) = ResolveParent(root, path, false);
            switch (parent)
            {
                case JsonObject obj when !obj.Remove(segment):
                    throw new JsonPatchException($"Path not found for remove: '{path}'.");
                case JsonObject:
                    return root;
                case JsonArray arr:
                {
                    if (!int.TryParse(segment, out var idx) || idx < 0 || idx >= arr.Count)
                        throw new JsonPatchException($"Invalid array index for remove: '{segment}'.");

                    arr.RemoveAt(idx);
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot remove at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Replace(JsonNode? root, string path, JsonNode? value)
        {
            if (JsonPointer.IsRoot(path))
                return value?.DeepClone();

            var (parent, segment) = ResolveParent(root, path, false);
            switch (parent)
            {
                case JsonObject obj when !obj.ContainsKey(segment):
                    throw new JsonPatchException($"Path not found for replace: '{path}'.");
                case JsonObject obj:
                    obj[segment] = value?.DeepClone();
                    return root;
                case JsonArray arr:
                {
                    if (!int.TryParse(segment, out var idx) || idx < 0 || idx >= arr.Count)
                        throw new JsonPatchException($"Invalid array index for replace: '{segment}'.");

                    arr[idx] = value?.DeepClone();
                    return root;
                }
                default:
                    throw new JsonPatchException($"Cannot replace at path '{path}': parent is not a container.");
            }
        }

        private static JsonNode? Move(JsonNode? root, string path, string? fromRaw)
        {
            if (string.IsNullOrWhiteSpace(fromRaw))
                throw new JsonPatchException("Missing 'from' for move operation.");

            var from = JsonPointer.Normalize(fromRaw);
            var source = GetRequired(root, from)?.DeepClone();
            root = Remove(root, from);
            return Add(root, path, source);
        }

        private static JsonNode? Copy(JsonNode? root, string path, string? fromRaw)
        {
            if (string.IsNullOrWhiteSpace(fromRaw))
                throw new JsonPatchException("Missing 'from' for copy operation.");

            var from = JsonPointer.Normalize(fromRaw);
            var source = GetRequired(root, from)?.DeepClone();
            return Add(root, path, source);
        }

        private static void Test(JsonNode? root, string path, JsonNode? expected)
        {
            var actual = JsonPointer.IsRoot(path) ? root : JsonPointer.Get(root ?? new JsonObject(), path);
            if (!JsonNode.DeepEquals(actual, expected))
                throw new JsonPatchException($"Test operation failed at '{path}'.");
        }

        private static JsonNode GetRequired(JsonNode? root, string path)
        {
            var n = JsonPointer.IsRoot(path) ? root : JsonPointer.Get(root ?? new JsonObject(), path);
            return n ?? throw new JsonPatchException($"Path not found: '{path}'.");
        }

        private static (JsonNode parent, string segment) ResolveParent(JsonNode? root, string path,
            bool createContainers)
        {
            var normalized = JsonPointer.Normalize(path);
            var segments = JsonPointer.EnumerateSegments(normalized).ToArray();
            if (segments.Length == 0)
                throw new JsonPatchException($"Invalid path: '{path}'.");

            root ??= new JsonObject();

            var current = root;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var seg = segments[i];
                var nextSeg = segments[i + 1];

                switch (current)
                {
                    case JsonObject obj:
                    {
                        if (obj.TryGetPropertyValue(seg, out var child) && child != null)
                        {
                            current = child;
                            break;
                        }

                        if (!createContainers)
                            throw new JsonPatchException($"Path not found: '{path}'.");

                        JsonNode created = int.TryParse(nextSeg, out _) || nextSeg == "-"
                            ? new JsonArray()
                            : new JsonObject();
                        obj[seg] = created;
                        current = created;
                        break;
                    }
                    case JsonArray arr:
                    {
                        if (!int.TryParse(seg, out var idx) || idx < 0)
                            throw new JsonPatchException($"Invalid array index: '{seg}'.");

                        while (createContainers && arr.Count <= idx)
                            arr.Add(null);

                        var child = idx < arr.Count ? arr[idx] : null;
                        if (child != null)
                        {
                            current = child;
                            break;
                        }

                        if (!createContainers)
                            throw new JsonPatchException($"Path not found: '{path}'.");

                        JsonNode created = int.TryParse(nextSeg, out _) || nextSeg == "-"
                            ? new JsonArray()
                            : new JsonObject();
                        if (idx >= arr.Count)
                            throw new JsonPatchException($"Invalid array index: '{seg}'.");

                        arr[idx] = created;
                        current = created;
                        break;
                    }
                    default:
                        throw new JsonPatchException(
                            $"Cannot traverse path '{path}': encountered a non-container node.");
                }
            }

            return (current, segments[^1]);
        }

        private static IEnumerable<JsonPatchOperation> ParseOperations(JsonArray arr)
        {
            foreach (var node in arr)
            {
                if (node is not JsonObject o)
                    throw new JsonPatchException("JSON Patch array elements must be objects.");

                var op = ReadRequiredString(o, "op");
                var path = ReadRequiredString(o, "path");
                var from = ReadOptionalString(o, "from");
                o.TryGetPropertyValue("value", out var value);
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
            catch
            {
                throw new JsonPatchException($"Member '{key}' must be a string.");
            }
        }

        private static string? ReadOptionalString(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out var n) || n == null)
                return null;

            if (n is not JsonValue v)
                throw new JsonPatchException($"Member '{key}' must be a string when present.");

            try
            {
                return v.GetValue<string>();
            }
            catch
            {
                throw new JsonPatchException($"Member '{key}' must be a string when present.");
            }
        }
    }

    /// <summary>
    ///     JSON Patch operation object (RFC 6902).
    ///     https://www.rfc-editor.org/rfc/rfc6902
    /// </summary>
    public sealed record JsonPatchOperation(string Op, string Path, string? From = null, JsonNode? Value = null);

    /// <summary>
    ///     Error raised when a JSON Patch cannot be applied.
    ///     https://www.rfc-editor.org/rfc/rfc6902
    /// </summary>
    public sealed class JsonPatchException : Exception
    {
        /// <summary>
        ///     Creates a JSON Patch exception.
        /// </summary>
        public JsonPatchException(string message) : base(message)
        {
        }
    }
}
