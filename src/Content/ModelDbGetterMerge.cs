using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Snapshots lazy patched <see cref="ModelDb" /> getter output once; nested getter calls pass
    ///         through unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">对 lazy patched <see cref="ModelDb" /> getter 输出做一次快照；嵌套 getter 调用原样透传。</para>
    /// </summary>
    internal static class ModelDbGetterMerge
    {
        [ThreadStatic] private static int _depth;

        internal static IEnumerable<TModel> MergeEnumerable<TModel>(
            IEnumerable<TModel> source,
            Func<IEnumerable<TModel>, IEnumerable<TModel>> append)
            where TModel : AbstractModel
        {
            if (++_depth > 1)
            {
                --_depth;
                return source;
            }

            try
            {
                var materialized = source as TModel[] ?? [.. source];
                return append(materialized);
            }
            finally
            {
                --_depth;
            }
        }

        internal static IReadOnlyList<TItem> MergeReadOnlyList<TItem>(
            IReadOnlyList<TItem> source,
            Func<IReadOnlyList<TItem>, IReadOnlyList<TItem>> append)
        {
            if (++_depth > 1)
            {
                --_depth;
                return source;
            }

            try
            {
                var materialized = source as TItem[] ?? [.. source];
                return append(materialized);
            }
            finally
            {
                --_depth;
            }
        }
    }
}
