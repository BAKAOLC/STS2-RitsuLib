namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Expands refresh invalidation along equivalence and UI propagation edges so selective refresh specs stay aligned
    ///     with mark-dirty and flush without requiring every decorator to be listed explicitly.
    ///     沿等价边和 UI 传播边扩展刷新失效，使选择性刷新规范与
    ///     mark-dirty 和 flush 保持一致，而无需显式列出每个装饰器。
    /// </summary>
    internal static class ModSettingsBindingInvalidationTopology
    {
        internal static HashSet<IModSettingsBinding> ExpandClosure(IModSettingsBinding seed)
        {
            var visited = new HashSet<IModSettingsBinding>(ModSettingsBindingReferenceEquality.Instance);
            var queue = new Queue<IModSettingsBinding>();
            queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!visited.Add(node))
                    continue;

                if (node is IModSettingsUiRefreshEquivalence eq)
                    foreach (var alias in eq.UiRefreshAlsoTreatAsDirty)
                        queue.Enqueue(alias);

                // ReSharper disable once InvertIf
                if (node is IModSettingsUiRefreshPropagation propagation)
                    foreach (var extra in propagation.ExtraBindingsToMarkDirtyForUi)
                        queue.Enqueue(extra);
            }

            return visited;
        }

        internal static HashSet<IModSettingsBinding> ExpandUnion(IEnumerable<IModSettingsBinding> seeds)
        {
            var union = new HashSet<IModSettingsBinding>(ModSettingsBindingReferenceEquality.Instance);
            foreach (var seed in seeds)
            foreach (var node in ExpandClosure(seed))
                union.Add(node);

            return union;
        }
    }
}
