namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Visual family of a mod card pile. Drives which UI chrome (top bar button, bottom-row combat button,
    ///     or extra hand) is created for the pile, and how <see cref="ModCardPileAnchor" /> is interpreted.
    ///     mod 卡牌牌堆的视觉族。决定为牌堆创建哪种 UI chrome（top bar button、底部战斗按钮或 extra hand），
    ///     以及如何解释 <see cref="ModCardPileAnchor" />。
    /// </summary>
    public enum ModCardPileUiStyle
    {
        /// <summary>
        ///     No UI chrome. Cards fly to the coordinate declared by <c>Anchor.Custom(...)</c>; suitable for
        ///     purely invisible holding piles.
        ///     无 UI chrome。卡牌会飞向 <c>Anchor.Custom(...)</c> 声明的坐标；适合纯不可见的 holding 牌堆。
        /// </summary>
        Headless = 0,

        /// <summary>
        ///     Button in the top bar, next to the vanilla deck button (<c>NTopBarDeckButton</c>).
        ///     top bar 中的按钮，位于原版 deck 按钮（<c>NTopBarDeckButton</c>）旁边。
        /// </summary>
        TopBarDeck = 1,

        /// <summary>
        ///     Button on the bottom-left of the combat UI (next to the draw pile).
        ///     战斗 UI 左下角的按钮（位于抽牌堆旁边）。
        /// </summary>
        BottomLeft = 2,

        /// <summary>
        ///     Button on the bottom-right of the combat UI (next to the exhaust pile).
        ///     战斗 UI 右下角的按钮（位于消耗牌堆旁边）。
        /// </summary>
        BottomRight = 3,

        /// <summary>
        ///     Interactive extra-hand container. Visible cards use vanilla-compatible holders for hover tips,
        ///     focus, glow, layout, and optional manual play; see <see cref="ModCardPileSpec.ExtraHand" />.
        ///     交互式额外手牌容器。可见卡牌使用兼容原版的 holder，支持悬停提示、焦点、发光、布局与
        ///     可选手动打出；参见 <see cref="ModCardPileSpec.ExtraHand" />。
        /// </summary>
        ExtraHand = 4,
    }
}
