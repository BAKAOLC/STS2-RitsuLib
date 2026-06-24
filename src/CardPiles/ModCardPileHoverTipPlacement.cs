namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     How the mod pile button positions its hover tip relative to the pile control. Use non-
    ///     <see cref="Auto" /> values when a fixed automatic rule does not match your
    ///     <see cref="ModCardPileAnchorKind.Custom" /> layout (for example a pile pinned near the bottom of the
    ///     screen often wants <see cref="AboveButtonCentered" /> so the tip grows upward from the button).
    ///     mod 牌堆按钮如何相对牌堆 control 放置 hover tip。当固定自动规则不适合你的
    ///     <see cref="ModCardPileAnchorKind.Custom" /> 布局时，使用非 <see cref="Auto" /> 值
    ///     （例如固定在屏幕底部附近的牌堆通常需要 <see cref="AboveButtonCentered" />，让 tip 从按钮向上展开）。
    /// </summary>
    public enum ModCardPileHoverTipPlacement
    {
        /// <summary>
        ///     Placement follows <see cref="ModCardPileUiStyle" />, <see cref="ModCardPileAnchorKind" />, and
        ///     top-bar deck rules inside <see cref="Nodes.NModCardPileButton" />.
        ///     放置方式遵循 <see cref="ModCardPileUiStyle" />、<see cref="ModCardPileAnchorKind" />，
        ///     以及 <see cref="Nodes.NModCardPileButton" /> 内的 top-bar deck 规则。
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Tip sits below the anchor rect; its trailing (right) edge aligns with the right edge of the rect
        ///     (same geometry as the vanilla top-bar deck button).
        ///     tip 位于 anchor rect 下方；其 trailing（右）边缘与 rect 右边缘对齐
        ///     （与原版 top-bar deck 按钮相同的几何布局）。
        /// </summary>
        BelowButtonTrailingEdge = 1,

        /// <summary>
        ///     Tip sits above the anchor rect, horizontally centered; content extends upward (good when the pile
        ///     sits low on the screen).
        ///     tip 位于 anchor rect 上方并水平居中；内容向上展开（适合牌堆位于屏幕较低处时）。
        /// </summary>
        AboveButtonCentered = 2,

        /// <summary>
        ///     Tip sits below the anchor rect, horizontally centered; content extends downward (good when the pile
        ///     sits high on the screen).
        ///     tip 位于 anchor rect 下方并水平居中；内容向下展开（适合牌堆位于屏幕较高处时）。
        /// </summary>
        BelowButtonCentered = 3,
    }
}
