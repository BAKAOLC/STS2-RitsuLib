namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Declares an optional card-library compendium pool filter for a stand-alone
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" />. By default such pools do not receive a filter row;
    ///     register this object to add one (icon path and stable id are required).
    /// </summary>
    public sealed class CardLibraryCompendiumSharedPoolFilterRegistration
    {
        /// <summary>
        ///     Mod that registered this filter (for logging).
        /// </summary>
        public required string OwningModId { get; init; }

        /// <summary>
        ///     Unique key among all compendium shared-pool filters (ASCII letters, digits, underscore only).
        /// </summary>
        public required string StableId { get; init; }

        /// <summary>
        ///     Godot resource path for the filter button icon (64px-style texture, same usage as mod character icons).
        /// </summary>
        public required string IconTexturePath { get; init; }

        /// <summary>
        ///     Concrete card pool type whose <c>AllCardIds</c> define the filter predicate.
        /// </summary>
        public required Type CardPoolType { get; init; }

        /// <summary>
        ///     Optional placement rules; when <c>null</c> or empty, the filter row is appended after existing siblings
        ///     (end of the filter strip). When non-empty, the first rule with a resolvable vanilla anchor sets the base
        ///     index and mod-to-mod constraints from the full list are merged (see patch documentation).
        /// </summary>
        public IReadOnlyList<CardLibraryCompendiumPlacementRule>? PlacementRules { get; init; }
    }
}
