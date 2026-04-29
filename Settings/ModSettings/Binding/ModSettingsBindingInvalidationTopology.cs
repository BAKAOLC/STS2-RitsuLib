namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Expands refresh invalidation along equivalence and UI propagation edges so selective refresh specs stay aligned
    ///     with mark-dirty and flush without requiring every decorator to be listed explicitly.
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
