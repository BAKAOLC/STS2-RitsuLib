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
    internal readonly record struct ModelHookListener<TListener>(TListener Listener, AbstractModel? Model)
        where TListener : class;

    /// <summary>
    ///     Shared dispatcher for model and capability backed hook listener streams.
    ///     模型和 capability 驱动的 hook listener 流共用分发器。
    /// </summary>
    internal static class ModelHookListenerDispatcher
    {
        internal static IEnumerable<ModelHookListener<TListener>> FromCombat<TListener>(
            CombatStateLike combatState,
            ModelHookListenerRegistry<TListener>? globalListeners = null,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            return FromModels(combatState.IterateHookListeners(), globalListeners, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromRun<TListener>(
            IRunState runState,
            CombatStateLike? combatState,
            ModelHookListenerRegistry<TListener>? globalListeners = null,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            return FromModels(runState.IterateHookListeners(combatState), globalListeners, extraModels);
        }

        internal static IEnumerable<ModelHookListener<TListener>> FromModels<TListener>(
            IEnumerable<AbstractModel> models,
            ModelHookListenerRegistry<TListener>? globalListeners = null,
            params AbstractModel?[] extraModels)
            where TListener : class
        {
            HashSet<object> seen = new(ReferenceEqualityComparer.Instance);

            foreach (var model in models)
            foreach (var listener in FromModel<TListener>(model, seen))
                yield return listener;

            foreach (var model in extraModels)
                if (model != null)
                    foreach (var listener in FromModel<TListener>(model, seen))
                        yield return listener;

            if (globalListeners == null)
                yield break;

            foreach (var listener in globalListeners.Snapshot())
                if (seen.Add(listener))
                    yield return new(listener, null);
        }

        private static IEnumerable<ModelHookListener<TListener>> FromModel<TListener>(
            AbstractModel model,
            HashSet<object> seen)
            where TListener : class
        {
            if (model is TListener modelListener && seen.Add(modelListener))
                yield return new(modelListener, model);

            if (!ModelCapabilities.TryGet(model, out var capabilities))
                yield break;

            foreach (var capability in capabilities.All)
                if (capability is TListener listener && seen.Add(listener))
                    yield return new(listener, capability as AbstractModel);
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

        internal void Register(TListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            lock (_syncRoot)
            {
                if (!_listeners.Contains(listener))
                    _listeners.Add(listener);
            }
        }

        internal TListener[] Snapshot()
        {
            lock (_syncRoot)
            {
                return _listeners.ToArray();
            }
        }
    }
}
