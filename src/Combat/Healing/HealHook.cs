using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     Dispatches creature healing amount hooks to model, capability, and registered global listeners.
    ///     将生物治疗数值 hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class HealHook
    {
        private static readonly ModelHookListenerRegistry<IHealHookListener> GlobalListeners = new();

        /// <summary>
        ///     Registers a process-wide listener. Model-owned effects should usually implement
        ///     <see cref="IHealHookListener" /> directly.
        ///     注册一个进程级监听器。模型所属效果通常应直接实现 <see cref="IHealHookListener" />。
        /// </summary>
        public static void RegisterGlobalListener(IHealHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     Applies healing amount hooks.
        ///     应用治疗数值 hook。
        /// </summary>
        public static decimal ModifyAmount(HealContext context, decimal amount)
        {
            var additiveAmount = ApplyAdditive(context, amount);
            var multiplicativeAmount = ApplyMultiplicative(context, additiveAmount);
            var modifiedAmount = ApplyLate(context, multiplicativeAmount);

            return Math.Max(0m, modifiedAmount);
        }

        private static decimal ApplyAdditive(HealContext context, decimal amount)
        {
            return amount + IterateListeners(context)
                .Sum(listenerContext => listenerContext.Listener.ModifyHealAdditive(context, amount));
        }

        private static decimal ApplyMultiplicative(HealContext context, decimal amount)
        {
            return IterateListeners(context).Aggregate(amount,
                (current, listenerContext) =>
                    current * listenerContext.Listener.ModifyHealMultiplicative(context, amount));
        }

        private static decimal ApplyLate(HealContext context, decimal amount)
        {
            return IterateListeners(context).Aggregate(amount,
                static (current, listenerContext) => listenerContext.Listener.ModifyHealAmount(
                    listenerContext.Context,
                    current));
        }

        private static IEnumerable<ListenerContext> IterateListeners(HealContext context)
        {
            if (context.RunState != null)
            {
                foreach (var entry in ModelHookListenerDispatcher.FromRun(
                             context.RunState,
                             context.CombatState,
                             GlobalListeners))
                    yield return new(entry.Listener, context);
                yield break;
            }

            if (context.CombatState == null)
                yield break;

            foreach (var entry in ModelHookListenerDispatcher.FromCombat(
                         context.CombatState,
                         GlobalListeners))
                yield return new(entry.Listener, context);
        }

        private readonly record struct ListenerContext(IHealHookListener Listener, HealContext Context);
    }
}
