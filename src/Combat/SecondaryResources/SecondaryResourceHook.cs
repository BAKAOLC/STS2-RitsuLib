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
    ///     <para xml:lang="en">Dispatches secondary-resource hooks to model, capability, and global listeners.</para>
    ///     <para xml:lang="zh-CN">将次级资源钩子分发给模型、能力和全局监听器。</para>
    /// </summary>
    public static class SecondaryResourceHook
    {
        private static readonly ModelHookListenerRegistry<ISecondaryResourceHookListener> GlobalListeners = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide listener. Model-owned effects should normally implement
        ///         <see cref="ISecondaryResourceHookListener" /> on that model instead.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册进程级监听器。由模型承载的效果通常应让该模型直接实现
        ///         <see cref="ISecondaryResourceHookListener" />。
        ///     </para>
        /// </summary>
        public static void RegisterGlobalListener(ISecondaryResourceHookListener listener)
        {
            GlobalListeners.Register(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies maximum-amount modifiers.</para>
        ///     <para xml:lang="zh-CN">应用最大数量修正。</para>
        /// </summary>
        public static decimal ModifyMaxAmount(SecondaryResourceMaxContext context, decimal amount)
        {
            return IterateListeners(context.CombatState).Aggregate(amount,
                (current, listener) => listener.ModifyMaxSecondaryResource(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies gain-amount modifiers.</para>
        ///     <para xml:lang="zh-CN">应用增加量修正。</para>
        /// </summary>
        public static decimal ModifyGain(SecondaryResourceContext context, decimal amount)
        {
            return IterateListeners(context.CombatState, context.Source).Aggregate(amount,
                (current, listener) => listener.ModifySecondaryResourceGain(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies normal and late cost modifiers.</para>
        ///     <para xml:lang="zh-CN">应用常规和后置费用修正。</para>
        /// </summary>
        public static decimal ModifyCost(SecondaryResourceCostContext context, decimal cost)
        {
            var modifiedCost = IterateCostListeners(context.CombatState, context.Card).Aggregate(cost,
                (current, listener) => listener.ModifySecondaryResourceCost(context, current));

            return IterateCostListeners(context.CombatState, context.Card).Aggregate(modifiedCost,
                (current, listener) => listener.ModifySecondaryResourceCostLate(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies secondary X-value modifiers.</para>
        ///     <para xml:lang="zh-CN">应用次级 X 值修正。</para>
        /// </summary>
        public static int ModifyXValue(SecondaryResourceXContext context, int value)
        {
            return IterateListeners(context.CombatState, context.Card).Aggregate(value,
                (current, listener) => listener.ModifySecondaryResourceXValue(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether every listener permits a resource gain.</para>
        ///     <para xml:lang="zh-CN">判断所有监听器是否都允许增加资源。</para>
        /// </summary>
        public static bool ShouldGain(SecondaryResourceContext context, decimal amount)
        {
            return IterateListeners(context.CombatState, context.Source)
                .All(listener => listener.ShouldGainSecondaryResource(context, amount));
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether every listener permits a resource payment.</para>
        ///     <para xml:lang="zh-CN">判断所有监听器是否都允许支付资源。</para>
        /// </summary>
        public static bool ShouldSpend(SecondaryResourceSpendContext context)
        {
            return IterateListeners(context.CombatState, context.Card, context.Source)
                .All(listener => listener.ShouldSpendSecondaryResource(context));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies insufficient-payment policy modifiers.</para>
        ///     <para xml:lang="zh-CN">应用资源不足支付策略修正。</para>
        /// </summary>
        public static SecondaryResourceInsufficientPayment ModifyInsufficientPayment(
            SecondaryResourceInsufficientPaymentContext context,
            SecondaryResourceInsufficientPayment payment)
        {
            return IterateListeners(context.CombatState, context.Card, context.Source).Aggregate(payment,
                (current, listener) => listener.ModifySecondaryResourceInsufficientPayment(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Applies replacement-payment planners for a shortfall.</para>
        ///     <para xml:lang="zh-CN">应用缺口替代支付规划器。</para>
        /// </summary>
        public static SecondaryResourceShortfallResolution ResolveShortfall(
            SecondaryResourceShortfallResolutionContext context,
            SecondaryResourceShortfallResolution resolution)
        {
            return IterateListeners(context.CombatState, context.Card, context.Source).Aggregate(resolution,
                (current, listener) => listener.ResolveSecondaryResourceShortfall(context, current));
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether every listener permits a reset.</para>
        ///     <para xml:lang="zh-CN">判断所有监听器是否都允许重置资源。</para>
        /// </summary>
        public static bool ShouldReset(SecondaryResourceContext context)
        {
            return IterateListeners(context.CombatState, context.Source)
                .All(listener => listener.ShouldResetSecondaryResource(context));
        }

        /// <summary>
        ///     <para xml:lang="en">Runs listeners after an amount changes.</para>
        ///     <para xml:lang="zh-CN">在数量变化后运行监听器。</para>
        /// </summary>
        public static async Task AfterChanged(SecondaryResourceChangeContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState, context.Source))
                await listener.AfterSecondaryResourceChanged(context);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs listeners after a resource payment.</para>
        ///     <para xml:lang="zh-CN">在资源支付后运行监听器。</para>
        /// </summary>
        public static async Task AfterSpent(SecondaryResourceSpendContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState, context.Card, context.Source))
                await listener.AfterSecondaryResourceSpent(context);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs listeners after a payment with a remaining shortfall.</para>
        ///     <para xml:lang="zh-CN">在仍有缺口的支付提交后运行监听器。</para>
        /// </summary>
        public static async Task AfterShortfallPayment(SecondaryResourceShortfallContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState, context.Card, context.Source))
                await listener.AfterSecondaryResourceShortfallPayment(context);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs listeners after a reset.</para>
        ///     <para xml:lang="zh-CN">在资源重置后运行监听器。</para>
        /// </summary>
        public static async Task AfterReset(SecondaryResourceChangeContext context)
        {
            foreach (var listener in IterateListeners(context.CombatState, context.Source))
                await listener.AfterSecondaryResourceReset(context);
        }

        private static IEnumerable<ISecondaryResourceHookListener> IterateListeners(
            CombatStateLike combatState,
            params AbstractModel?[] extraModels)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                yield break;

            var combatExtraModels = GetCombatExtraModels(extraModels);
            foreach (var entry in ModelHookListenerDispatcher.FromCombat(
                         combatState,
                         GlobalListeners,
                         combatExtraModels))
                yield return entry.Listener;
        }

        private static IEnumerable<ISecondaryResourceHookListener> IterateCostListeners(
            CombatStateLike combatState,
            params AbstractModel?[] extraModels)
        {
            if (!ModSecondaryResourceRegistry.HasAny)
                yield break;

            var combatExtraModels = GetCombatExtraModels(extraModels);
            foreach (var entry in ModelHookListenerDispatcher.FromCombatWithAdapters(
                         combatState,
                         GlobalListeners,
                         SecondaryResourceModelHookRegistry.Bind,
                         combatExtraModels))
                yield return entry.Listener;
        }

        private static AbstractModel?[] GetCombatExtraModels(AbstractModel?[] models)
        {
            var validCount = models.Count(model => model?.ShouldReceiveCombatHooks == true);

            if (validCount == models.Length)
                return models;
            if (validCount == 0)
                return [];

            var result = new AbstractModel?[validCount];
            var resultIndex = 0;
            foreach (var model in models)
                if (model?.ShouldReceiveCombatHooks == true)
                    result[resultIndex++] = model;

            return result;
        }
    }
}
