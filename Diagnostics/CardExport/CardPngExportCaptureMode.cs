namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     How much surrounding UI to include when rasterizing a card.
    ///     栅格化卡牌时要包含多少周边 UI。
    /// </summary>
    public enum CardPngExportCaptureMode
    {
        /// <summary>
        ///     Only the <c>NCard</c> control (game-accurate card chrome, portrait, text).
        ///     仅 <c>NCard</c> 控件（与游戏一致的卡牌边框、肖像和文本）。
        /// </summary>
        CardOnly,

        /// <summary>
        ///     Card plus a right-hand column that approximates hover tips: text tips use the real
        ///     <c>hover_tip.tscn</c>; card-reference tips render as a scaled mini <c>NCard</c>.
        ///     Layout is fixed (not the same global positioning as in-game tooltips).
        ///     卡牌加右侧列，用于近似悬停提示：文本提示使用真实
        ///     <c>hover_tip.tscn</c>；卡牌引用提示渲染为缩放后的迷你 <c>NCard</c>。
        ///     布局是固定的（不同于游戏内工具提示的全局定位）。
        /// </summary>
        CardWithHoverTipsPanel,
    }
}
