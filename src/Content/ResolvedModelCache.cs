using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves registered model types after <see cref="ModelDb.Init" /> and caches them by catalog.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="ModelDb.Init" /> 后解析已注册模型类型，并按目录缓存。
    ///     </para>
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

                catalogs = [.. _catalogs.Values];
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
            return
            [
                .. modelTypes
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                    .Select(ModelDb.GetId)
                    .Select(ModelDb.GetById<TModel>),
            ];
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
    ///     <para xml:lang="zh-CN">注册冻结与已解析模型缓存的生命周期阶段。</para>
    /// </summary>
    internal enum ContentRegistryPhase
    {
        /// <summary>
        ///     <para xml:lang="en">Open for mod registration.</para>
        ///     <para xml:lang="zh-CN">允许模组注册。</para>
        /// </summary>
        Open = 0,

        /// <summary>
        ///     <para xml:lang="en">Registrations are frozen before <see cref="ModelDb.Init" /> runs.</para>
        ///     <para xml:lang="zh-CN">在 <see cref="ModelDb.Init" /> 运行前冻结注册。</para>
        /// </summary>
        Frozen = 1,

        /// <summary>
        ///     <para xml:lang="en">Resolved-model caches have been warmed.</para>
        ///     <para xml:lang="zh-CN">已预热解析后的模型缓存。</para>
        /// </summary>
        Resolved = 2,
    }
}
