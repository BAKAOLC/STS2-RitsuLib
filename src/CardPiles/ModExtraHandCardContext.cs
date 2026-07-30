using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides card and presentation state to extra-hand layout and lifecycle callbacks.
    ///     </para>
    ///     <para xml:lang="zh-CN">向额外手牌的布局与生命周期回调提供卡牌及展示状态。</para>
    /// </summary>
    public sealed class ModExtraHandCardContext
    {
        internal ModExtraHandCardContext(
            ModCardPileDefinition definition,
            NModExtraHand container,
            CardModel card,
            NHandCardHolder holder,
            int index,
            int count,
            bool isFocused,
            ModExtraHandCardTransform defaultTransform)
        {
            Definition = definition;
            Container = container;
            Card = card;
            Holder = holder;
            Index = index;
            Count = count;
            IsFocused = isFocused;
            DefaultTransform = defaultTransform;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition of the extra-hand pile.</para>
        ///     <para xml:lang="zh-CN">获取额外手牌牌堆的注册定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the extra-hand container.</para>
        ///     <para xml:lang="zh-CN">获取额外手牌容器。</para>
        /// </summary>
        public NModExtraHand Container { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card represented by the holder.</para>
        ///     <para xml:lang="zh-CN">获取该卡牌容器所表示的卡牌。</para>
        /// </summary>
        public CardModel Card { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the interactive card holder.</para>
        ///     <para xml:lang="zh-CN">获取可交互的卡牌容器。</para>
        /// </summary>
        public NHandCardHolder Holder { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card node owned by <see cref="Holder" />.</para>
        ///     <para xml:lang="zh-CN">获取由 <see cref="Holder" /> 持有的卡牌节点。</para>
        /// </summary>
        public NCard CardNode => Holder.CardNode!;

        /// <summary>
        ///     <para xml:lang="en">Gets the card's zero-based index in the visible pile order.</para>
        ///     <para xml:lang="zh-CN">获取该卡牌在可见牌堆顺序中的从零开始索引。</para>
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the number of visible cards in the container.</para>
        ///     <para xml:lang="zh-CN">获取容器中的可见卡牌数量。</para>
        /// </summary>
        public int Count { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this holder currently has pointer or controller focus.</para>
        ///     <para xml:lang="zh-CN">获取该卡牌容器当前是否具有指针或手柄焦点。</para>
        /// </summary>
        public bool IsFocused { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the transform produced by the built-in layout before the custom resolver runs.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取自定义解析器运行前由内置布局生成的变换。</para>
        /// </summary>
        public ModExtraHandCardTransform DefaultTransform { get; }
    }
}
