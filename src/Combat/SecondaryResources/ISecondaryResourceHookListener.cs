namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Optional listener for secondary-resource gameplay hooks.
    ///     次级资源 gameplay hook 的可选监听器。
    /// </summary>
    public interface ISecondaryResourceHookListener
    {
        /// <summary>
        ///     Modifies a resource gain amount.
        ///     修正资源获得数量。
        /// </summary>
        decimal ModifySecondaryResourceGain(SecondaryResourceContext context, decimal amount)
        {
            return amount;
        }

        /// <summary>
        ///     Modifies the calculated max amount for max-bearing resources.
        ///     修正具有上限概念的资源所计算出的最大数量。
        /// </summary>
        decimal ModifyMaxSecondaryResource(SecondaryResourceMaxContext context, decimal amount)
        {
            return amount;
        }

        /// <summary>
        ///     Modifies a card secondary-resource cost.
        ///     修正卡牌的次级资源费用。
        /// </summary>
        decimal ModifySecondaryResourceCost(SecondaryResourceCostContext context, decimal cost)
        {
            return cost;
        }

        /// <summary>
        ///     Modifies a card secondary-resource cost after the normal cost-modifier pass.
        ///     在普通费用修正阶段之后修正卡牌的次级资源费用。
        /// </summary>
        decimal ModifySecondaryResourceCostLate(SecondaryResourceCostContext context, decimal cost)
        {
            return cost;
        }

        /// <summary>
        ///     Modifies a captured secondary X value.
        ///     修正捕获到的次级 X 值。
        /// </summary>
        int ModifySecondaryResourceXValue(SecondaryResourceXContext context, int value)
        {
            return value;
        }

        /// <summary>
        ///     Returns false to block a resource gain.
        ///     返回 false 以阻止资源获得。
        /// </summary>
        bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
        {
            return true;
        }

        /// <summary>
        ///     Returns false to block a resource spend.
        ///     返回 false 以阻止资源消耗。
        /// </summary>
        bool ShouldSpendSecondaryResource(SecondaryResourceSpendContext context)
        {
            return true;
        }

        /// <summary>
        ///     Dynamically modifies the insufficient-payment policy for a required card payment.
        ///     动态修正必需卡牌支付的资源不足策略。
        /// </summary>
        SecondaryResourceInsufficientPayment ModifySecondaryResourceInsufficientPayment(
            SecondaryResourceInsufficientPaymentContext context,
            SecondaryResourceInsufficientPayment payment)
        {
            return payment;
        }

        /// <summary>
        ///     Resolves whether another payment source can cover a required card-payment shortfall.
        ///     解析是否可用其他支付来源覆盖必需卡牌支付短缺。
        /// </summary>
        SecondaryResourceShortfallResolution ResolveSecondaryResourceShortfall(
            SecondaryResourceShortfallResolutionContext context,
            SecondaryResourceShortfallResolution current)
        {
            return current;
        }

        /// <summary>
        ///     Returns false to suppress the built-in turn-start reset for this resource.
        ///     返回 false 以阻止该资源的内建回合开始重置。
        /// </summary>
        bool ShouldResetSecondaryResource(SecondaryResourceContext context)
        {
            return true;
        }

        /// <summary>
        ///     Runs after a resource amount changes.
        ///     在资源数量变化后运行。
        /// </summary>
        Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Runs after a resource is spent.
        ///     在资源被消耗后运行。
        /// </summary>
        Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Runs after a required card payment was committed with a secondary-resource shortfall.
        ///     在必需卡牌支付以次级资源短缺形式提交后运行。
        /// </summary>
        Task AfterSecondaryResourceShortfallPayment(SecondaryResourceShortfallContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Runs after a built-in reset policy changes the resource.
        ///     在内建重置策略改变资源后运行。
        /// </summary>
        Task AfterSecondaryResourceReset(SecondaryResourceChangeContext context)
        {
            return Task.CompletedTask;
        }
    }
}
