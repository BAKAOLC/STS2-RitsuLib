namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Where a registered mod keyword’s inline card text (gold title + period) is merged into the rendered card
    ///     description, mirroring vanilla <c>CardKeywordOrder</c> behavior.
    /// </summary>
    public enum ModKeywordCardDescriptionPlacement
    {
        /// <summary>
        ///     Do not inject keyword text into the card description (default).
        /// </summary>
        None = 0,

        /// <summary>
        ///     Insert before the main description block (vanilla “before description” keywords).
        /// </summary>
        BeforeCardDescription = 1,

        /// <summary>
        ///     Append after the main description block (vanilla “after description” keywords).
        /// </summary>
        AfterCardDescription = 2,
    }
}
