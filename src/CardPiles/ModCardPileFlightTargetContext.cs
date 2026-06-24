using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Context passed to <see cref="ModCardPileSpec.FlightTargetPositionResolver" /> each time a card
    ///     requests a fly-in target position for a mod pile.
    ///     每当卡牌请求飞入 mod 牌堆的目标位置时，传给
    ///     <see cref="ModCardPileSpec.FlightTargetPositionResolver" /> 的上下文。
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
        ///     ritsulib 为此请求计算的默认目标位置。
        /// </summary>
        public Vector2 DefaultTargetPosition { get; }

        /// <summary>
        ///     Definition of the target pile.
        ///     目标牌堆的定义。
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     Live card node that is flying into the pile, when available.
        ///     正在飞入牌堆的实时卡牌节点（可用时）。
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
