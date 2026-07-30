using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Merges a source sequence with the resolved models in a global catalog.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将来源序列与全局目录中已解析的模型合并。
        ///     </para>
        /// </summary>
        internal static IEnumerable<TModel> MergeGlobalCatalog<TModel>(
            ContentCatalogId catalogId,
            IEnumerable<TModel> source)
            where TModel : AbstractModel
        {
            var catalog = GetCatalog(catalogId);
            var additional = ResolvedModelCache.GetGlobal<TModel>(catalogId);
            return ContentMergeStrategies.GetEnumerable<TModel>(catalog.MergeMode).Merge(source, additional);
        }

        /// <summary>
        ///     <para xml:lang="en">Merges a source sequence with the resolved models in an act-scoped catalog.</para>
        ///     <para xml:lang="zh-CN">将来源序列与章节作用域目录中已解析的模型合并。</para>
        /// </summary>
        internal static IEnumerable<TModel> MergeScopedCatalog<TModel>(
            ContentCatalogId catalogId,
            Type scopeType,
            IEnumerable<TModel> source)
            where TModel : AbstractModel
        {
            var catalog = GetCatalog(catalogId);
            var additional = ResolvedModelCache.GetScoped<TModel>(catalogId, scopeType);
            return ContentMergeStrategies.GetEnumerable<TModel>(catalog.MergeMode).Merge(source, additional);
        }

        /// <summary>
        ///     <para xml:lang="en">Merges a source list with the resolved models in a global catalog.</para>
        ///     <para xml:lang="zh-CN">将来源列表与全局目录中已解析的模型合并。</para>
        /// </summary>
        internal static IReadOnlyList<TModel> MergeGlobalCatalogList<TModel>(
            ContentCatalogId catalogId,
            IReadOnlyList<TModel> source)
            where TModel : AbstractModel
        {
            var additional = ResolvedModelCache.GetGlobal<TModel>(catalogId);
            return ContentMergeStrategies.GetList<TModel>().Merge(source, additional);
        }
    }
}
