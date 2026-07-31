using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Deep-merges Design Tokens Format Module token trees while preserving leaf-token boundaries.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         深度合并设计令牌格式模块令牌树，同时保留叶令牌边界。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Group objects without <c>$value</c> are merged recursively. Leaf objects containing
    ///         <c>$value</c>, arrays, and scalar values replace the corresponding base value as a unit.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         不含 <c>$value</c> 的分组对象会递归合并；含 <c>$value</c> 的叶对象、数组及标量值会整体替换
    ///         基础树中的对应值。
    ///     </para>
    /// </remarks>
    internal static class RitsuShellThemeMerger
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges <paramref name="overlay" /> over <paramref name="baseTree" /> in place. New keys are
        ///         added; overlapping groups are merged recursively, while leaves and other values are replaced.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="overlay" /> 原地叠加到 <paramref name="baseTree" />。新键会被添加；
        ///         重叠的分组会递归合并，叶令牌及其他值则会被替换。
        ///     </para>
        /// </summary>
        /// <param name="baseTree">
        ///     <para xml:lang="en">The mutable base tree to update.</para>
        ///     <para xml:lang="zh-CN">要更新的可变基础树。</para>
        /// </param>
        /// <param name="overlay">
        ///     <para xml:lang="en">
        ///         The overlay JSON object. A non-object value leaves <paramref name="baseTree" /> unchanged.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用作覆盖的 JSON 对象；若不是对象，则不会更改 <paramref name="baseTree" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Determines whether <paramref name="element" /> is an object containing <c>$value</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定 <paramref name="element" /> 是否为包含 <c>$value</c> 的对象。
        ///     </para>
        /// </summary>
        /// <param name="element">
        ///     <para xml:lang="en">The JSON element to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的 JSON 元素。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the element is a leaf token; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若该元素为叶令牌，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
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
    ///     <para xml:lang="en">
    ///         Represents a cloned leaf token after merging. Scalar <c>$value</c> properties are stored as CLR
    ///         values, while composite values and extension data retain independent <see cref="JsonElement" />
    ///         clones.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示合并后克隆的叶令牌。标量 <c>$value</c> 属性存储为 CLR 值，复合值及扩展数据则保留为独立的
    ///         <see cref="JsonElement" /> 克隆。
    ///     </para>
    /// </summary>
    /// <param name="Value">
    ///     <para xml:lang="en">
    ///         The raw token value: a string, number, Boolean, <see langword="null" />, or cloned composite JSON.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         原始令牌值：字符串、数值、布尔值、<see langword="null" /> 或克隆后的复合 JSON。
    ///     </para>
    /// </param>
    /// <param name="Type">
    ///     <para xml:lang="en">
    ///         The optional token type, such as <c>color</c>, <c>dimension</c>, or <c>fontFamily</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的令牌类型，例如 <c>color</c>、<c>dimension</c> 或 <c>fontFamily</c>。
    ///     </para>
    /// </param>
    /// <param name="Description">
    ///     <para xml:lang="en">The optional human-readable description.</para>
    ///     <para xml:lang="zh-CN">可选的易读说明。</para>
    /// </param>
    /// <param name="Extensions">
    ///     <para xml:lang="en">The optional cloned <c>$extensions</c> vendor metadata.</para>
    ///     <para xml:lang="zh-CN">可选的已克隆 <c>$extensions</c> 供应方元数据。</para>
    /// </param>
    internal sealed record LeafToken(object? Value, string? Type, string? Description, JsonElement? Extensions);
}
