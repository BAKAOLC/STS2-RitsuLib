namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how a mod pile control places its hover tip relative to its button bounds.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定模组牌堆控件如何相对于按钮边界放置悬停提示。
    ///     </para>
    /// </summary>
    public enum ModCardPileHoverTipPlacement
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Uses the built-in rule for the pile's UI style and anchor kind.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用该牌堆界面样式与锚点类型对应的内置规则。</para>
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places the tip below the button with their trailing edges aligned, matching the base-game
        ///         top-bar deck button.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将提示放在按钮下方并对齐二者的右边缘，与游戏原有的顶部栏牌组按钮一致。
        ///     </para>
        /// </summary>
        BelowButtonTrailingEdge = 1,

        /// <summary>
        ///     <para xml:lang="en">Places the tip above the button and centers it horizontally.</para>
        ///     <para xml:lang="zh-CN">将提示放在按钮上方并水平居中。</para>
        /// </summary>
        AboveButtonCentered = 2,

        /// <summary>
        ///     <para xml:lang="en">Places the tip below the button and centers it horizontally.</para>
        ///     <para xml:lang="zh-CN">将提示放在按钮下方并水平居中。</para>
        /// </summary>
        BelowButtonCentered = 3,
    }
}
