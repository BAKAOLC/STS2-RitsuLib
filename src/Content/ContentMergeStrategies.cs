using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a strategy for combining a model sequence with resolved mod models.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义将模型序列与已解析模组模型合并的策略。</para>
    /// </summary>
    internal interface IContentEnumerableMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        IEnumerable<TModel> Merge(IEnumerable<TModel> source, TModel[] additional);
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a strategy for combining a model list with resolved mod models.</para>
    ///     <para xml:lang="zh-CN">定义将模型列表与已解析模组模型合并的策略。</para>
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
        ///     <para xml:lang="zh-CN">追加 ID 尚未出现在 <paramref name="destination" /> 中的项。</para>
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
                return source as TModel[] ?? [.. source];

            return [.. source.Concat(additional).DistinctBy(static model => model.Id)];
        }
    }

    internal sealed class MergeDistinctByIdEnumerableStrategy<TModel> : IContentEnumerableMergeStrategy<TModel>
        where TModel : AbstractModel
    {
        internal static readonly MergeDistinctByIdEnumerableStrategy<TModel> Instance = new();

        public IEnumerable<TModel> Merge(IEnumerable<TModel> source, TModel[] additional)
        {
            return additional.Length == 0
                ? source
                : [.. source.Concat(additional).DistinctBy(static model => model.Id)];
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

            var result = new List<TModel>(source);
            ContentMergeStrategies.AppendDistinctById(result, additional);
            return result;
        }
    }
}
