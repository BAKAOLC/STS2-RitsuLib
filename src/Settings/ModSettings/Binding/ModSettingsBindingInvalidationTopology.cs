namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Expands refresh invalidation through binding-equivalence and UI-propagation relationships so selective
    ///         refresh rules account for related decorators without listing each one explicitly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         沿绑定等价关系和界面传播关系扩展刷新失效集合，使选择性刷新规则无须逐一列出即可涵盖相关装饰器。
    ///     </para>
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
