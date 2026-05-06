using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Common subset of data exposed by mod card pile flight contexts.
    /// </summary>
    public interface IModCardPileFlightContext
    {
        /// <summary>
        ///     Definition associated with the flight request.
        /// </summary>
        ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     Ritsulib's default position for this request.
        /// </summary>
        Vector2 DefaultPosition { get; }

        /// <summary>
        ///     Source pile for this request, when applicable.
        /// </summary>
        CardPile? StartPile { get; }

        /// <summary>
        ///     Destination pile for this request, when applicable.
        /// </summary>
        CardPile? TargetPile { get; }

        /// <summary>
        ///     Live card node involved in the flight, when available.
        /// </summary>
        NCard? CardNode { get; }

        /// <summary>
        ///     Card model involved in the flight, when available.
        /// </summary>
        CardModel? CardModel { get; }
    }
}
