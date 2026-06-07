#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Dispatches secondary-resource hooks to model, capability, and registered global listeners.
    ///     将次级资源 hook 分发给模型、capability 和已注册的全局监听器。
    /// </summary>
    public static class SecondaryResourceHook
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<ISecondaryResourceHookListener> GlobalListeners = [];

        /// <summary>
        ///     Registers a process-wide listener. Use sparingly; model-owned effects should usually implement the
        ///     listener interface directly.
        ///     注册一个进程级监听器。应谨慎使用；模型所属效果通常应直接实现监听接口。
        /// </summary>
        public static void RegisterGlobalListener(ISecondaryResourceHookListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
            lock (SyncRoot)
            {
                if (!GlobalListeners.Contains(listener))
                    GlobalListeners.Add(listener);
            }
        }

        /// <summary>
        ///     Applies max-amount hooks.
        ///     应用最大数量 hook。
        /// </summary>
        public static decimal ModifyMaxAmount(SecondaryResourceMaxContext context, decimal amount)
        {
            return IterateListeners(context.CombatState).Aggregate(amount,
                (current, listener) => listener.ModifyMaxSecondaryResource(context, current));
        }

        /// <summary>
        ///     Applies gain-amount hooks.
        ///     应用获得数量 hook。
        /// </summary>
        public static decimal ModifyGain(SecondaryResourceContext context, decimal amount)
        {
            return IterateListeners(context.CombatState).Aggregate(amount,
                (current, listener) => listener.ModifySecondaryResourceGain(context, current));
        }

        /// <summary>
        ///     Applies cost hooks.
        ///     应用费用 hook。
        /// </summary>
        public static decimal ModifyCost(SecondaryResourceCostContext context, decimal amount)
        {
            return IterateListeners(context.CombatState).Aggregate(amount,
                (current, listener) => listener.ModifySecondaryResourceCost(context, current));
        }

        /// <summary>
        ///     Applies secondary X-value hooks.
        ///     应用次级 X 值 hook。
        /// </summary>
        public static int ModifyXValue(SecondaryResourceXContext context, int value)
        {
            return IterateListeners(context.CombatState).Aggregate(value,
                (current, listener) => listener.ModifySecondaryResourceXValue(context, current));
        }

        /// <summary>
        ///     Returns whether a gain should proceed.
        ///     返回是否应继续执行获得。
        /// </summary>
        public static bool ShouldGain(SecondaryResourceContext context, decimal amount)
        {
            return IterateListeners(context.CombatState)
                .All(listener => listener.ShouldGainSecondaryResource(context, amount));
        }

        /// <summary>
        ///     Returns whether a spend should proceed.
        ///     返回是否应继续执行消耗。
        /// </summary>
        public static bool ShouldSpend(SecondaryResourceSpendContext context)
        {
            return IterateListeners(context.CombatState)
                .All(listener => listener.ShouldSpendSecondaryResource(context));
        }

        /// <summary>
        ///     Returns whether a built-in reset should proceed.
        ///     返回是否应继续执行内建重置。
        /// </summary>
        public static bool ShouldReset(SecondaryResourceContext context)
        {
            return IterateListeners(context.CombatState)
                .All(listener => listener.ShouldResetSecondaryResource(context));
        }

        /// <summary>
        ///     Runs after-change hooks.
        ///     运行变化后 hook。
        /// </summary>
        public static async Task AfterChanged(SecondaryResourceChangeContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState))
                await listener.AfterSecondaryResourceChanged(context);
        }

        /// <summary>
        ///     Runs after-spent hooks.
        ///     运行消耗后 hook。
        /// </summary>
        public static async Task AfterSpent(SecondaryResourceSpendContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState))
                await listener.AfterSecondaryResourceSpent(context);
        }

        /// <summary>
        ///     Runs after-reset hooks.
        ///     运行重置后 hook。
        /// </summary>
        public static async Task AfterReset(SecondaryResourceChangeContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState))
                await listener.AfterSecondaryResourceReset(context);
        }

        private static IEnumerable<ISecondaryResourceHookListener> IterateListeners(CombatStateLike combatState)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                yield break;

            HashSet<object> seen = new(ReferenceEqualityComparer.Instance);

            foreach (var model in combatState.IterateHookListeners())
            {
                if (model is ISecondaryResourceHookListener modelListener && seen.Add(modelListener))
                    yield return modelListener;

                foreach (var capability in IterateCapabilityListeners(model))
                    if (seen.Add(capability))
                        yield return capability;
            }

            ISecondaryResourceHookListener[] globals;
            lock (SyncRoot)
            {
                globals = GlobalListeners.ToArray();
            }

            foreach (var listener in globals)
                if (seen.Add(listener))
                    yield return listener;
        }

        private static IEnumerable<ISecondaryResourceHookListener> IterateCapabilityListeners(AbstractModel model)
        {
            if (!ModelCapabilities.TryGet(model, out var capabilities))
                yield break;

            foreach (var capability in capabilities.All)
                if (capability is ISecondaryResourceHookListener listener)
                    yield return listener;
        }
    }
}
