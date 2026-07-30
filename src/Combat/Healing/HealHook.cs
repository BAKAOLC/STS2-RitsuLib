using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.Healing
{
    /// <summary>
    ///     <para xml:lang="en">Dispatches healing-amount hooks to models, capabilities, and registered global listeners.</para>
    ///     <para xml:lang="zh-CN">将治疗量钩子分发给模型、模型能力和已注册的全局监听器。</para>
    /// </summary>
    public static class HealHook
    {
        private static readonly ModelHookListenerRegistry<IHealHookListener> GlobalListeners = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide listener. Effects owned by a model should normally implement
        ///         <see cref="IHealHookListener" /> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册进程级监听器。由模型持有的效果通常应直接实现 <see cref="IHealHookListener" />。
        ///     </para>
        /// </summary>
        public static void RegisterGlobalListener(IHealHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies healing-amount hooks and clamps the result to zero or greater.</para>
        ///     <para xml:lang="zh-CN">应用治疗量钩子，并将结果限制为不小于零。</para>
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
