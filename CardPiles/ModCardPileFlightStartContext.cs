using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightStartPositionResolver" /> when shuffle-style fly
    ///     visuals need a source/start position for a mod pile.
    /// </summary>
    public sealed class ModCardPileFlightStartContext
    {
        internal ModCardPileFlightStartContext(
            ModCardPileDefinition definition,
            CardPile startPile,
            CardPile targetPile,
            Vector2 defaultStartPosition)
        {
            Definition = definition;
            StartPile = startPile;
            TargetPile = targetPile;
            DefaultStartPosition = defaultStartPosition;
        }

        /// <summary>
        ///     Definition of the source pile.
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     Source pile for this shuffle fly visual.
        /// </summary>
        public CardPile StartPile { get; }

        /// <summary>
        ///     Destination pile for this shuffle fly visual.
        /// </summary>
        public CardPile TargetPile { get; }

        /// <summary>
        ///     Ritsulib's default start position for this request.
        /// </summary>
        public Vector2 DefaultStartPosition { get; }
    }
}
