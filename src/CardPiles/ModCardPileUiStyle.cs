namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies the kind of UI control created for a mod card pile and how its
    ///         <see cref="ModCardPileAnchor" /> is interpreted.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定为模组卡牌牌堆创建的界面控件类型，以及如何解释其 <see cref="ModCardPileAnchor" />。
    ///     </para>
    /// </summary>
    public enum ModCardPileUiStyle
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates no UI control. Card-flight positions use a custom anchor when supplied, or the
        ///         viewport center plus the anchor offset otherwise.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不创建界面控件。提供自定义锚点时，卡牌飞行动画使用该锚点；否则使用视口中心加锚点偏移量。
        ///     </para>
        /// </summary>
        Headless = 0,

        /// <summary>
        ///     <para xml:lang="en">Creates a top-bar button next to the base-game deck button.</para>
        ///     <para xml:lang="zh-CN">在游戏原有牌组按钮旁创建顶部栏按钮。</para>
        /// </summary>
        TopBarDeck = 1,

        /// <summary>
        ///     <para xml:lang="en">Creates a combat UI button near the draw pile.</para>
        ///     <para xml:lang="zh-CN">在抽牌堆附近创建战斗界面按钮。</para>
        /// </summary>
        BottomLeft = 2,

        /// <summary>
        ///     <para xml:lang="en">Creates a combat UI button near the exhaust pile.</para>
        ///     <para xml:lang="zh-CN">在消耗牌堆附近创建战斗界面按钮。</para>
        /// </summary>
        BottomRight = 3,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an interactive extra-hand container. Visible cards use base-game-compatible holders
        ///         for focus, hover tips, highlighting, layout, and optional manual play.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建交互式额外手牌容器。可见卡牌使用兼容游戏原有行为的卡牌容器，支持焦点、悬停提示、
        ///         高亮、布局与可选的手动打出。
        ///     </para>
        /// </summary>
        ExtraHand = 4,
    }
}
