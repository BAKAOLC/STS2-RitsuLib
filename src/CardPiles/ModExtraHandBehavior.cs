namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures optional base-game hand semantics for cards stored in an extra-hand pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">配置额外手牌牌堆中的卡牌所参与的可选游戏原有手牌语义。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         These values do not control presentation quality, multiplayer synchronization, action ownership,
    ///         resource-payment correctness, or card-node reuse. RitsuLib preserves those invariants whenever the
    ///         corresponding operation is enabled. Extra-hand cards remain separate from the base-game maximum
    ///         hand size and draw capacity under every combination.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         这些值不控制表现质量、多人同步、行动归属、资源支付正确性或卡牌节点复用。只要启用了对应操作，
    ///         RitsuLib 就会始终保证这些不变量。无论采用何种组合，额外手牌卡牌都与游戏原有最大手牌数量及
    ///         抽牌容量相互独立。
    ///     </para>
    /// </remarks>
    [Flags]
    public enum ModExtraHandBehavior
    {
        /// <summary>
        ///     <para xml:lang="en">Does not add optional hand semantics.</para>
        ///     <para xml:lang="zh-CN">不添加可选手牌语义。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies base-game end-of-turn in-hand and Ethereal processing to cards in the pile.
        ///     </para>
        ///     <para xml:lang="zh-CN">对牌堆中的卡牌应用游戏原有回合结束手牌效果及虚无处理。</para>
        /// </summary>
        ApplyHandTurnEndRules = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Includes cards in the base-game end-of-turn retain and discard flush.
        ///     </para>
        ///     <para xml:lang="zh-CN">在游戏原有回合结束保留及弃牌清理中包含这些卡牌。</para>
        /// </summary>
        FlushWithHand = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Enables every currently defined optional hand behavior.</para>
        ///     <para xml:lang="zh-CN">启用当前定义的全部可选手牌行为。</para>
        /// </summary>
        FullHand = ApplyHandTurnEndRules
                   | FlushWithHand,
    }
}
