namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingFlushPlanner
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects dirty bindings that are not immediate save targets of another dirty binding. Saving only
        ///         these roots prevents decorator chains from persisting the same logical setting more than once.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         选择不是其他脏绑定直接保存目标的脏绑定。仅保存这些根绑定可避免装饰器链重复持久化同一逻辑设置。
        ///     </para>
        /// </summary>
        internal static List<IModSettingsBinding> SelectEffectiveSaveRoots(HashSet<IModSettingsBinding> dirty)
        {
            if (dirty.Count == 0)
                return [];

            var covered = new HashSet<IModSettingsBinding>(ModSettingsBindingReferenceEquality.Instance);
            foreach (var binding in dirty)
            {
                if (binding is not IModSettingsBindingSaveDispatch dispatch)
                    continue;
                foreach (var target in dispatch.ImmediateSaveTargets)
                    if (dirty.Contains(target))
                        covered.Add(target);
            }

            var roots = new List<IModSettingsBinding>(dirty.Count);
            roots.AddRange(dirty.Where(binding => !covered.Contains(binding)));

            return roots;
        }
    }
}
