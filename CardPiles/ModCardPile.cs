using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Runtime instance of a mod card pile. Behaves as a vanilla <see cref="CardPile" /> and simply carries
    ///     a back-reference to its <see cref="ModCardPileDefinition" /> so UI and patch code can look up mod
    ///     metadata (icon, localization, style).
    /// </summary>
    public sealed class ModCardPile : CardPile
    {
        /// <summary>
        ///     Creates a pile whose <see cref="CardPile.Type" /> matches <paramref name="definition" />'s minted value.
        /// </summary>
        /// <param name="definition">Registry entry this pile was created from.</param>
        public ModCardPile(ModCardPileDefinition definition) : base(definition.PileType)
        {
            Definition = definition;
        }

        /// <summary>
        ///     Back-reference to the immutable definition this pile was built from.
        /// </summary>
        public ModCardPileDefinition Definition { get; }
    }
}
