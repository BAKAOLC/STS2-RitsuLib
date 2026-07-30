using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Json;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides JSON Pointer and JSON Merge Patch primitives used by runtime mod-data
    ///         interoperability.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供运行时模组数据互操作所用的 JSON 指针和 JSON 合并补丁基础操作。</para>
    /// </summary>
    internal static class ModDataJsonInteropPrimitives
    {
        internal static bool IsRootPointer(string? pointer)
        {
            return JsonPointer.IsRoot(pointer);
        }

        internal static JsonNode? GetNodeAt(JsonNode root, string jsonPointer)
        {
            return JsonPointer.Get(root, jsonPointer);
        }

        internal static void SetNodeAt(JsonObject documentRoot, string jsonPointer, JsonNode? value)
        {
            JsonPointer.Set(documentRoot, jsonPointer, value);
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

            var merged = new JsonObject();
            MergePatch7386(merged, mergePatch);
            SetNodeAt(documentRoot, jsonPointer, merged);
        }

        internal static void MergePatch7386(JsonObject target, JsonObject patch)
        {
            JsonMergePatch.ApplyInPlace(target, patch);
        }
    }
}
