using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves registered types after <see cref="ModelDb.Init" /> and serves cached models for getter
    ///         merges.
    ///     </para>
    ///     <para xml:lang="zh-CN">在 <see cref="ModelDb.Init" /> 后解析已注册类型，并为 getter 合并提供缓存模型。</para>
    /// </summary>
    internal static class ResolvedModelCache
    {
        private static readonly Lock Gate = new();

        private static Dictionary<ContentCatalogId, ContentCatalogEntry> _catalogs = [];
        private static Dictionary<ContentCatalogId, object> _globalCache = [];
        private static Dictionary<ContentCatalogId, Dictionary<Type, object>> _scopedCache = [];
        private static ContentRegistryPhase _phase = ContentRegistryPhase.Open;

        internal static ContentRegistryPhase Phase
        {
            get
            {
                lock (Gate)
                {
                    return _phase;
                }
            }
        }

        internal static void Configure(IReadOnlyList<ContentCatalogEntry> catalogs)
        {
            lock (Gate)
            {
                _catalogs = catalogs.ToDictionary(static entry => entry.Id);
            }
        }

        internal static void MarkFrozen()
        {
            lock (Gate)
            {
                if (_phase == ContentRegistryPhase.Open)
                    _phase = ContentRegistryPhase.Frozen;
            }
        }

        internal static void Warm()
        {
            ContentCatalogEntry[] catalogs;
            lock (Gate)
            {
                if (_phase >= ContentRegistryPhase.Resolved)
                    return;

                catalogs = _catalogs.Values.ToArray();
            }

            var globalCache = new Dictionary<ContentCatalogId, object>();
            var scopedCache = new Dictionary<ContentCatalogId, Dictionary<Type, object>>();
            foreach (var catalog in catalogs)
                if (catalog.IsScoped)
                    scopedCache[catalog.Id] = catalog.WarmScoped!(catalog.ScopedRegistry!());
                else
                    globalCache[catalog.Id] = catalog.WarmGlobal!(catalog.GlobalTypes!());

            lock (Gate)
            {
                if (_phase >= ContentRegistryPhase.Resolved)
                    return;

                _globalCache = globalCache;
                _scopedCache = scopedCache;
                _phase = ContentRegistryPhase.Resolved;
            }
        }

        internal static TModel[] GetGlobal<TModel>(ContentCatalogId id)
            where TModel : AbstractModel
        {
            ContentCatalogEntry catalog;
            lock (Gate)
            {
                if (_phase >= ContentRegistryPhase.Resolved &&
                    _globalCache.TryGetValue(id, out var cached))
                    return (TModel[])cached;

                catalog = _catalogs[id];
            }

            return ResolveUncached<TModel>(catalog.GlobalTypes!());
        }

        internal static TModel[] GetScoped<TModel>(ContentCatalogId id, Type scopeType)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(scopeType);

            ContentCatalogEntry catalog;
            lock (Gate)
            {
                if (_phase >= ContentRegistryPhase.Resolved &&
                    _scopedCache.TryGetValue(id, out var byScope) &&
                    byScope.TryGetValue(scopeType, out var cached))
                    return (TModel[])cached;

                catalog = _catalogs[id];
            }

            var registry = catalog.ScopedRegistry!();
            return !registry.TryGetValue(scopeType, out var modelTypes)
                ? []
                : ResolveUncached<TModel>(modelTypes);
        }

        internal static TModel[] ResolveUncached<TModel>(IEnumerable<Type> modelTypes)
            where TModel : AbstractModel
        {
            return modelTypes
                .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                .Select(ModelDb.GetId)
                .Select(ModelDb.GetById<TModel>)
                .ToArray();
        }

        internal static Dictionary<Type, object> ResolveScopedUncached<TModel>(
            Dictionary<Type, HashSet<Type>> registry)
            where TModel : AbstractModel
        {
            var cache = new Dictionary<Type, object>();
            foreach (var (scopeType, modelTypes) in registry)
                cache[scopeType] = ResolveUncached<TModel>(modelTypes);

            return cache;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registration freeze and resolved-model cache lifecycle.</para>
    ///     <para xml:lang="zh-CN">注册冻结与已解析模型缓存的生命周期。</para>
    /// </summary>
    internal enum ContentRegistryPhase
    {
        /// <summary>
        ///     <para xml:lang="en">Open for mod registration.</para>
        ///     <para xml:lang="zh-CN">允许 mod 注册。</para>
        /// </summary>
        Open = 0,

        /// <summary>
        ///     <para xml:lang="en">Registrations frozen at <see cref="ModelDb.Init" /> prefix.</para>
        ///     <para xml:lang="zh-CN">在 <see cref="ModelDb.Init" /> Prefix 冻结注册。</para>
        /// </summary>
        Frozen = 1,

        /// <summary>
        ///     <para xml:lang="en">Caches warmed after <see cref="ModelDb.Init" />.</para>
        ///     <para xml:lang="zh-CN"><see cref="ModelDb.Init" /> 后缓存已预热。</para>
        /// </summary>
        Resolved = 2,
    }
}
