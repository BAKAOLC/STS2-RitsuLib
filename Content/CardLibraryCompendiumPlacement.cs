namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Whether a compendium pool filter should appear immediately before or after its anchor.
    /// </summary>
    public enum CardLibraryCompendiumFilterInsertRelation
    {
        /// <summary>
        ///     Insert at the anchor node’s current sibling index (pushes the anchor and later nodes right).
        /// </summary>
        Before,

        /// <summary>
        ///     Insert immediately after the anchor (<c>anchorIndex + 1</c>).
        /// </summary>
        After,
    }

    /// <summary>
    ///     One placement preference in a priority list: the first rule whose anchor can be resolved wins for
    ///     vanilla anchors; mod-to-mod constraints from all rules are merged afterward (see
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.CardLibraryCompendiumPatch" /> summary).
    /// </summary>
    public sealed class CardLibraryCompendiumPlacementRule
    {
        /// <summary>
        ///     Unique name of a vanilla pool filter in the compendium strip. Prefer
        ///     <see cref="CardLibraryCompendiumVanillaFilterNames" /> constants
        ///     (e.g. <see cref="CardLibraryCompendiumVanillaFilterNames.MiscPool" />).
        /// </summary>
        public string? VanillaFilterAnchorUniqueName { get; init; }

        /// <summary>
        ///     Another mod character’s public <c>ModelId.Entry</c> (same string used in filter node
        ///     <c>MOD_FILTER_*</c>).
        /// </summary>
        public string? ModCharacterModelIdEntry { get; init; }

        /// <summary>
        ///     Stable id of another mod-registered shared compendium filter (<c>MOD_FILTER_SHARED_*</c>).
        /// </summary>
        public string? ModSharedCompendiumFilterStableId { get; init; }

        /// <inheritdoc cref="CardLibraryCompendiumFilterInsertRelation" />
        public CardLibraryCompendiumFilterInsertRelation Relation { get; init; }

        internal void ThrowIfInvalid()
        {
            var n = (string.IsNullOrWhiteSpace(VanillaFilterAnchorUniqueName) ? 0 : 1)
                    + (string.IsNullOrWhiteSpace(ModCharacterModelIdEntry) ? 0 : 1)
                    + (string.IsNullOrWhiteSpace(ModSharedCompendiumFilterStableId) ? 0 : 1);
            if (n != 1)
                throw new InvalidOperationException(
                    "Each placement rule must set exactly one of VanillaFilterAnchorUniqueName, " +
                    "ModCharacterModelIdEntry, or ModSharedCompendiumFilterStableId.");
        }

        internal static void ThrowIfInvalidRules(IReadOnlyList<CardLibraryCompendiumPlacementRule>? rules)
        {
            if (rules is null)
                return;

            foreach (var r in rules)
                r.ThrowIfInvalid();
        }
    }

    /// <summary>
    ///     Built-in placement rule lists for card-library compendium mod rows.
    /// </summary>
    public static class CardLibraryCompendiumPlacementDefaults
    {
        /// <summary>
        ///     Default mod-character row behavior: try immediately before colorless, then ancients, then misc (same
        ///     ordering as the original <c>CardLibraryCompendiumPatch</c> anchor scan).
        /// </summary>
        public static IReadOnlyList<CardLibraryCompendiumPlacementRule> DefaultCharacterRowRules { get; } =
        [
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.ColorlessPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.AncientsPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
            new()
            {
                VanillaFilterAnchorUniqueName = CardLibraryCompendiumVanillaFilterNames.MiscPool,
                Relation = CardLibraryCompendiumFilterInsertRelation.Before,
            },
        ];
    }
}
