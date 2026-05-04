using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    internal static class ModDataJsonInteropPrimitives
    {
        internal static bool IsRootPointer(string? pointer)
        {
            if (string.IsNullOrEmpty(pointer))
                return true;

            var t = pointer.Trim();
            return t.Length == 0 || t == "/";
        }

        internal static JsonNode? GetNodeAt(JsonNode root, string jsonPointer)
        {
            if (IsRootPointer(jsonPointer))
                return root;

            var current = root;
            foreach (var seg in EnumeratePointerSegments(jsonPointer))
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

        internal static void SetNodeAt(JsonObject documentRoot, string jsonPointer, JsonNode? value)
        {
            if (IsRootPointer(jsonPointer))
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

            var segments = EnumeratePointerSegments(jsonPointer).ToArray();
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

        internal static void MergeObjectAt(JsonObject documentRoot, string jsonPointer, JsonObject mergePatch)
        {
            if (IsRootPointer(jsonPointer))
            {
                MergePatch7386(documentRoot, mergePatch);
                return;
            }

            var target = GetNodeAt(documentRoot, jsonPointer);
            if (target is JsonObject existing)
            {
                MergePatch7386(existing, mergePatch);
                return;
            }

            var clone = mergePatch.DeepClone() as JsonObject ?? new JsonObject();
            SetNodeAt(documentRoot, jsonPointer, clone);
        }

        internal static void MergePatch7386(JsonObject target, JsonObject patch)
        {
            foreach (var kv in patch)
            {
                if (IsJsonNull(kv.Value))
                {
                    target.Remove(kv.Key);
                    continue;
                }

                if (kv.Value is JsonObject patchObj && target.TryGetPropertyValue(kv.Key, out var tn) &&
                    tn is JsonObject existingObj)
                {
                    MergePatch7386(existingObj, patchObj);
                    continue;
                }

                target[kv.Key] = kv.Value!.DeepClone();
            }
        }

        private static bool IsJsonNull(JsonNode? node)
        {
            if (node == null)
                return true;

            return node.GetValueKind() == JsonValueKind.Null;
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

        private static IEnumerable<string> EnumeratePointerSegments(string jsonPointer)
        {
            var t = jsonPointer.TrimStart();
            if (t.Length == 0)
                yield break;

            if (t[0] == '/')
                t = t[1..];

            if (t.Length == 0)
                yield break;

            foreach (var seg in t.Split('/'))
                yield return DecodePointerSegment(seg);
        }

        private static string DecodePointerSegment(string segment)
        {
            return segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
        }
    }
}
