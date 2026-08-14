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
using STS2RitsuLib.Interactions.RightClick.Patches;
using STS2RitsuLib.Patching;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Displays a <see cref="ModCardPileUiStyle.ExtraHand" /> pile with interactive vanilla hand-card
    ///         holders.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用可交互的原版手牌容器显示 <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆。
    ///     </para>
    /// </summary>
    public sealed partial class NModExtraHand : Control
    {
        internal const float DefaultChromeWidth = 600f;
        internal const float DefaultChromeHeight = 280f;
        internal static readonly Vector2 DefaultChromeSize = new(DefaultChromeWidth, DefaultChromeHeight);
        private static readonly ModCardPileExtraHandSpec DefaultLayout = new();

        private static readonly Action<NHandCardHolder, NCard> SetHolderCard =
            PrivateAccess.DeclaredMethodDelegate<NHandCardHolder, Action<NHandCardHolder, NCard>>(
                "SetCard", typeof(NCard));

        private readonly HashSet<CardModel> _arrivingCards = [];

        private readonly Control _cardLayer = new()
        {
            Name = "Cards",
            MouseFilter = MouseFilterEnum.Pass,
        };

        private readonly Dictionary<CardModel, NHandCardHolder> _holders = [];
        private Tween? _disabledTween;
        private NHandCardHolder? _focusedHolder;
        private bool _invalidBuiltInLayoutWarningLogged;
        private bool _invalidLayoutResolverWarningLogged;
        private bool _isDisabled;
        private bool _turnPresentationDisabled;
        private ModCardPile? _pile;
        private Player? _player;
        private NPlayerHand? _vanillaHand;
        private double _visualRefreshElapsed;

        /// <summary>
        ///     <para xml:lang="en">Gets the registered definition represented by this container.</para>
        ///     <para xml:lang="zh-CN">获取此容器所表示的已注册定义。</para>
        /// </summary>
        public ModCardPileDefinition Definition { get; private init; } = null!;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the runtime manual-card-play setting requested for this container. A
        ///         <see langword="true" /> value still requires the surrounding combat and base-game hand state to
        ///         permit play. Querying this property does not change container state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此容器请求的运行时手动出牌设置。即使值为 <see langword="true" />，实际出牌仍要求周围
        ///         战斗状态与游戏原有手牌状态同时允许。查询此属性不会改变容器状态。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         A new container starts with <see cref="ModCardPileExtraHandSpec.AllowCardPlay" />. Runtime changes
        ///         apply only to this container and do not persist when combat creates another container. Runtime
        ///         availability cannot grant card-play capability when the definition disallows it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         新容器使用 <see cref="ModCardPileExtraHandSpec.AllowCardPlay" /> 作为初始值。运行时更改仅作用于
        ///         当前容器，不会在战斗创建新容器后保留；定义未允许出牌时，运行时可用性不能授予出牌能力。
        ///     </para>
        /// </remarks>
        public bool CardPlayEnabled { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an extra-hand container for <paramref name="definition" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <paramref name="definition" /> 创建额外手牌容器。</para>
        /// </summary>
        public static NModExtraHand Create(ModCardPileDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var hand = new NModExtraHand
            {
                Definition = definition,
                CardPlayEnabled = definition.ExtraHand.AllowCardPlay,
                Name = $"ModExtraHand_{definition.Id}",
                MouseFilter = MouseFilterEnum.Pass,
                CustomMinimumSize = DefaultChromeSize,
                Size = DefaultChromeSize,
                PivotOffset = new(DefaultChromeWidth * 0.5f, DefaultChromeHeight * 0.5f),
            };
            hand.AddChild(hand._cardLayer);
            return hand;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds the container to <paramref name="player" /> and starts mirroring its runtime pile.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将容器绑定到 <paramref name="player" />，并开始同步其运行时牌堆。
        ///     </para>
        /// </summary>
        public void Initialize(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            _player = player;
            AttachVanillaHand(NPlayerHand.Instance ?? NCombatRoom.Instance?.Ui?.Hand);
            AttachPile(ModCardPileStorage.Resolve(Definition.PileType, player));
            if (player.Creature.CombatState is { } state)
                UpdateDisabledState(state);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enables or disables current manual-card-play availability for this container at runtime. The last
        ///         completed call determines the state. Repeating the current value has no effect.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在运行时启用或禁用此容器当前的手动出牌可用性。最后完成的调用决定当前状态；重复设置当前值
        ///         不会产生额外效果。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Disabling updates existing and future holders, playable highlights, controller navigation, and
        ///         disabled presentation. Active uncommitted targeting is canceled and restored to this hand;
        ///         already queued card actions are not canceled. Base-game hand selection modes temporarily keep
        ///         this hand unavailable independently of the requested runtime value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         禁用时会同步现有与后续卡牌容器、可打出高亮、手柄导航及禁用表现。尚未提交的活动目标选择
        ///         会被取消并将卡牌恢复到此手牌区；已经进入队列的卡牌行动不会被取消。游戏原有手牌进入选牌
        ///         模式时，也会独立于所请求的运行时值而临时保持此手牌区不可用。
        ///     </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">
        ///         <paramref name="enabled" /> is <see langword="true" />, but this container's definition does not
        ///         grant <see cref="ModCardPileExtraHandSpec.AllowCardPlay" /> capability.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="enabled" /> 为 <see langword="true" />，但此容器的定义未授予
        ///         <see cref="ModCardPileExtraHandSpec.AllowCardPlay" /> 能力。
        ///     </para>
        /// </exception>
        public void SetCardPlayEnabled(bool enabled)
        {
            if (enabled && !Definition.ExtraHand.AllowCardPlay)
                throw new InvalidOperationException(
                    $"Extra hand '{Definition.Id}' does not allow manual card play.");
            if (CardPlayEnabled == enabled)
                return;

            CardPlayEnabled = enabled;
            if (!enabled)
                ModExtraHandPlayCoordinator.CancelActiveTargeting(this);
            RefreshCardPlayAvailability();
            UpdateDisabledPresentation();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the displayed node for <paramref name="card" />, or <see langword="null" /> when it is
        ///         not mounted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="card" /> 的显示节点；该卡牌未挂载时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        public NCard? GetCard(CardModel card)
        {
            return GetHolder(card)?.CardNode;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the interactive holder for <paramref name="card" />, or <see langword="null" /> when it
        ///         is not mounted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="card" /> 的交互式卡牌容器；该卡牌未挂载时返回
        ///         <see langword="null" />。
        ///     </para>
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
            CombatManager.Instance.PlayerActionsDisabledChanged += OnPlayerActionsDisabledChanged;
            CombatManager.Instance.PlayerUnendedTurn += OnPlayerUnendedTurn;
            CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
            ModCardPileButtonRegistry.RegisterExtraHand(Definition, this);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            base._ExitTree();
            _disabledTween?.Kill();
            DetachVanillaHand();
            CombatManager.Instance.PlayerActionsDisabledChanged -= OnPlayerActionsDisabledChanged;
            CombatManager.Instance.PlayerUnendedTurn -= OnPlayerUnendedTurn;
            CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
            ModExtraHandPlayCoordinator.DetachContainer(this);
            ModCardPileButtonRegistry.UnregisterExtraHand(Definition, this);
            DetachPile();
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            base._Process(delta);
            NotifyArrivedCards();
            _visualRefreshElapsed += delta;
            if (_visualRefreshElapsed < 0.1)
                return;

            _visualRefreshElapsed = 0;
            foreach (var holder in _holders.Values)
                RefreshHolderVisuals(holder);
        }

        internal void ReleaseHolderForQueuedPlay(CardModel card)
        {
            _holders.Remove(card, out var holder);
            if (ReferenceEquals(_focusedHolder, holder))
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

            if (holder.GetParent() != _cardLayer)
                holder.Reparent(_cardLayer);
            holder.CancelDrag();
            holder.SetIndexLabel(0);
            holder.Hitbox.MouseFilter = MouseFilterEnum.Stop;
            ApplyCardPlayAvailability(holder);
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

        internal bool TryBeginHandEntryAnimation(NCard sourceCard)
        {
            if (!Definition.CardShouldBeVisible
                || sourceCard.Model is not { } card
                || card.Pile?.Type != Definition.PileType
                || GetHolder(card) is not { } holder)
                return false;

            var sourcePosition = sourceCard.GlobalPosition;
            if (holder.CardNode == null)
                SetHolderCard(holder, sourceCard);
            else if (!ReferenceEquals(holder.CardNode, sourceCard)) sourceCard.QueueFree();

            holder.GlobalPosition = sourcePosition;
            holder.SetAngleInstantly(0f);
            holder.SetScaleInstantly(Vector2.One);
            if (holder.CardNode != null)
                holder.CardNode.Position = Vector2.Zero;
            _arrivingCards.Add(card);
            ArrangeCards();
            return true;
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

            foreach (var holder in _holders.Values.Where(IsInstanceValid))
                holder.QueueFree();
            _holders.Clear();
            _arrivingCards.Clear();
            _focusedHolder = null;
        }

        private void OnCardAdded(CardModel card)
        {
            AddVisualFor(card, null, true);
            ArrangeCards();
        }

        private void OnCardRemoved(CardModel card)
        {
            _arrivingCards.Remove(card);
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

        private void NotifyArrivedCards()
        {
            foreach (var card in _arrivingCards.ToArray())
            {
                var holder = GetHolder(card);
                if (holder == null)
                {
                    _arrivingCards.Remove(card);
                    continue;
                }

                if (holder.Position.DistanceSquaredTo(holder.TargetPosition) >= 1f)
                    continue;

                _arrivingCards.Remove(card);
                NotifyCardArrived(card);
            }
        }

        private void AddVisualFor(CardModel card, NCard? existingCard, bool invokeCreated)
        {
            if (!Definition.CardShouldBeVisible || _holders.ContainsKey(card))
                return;

            var hand = NPlayerHand.Instance ?? NCombatRoom.Instance?.Ui?.Hand;
            var ncard = existingCard ?? NCard.Create(card);
            if (hand == null || ncard == null)
                return;
            AttachVanillaHand(hand);

            var holder = NHandCardHolder.Create(ncard, hand);
            _holders[card] = holder;
            _cardLayer.AddChild(holder);
            ModRightClickCardHolderPatch.ConnectModPileHolder(holder, Definition.PileType);
            holder.SetIndexLabel(0);
            ApplyCardPlayAvailability(holder);
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
            if (!IsCardPlayAvailable || holder.CardModel == null || _player == null)
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

        private bool IsCardPlayAvailable => CardPlayEnabled
                                            && _vanillaHand?.CurrentMode is null or NPlayerHand.Mode.Play;

        private void AttachVanillaHand(NPlayerHand? hand)
        {
            if (ReferenceEquals(_vanillaHand, hand))
                return;

            DetachVanillaHand();
            _vanillaHand = hand;
            if (_vanillaHand == null)
                return;

            _vanillaHand.ModeChanged += OnVanillaHandModeChanged;
            OnVanillaHandModeChanged();
        }

        private void DetachVanillaHand()
        {
            if (_vanillaHand != null && IsInstanceValid(_vanillaHand))
                _vanillaHand.ModeChanged -= OnVanillaHandModeChanged;
            _vanillaHand = null;
        }

        private void OnVanillaHandModeChanged()
        {
            RefreshCardPlayAvailability();
            UpdateDisabledPresentation();
        }

        private void OnPlayerActionsDisabledChanged(CombatState state)
        {
            UpdateDisabledState(state);
        }

        private void OnPlayerUnendedTurn(Player _)
        {
            if (_player?.Creature.CombatState is { } state)
                UpdateDisabledState(state);
        }

        private void OnCombatStateChanged(CombatState state)
        {
            UpdateDisabledState(state);
        }

        private void UpdateDisabledState(ICombatState state)
        {
            if (_player == null)
                return;

            var disabled = CombatManager.Instance.PlayerActionsDisabled;
            if (!disabled
                && CombatManager.Instance.PlayersTakingExtraTurn.Count > 0
                && !CombatManager.Instance.PlayersTakingExtraTurn.Contains(_player))
                disabled = true;

            if (!disabled)
            {
                _turnPresentationDisabled = false;
                UpdateDisabledPresentation();
                return;
            }

            var anotherPlayerIsNotReady = state.Players
                .Where(player => !ReferenceEquals(player, _player))
                .Any(player => !CombatManager.Instance.IsPlayerReadyToEndTurn(player));
            if (state.CurrentSide == CombatSide.Enemy || anotherPlayerIsNotReady)
            {
                _turnPresentationDisabled = true;
                UpdateDisabledPresentation();
            }
        }

        private void UpdateDisabledPresentation()
        {
            if (_turnPresentationDisabled || !IsCardPlayAvailable)
                AnimDisable();
            else
                AnimEnable();
        }

        private void AnimDisable()
        {
            if (_isDisabled)
                return;

            _isDisabled = true;
            SetControllerNavigationEnabled(false);
            AnimateDisabledStyle(Definition.ExtraHand.DisabledOffset, Definition.ExtraHand.DisabledModulate);
        }

        private void AnimEnable()
        {
            if (!_isDisabled)
                return;

            _isDisabled = false;
            SetControllerNavigationEnabled(true);
            AnimateDisabledStyle(Vector2.Zero, Colors.White);
        }

        private void AnimateDisabledStyle(Vector2 position, Color modulate)
        {
            _disabledTween?.Kill();
            var duration = Definition.ExtraHand.DisabledTransitionDuration;
            if (duration == 0.0 || !IsInsideTree())
            {
                _cardLayer.Position = position;
                _cardLayer.Modulate = modulate;
                return;
            }

            _disabledTween = CreateTween().SetParallel();
            _disabledTween.TweenProperty(_cardLayer, "position", position, duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _disabledTween.TweenProperty(_cardLayer, "modulate", modulate, duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        private void SetControllerNavigationEnabled(bool enabled)
        {
            foreach (var holder in _holders.Values.Where(IsInstanceValid))
                holder.FocusMode = enabled && IsCardPlayAvailable ? FocusModeEnum.All : FocusModeEnum.None;
        }

        private void RefreshCardPlayAvailability()
        {
            foreach (var holder in _holders.Values.Where(IsInstanceValid))
            {
                ApplyCardPlayAvailability(holder);
                RefreshHolderVisuals(holder);
            }
        }

        private void ApplyCardPlayAvailability(NHandCardHolder holder)
        {
            holder.SetClickable(IsCardPlayAvailable);
            holder.FocusMode = !_isDisabled && IsCardPlayAvailable ? FocusModeEnum.All : FocusModeEnum.None;
        }

        private void RefreshHolderVisuals(NHandCardHolder holder)
        {
            if (!IsInstanceValid(holder) || holder.CardNode == null || !holder.IsNodeReady())
                return;

            holder.UpdateCard();
            if (!Definition.ExtraHand.ShowPlayableGlow || !IsCardPlayAvailable)
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
            var spacingIsFinite = float.IsFinite(extra.Spacing);
            var cardScaleIsFinite = IsFiniteVector(extra.CardScale);
            var hoverScaleIsFinite = IsFiniteVector(extra.HoverScale);
            var spacing = spacingIsFinite ? extra.Spacing : DefaultLayout.Spacing;
            var cardScale = cardScaleIsFinite ? extra.CardScale : DefaultLayout.CardScale;
            var hoverScale = hoverScaleIsFinite ? extra.HoverScale : DefaultLayout.HoverScale;
            var totalSpan = spacing * (ordered.Length - 1);
            var totalSpanIsFinite = float.IsFinite(totalSpan);
            if (!totalSpanIsFinite)
                totalSpan = DefaultLayout.Spacing * (ordered.Length - 1);
            if (extra.Direction != ModExtraHandLayoutDirection.VanillaHand
                && (!spacingIsFinite || !cardScaleIsFinite || !hoverScaleIsFinite || !totalSpanIsFinite)
                && !_invalidBuiltInLayoutWarningLogged)
            {
                _invalidBuiltInLayoutWarningLogged = true;
                RitsuLibFramework.Logger.Warn(
                    $"[CardPiles] Extra-hand layout settings for '{Definition.Id}' contain a non-finite value; "
                    + "using built-in defaults for the affected values. "
                    + $"Spacing={extra.Spacing}, CardScale={extra.CardScale}, HoverScale={extra.HoverScale}.");
            }

            var center = Size * 0.5f;
            if (!IsFiniteVector(center))
                center = DefaultChromeSize * 0.5f;
            var focusedIndex = Array.FindIndex(ordered,
                entry => ReferenceEquals(entry.Holder, _focusedHolder));
            for (var i = 0; i < ordered.Length; i++)
            {
                var (card, holder) = ordered[i];
                holder.SetIndexLabel(0);
                var focused = ReferenceEquals(holder, _focusedHolder);
                var defaultTransform = extra.Direction == ModExtraHandLayoutDirection.VanillaHand
                    ? ResolveVanillaTransform(holder, i, ordered.Length, focusedIndex, center)
                    : ResolveLinearTransform(extra.Direction, spacing, cardScale, hoverScale, i, focused, center,
                        totalSpan);
                if (!IsFiniteTransform(defaultTransform))
                    defaultTransform = new(center, focused ? DefaultLayout.HoverScale : DefaultLayout.CardScale, 0f,
                        focused ? 1 : 0);
                var context = new ModExtraHandCardContext(
                    Definition, this, card, holder, i, ordered.Length, focused, defaultTransform);
                var resolvedTransform = extra.LayoutResolver?.Invoke(context);
                var transform = resolvedTransform is { } resolved && IsFiniteTransform(resolved)
                    ? resolved
                    : defaultTransform;
                if (resolvedTransform is { } invalid
                    && !IsFiniteTransform(invalid)
                    && !_invalidLayoutResolverWarningLogged)
                {
                    _invalidLayoutResolverWarningLogged = true;
                    RitsuLibFramework.Logger.Warn(
                        $"[CardPiles] LayoutResolver for '{Definition.Id}' returned a non-finite transform for "
                        + $"card '{card.Id}'; using the built-in transform. Returned transform: {invalid}.");
                }

                holder.SetDeferred("z_index", transform.ZIndex);
                if (extra.Direction == ModExtraHandLayoutDirection.VanillaHand
                    && focused
                    && extra.LayoutResolver == null)
                {
                    holder.SetAngleInstantly(transform.RotationDegrees);
                    holder.SetScaleInstantly(transform.Scale);
                    var currentX = float.IsFinite(holder.Position.X) ? holder.Position.X : transform.Position.X;
                    holder.Position = new(currentX, transform.Position.Y);
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

            return;

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
                ModExtraHandLayoutDirection direction,
                float spacing,
                Vector2 cardScale,
                Vector2 hoverScale,
                int index,
                bool focused,
                Vector2 center,
                float totalSpan)
            {
                var position = direction == ModExtraHandLayoutDirection.Horizontal
                    ? new Vector2(center.X - totalSpan * 0.5f + spacing * index, center.Y)
                    : new Vector2(center.X, center.Y - totalSpan * 0.5f + spacing * index);
                return new(
                    position,
                    focused ? hoverScale : cardScale,
                    0f,
                    focused ? 100 : 0);
            }

            static bool IsFiniteVector(Vector2 value)
            {
                return float.IsFinite(value.X) && float.IsFinite(value.Y);
            }

            static bool IsFiniteTransform(ModExtraHandCardTransform value)
            {
                return IsFiniteVector(value.Position)
                       && IsFiniteVector(value.Scale)
                       && float.IsFinite(value.RotationDegrees);
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
