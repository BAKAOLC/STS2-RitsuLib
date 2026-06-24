using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Json;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
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

            var clone = mergePatch.DeepClone() as JsonObject ?? new JsonObject();
            SetNodeAt(documentRoot, jsonPointer, clone);
        }

        internal static void MergePatch7386(JsonObject target, JsonObject patch)
        {
            JsonMergePatch.ApplyInPlace(target, patch);
        }
    }
}
