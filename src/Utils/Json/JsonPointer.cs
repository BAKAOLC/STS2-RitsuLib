using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides RFC 6901 JSON Pointer helpers for <see cref="JsonNode" /> DOM navigation and mutation.
    ///         The empty string selects the document root; <c>/</c> selects an object member whose key is empty.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="JsonNode" /> DOM 导航和修改提供 RFC 6901 JSON 指针辅助方法。空字符串选择文档根；
    ///         <c>/</c> 选择键为空字符串的对象成员。
    ///     </para>
    /// </summary>
    public static class JsonPointer
    {
        /// <summary>
        ///     <para xml:lang="en">Checks whether the pointer is empty and therefore selects the document root.</para>
        ///     <para xml:lang="zh-CN">检查指针是否为空，从而选择文档根。</para>
        /// </summary>
        public static bool IsRoot(string? pointer)
        {
            if (string.IsNullOrEmpty(pointer))
                return true;

            var t = pointer.Trim();
            return t.Length == 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes a JSON Pointer fragment for DOM navigation. Empty input remains empty; authors may
        ///         omit the leading slash for non-root pointers.
        ///     </para>
        ///     <para xml:lang="zh-CN">规范化用于 DOM 导航的 JSON 指针片段。空输入保持为空；非根指针编写时可省略前导斜杠。</para>
        /// </summary>
        public static string Normalize(string rawPointer)
        {
            var t = rawPointer.Trim();
            if (t.Length == 0)
                return string.Empty;

            return t.StartsWith('/') ? t : "/" + t;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a node under <paramref name="root" /> by JSON Pointer, or returns
        ///         <see langword="null" /> when no node exists at the pointer.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过 JSON 指针解析 <paramref name="root" /> 下的节点；该指针位置不存在节点时返回 <see langword="null" />。</para>
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
        ///     <para xml:lang="en">
        ///         Sets <paramref name="value" /> at <paramref name="jsonPointer" /> under an object root. A
        ///         <see langword="null" /> value removes a targeted object property; the empty pointer replaces or
        ///         clears the root object's members.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在对象根下的 <paramref name="jsonPointer" /> 位置设置 <paramref name="value" />。
        ///         <see langword="null" /> 值会移除目标对象属性；空指针会替换或清空根对象的成员。
        ///     </para>
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
        ///     <para xml:lang="en">Enumerates decoded JSON Pointer segments. In particular, <c>/</c> yields one empty segment.</para>
        ///     <para xml:lang="zh-CN">枚举已解码的 JSON 指针段。特别地，<c>/</c> 会产生一个空段。</para>
        /// </summary>
        public static IEnumerable<string> EnumerateSegments(string jsonPointer)
        {
            var t = jsonPointer.TrimStart();
            if (t.Length == 0)
                yield break;

            if (t[0] == '/')
                t = t[1..];

            foreach (var seg in t.Split('/'))
                yield return DecodeSegment(seg);
        }

        /// <summary>
        ///     <para xml:lang="en">Decodes the RFC 6901 <c>~0</c> and <c>~1</c> segment escapes.</para>
        ///     <para xml:lang="zh-CN">解码 RFC 6901 的 <c>~0</c> 和 <c>~1</c> 段转义。</para>
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
