using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Deep-merges DTFM token trees while preserving leaf semantics.
    ///     深度合并 DTFM 令牌树，同时保留叶节点语义。
    /// </summary>
    /// <remarks>
    ///     Group nodes (objects without <c>$value</c>) are merged recursively; leaf nodes (objects carrying
    ///     <c>$value</c>) are replaced wholesale. Arrays and scalars are replaced wholesale.
    ///     分组节点 (没有的对象 <c>$value</c>) are 递归合并; leaf 节点 (携带的对象
    ///     <c>$value</c>) are 整体替换. 数组和标量 are 整体替换。
    /// </remarks>
    internal static class RitsuShellThemeMerger
    {
        /// <summary>
        ///     Merges <paramref name="overlay" /> on top of <paramref name="baseTree" /> in-place. New keys are added,
        ///     overlapping keys recurse for groups and replace for leaves.
        ///     将 <paramref name="overlay" /> 原地合并到 <paramref name="baseTree" /> 之上。新键会被添加，
        ///     重叠键在分组中递归合并，在叶节点中替换。
        /// </summary>
        /// <param name="baseTree">
        ///     The mutable base dictionary (modified in place).
        ///     可变基准字典（原地修改）。
        /// </param>
        /// <param name="overlay">
        ///     The overlay JSON element (must be an object).
        ///     覆盖用 JSON 元素（必须是对象）。
        /// </param>
        public static void MergeInto(Dictionary<string, object?> baseTree, JsonElement overlay)
        {
            if (overlay.ValueKind != JsonValueKind.Object)
                return;

            foreach (var pair in overlay.EnumerateObject())
            {
                var key = pair.Name;
                var value = pair.Value;

                if (IsLeafToken(value))
                {
                    baseTree[key] = CloneLeaf(value);
                    continue;
                }

                if (value.ValueKind != JsonValueKind.Object)
                {
                    baseTree[key] = ClonePrimitive(value);
                    continue;
                }

                if (baseTree.TryGetValue(key, out var existing) &&
                    existing is Dictionary<string, object?> existingGroup)
                {
                    MergeInto(existingGroup, value);
                    continue;
                }

                var newGroup = new Dictionary<string, object?>(StringComparer.Ordinal);
                MergeInto(newGroup, value);
                baseTree[key] = newGroup;
            }
        }

        /// <summary>
        ///     Determines whether a JSON object is a DTFM leaf token (<c>$value</c> present).
        ///     判断 JSON 对象是否为 DTFM 叶令牌（存在 <c>$value</c>）。
        /// </summary>
        public static bool IsLeafToken(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty("$value", out _);
        }

        private static object? ClonePrimitive(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.Clone(),
            };
        }

        private static LeafToken CloneLeaf(JsonElement element)
        {
            string? type = null;
            string? description = null;
            object? value = null;
            JsonElement? extensions = null;

            foreach (var prop in element.EnumerateObject())
                switch (prop.Name)
                {
                    case "$type":
                        type = prop.Value.GetString();
                        break;
                    case "$description":
                        description = prop.Value.GetString();
                        break;
                    case "$value":
                        value = ClonePrimitive(prop.Value);
                        break;
                    case "$extensions":
                        extensions = prop.Value.Clone();
                        break;
                }

            return new(value, type, description, extensions);
        }
    }

    /// <summary>
    ///     Cloned representation of a DTFM leaf token after merging. Holds the raw <c>$value</c> as a CLR primitive
    ///     (string, long, double, bool) and the <c>$type</c> hint.
    ///     合并后 DTFM 叶令牌的克隆表示。将原始 <c>$value</c> 保存为 CLR 基元
    ///     （string、long、double、bool）以及 <c>$type</c> 提示。
    /// </summary>
    /// <param name="Value">
    ///     Raw value (string with optional <c>{ref}</c>, number, or boolean).
    ///     原始值 (string 与 可选 <c>{ref}</c>, 数字, or 布尔值)。
    /// </param>
    /// <param name="Type">
    ///     Token type hint (e.g. <c>color</c>, <c>dimension</c>, <c>fontFamily</c>).
    ///     令牌类型提示（例如 <c>color</c>、<c>dimension</c>、<c>fontFamily</c>）。
    /// </param>
    /// <param name="Description">
    ///     Optional human description.
    ///     可选的人类可读描述。
    /// </param>
    /// <param name="Extensions">
    ///     Optional <c>$extensions</c> blob (vendor metadata).
    ///     可选 <c>$extensions</c> blob（供应商元数据）。
    /// </param>
    internal sealed record LeafToken(object? Value, string? Type, string? Description, JsonElement? Extensions);
}
