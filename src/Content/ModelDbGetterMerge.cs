using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Materializes a model sequence before extending it while allowing nested model lookups to pass
    ///         through unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         扩展模型序列前将其实例化，同时让嵌套模型查找保持原样。
    ///     </para>
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
