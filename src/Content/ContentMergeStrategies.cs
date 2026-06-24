using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Merge strategies for combining vanilla <see cref="ModelDb" /> getter output with resolved mod
    ///         models.
    ///     </para>
    ///     <para xml:lang="zh-CN">将原版 <see cref="ModelDb" /> getter 输出与已解析 mod 模型合并的策略。</para>
    /// </summary>
    internal interface IContentEnumerableMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        IEnumerable<TModel> Merge(IEnumerable<TModel> source, TModel[] additional);
    }

    /// <summary>
    ///     <para xml:lang="en">Merge strategy for <see cref="IReadOnlyList{T}" /> getter output.</para>
    ///     <para xml:lang="zh-CN">用于 <see cref="IReadOnlyList{T}" /> getter 输出的合并策略。</para>
    /// </summary>
    internal interface IContentListMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        IReadOnlyList<TModel> Merge(IReadOnlyList<TModel> source, TModel[] additional);
    }

    internal static class ContentMergeStrategies
    {
        internal static IContentEnumerableMergeStrategy<TModel> GetEnumerable<TModel>(ContentMergeMode mode)
            where TModel : AbstractModel
        {
            return mode switch
            {
                ContentMergeMode.MergeDistinctById => MergeDistinctByIdEnumerableStrategy<TModel>.Instance,
                _ => AppendDistinctByIdEnumerableStrategy<TModel>.Instance,
            };
        }

        internal static IContentListMergeStrategy<TModel> GetList<TModel>()
            where TModel : AbstractModel
        {
            return DistinctByIdListStrategy<TModel>.Instance;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends items whose <see cref="AbstractModel.Id" /> is not already in
        ///         <paramref name="destination" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">追加 Id 尚未出现在 <paramref name="destination" /> 中的项。</para>
        /// </summary>
        internal static void AppendDistinctById<TModel>(List<TModel> destination, IEnumerable<TModel> items)
            where TModel : AbstractModel
        {
            var known = destination.Count == 0
                ? []
                : destination.Select(static model => model.Id).ToHashSet();

            destination.AddRange(items.Where(item => known.Add(item.Id)));
        }
    }

    internal sealed class AppendDistinctByIdEnumerableStrategy<TModel> : IContentEnumerableMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        internal static readonly AppendDistinctByIdEnumerableStrategy<TModel> Instance = new();

        public IEnumerable<TModel> Merge(IEnumerable<TModel> source, TModel[] additional)
        {
            if (additional.Length == 0)
                return source as TModel[] ?? source.ToArray();

            return source.Concat(additional).DistinctBy(static model => model.Id).ToArray();
        }
    }

    internal sealed class MergeDistinctByIdEnumerableStrategy<TModel> : IContentEnumerableMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        internal static readonly MergeDistinctByIdEnumerableStrategy<TModel> Instance = new();

        public IEnumerable<TModel> Merge(IEnumerable<TModel> source, TModel[] additional)
        {
            if (additional.Length == 0)
                return source;

            return source.Concat(additional).DistinctBy(static model => model.Id).ToList();
        }
    }

    internal sealed class DistinctByIdListStrategy<TModel> : IContentListMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        internal static readonly DistinctByIdListStrategy<TModel> Instance = new();

        public IReadOnlyList<TModel> Merge(IReadOnlyList<TModel> source, TModel[] additional)
        {
            if (additional.Length == 0)
                return source;

            var result = source as List<TModel> ?? source.ToList();
            ContentMergeStrategies.AppendDistinctById(result, additional);
            return result;
        }
    }
}
