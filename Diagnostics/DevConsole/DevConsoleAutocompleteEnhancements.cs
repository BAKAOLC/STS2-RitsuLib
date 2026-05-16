namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Dev-console autocomplete behaviors that can be bound per command argument.
    /// </summary>
    [Flags]
    public enum DevConsoleAutocompleteEnhancements
    {
        /// <summary>
        ///     No enhancements.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Allows matching candidates by localized title text, not only entry id prefix.
        /// </summary>
        LocalizedTitleMatch = 1 << 0,

        /// <summary>
        ///     Appends <c> (localized-title)</c> to displayed candidates and keeps
        ///     <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" /> canonical.
        /// </summary>
        LocalizedDisplayLabels = 1 << 1,

        /// <summary>
        ///     Enables tail shorthand matching for ritsulib-registered mod entry ids when no custom matcher is supplied.
        /// </summary>
        RitsuLibOwnedIdShorthandMatch = 1 << 2,

        /// <summary>
        ///     Removes duplicate candidates while preserving order.
        /// </summary>
        DeduplicateCandidates = 1 << 3,

        /// <summary>
        ///     Localized title matching and display labels for model entry ids.
        /// </summary>
        ModelEntryId = LocalizedTitleMatch | LocalizedDisplayLabels | DeduplicateCandidates,

        /// <summary>
        ///     <see cref="ModelEntryId" /> plus ritsulib-owned id shorthand matching.
        /// </summary>
        RitsuLibModEntryId = ModelEntryId | RitsuLibOwnedIdShorthandMatch,
    }
}
