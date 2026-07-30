namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies which surrounding interface elements to include when rendering a card to PNG.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定将卡牌渲染为 PNG 时包含哪些周边界面元素。
    ///     </para>
    /// </summary>
    public enum CardPngExportCaptureMode
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Includes only the <c>NCard</c> control, preserving the game's card frame, portrait, and text.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅包含 <c>NCard</c> 控件，并保留游戏中的卡牌边框、图像和文本。
        ///     </para>
        /// </summary>
        CardOnly,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Includes the card and fixed hover-tip columns. Text tips use <c>hover_tip.tscn</c>, while referenced
        ///         cards use scaled <c>NCard</c> controls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         包含卡牌和固定布局的悬停提示列。文本提示使用 <c>hover_tip.tscn</c>，引用的卡牌则使用缩放后的
        ///         <c>NCard</c> 控件。
        ///     </para>
        /// </summary>
        CardWithHoverTipsPanel,
    }
}
