using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Merges RitsuLib-registered run modifiers and exclusivity groups into <see cref="ModelDb" />
    ///         lists.
    ///     </para>
    ///     <para xml:lang="zh-CN">将 RitsuLib 注册的运行修饰符与互斥组合并到 <see cref="ModelDb" /> 列表中。</para>
    /// </summary>
    internal static class ModifierContentMerge
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts registered modifiers before or after <paramref name="source" /> using
        ///         <see cref="ModifierRegistration.ModifierListSortOrder" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按 <see cref="ModifierRegistration.ModifierListSortOrder" /> 在 <paramref name="source" />
        ///         前后插入已注册修饰符。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<ModifierModel> InsertModifiers(
            IReadOnlyList<ModifierModel> source,
            IReadOnlyList<ModifierRegistration> registrations)
        {
            if (registrations.Count == 0)
                return source;

            var before = ResolveSlice(registrations, true);
            var after = ResolveSlice(registrations, false);
            if (before.Length == 0 && after.Length == 0)
                return source;

            return ConcatDistinctByModelId(before, source, after);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges registered exclusivity groups into <paramref name="source" />, combining overlapping
        ///         sets.
        ///     </para>
        ///     <para xml:lang="zh-CN">将已注册互斥组合并到 <paramref name="source" />，并合并存在交集的集合。</para>
        /// </summary>
        public static IReadOnlyList<IReadOnlySet<ModifierModel>> MergeMutuallyExclusiveModifiers(
            IReadOnlyList<IReadOnlySet<ModifierModel>> source,
            IReadOnlyList<HashSet<Type>> registeredGroups)
        {
            var customGroups = BuildExclusiveGroups(registeredGroups);
            if (customGroups.Count == 0)
                return source;

            return
            [
                .. customGroups
                    .Aggregate(
                        source.Select(static set => set.ToHashSet()).ToList(),
                        static (groups, customGroup) =>
                        {
                            var overlapping = groups.Where(g => g.Overlaps(customGroup)).ToList();
                            var merged = customGroup.ToHashSet();
                            foreach (var group in overlapping)
                            {
                                groups.Remove(group);
                                merged.UnionWith(group);
                            }

                            groups.Add(merged);
                            return groups;
                        })
                    .Select(static IReadOnlySet<ModifierModel> (set) => set),
            ];
        }

        private static ModifierModel[] ResolveSlice(
            IReadOnlyList<ModifierRegistration> registrations,
            bool negativeSortOrder)
        {
            return
            [
                .. registrations
                    .Where(r => negativeSortOrder
                        ? r.ModifierListSortOrder < 0
                        : r.ModifierListSortOrder >= 0)
                    .OrderBy(static r => r.ModifierListSortOrder)
                    .ThenBy(static r => r.ModifierType.FullName ?? r.ModifierType.Name, StringComparer.Ordinal)
                    .Select(static r => ModelDb.GetById<ModifierModel>(ModelDb.GetId(r.ModifierType)))
                    .DistinctBy(static model => model.Id),
            ];
        }

        private static List<HashSet<ModifierModel>> BuildExclusiveGroups(IReadOnlyList<HashSet<Type>> registeredGroups)
        {
            var groups = new List<HashSet<ModifierModel>>();
            foreach (var typeSet in registeredGroups)
            {
                var members = typeSet
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                    .Select(static t => ModelDb.GetById<ModifierModel>(ModelDb.GetId(t)))
                    .ToHashSet();
                if (members.Count < 2)
                    continue;

                var overlapping = groups.Where(g => g.Overlaps(members)).ToList();
                foreach (var group in overlapping)
                {
                    groups.Remove(group);
                    members.UnionWith(group);
                }

                groups.Add(members);
            }

            return groups;
        }

        private static List<ModifierModel> ConcatDistinctByModelId(
            ModifierModel[] before,
            IReadOnlyList<ModifierModel> source,
            ModifierModel[] after)
        {
            var result = new List<ModifierModel>(before.Length + source.Count + after.Length);
            ContentMergeStrategies.AppendDistinctById(result, before);
            ContentMergeStrategies.AppendDistinctById(result, source);
            ContentMergeStrategies.AppendDistinctById(result, after);
            return result;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registration metadata for a mod run modifier list entry.</para>
    ///     <para xml:lang="zh-CN">mod 运行修饰符列表条目的注册元数据。</para>
    /// </summary>
    /// <param name="ModifierType">
    ///     <para xml:lang="en">Concrete <see cref="ModifierModel" /> type.</para>
    ///     <para xml:lang="zh-CN">具体 <see cref="ModifierModel" /> 类型。</para>
    /// </param>
    /// <param name="ModifierListSortOrder">
    ///     <para xml:lang="en">Negative values insert before the current list segment; non-negative values insert after.</para>
    ///     <para xml:lang="zh-CN">负值插入当前列表段之前；非负值插入之后。</para>
    /// </param>
    internal readonly record struct ModifierRegistration(Type ModifierType, int ModifierListSortOrder);
}
