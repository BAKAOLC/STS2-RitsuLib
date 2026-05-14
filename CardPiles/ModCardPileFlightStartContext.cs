using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightStartPositionResolver" /> when shuffle-style fly
    ///     visuals need a source/start position for a mod pile.
    ///     当 shuffle 风格飞行动画需要 mod 牌堆的源/起点位置时，传给
    ///     <see cref="ModCardPileSpec.FlightStartPositionResolver" /> 的上下文。
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
        ///     ritsulib 为此请求计算的默认起点位置。
        /// </summary>
        public Vector2 DefaultStartPosition { get; }

        /// <summary>
        ///     Source pile for this shuffle fly visual.
        ///     此 shuffle 飞行动画的源牌堆。
        /// </summary>
        public CardPile StartPile { get; }

        /// <summary>
        ///     Destination pile for this shuffle fly visual.
        ///     此 shuffle 飞行动画的目标牌堆。
        /// </summary>
        public CardPile TargetPile { get; }

        /// <summary>
        ///     Definition of the source pile.
        ///     源牌堆的定义。
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
