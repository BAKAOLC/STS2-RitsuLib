#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Listener resolved from a model, model capability, or global listener registry.</para>
    ///     <para xml:lang="zh-CN">从模型、模型能力或全局监听器注册表解析出的监听器。</para>
    /// </summary>
    public readonly record struct ModelHookListener<TListener>(TListener Listener, AbstractModel? Model)
        where TListener : class;

    /// <summary>
    ///     <para xml:lang="en">Shared dispatcher for model- and capability-backed hook listener streams.</para>
    ///     <para xml:lang="zh-CN">由模型和能力提供的钩子监听器流所共用的分发器。</para>
    /// </summary>
    public static class ModelHookListenerDispatcher
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves listeners from combat hook models, attached capabilities, and optional extra models.</para>
        ///     <para xml:lang="zh-CN">从战斗钩子模型、已附加能力与可选的额外模型中解析监听器。</para>
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromCombat<TListener>(
            CombatStateLike combatState,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(combatState);
            ArgumentNullException.ThrowIfNull(extraModels);
            return FromModelsCore<TListener>(combatState.IterateHookListeners(), null, null, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromCombat<TListener>(
            CombatStateLike combatState,
            ModelHookListenerRegistry<TListener> globalListeners,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            return FromModelsCore(combatState.IterateHookListeners(), globalListeners, null, extraModels);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves combat listeners and inserts an optional adapter immediately after each source model or
        ///         model-backed capability for which <paramref name="adapterResolver" /> returns one.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析战斗监听器；当 <paramref name="adapterResolver" /> 为来源模型或由模型承载的能力返回适配器时，
        ///         将该适配器紧接在对应来源之后插入。
        ///     </para>
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromCombatWithAdapters<TListener>(
            CombatStateLike combatState,
            Func<AbstractModel, TListener?> adapterResolver,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(combatState);
            ArgumentNullException.ThrowIfNull(adapterResolver);
            ArgumentNullException.ThrowIfNull(extraModels);
            return FromModelsCore(combatState.IterateHookListeners(), null, adapterResolver, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromCombatWithAdapters<TListener>(
            CombatStateLike combatState,
            ModelHookListenerRegistry<TListener> globalListeners,
            Func<AbstractModel, TListener?> adapterResolver,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(adapterResolver);
            return FromModelsCore(combatState.IterateHookListeners(), globalListeners, adapterResolver, extraModels);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves listeners from run hook models, attached capabilities, and optional extra models.</para>
        ///     <para xml:lang="zh-CN">从局内钩子模型、已附加能力与可选的额外模型中解析监听器。</para>
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromRun<TListener>(
            IRunState runState,
            CombatStateLike? combatState,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(runState);
            ArgumentNullException.ThrowIfNull(extraModels);
            return FromModelsCore<TListener>(runState.IterateHookListeners(combatState), null, null, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromRun<TListener>(
            IRunState runState,
            CombatStateLike? combatState,
            ModelHookListenerRegistry<TListener> globalListeners,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            return FromModels(runState.IterateHookListeners(combatState), globalListeners, extraModels);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves listeners from an explicit model sequence, attached capabilities, and optional extra
        ///         models.
        ///     </para>
        ///     <para xml:lang="zh-CN">从显式模型序列、已附加能力与可选的额外模型中解析监听器。</para>
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromModels<TListener>(
            IEnumerable<AbstractModel> models,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(models);
            ArgumentNullException.ThrowIfNull(extraModels);
            return FromModelsCore<TListener>(models, null, null, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromModels<TListener>(
            IEnumerable<AbstractModel> models,
            ModelHookListenerRegistry<TListener> globalListeners,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            return FromModelsCore(models, globalListeners, null, extraModels);
        }

        private static IEnumerable<ModelHookListener<TListener>> FromModelsCore<TListener>(
            IEnumerable<AbstractModel> models,
            ModelHookListenerRegistry<TListener>? globalListeners,
            Func<AbstractModel, TListener?>? adapterResolver,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            HashSet<TListener> seen = new(ReferenceEqualityComparer.Instance);
            HashSet<AbstractModel>? adaptedModels = null;

            foreach (var model in models)
            {
                if (model is TListener modelListener && seen.Add(modelListener))
                    yield return new(modelListener, model);

                if (TryResolveAdapter(model, out var adapter))
                    yield return new(adapter, model);

                if (!ModelCapabilities.TryGet(model, out var capabilities) || capabilities.Count == 0)
                    continue;

                var candidates = capabilities.GetAttachedSnapshot();
                foreach (var capability in candidates)
                {
                    if (!ReferenceEquals(capability.Owner, model))
                        continue;

                    if (capability is TListener listener && seen.Add(listener))
                        yield return new(listener, capability as AbstractModel);

                    if (capability is AbstractModel capabilityModel &&
                        TryResolveAdapter(capabilityModel, out adapter))
                        yield return new(adapter, capabilityModel);
                }
            }

            foreach (var model in extraModels)
            {
                switch (model)
                {
                    case null:
                        continue;
                    case TListener modelListener when seen.Add(modelListener):
                        yield return new(modelListener, model);
                        break;
                }

                if (TryResolveAdapter(model, out var adapter))
                    yield return new(adapter, model);

                if (!ModelCapabilities.TryGet(model, out var capabilities) || capabilities.Count == 0)
                    continue;

                var candidates = capabilities.GetAttachedSnapshot();
                foreach (var capability in candidates)
                {
                    if (!ReferenceEquals(capability.Owner, model))
                        continue;

                    if (capability is TListener listener && seen.Add(listener))
                        yield return new(listener, capability as AbstractModel);

                    if (capability is AbstractModel capabilityModel &&
                        TryResolveAdapter(capabilityModel, out adapter))
                        yield return new(adapter, capabilityModel);
                }
            }

            if (globalListeners == null)
                yield break;

            foreach (var listener in globalListeners.Snapshot())
                if (seen.Add(listener))
                    yield return new(listener, null);
            yield break;

            bool TryResolveAdapter(AbstractModel model, out TListener adapter)
            {
                adapter = null!;
                if (adapterResolver == null || adaptedModels?.Contains(model) == true)
                    return false;

                var resolved = adapterResolver(model);
                if (resolved == null)
                    return false;

                adaptedModels ??= new(ReferenceEqualityComparer.Instance);
                adaptedModels.Add(model);
                if (!seen.Add(resolved))
                    return false;

                adapter = resolved;
                return true;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Thread-safe process-wide hook listener registry.</para>
    ///     <para xml:lang="zh-CN">线程安全的进程级钩子监听器注册表。</para>
    /// </summary>
    internal sealed class ModelHookListenerRegistry<TListener>
        where TListener : class
    {
        private readonly List<TListener> _listeners = [];
        private readonly Lock _syncRoot = new();
        private TListener[] _snapshot = [];

        internal void Register(TListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            lock (_syncRoot)
            {
                if (_listeners.Contains(listener))
                    return;

                _listeners.Add(listener);
                Volatile.Write(ref _snapshot, [.. _listeners]);
            }
        }

        internal TListener[] Snapshot()
        {
            return Volatile.Read(ref _snapshot);
        }
    }
}
