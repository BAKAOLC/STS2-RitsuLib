using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides information for resolving the start position of a shuffle-flight visual whose source is
    ///         a registered mod pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于解析洗牌飞行动画起始位置的信息；该动画的来源是已注册的模组牌堆。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the start position RitsuLib resolved for this request.</para>
        ///     <para xml:lang="zh-CN">获取 RitsuLib 为本次请求解析的起始位置。</para>
        /// </summary>
        public Vector2 DefaultStartPosition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the source pile of the shuffle-flight visual.</para>
        ///     <para xml:lang="zh-CN">获取洗牌飞行动画的来源牌堆。</para>
        /// </summary>
        public CardPile StartPile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the destination pile of the shuffle-flight visual.</para>
        ///     <para xml:lang="zh-CN">获取洗牌飞行动画的目标牌堆。</para>
        /// </summary>
        public CardPile TargetPile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition of the source pile.</para>
        ///     <para xml:lang="zh-CN">获取来源牌堆的已注册定义。</para>
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
