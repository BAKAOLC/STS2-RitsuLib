namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Optional presentation overrides for the RitsuLib mod settings screen. Safe to set at mod load time.
    ///     RitsuLib Mod 设置屏幕的可选呈现覆盖项。可安全地在 Mod 加载时设置。
    /// </summary>
    public static class ModSettingsUiPresentation
    {
        /// <summary>
        ///     Default maximum height for paragraph entry body text (from <c>AddParagraph</c>) before the block uses
        ///     internal vertical scrolling. <c>null</c> (default) means no height cap — text grows with the layout.
        ///     Per-entry <see cref="ParagraphModSettingsEntryDefinition.MaxBodyHeight" /> overrides this when set.
        ///     段落条目正文文本（来自 <c>AddParagraph</c>）在块使用
        ///     内部垂直滚动前的默认最大高度。<c>null</c>（默认值）表示无高度上限，文本随布局增长。
        ///     设置后，逐条目的 <see cref="ParagraphModSettingsEntryDefinition.MaxBodyHeight" /> 会覆盖此值。
        /// </summary>
        public static float? ParagraphMaxBodyHeight { get; set; }
    }
}
