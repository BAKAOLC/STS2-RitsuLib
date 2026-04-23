namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     How the mod pile button positions its hover tip relative to the pile control. Use non-
    ///     <see cref="Auto" /> values when a fixed automatic rule does not match your
    ///     <see cref="ModCardPileAnchorKind.Custom" /> layout (for example a pile pinned near the bottom of the
    ///     screen often wants <see cref="AboveButtonCentered" /> so the tip grows upward from the button).
    /// </summary>
    public enum ModCardPileHoverTipPlacement
    {
        /// <summary>
        ///     Placement follows <see cref="ModCardPileUiStyle" />, <see cref="ModCardPileAnchorKind" />, and
        ///     top-bar deck rules inside <see cref="Nodes.NModCardPileButton" />.
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Tip sits below the anchor rect; its trailing (right) edge aligns with the right edge of the rect
        ///     (same geometry as the vanilla top-bar deck button).
        /// </summary>
        BelowButtonTrailingEdge = 1,

        /// <summary>
        ///     Tip sits above the anchor rect, horizontally centered; content extends upward (good when the pile
        ///     sits low on the screen).
        /// </summary>
        AboveButtonCentered = 2,

        /// <summary>
        ///     Tip sits below the anchor rect, horizontally centered; content extends downward (good when the pile
        ///     sits high on the screen).
        /// </summary>
        BelowButtonCentered = 3,
    }
}
