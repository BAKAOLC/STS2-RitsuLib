using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Optional per-character ordering for the card-library compendium pool-filter row. When the property is
    ///     <c>null</c> or empty, <see cref="CardLibraryCompendiumPlacementDefaults.DefaultCharacterRowRules" /> applies.
    /// </summary>
    public interface IModCharacterCardLibraryCompendiumPlacement
    {
        /// <summary>
        ///     Priority-ordered placement rules; the first rule with a resolvable vanilla anchor sets the base index,
        ///     and mod-to-mod constraints from the full list are merged afterward. When <c>null</c> or empty, the
        ///     default character row rules are used.
        /// </summary>
        IReadOnlyList<CardLibraryCompendiumPlacementRule>? CardLibraryCompendiumPlacementRules { get; }
    }
}
