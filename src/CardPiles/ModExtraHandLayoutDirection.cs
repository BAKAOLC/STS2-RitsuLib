namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Built-in arrangement direction for an extra-hand card pile.
    ///     额外手牌牌堆的内置排列方向。
    /// </summary>
    public enum ModExtraHandLayoutDirection
    {
        /// <summary>
        ///     Cards are arranged from left to right.
        ///     卡牌从左向右排列。
        /// </summary>
        Horizontal = 0,

        /// <summary>
        ///     Cards are arranged from top to bottom.
        ///     卡牌从上向下排列。
        /// </summary>
        Vertical = 1,

        /// <summary>
        ///     Cards use the vanilla hand fan, scale, rotation, and focused-card displacement rules.
        ///     卡牌使用原版手牌的扇形、缩放、旋转与焦点卡牌让位规则。
        /// </summary>
        VanillaHand = 2,
    }
}
