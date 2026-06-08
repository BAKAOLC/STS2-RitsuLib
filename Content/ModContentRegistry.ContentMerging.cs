using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        /// <para xml:lang="en">Merges vanilla getter output with a global catalog row via <see cref="ResolvedModelCache" /> and <see cref="ContentMergeStrategies" />.</para>
        /// <para xml:lang="zh-CN">通过 <see cref="ResolvedModelCache" /> 与 <see cref="ContentMergeStrategies" /> 将原版 getter 输出与全局 catalog 行合并。</para>
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
        /// <para xml:lang="en">Merges vanilla getter output with an act-scoped catalog row.</para>
        /// <para xml:lang="zh-CN">将原版 getter 输出与 act 作用域 catalog 行合并。</para>
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
        /// <para xml:lang="en">Merges a read-only list getter with a global catalog row.</para>
        /// <para xml:lang="zh-CN">将只读列表 getter 与全局 catalog 行合并。</para>
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
