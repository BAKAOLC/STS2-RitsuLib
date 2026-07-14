using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Interactive extra-hand container for <see cref="ModCardPileUiStyle.ExtraHand" /> piles. Cards are
    ///     hosted by vanilla <see cref="NHandCardHolder" /> nodes so focus, hover tips, controller navigation,
    ///     playable glow, and card-play targeting behave consistently with the player hand.
    ///     <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆使用的交互式额外手牌容器。卡牌由原版
    ///     <see cref="NHandCardHolder" /> 承载，使焦点、悬停提示、手柄导航、可打出发光与打牌目标选择
    ///     与玩家手牌保持一致。
    /// </summary>
    public sealed partial class NModExtraHand : Control
    {
        internal const float DefaultChromeWidth = 600f;
        internal const float DefaultChromeHeight = 280f;
        internal static readonly Vector2 DefaultChromeSize = new(DefaultChromeWidth, DefaultChromeHeight);

        private readonly Dictionary<CardModel, NHandCardHolder> _holders = [];
        private NHandCardHolder? _focusedHolder;
        private ModCardPile? _pile;
        private Player? _player;
        private double _visualRefreshElapsed;

        /// <summary>
        ///     Back-reference to the registry entry.
        ///     指向 registry entry 的反向引用。
        /// </summary>
        public ModCardPileDefinition Definition { get; private set; } = null!;

        /// <summary>
        ///     Builds a new extra-hand container for <paramref name="definition" />.
        ///     为 <paramref name="definition" /> 构建新的额外手牌容器。
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
        ///     Returns the displayed card node, or null when the card is not currently mounted.
        ///     返回已显示的卡牌节点；卡牌当前未挂载时返回 null。
        /// </summary>
        public NCard? GetCard(CardModel card)
        {
            return GetHolder(card)?.CardNode;
        }

        /// <summary>
        ///     Returns the interactive holder for a displayed card.
        ///     返回已显示卡牌的交互式 holder。
        /// </summary>
        public NHandCardHolder? GetHolder(CardModel card)
        {
            if (!_holders.TryGetValue(card, out var holder))
                return null;
            return IsInstanceValid(holder) ? holder : null;
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
            ModExtraHandPlayCoordinator.DetachContainer(this);
            ModCardPileButtonRegistry.UnregisterExtraHand(Definition, this);
            DetachPile();
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            base._Process(delta);
            _visualRefreshElapsed += delta;
            if (_visualRefreshElapsed < 0.1)
                return;

            _visualRefreshElapsed = 0;
            foreach (var holder in _holders.Values)
                RefreshHolderVisuals(holder);
        }

        internal void ReleaseHolderForQueuedPlay(CardModel card)
        {
            _holders.Remove(card);
            if (_focusedHolder?.CardModel == card)
                _focusedHolder = null;
            ArrangeCards();
        }

        internal void RestoreCancelledPlay(CardModel card, NHandCardHolder holder)
        {
            if (!IsInstanceValid(holder))
            {
                AddVisualFor(card, null, false);
                ArrangeCards();
                return;
            }

            if (holder.GetParent() != this)
                holder.Reparent(this);
            holder.CancelDrag();
            holder.Hitbox.MouseFilter = MouseFilterEnum.Stop;
            _holders[card] = holder;
            ArrangeCards();
        }

        internal void RestoreCancelledQueuedCard(CardModel card, NCard? cardNode)
        {
            AddVisualFor(card, cardNode, false);
            ArrangeCards();
        }

        internal void NotifyCardArrived(CardModel card)
        {
            var holder = GetHolder(card);
            if (holder != null)
                Definition.ExtraHand.OnCardArrived?.Invoke(BuildContext(card, holder));
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
                AddVisualFor(card, null, true);
            ArrangeCards();
        }

        private void DetachPile()
        {
            if (_pile != null)
            {
                _pile.CardAdded -= OnCardAdded;
                _pile.CardRemoved -= OnCardRemoved;
                _pile = null;
            }

            foreach (var holder in _holders.Values)
                if (IsInstanceValid(holder))
                    holder.QueueFree();
            _holders.Clear();
            _focusedHolder = null;
        }

        private void OnCardAdded(CardModel card)
        {
            AddVisualFor(card, null, true);
            ArrangeCards();
        }

        private void OnCardRemoved(CardModel card)
        {
            if (!_holders.Remove(card, out var holder))
                return;

            if (ReferenceEquals(_focusedHolder, holder))
            {
                _focusedHolder = null;
                RunManager.Instance.HoveredModelTracker.OnLocalCardUnhovered();
            }

            if (IsInstanceValid(holder))
                holder.QueueFree();
            ArrangeCards();
        }

        private void AddVisualFor(CardModel card, NCard? existingCard, bool invokeCreated)
        {
            if (!Definition.CardShouldBeVisible || _holders.ContainsKey(card))
                return;

            var hand = NPlayerHand.Instance ?? NCombatRoom.Instance?.Ui?.Hand;
            var ncard = existingCard ?? NCard.Create(card);
            if (hand == null || ncard == null)
                return;

            var holder = NHandCardHolder.Create(ncard, hand);
            _holders[card] = holder;
            AddChild(holder);
            holder.SetClickable(Definition.ExtraHand.AllowCardPlay);
            holder.Connect(NCardHolder.SignalName.Pressed,
                Callable.From<NCardHolder>(OnHolderPressed));
            holder.Connect(NHandCardHolder.SignalName.HolderMouseClicked,
                Callable.From<NCardHolder>(OnHolderPressed));
            holder.Connect(NHandCardHolder.SignalName.HolderFocused,
                Callable.From<NHandCardHolder>(OnHolderFocused));
            holder.Connect(NHandCardHolder.SignalName.HolderUnfocused,
                Callable.From<NHandCardHolder>(OnHolderUnfocused));
            RefreshHolderVisuals(holder);

            if (invokeCreated)
                Definition.ExtraHand.OnCardVisualCreated?.Invoke(BuildContext(card, holder));
        }

        private void OnHolderFocused(NHandCardHolder holder)
        {
            _focusedHolder = holder;
            if (holder.CardModel != null)
                RunManager.Instance.HoveredModelTracker.OnLocalCardHovered(holder.CardModel);
            ArrangeCards();
        }

        private void OnHolderUnfocused(NHandCardHolder holder)
        {
            if (ReferenceEquals(_focusedHolder, holder))
                _focusedHolder = null;
            RunManager.Instance.HoveredModelTracker.OnLocalCardUnhovered();
            ArrangeCards();
        }

        private void OnHolderPressed(NCardHolder holder)
        {
            if (holder is not NHandCardHolder handHolder || !CanStartCardPlay(handHolder))
                return;

            ModExtraHandPlayCoordinator.TryBegin(this, handHolder);
        }

        private bool CanStartCardPlay(NHandCardHolder holder)
        {
            if (!Definition.ExtraHand.AllowCardPlay || holder.CardModel == null || _player == null)
                return false;
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding)
                return false;
            if (CombatManager.Instance.PlayerActionsDisabled || NOverlayStack.Instance?.ScreenCount is > 0)
                return false;
            if (NTargetManager.Instance.IsInSelection || NPlayerHand.Instance?.InCardPlay == true)
                return false;
            if (NPlayerHand.Instance?.PeekButton.IsPeeking == true)
                return false;
            if (CombatManager.Instance.PlayersTakingExtraTurn.Count > 0
                && !CombatManager.Instance.PlayersTakingExtraTurn.Contains(_player))
                return false;
            return !ModExtraHandPlayCoordinator.IsPlaying;
        }

        private void RefreshHolderVisuals(NHandCardHolder holder)
        {
            if (!IsInstanceValid(holder) || holder.CardNode == null || !holder.IsNodeReady())
                return;

            holder.UpdateCard();
            if (!Definition.ExtraHand.ShowPlayableGlow)
                holder.CardNode.CardHighlight.AnimHide();
        }

        private void ArrangeCards()
        {
            var ordered = (_pile?.Cards ?? [])
                .Select(card => (Card: card, Holder: GetHolder(card)))
                .Where(entry => entry.Holder != null
                                && entry.Holder.IsInsideTree()
                                && !ModExtraHandPlayCoordinator.IsActiveHolder(entry.Holder))
                .Select(entry => (entry.Card, Holder: entry.Holder!))
                .ToArray();
            if (ordered.Length == 0)
                return;

            var extra = Definition.ExtraHand;
            var totalSpan = extra.Spacing * (ordered.Length - 1);
            var center = Size * 0.5f;
            var focusedIndex = Array.FindIndex(ordered,
                entry => ReferenceEquals(entry.Holder, _focusedHolder));
            for (var i = 0; i < ordered.Length; i++)
            {
                var (card, holder) = ordered[i];
                var focused = ReferenceEquals(holder, _focusedHolder);
                var defaultTransform = extra.Direction == ModExtraHandLayoutDirection.VanillaHand
                    ? ResolveVanillaTransform(holder, i, ordered.Length, focusedIndex, center)
                    : ResolveLinearTransform(extra, i, focused, center, totalSpan);
                var context = new ModExtraHandCardContext(
                    Definition, this, card, holder, i, ordered.Length, focused, defaultTransform);
                var transform = extra.LayoutResolver?.Invoke(context) ?? defaultTransform;

                holder.SetDeferred("z_index", transform.ZIndex);
                if (extra.Direction == ModExtraHandLayoutDirection.VanillaHand
                    && focused
                    && extra.LayoutResolver == null)
                {
                    holder.SetAngleInstantly(transform.RotationDegrees);
                    holder.SetScaleInstantly(transform.Scale);
                    holder.Position = new(holder.Position.X, transform.Position.Y);
                }

                holder.SetTargetPosition(transform.Position);
                holder.SetTargetScale(transform.Scale);
                holder.SetTargetAngle(transform.RotationDegrees);

                var previous = ordered[(i + ordered.Length - 1) % ordered.Length].Holder.GetPath();
                var next = ordered[(i + 1) % ordered.Length].Holder.GetPath();
                if (extra.Direction != ModExtraHandLayoutDirection.Vertical)
                {
                    holder.FocusNeighborLeft = previous;
                    holder.FocusNeighborRight = next;
                    holder.FocusNeighborBottom = holder.GetPath();
                }
                else
                {
                    holder.FocusNeighborTop = previous;
                    holder.FocusNeighborBottom = next;
                }
            }

            static ModExtraHandCardTransform ResolveVanillaTransform(
                NHandCardHolder holder,
                int index,
                int count,
                int focusedIndex,
                Vector2 center)
            {
                var focused = index == focusedIndex;
                var position = HandPosHelper.GetPosition(count, index);
                if (focusedIndex >= 0)
                {
                    var distance = Mathf.Abs(focusedIndex - index);
                    var displacement = Mathf.Lerp(100f, 0f, Mathf.Min(1f, distance / 4f));
                    position += Vector2.Left * Mathf.Sign(focusedIndex - index) * displacement;
                }

                if (focused)
                    position.Y = -holder.Hitbox.Size.Y * 0.5f + 2f;

                return new(
                    center + position,
                    focused ? Vector2.One : HandPosHelper.GetScale(count),
                    focused ? 0f : HandPosHelper.GetAngle(count, index),
                    focused ? 1 : 0);
            }

            static ModExtraHandCardTransform ResolveLinearTransform(
                ModCardPileExtraHandSpec extra,
                int index,
                bool focused,
                Vector2 center,
                float totalSpan)
            {
                var position = extra.Direction == ModExtraHandLayoutDirection.Horizontal
                    ? new Vector2(center.X - totalSpan * 0.5f + extra.Spacing * index, center.Y)
                    : new Vector2(center.X, center.Y - totalSpan * 0.5f + extra.Spacing * index);
                return new(
                    position,
                    focused ? extra.HoverScale : extra.CardScale,
                    0f,
                    focused ? 100 : 0);
            }
        }

        private ModExtraHandCardContext BuildContext(CardModel card, NHandCardHolder holder)
        {
            var visibleCards = (_pile?.Cards ?? [])
                .Where(candidate => GetHolder(candidate) != null)
                .ToArray();
            var index = Array.IndexOf(visibleCards, card);
            var defaultTransform = new ModExtraHandCardTransform(
                holder.Position,
                holder.Scale,
                holder.RotationDegrees,
                holder.ZIndex);
            return new(Definition, this, card, holder, Math.Max(0, index), visibleCards.Length,
                ReferenceEquals(holder, _focusedHolder), defaultTransform);
        }
    }
}
