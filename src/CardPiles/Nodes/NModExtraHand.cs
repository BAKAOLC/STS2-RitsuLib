using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Extra hand-like container for <see cref="ModCardPileUiStyle.ExtraHand" /> piles. Renders the pile's
    ///     cards as individual <see cref="NCard" /> nodes laid out horizontally, mirroring the vanilla
    ///     <c>NPlayerHand</c> in intent but using a much simpler layout.
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆使用的类似 extra hand 的容器。它将牌堆中的卡牌
    ///     渲染为水平排列的单个 <see cref="NCard" /> 节点，意图上对应原版 <c>NPlayerHand</c>，
    ///     但使用更简单的布局。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Rather than patching the <c>CardPileCmd.Add</c> async state machine (which would conflict with
    ///         baselib's existing transpiler), <see cref="NModExtraHand" /> listens to the pile's
    ///         <c>CardAdded</c> / <c>CardRemoved</c> events and keeps its own <see cref="NCard" /> roster in
    ///         sync. The vanilla fly animation delivers the card to the pile's registered target position
    ///         (returned by <see cref="ModCardPileLayout.GetTargetPosition" />), and this container then owns
    ///         the long-lived visual.
    ///     </para>
    ///     <para>
    ///         它不 patch <c>CardPileCmd.Add</c> async state machine（那会与 baselib 现有 transpiler 冲突），
    ///         而是由 <see cref="NModExtraHand" /> 监听牌堆的 <c>CardAdded</c> / <c>CardRemoved</c> 事件，并维护自己的 <see cref="NCard" />
    ///         roster。原版 fly 动画会把卡牌送到牌堆注册的目标位置
    ///         （由 <see cref="ModCardPileLayout.GetTargetPosition" /> 返回），随后此容器拥有长期存在的 visual。
    ///     </para>
    /// </remarks>
    public sealed partial class NModExtraHand : Control
    {
        internal const float DefaultChromeWidth = 600f;

        internal const float DefaultChromeHeight = 280f;

        private const float CardSpacing = 120f;

        internal static readonly Vector2 DefaultChromeSize = new(DefaultChromeWidth, DefaultChromeHeight);
        private readonly Dictionary<CardModel, NCard> _cards = [];

        private ModCardPile? _pile;
        private Player? _player;

        /// <summary>
        ///     Back-reference to the registry entry.
        ///     指向 registry entry 的反向引用。
        /// </summary>
        public ModCardPileDefinition Definition { get; private set; } = null!;

        /// <summary>
        ///     Builds a new extra-hand container for <paramref name="definition" />. Add it to the combat UI
        ///     and call <see cref="Initialize" /> with the local player once the pile is available.
        ///     为 <paramref name="definition" /> 构建新的 extra-hand 容器。将其加入 combat UI，并在牌堆可用后
        ///     用本地玩家调用 <see cref="Initialize" />。
        /// </summary>
        public static NModExtraHand Create(ModCardPileDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            return new()
            {
                Definition = definition,
                Name = $"ModExtraHand_{definition.Id}",
                MouseFilter = MouseFilterEnum.Pass,
                CustomMinimumSize = DefaultChromeSize,
                Size = DefaultChromeSize,
                PivotOffset = new(DefaultChromeWidth * 0.5f, DefaultChromeHeight * 0.5f),
            };
        }

        /// <summary>
        ///     Binds the container to <paramref name="player" /> and begins mirroring the underlying pile.
        ///     将容器绑定到 <paramref name="player" />，并开始镜像底层牌堆。
        /// </summary>
        public void Initialize(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            _player = player;
            AttachPile(ModCardPileStorage.Resolve(Definition.PileType, player));
        }

        /// <summary>
        ///     Returns the <see cref="NCard" /> displayed for <paramref name="card" />, or null when the card
        ///     is not currently in this pile.
        ///     返回为 <paramref name="card" /> 显示的 <see cref="NCard" />；当该卡牌当前不在此牌堆中时返回 null。
        /// </summary>
        public NCard? GetCard(CardModel card)
        {
            return _cards.GetValueOrDefault(card);
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            base._EnterTree();
            ModCardPileButtonRegistry.RegisterExtraHand(Definition, this);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            base._ExitTree();
            ModCardPileButtonRegistry.UnregisterExtraHand(Definition, this);
            DetachPile();
        }

        private void AttachPile(ModCardPile? pile)
        {
            if (ReferenceEquals(_pile, pile))
                return;

            DetachPile();
            _pile = pile;
            if (_pile == null)
                return;

            _pile.CardAdded += OnCardAdded;
            _pile.CardRemoved += OnCardRemoved;
            foreach (var card in _pile.Cards)
                AddVisualFor(card);
            ArrangeCards();
        }

        private void DetachPile()
        {
            if (_pile == null)
                return;

            _pile.CardAdded -= OnCardAdded;
            _pile.CardRemoved -= OnCardRemoved;
            _pile = null;

            foreach (var ncard in _cards.Values)
                ncard.QueueFreeSafelyIfValid();
            _cards.Clear();
        }

        private void OnCardAdded(CardModel card)
        {
            AddVisualFor(card);
            ArrangeCards();
        }

        private void OnCardRemoved(CardModel card)
        {
            if (!_cards.Remove(card, out var ncard))
                return;

            ncard.QueueFreeSafelyIfValid();
            ArrangeCards();
        }

        private void AddVisualFor(CardModel card)
        {
            if (_cards.ContainsKey(card))
                return;

            var ncard = NCard.Create(card);
            if (ncard == null)
                return;

            _cards[card] = ncard;
            AddChild(ncard);
        }

        private void ArrangeCards()
        {
            if (_cards.Count == 0)
                return;

            var orderedCards = _pile?.Cards
                                   .Select(card => _cards.GetValueOrDefault(card))
                                   .OfType<NCard>()
                                   .Where(ncard => ncard.IsInsideTree())
                                   .ToArray()
                               ?? _cards.Values.Where(ncard => ncard.IsInsideTree()).ToArray();
            if (orderedCards.Length == 0)
                return;

            var totalWidth = CardSpacing * (orderedCards.Length - 1);
            var startX = Size.X * 0.5f - totalWidth * 0.5f;
            var y = Size.Y * 0.5f;
            var i = 0;
            foreach (var ncard in orderedCards)
            {
                ncard.Position = new(startX + CardSpacing * i - ncard.Size.X * 0.5f,
                    y - ncard.Size.Y * 0.5f);
                i++;
            }
        }
    }

    internal static class NModExtraHandNCardExtensions
    {
        internal static void QueueFreeSafelyIfValid(this NCard ncard)
        {
            if (ncard == null)
                return;
            if (!GodotObject.IsInstanceValid(ncard))
                return;
            ncard.QueueFree();
        }
    }
}
