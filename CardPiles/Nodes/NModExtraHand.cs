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
    /// </remarks>
    public sealed partial class NModExtraHand : Control
    {
        private const float CardSpacing = 120f;
        private readonly Dictionary<CardModel, NCard> _cards = [];

        private ModCardPile? _pile;
        private Player? _player;

        /// <summary>
        ///     Back-reference to the registry entry.
        /// </summary>
        public ModCardPileDefinition Definition { get; private set; } = null!;

        /// <summary>
        ///     Builds a new extra-hand container for <paramref name="definition" />. Add it to the combat UI
        ///     and call <see cref="Initialize" /> with the local player once the pile is available.
        /// </summary>
        public static NModExtraHand Create(ModCardPileDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            return new()
            {
                Definition = definition,
                Name = $"ModExtraHand_{definition.Id}",
                MouseFilter = MouseFilterEnum.Pass,
                CustomMinimumSize = new(600f, 280f),
                Size = new(600f, 280f),
                PivotOffset = new(300f, 140f),
            };
        }

        /// <summary>
        ///     Binds the container to <paramref name="player" /> and begins mirroring the underlying pile.
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
