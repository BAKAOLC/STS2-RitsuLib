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
        ///     Appends registered mod pile ids to pile-argument candidate lists.
        /// </summary>
        IncludeModPileCandidates = 1 << 4,

        /// <summary>
        ///     Allows matching pile tokens by localized title text.
        /// </summary>
        PileNameLocalizedTitleMatch = 1 << 5,

        /// <summary>
        ///     Appends localized pile titles in parentheses for pile-argument candidates.
        /// </summary>
        PileNameDisplayLabels = 1 << 6,

        /// <summary>
        ///     Localized title matching and display labels for model entry ids.
        /// </summary>
        ModelEntryId = LocalizedTitleMatch | LocalizedDisplayLabels | DeduplicateCandidates,

        /// <summary>
        ///     <see cref="ModelEntryId" /> plus ritsulib-owned id shorthand matching.
        /// </summary>
        RitsuLibModEntryId = ModelEntryId | RitsuLibOwnedIdShorthandMatch,

        /// <summary>
        ///     Full pile-name autocomplete: mod pile ids, localized match/display, owned-id shorthand, de-dupe.
        /// </summary>
        PileName = IncludeModPileCandidates |
                   PileNameLocalizedTitleMatch |
                   PileNameDisplayLabels |
                   DeduplicateCandidates |
                   RitsuLibOwnedIdShorthandMatch,

        /// <summary>
        ///     Allows matching ancient choice tokens by option or relic localized title (and relic entry id).
        /// </summary>
        AncientChoiceLocalizedTitleMatch = 1 << 7,

        /// <summary>
        ///     Appends localized option/relic titles for <c>ancient</c> second-argument candidates.
        /// </summary>
        AncientChoiceDisplayLabels = 1 << 8,

        /// <summary>
        ///     <c>ancient</c> choice argument: localized match/display on event options (often relic rewards).
        /// </summary>
        AncientChoice = AncientChoiceLocalizedTitleMatch |
                        AncientChoiceDisplayLabels |
                        DeduplicateCandidates,
    }
}
