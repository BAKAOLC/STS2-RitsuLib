using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightStartPositionResolver" /> when shuffle-style fly
    ///     visuals need a source/start position for a mod pile.
    /// </summary>
    public sealed class ModCardPileFlightStartContext : IModCardPileFlightContext
    {
        internal ModCardPileFlightStartContext(
            ModCardPileDefinition definition,
            CardPile startPile,
            CardPile targetPile,
            Vector2 defaultStartPosition,
            NCard? cardNode = null)
        {
            Definition = definition;
            StartPile = startPile;
            TargetPile = targetPile;
            DefaultStartPosition = defaultStartPosition;
            CardNode = cardNode;
        }

        /// <summary>
        ///     Ritsulib's default start position for this request.
        /// </summary>
        public Vector2 DefaultStartPosition { get; }

        /// <summary>
        ///     Source pile for this shuffle fly visual.
        /// </summary>
        public CardPile StartPile { get; }

        /// <summary>
        ///     Destination pile for this shuffle fly visual.
        /// </summary>
        public CardPile TargetPile { get; }

        /// <summary>
        ///     Definition of the source pile.
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <inheritdoc />
        public Vector2 DefaultPosition => DefaultStartPosition;

        /// <inheritdoc />
        public NCard? CardNode { get; }

        /// <inheritdoc />
        public CardModel? CardModel => CardNode?.Model;
    }
}
