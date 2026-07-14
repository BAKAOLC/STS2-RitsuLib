using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Live card presentation context supplied to extra-hand layout and lifecycle callbacks.
    ///     传给额外手牌布局与视觉生命周期回调的实时卡牌展示上下文。
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
        ///     Registered pile definition.
        ///     已注册的牌堆定义。
        /// </summary>
        public ModCardPileDefinition Definition { get; }

        /// <summary>
        ///     Live extra-hand container.
        ///     实时额外手牌容器。
        /// </summary>
        public NModExtraHand Container { get; }

        /// <summary>
        ///     Card model represented by this visual.
        ///     此视觉所代表的卡牌模型。
        /// </summary>
        public CardModel Card { get; }

        /// <summary>
        ///     Interactive vanilla-compatible card holder.
        ///     可交互且兼容原版的卡牌 holder。
        /// </summary>
        public NHandCardHolder Holder { get; }

        /// <summary>
        ///     Card node owned by <see cref="Holder" />.
        ///     由 <see cref="Holder" /> 持有的卡牌节点。
        /// </summary>
        public NCard CardNode => Holder.CardNode!;

        /// <summary>
        ///     Zero-based pile-order index.
        ///     从零开始的牌堆顺序索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Number of visible cards in the container.
        ///     容器内可见卡牌总数。
        /// </summary>
        public int Count { get; }

        /// <summary>
        ///     Whether this holder currently owns hover/controller focus.
        ///     此 holder 当前是否拥有悬停或手柄焦点。
        /// </summary>
        public bool IsFocused { get; }

        /// <summary>
        ///     Transform produced by the built-in layout before a custom resolver runs.
        ///     自定义 resolver 运行前由内置布局产生的变换。
        /// </summary>
        public ModExtraHandCardTransform DefaultTransform { get; }
    }
}
