using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightTargetPositionResolver" /> each time a card
    ///     requests a fly-in target position for a mod pile.
    /// </summary>
    public sealed class ModCardPileFlightTargetContext
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

        /// <summary>Definition of the target pile.</summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>Live card node that is flying into the pile, when available.</summary>
        public NCard? CardNode { get; }

        /// <summary>Ritsulib's default target position for this request.</summary>
        public Vector2 DefaultTargetPosition { get; }
    }
}
