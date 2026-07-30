using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides information for resolving a card-flight target position in a registered mod pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于解析卡牌飞入已注册模组牌堆时目标位置的信息。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the target position RitsuLib resolved for this request.</para>
        ///     <para xml:lang="zh-CN">获取 RitsuLib 为本次请求解析的目标位置。</para>
        /// </summary>
        public Vector2 DefaultTargetPosition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition of the destination pile.</para>
        ///     <para xml:lang="zh-CN">获取目标牌堆的已注册定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the live card node being positioned, or <see langword="null" /> when the caller did not
        ///         provide one.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取正在定位的实时卡牌节点；调用方未提供节点时为 <see langword="null" />。
        ///     </para>
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
