namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">Specifies a built-in card arrangement for an extra-hand pile.</para>
    ///     <para xml:lang="zh-CN">指定额外手牌牌堆的内置卡牌排列方式。</para>
    /// </summary>
    public enum ModExtraHandLayoutDirection
    {
        /// <summary>
        ///     <para xml:lang="en">Arranges cards from left to right.</para>
        ///     <para xml:lang="zh-CN">从左向右排列卡牌。</para>
        /// </summary>
        Horizontal = 0,

        /// <summary>
        ///     <para xml:lang="en">Arranges cards from top to bottom.</para>
        ///     <para xml:lang="zh-CN">从上向下排列卡牌。</para>
        /// </summary>
        Vertical = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses the base-game hand's fan, scale, rotation, and focused-card displacement rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用游戏原有手牌的扇形、缩放、旋转与焦点卡牌让位规则。
        ///     </para>
        /// </summary>
        VanillaHand = 2,
    }
}
