#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using MegaCrit.Sts2.Core.Models;
#endif
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     Dispatches creature healing amount hooks to model, capability, and registered global listeners.
    ///     将生物治疗数值 hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class HealHook
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<IHealHookListener> GlobalListeners = [];

        /// <summary>
        ///     Registers a process-wide listener. Model-owned effects should usually implement
        ///     <see cref="IHealHookListener" /> directly.
        ///     注册一个进程级监听器。模型所属效果通常应直接实现 <see cref="IHealHookListener" />。
        /// </summary>
        public static void RegisterGlobalListener(IHealHookListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            lock (SyncRoot)
            {
                if (!GlobalListeners.Contains(listener))
                    GlobalListeners.Add(listener);
            }
        }

        /// <summary>
        ///     Applies healing amount hooks.
        ///     应用治疗数值 hook。
        /// </summary>
        public static decimal ModifyAmount(HealContext context, decimal amount)
        {
            var modifiedAmount = IterateListeners(context).Aggregate(amount,
                static (current, listenerContext) => listenerContext.Listener.ModifyHealAmount(
                    listenerContext.Context,
                    current));

            return Math.Max(0m, modifiedAmount);
        }

        private static IEnumerable<ListenerContext> IterateListeners(HealContext context)
        {
            HashSet<object> seen = new(ReferenceEqualityComparer.Instance);

            foreach (var model in IterateModelSources(context))
            foreach (var listener in IterateModelListeners(model, seen))
                yield return new(listener, context);

            IHealHookListener[] globals;
            lock (SyncRoot)
            {
                globals = GlobalListeners.ToArray();
            }

            foreach (var listener in globals)
                if (seen.Add(listener))
                    yield return new(listener, context);
        }

        private static IEnumerable<AbstractModel> IterateModelSources(HealContext context)
        {
            if (context.RunState != null)
            {
                foreach (var model in context.RunState.IterateHookListeners(context.CombatState))
                    yield return model;
                yield break;
            }

            if (context.CombatState == null)
                yield break;

            foreach (var model in context.CombatState.IterateHookListeners())
                yield return model;
        }

        private static IEnumerable<IHealHookListener> IterateModelListeners(AbstractModel model, HashSet<object> seen)
        {
            if (model is IHealHookListener modelListener && seen.Add(modelListener))
                yield return modelListener;

            foreach (var capability in IterateCapabilityListeners(model))
                if (seen.Add(capability))
                    yield return capability;
        }

        private static IEnumerable<IHealHookListener> IterateCapabilityListeners(AbstractModel model)
        {
            if (!ModelCapabilities.TryGet(model, out var capabilities))
                yield break;

            foreach (var capability in capabilities.All)
                if (capability is IHealHookListener listener)
                    yield return listener;
        }

        private readonly record struct ListenerContext(IHealHookListener Listener, HealContext Context);
    }
}
