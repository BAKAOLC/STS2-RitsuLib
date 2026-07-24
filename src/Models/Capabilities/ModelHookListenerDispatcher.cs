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
    ///     Listener resolved from a model, model capability, or global listener registry.
    ///     从模型、模型 capability 或全局监听器注册表解析出的监听器。
    /// </summary>
    public readonly record struct ModelHookListener<TListener>(TListener Listener, AbstractModel? Model)
        where TListener : class;

    /// <summary>
    ///     Shared dispatcher for model and capability backed hook listener streams.
    ///     模型和 capability 驱动的 hook listener 流共用分发器。
    /// </summary>
    public static class ModelHookListenerDispatcher
    {
        /// <summary>
        ///     Resolves listeners from combat hook models, attached capabilities, and optional extra models.
        ///     从战斗 hook 模型、已附加 capability 与可选额外模型中解析监听器。
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromCombat<TListener>(
            CombatStateLike combatState,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
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
        ///     Resolves combat listeners and inserts an optional adapter immediately after each matching model listener.
        ///     解析战斗监听器，并在每个匹配模型监听器之后立即插入可选适配器。
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromCombatWithAdapters<TListener>(
            CombatStateLike combatState,
            Func<AbstractModel, TListener?> adapterResolver,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            ArgumentNullException.ThrowIfNull(adapterResolver);
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
        ///     Resolves listeners from run hook models, attached capabilities, and optional extra models.
        ///     从跑局 hook 模型、已附加 capability 与可选额外模型中解析监听器。
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromRun<TListener>(
            IRunState runState,
            CombatStateLike? combatState,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
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
        ///     Resolves listeners from an explicit model sequence, attached capabilities, and optional extra models.
        ///     从显式模型序列、已附加 capability 与可选额外模型中解析监听器。
        /// </summary>
        public static IEnumerable<ModelHookListener<TListener>> FromModels<TListener>(
            IEnumerable<AbstractModel> models,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
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
    ///     Thread-safe process-wide hook listener registry.
    ///     线程安全的进程级 hook listener 注册表。
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
                Volatile.Write(ref _snapshot, _listeners.ToArray());
            }
        }

        internal TListener[] Snapshot()
        {
            return Volatile.Read(ref _snapshot);
        }
    }
}
