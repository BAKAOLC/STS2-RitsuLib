using System.Text.Json;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Json
{
    /// <summary>
    ///     <para xml:lang="en">Provides RFC 7386 JSON Merge Patch operations for <see cref="JsonNode" /> DOM values.</para>
    ///     <para xml:lang="zh-CN">为 <see cref="JsonNode" /> DOM 值提供 RFC 7386 JSON 合并补丁操作。</para>
    /// </summary>
    public static class JsonMergePatch
    {
        /// <summary>
        ///     <para xml:lang="en">Applies an RFC 7386 merge patch to <paramref name="target" /> and returns the merged result. A non-object <paramref name="patch" /> replaces the target.</para>
        ///     <para xml:lang="zh-CN">将 RFC 7386 合并补丁应用于 <paramref name="target" /> 并返回合并结果。非对象的 <paramref name="patch" /> 会替换目标。</para>
        /// </summary>
        public static JsonNode? Apply(JsonNode? target, JsonNode? patch)
        {
            if (!TryGetObject(patch, out var patchObj))
                return IsJsonNull(patch) ? null : patch?.DeepClone();

            var output = TryGetObject(target, out var targetObj)
                ? targetObj.DeepClone() as JsonObject ?? new JsonObject()
                : new();
            ApplyInPlace(output, patchObj);
            return output;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies an RFC 7386 merge patch to <paramref name="target" /> in place.</para>
        ///     <para xml:lang="zh-CN">将 RFC 7386 合并补丁原地应用于 <paramref name="target" />。</para>
        /// </summary>
        public static void ApplyInPlace(JsonObject target, JsonObject patch)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(patch);

            foreach (var kv in patch)
            {
                if (IsJsonNull(kv.Value))
                {
                    target.Remove(kv.Key);
                    continue;
                }

                if (TryGetObject(kv.Value, out var patchChild))
                {
                    JsonObject targetChild;
                    if (target.TryGetPropertyValue(kv.Key, out var existing) &&
                        TryGetObject(existing, out var existingObj))
                        targetChild = existingObj;
                    else
                    {
                        targetChild = new();
                        target[kv.Key] = targetChild;
                    }

                    ApplyInPlace(targetChild, patchChild);
                    continue;
                }

                target[kv.Key] = kv.Value!.DeepClone();
            }
        }

        private static bool TryGetObject(JsonNode? node, out JsonObject obj)
        {
            obj = node as JsonObject ?? null!;
            return node is JsonObject;
        }

        private static bool IsJsonNull(JsonNode? node)
        {
            if (node == null)
                return true;

            return node.GetValueKind() == JsonValueKind.Null;
        }
    }
}
