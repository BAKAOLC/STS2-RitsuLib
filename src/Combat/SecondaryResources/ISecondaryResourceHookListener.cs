namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Participates in secondary-resource calculations and state changes.</para>
    ///     <para xml:lang="zh-CN">参与次级资源计算和状态变化。</para>
    /// </summary>
    public interface ISecondaryResourceHookListener
    {
        /// <summary>
        ///     <para xml:lang="en">Modifies a proposed resource gain.</para>
        ///     <para xml:lang="zh-CN">修正拟增加的资源数量。</para>
        /// </summary>
        decimal ModifySecondaryResourceGain(SecondaryResourceContext context, decimal amount)
        {
            return amount;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies the calculated maximum for a capped resource.</para>
        ///     <para xml:lang="zh-CN">修正有上限资源计算出的最大数量。</para>
        /// </summary>
        decimal ModifyMaxSecondaryResource(SecondaryResourceMaxContext context, decimal amount)
        {
            return amount;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies a card's secondary-resource cost.</para>
        ///     <para xml:lang="zh-CN">修正卡牌的次级资源费用。</para>
        /// </summary>
        decimal ModifySecondaryResourceCost(SecondaryResourceCostContext context, decimal cost)
        {
            return cost;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies a card's cost after the normal cost-modification pass.</para>
        ///     <para xml:lang="zh-CN">在常规费用修正阶段之后修正卡牌费用。</para>
        /// </summary>
        decimal ModifySecondaryResourceCostLate(SecondaryResourceCostContext context, decimal cost)
        {
            return cost;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies the secondary X value captured for a card play.</para>
        ///     <para xml:lang="zh-CN">修正一次出牌所捕获的次级 X 值。</para>
        /// </summary>
        int ModifySecondaryResourceXValue(SecondaryResourceXContext context, int value)
        {
            return value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns <see langword="false" /> to prevent a resource gain.</para>
        ///     <para xml:lang="zh-CN">返回 <see langword="false" /> 可阻止资源增加。</para>
        /// </summary>
        bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns <see langword="false" /> to prevent a resource payment.</para>
        ///     <para xml:lang="zh-CN">返回 <see langword="false" /> 可阻止资源支付。</para>
        /// </summary>
        bool ShouldSpendSecondaryResource(SecondaryResourceSpendContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies the insufficient-payment policy for a required card payment.</para>
        ///     <para xml:lang="zh-CN">修正卡牌必需支付的资源不足策略。</para>
        /// </summary>
        SecondaryResourceInsufficientPayment ModifySecondaryResourceInsufficientPayment(
            SecondaryResourceInsufficientPaymentContext context,
            SecondaryResourceInsufficientPayment payment)
        {
            return payment;
        }

        /// <summary>
        ///     <para xml:lang="en">Plans replacement payment for some or all of a required-payment shortfall.</para>
        ///     <para xml:lang="zh-CN">为必需支付的部分或全部缺口规划替代支付。</para>
        /// </summary>
        SecondaryResourceShortfallResolution ResolveSecondaryResourceShortfall(
            SecondaryResourceShortfallResolutionContext context,
            SecondaryResourceShortfallResolution current)
        {
            return current;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="false" /> to prevent a reset performed through
        ///         <see cref="SecondaryResourceCmd.Reset" /> or a turn-start policy.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <see langword="false" /> 可阻止通过 <see cref="SecondaryResourceCmd.Reset" />
        ///         或回合开始策略执行的重置。
        ///     </para>
        /// </summary>
        bool ShouldResetSecondaryResource(SecondaryResourceContext context)
        {
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after a resource amount changes.</para>
        ///     <para xml:lang="zh-CN">在资源数量变化后运行。</para>
        /// </summary>
        Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after a resource payment is committed.</para>
        ///     <para xml:lang="zh-CN">在资源支付提交后运行。</para>
        /// </summary>
        Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after a required card payment with a remaining shortfall is committed.</para>
        ///     <para xml:lang="zh-CN">在仍有缺口的卡牌必需支付提交后运行。</para>
        /// </summary>
        Task AfterSecondaryResourceShortfallPayment(SecondaryResourceShortfallContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs after a reset changes the resource amount.</para>
        ///     <para xml:lang="zh-CN">在重置改变资源数量后运行。</para>
        /// </summary>
        Task AfterSecondaryResourceReset(SecondaryResourceChangeContext context)
        {
            return Task.CompletedTask;
        }
    }
}
