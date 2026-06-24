using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     RFC 6901 JSON Pointer helpers for <see cref="JsonNode" /> DOM navigation and mutation.
    ///     https://www.rfc-editor.org/rfc/rfc6901
    ///     RFC 6901 JSON Pointer helpers 用于 <see cref="JsonNode" /> DOM navigation 和 mutation.
    ///     https://www.rfc-edit或.或g/rfc/rfc6901
    /// </summary>
    public static class JsonPointer
    {
        /// <summary>
        ///     Checks whether the pointer selects the document root.
        ///     检查指针是否选择文档根。
        /// </summary>
        public static bool IsRoot(string? pointer)
        {
            if (string.IsNullOrEmpty(pointer))
                return true;

            var t = pointer.Trim();
            return t.Length == 0 || t == "/";
        }

        /// <summary>
        ///     Normalizes a JSON Pointer fragment for DOM navigation (leading slash optional when authoring).
        ///     规范化用于 DOM 导航的 JSON Pointer 片段（编写时可省略前导斜杠）。
        /// </summary>
        public static string Normalize(string rawPointer)
        {
            var t = rawPointer.Trim();
            if (t.Length == 0 || t == "/")
                return "/";

            return t.StartsWith('/') ? t : "/" + t;
        }

        /// <summary>
        ///     Resolves a node under <paramref name="root" /> by JSON Pointer, or <c>null</c> when not found.
        ///     通过 JSON Pointer 解析 <paramref name="root" /> 下的节点；未找到时为 <c>null</c>。
        /// </summary>
        public static JsonNode? Get(JsonNode root, string jsonPointer)
        {
            if (IsRoot(jsonPointer))
                return root;

            var current = root;
            foreach (var seg in EnumerateSegments(jsonPointer))
                switch (current)
                {
                    case JsonObject obj:
                    {
                        if (!obj.TryGetPropertyValue(seg, out current))
                            return null;
                        break;
                    }
                    case JsonArray arr when int.TryParse(seg, out var idx) && idx >= 0 && idx < arr.Count:
                        current = arr[idx];
                        break;
                    default:
                        return null;
                }

            return current;
        }

        /// <summary>
        ///     Sets <paramref name="value" /> at <paramref name="jsonPointer" /> under an object root.
        ///     Null removes the property when targeting an object.
        ///     在对象根下的 <paramref name="jsonPointer" /> 位置设置 <paramref name="value" />。
        ///     目标为对象时，null 会移除该属性。
        /// </summary>
        public static void Set(JsonObject documentRoot, string jsonPointer, JsonNode? value)
        {
            if (IsRoot(jsonPointer))
            {
                switch (value)
                {
                    case JsonObject obj:
                    {
                        documentRoot.Clear();
                        foreach (var p in obj)
                            documentRoot[p.Key] = p.Value?.DeepClone();
                        break;
                    }
                    case null:
                        documentRoot.Clear();
                        break;
                }

                return;
            }

            var segments = EnumerateSegments(jsonPointer).ToArray();
            if (segments.Length == 0)
                return;

            JsonNode? parent = documentRoot;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var seg = segments[i];
                parent = EnsureWalk(parent, seg);
                if (parent == null)
                    return;
            }

            var last = segments[^1];
            switch (parent)
            {
                case JsonObject po when value == null:
                    po.Remove(last);
                    break;
                case JsonObject po:
                    po[last] = value.DeepClone();
                    break;
                case JsonArray pa when int.TryParse(last, out var ix):
                {
                    while (pa.Count <= ix)
                        pa.Add(null);

                    pa[ix] = value?.DeepClone();
                    break;
                }
            }
        }

        /// <summary>
        ///     Enumerates decoded JSON Pointer segments.
        ///     枚举已解码的 JSON Pointer 段。
        /// </summary>
        public static IEnumerable<string> EnumerateSegments(string jsonPointer)
        {
            var t = jsonPointer.TrimStart();
            if (t.Length == 0)
                yield break;

            if (t[0] == '/')
                t = t[1..];

            if (t.Length == 0)
                yield break;

            foreach (var seg in t.Split('/'))
                yield return DecodeSegment(seg);
        }

        /// <summary>
        ///     Decodes a JSON Pointer segment (~0 and ~1).
        ///     Decodes a JSON Pointer segment (~0 和 ~1).
        /// </summary>
        public static string DecodeSegment(string segment)
        {
            return segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
        }

        private static JsonNode? EnsureWalk(JsonNode parent, string segment)
        {
            switch (parent)
            {
                case JsonObject o when o.TryGetPropertyValue(segment, out var child) && child != null:
                    return child;
                case JsonObject o:
                {
                    var created = new JsonObject();
                    o[segment] = created;
                    return created;
                }
                case JsonArray a when int.TryParse(segment, out var ix):
                {
                    while (a.Count <= ix)
                        a.Add(null);

                    if (a[ix] is JsonObject jo)
                        return jo;

                    var no = new JsonObject();
                    a[ix] = no;
                    return no;
                }
                default:
                    return null;
            }
        }
    }
}
