using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightTargetPositionResolver" /> each time a card
    ///     requests a fly-in target position for a mod pile.
    /// </summary>
    public sealed class ModCardPileFlightTargetContext : IModCardPileFlightContext
    {
        internal ModCardPileFlightTargetContext(
            ModCardPileDefinition definition,
            NCard? cardNode,
            Vector2 defaultTargetPosition)
        {
            Definition = definition;
            CardNode = cardNode;
            DefaultTargetPosition = defaultTargetPosition;
        }

        /// <summary>
        ///     Ritsulib's default target position for this request.
        /// </summary>
        public Vector2 DefaultTargetPosition { get; }

        /// <summary>
        ///     Definition of the target pile.
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     Live card node that is flying into the pile, when available.
        /// </summary>
        public NCard? CardNode { get; }

        /// <inheritdoc />
        public Vector2 DefaultPosition => DefaultTargetPosition;

        /// <inheritdoc />
        public CardPile? StartPile => null;

        /// <inheritdoc />
        public CardPile? TargetPile => null;

        /// <inheritdoc />
        public CardModel? CardModel => CardNode?.Model;
    }
}
