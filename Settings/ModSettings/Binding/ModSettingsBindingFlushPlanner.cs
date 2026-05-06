namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsBindingFlushPlanner
    {
        /// <summary>
        ///     When several decorators for the same logical control are all marked dirty, only the roots of the save-forwarding
        ///     DAG should run <see cref="IModSettingsBinding.Save" /> so inner persistence is not executed multiple times.
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
