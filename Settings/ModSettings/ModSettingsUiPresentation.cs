namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Optional presentation overrides for the RitsuLib mod settings screen. Safe to set at mod load time.
    /// </summary>
    public static class ModSettingsUiPresentation
    {
        /// <summary>
        ///     Default maximum height for paragraph entry body text (from <c>AddParagraph</c>) before the block uses
        ///     internal vertical scrolling. <c>null</c> (default) means no height cap — text grows with the layout.
        ///     Per-entry <see cref="ParagraphModSettingsEntryDefinition.MaxBodyHeight" /> overrides this when set.
        /// </summary>
        public static float? ParagraphMaxBodyHeight { get; set; }
    }
}
