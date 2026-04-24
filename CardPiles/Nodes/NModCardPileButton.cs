using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Procedurally built pile button: <see cref="ModCardPileUiStyle.TopBarDeck" /> and action mode
    ///     mirror the vanilla top-bar deck chrome (72×72 icon, deck count label); combat
    ///     <see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" />
    ///     mirror <c>NCombatCardPile</c> (full-slot icon, cloned <c>CountContainer</c>, 1.25 hover scale).
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" /> /
    ///             <see cref="ModCardPileUiStyle.TopBarDeck" /> buttons produced by
    ///             <see cref="ModCardPileRegistry" /> — the "pile mode" — where the backing data is a real
    ///             <see cref="ModCardPile" /> and the count tracks its card collection via events.
    ///         </item>
    ///         <item>
    ///             Non-pile top-bar action buttons produced by <see cref="ModTopBarButtonRegistry" /> — the
    ///             "action mode" — where the count comes from <see cref="ModTopBarButtonSpec.CountProvider" />
    ///             and the click runs <see cref="ModTopBarButtonSpec.OnClick" />. Action-mode buttons fall
    ///             back to the vanilla <c>%Deck</c>'s icon texture when
    ///             <see cref="ModTopBarButtonSpec.IconPath" /> is left unset so users don't have to ship a
    ///             custom PNG just to get a reasonably-styled button.
    ///         </item>
    ///     </list>
    ///     Sharing one node type here is deliberate — the user-facing request was to stop "splitting the
    ///     layout" for pile-backed vs. action-backed buttons, so both kinds look/animate/space identically.
    /// </summary>
    /// <remarks>
    ///     The button reacts to pointer hover / click via Godot's control signals, shows a
    ///     <see cref="HoverTip" /> built from the registered metadata, and on release either opens
    ///     <see cref="NCardPileScreen" /> (pile mode, mirroring the vanilla Draw / Discard / Exhaust buttons)
    ///     or dispatches <see cref="ModTopBarButtonDefinition.OnClick" /> (action mode).
    /// </remarks>
    public sealed partial class NModCardPileButton : Control
    {
        // Matches the vanilla `DeckContainer` MarginContainer (`scenes/ui/top_bar.tscn` line ~489) so
        // we occupy exactly one 80x80 slot inside the right-aligned HBoxContainer — the previous
        // 110x110 value made the button bigger than the deck and pushed the whole cluster around.
        private const float DefaultButtonWidth = 80f;

        private const float DefaultButtonHeight = 80f;

        private const float TopBarIconSize = 72f;

        private static readonly Vector2 TopBarHoverScale = Vector2.One * 1.1f;

        private static readonly Vector2 CombatPileHoverScale = Vector2.One * 1.25f;

        private static readonly StringName LabelThemeType = "Label";

        // Action-mode fields (null when Definition is set).
        private int _actionLastKnownCount = -1;

        // Shared state between the two modes.
        private Tween? _bumpTween;

        private Control? _countContainer;
        private MegaLabel _countLabel = null!;
        private int _currentCount;
        private bool _hovered;

        private HoverTip? _hoverTip;

        // Kept as the base Control type because we swap in either a TextureRect (when IconPath was
        // supplied) or a clone of the vanilla %Deck `Control/Icon` subtree (fallback for action mode
        // when no IconPath is given) — the latter is what makes bare action buttons render an icon
        // that is pixel-identical to the deck button the player is used to seeing.
        private Control _icon = null!;
        private Control _iconHost = null!;

        // Pile-mode fields (null when ActionDefinition is set).
        private ModCardPile? _pile;

        private Vector2 _pileHoverScale = TopBarHoverScale;
        private Player? _player;
        private bool _pressed;

        /// <summary>
        ///     Pile-mode registry entry. Non-null when the button is bound to a real
        ///     <see cref="ModCardPile" />; null while the button is running in action mode.
        /// </summary>
        public ModCardPileDefinition? Definition { get; private set; }

        /// <summary>
        ///     Action-mode registry entry. Non-null when the button was produced by
        ///     <see cref="ModTopBarButtonRegistry" /> rather than <see cref="ModCardPileRegistry" />; null in
        ///     pile mode.
        /// </summary>
        public ModTopBarButtonDefinition? ActionDefinition { get; private set; }

        /// <summary>True when this button is an action-mode instance (has no backing pile).</summary>
        public bool IsActionMode => ActionDefinition != null;

        private Control CountOffsetTarget => _countContainer ?? _countLabel;

        /// <summary>
        ///     Builds a new pile-mode button bound to <paramref name="definition" />.
        /// </summary>
        public static NModCardPileButton Create(ModCardPileDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var button = new NModCardPileButton
            {
                Definition = definition,
                Name = $"ModCardPileButton_{definition.Id}",
                MouseFilter = MouseFilterEnum.Stop,
                CustomMinimumSize = new(DefaultButtonWidth, DefaultButtonHeight),
                Size = new(DefaultButtonWidth, DefaultButtonHeight),
                PivotOffset = new(DefaultButtonWidth * 0.5f, DefaultButtonHeight * 0.5f),
            };
            button.BuildChildren();
            return button;
        }

        /// <summary>
        ///     Builds a new action-mode button bound to <paramref name="actionDefinition" />. The returned
        ///     node is identical to a pile-mode button visually (same icon box, same count label, same
        ///     hover / press animations) but dispatches clicks to
        ///     <see cref="ModTopBarButtonDefinition.OnClick" /> instead of opening
        ///     <see cref="NCardPileScreen" />, and polls
        ///     <see cref="ModTopBarButtonDefinition.CountProvider" /> on <see cref="Node._Process" /> for the
        ///     count display.
        /// </summary>
        public static NModCardPileButton CreateAction(ModTopBarButtonDefinition actionDefinition)
        {
            ArgumentNullException.ThrowIfNull(actionDefinition);

            var button = new NModCardPileButton
            {
                ActionDefinition = actionDefinition,
                Name = $"ModTopBarActionButton_{actionDefinition.Id}",
                MouseFilter = MouseFilterEnum.Stop,
                CustomMinimumSize = new(DefaultButtonWidth, DefaultButtonHeight),
                Size = new(DefaultButtonWidth, DefaultButtonHeight),
                PivotOffset = new(DefaultButtonWidth * 0.5f, DefaultButtonHeight * 0.5f),
            };
            button.BuildChildren();
            return button;
        }

        /// <summary>
        ///     Binds the button to <paramref name="player" />. In pile mode this resolves the underlying
        ///     <see cref="ModCardPile" /> and starts tracking card add / remove events; in action mode it
        ///     just remembers the player (used for <see cref="ModTopBarButtonContext" /> construction) and
        ///     primes the count label from the spec's <see cref="ModTopBarButtonSpec.CountProvider" />.
        /// </summary>
        public void Initialize(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            _player = player;

            if (ActionDefinition != null)
            {
                TryReplaceCountLabelWithVanillaDeckClone();
                // Action mode: when no IconPath was provided we swap our placeholder TextureRect for a
                // deep clone of the vanilla %Deck "Control/Icon" subtree. That's the only way to be
                // "exactly like the pile icon" — we inherit the sprite, the HSV shader material, and
                // any children the scene designer added, so bare action buttons cannot drift visually
                // from the vanilla deck button.
                if (string.IsNullOrWhiteSpace(ActionDefinition.IconPath))
                    TryReplaceIconWithVanillaDeckClone();
                _hoverTip = ModTopBarButtonHoverTipFactory.Create(ActionDefinition);
                PollActionCount(true);
                return;
            }

            if (UsesCombatBottomChrome())
                TryReplaceCountLabelWithVanillaCombatPileTemplate();
            else
                TryReplaceCountLabelWithVanillaDeckClone();

            if (Definition == null) return;
            AttachPile(ModCardPileStorage.Resolve(Definition.PileType, player));
            if (Definition.VisibleWhen != null)
                RefreshPileButtonVisibility();
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            base._EnterTree();
            if (Definition != null)
                ModCardPileButtonRegistry.RegisterButton(Definition, this);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            base._ExitTree();
            if (Definition != null)
                ModCardPileButtonRegistry.UnregisterButton(Definition, this);
            DetachPile();
            NHoverTipSet.Remove(this);
            _bumpTween?.Kill();
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            base._Process(delta);
            if (ActionDefinition != null)
            {
                // Action-mode bookkeeping: visibility and count are polled here because there is no pile to
                // subscribe to. Both predicates are best kept cheap per their docs.
                RefreshActionVisibility();
                PollActionCount(false);
                return;
            }

            if (Definition?.VisibleWhen != null)
                RefreshPileButtonVisibility();
        }

        /// <inheritdoc />
        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
                return;

            switch (mouse.Pressed)
            {
                case true when !_pressed:
                    _pressed = true;
                    OnPress();
                    return;
                case false when _pressed:
                    _pressed = false;
                    OnRelease();
                    break;
            }
        }

        private void BuildChildren()
        {
            if (UsesCombatBottomChrome())
                BuildCombatPileLayout();
            else
                BuildTopBarDeckLayout();

            _pileHoverScale = UsesCombatBottomChrome() ? CombatPileHoverScale : TopBarHoverScale;

            Connect(Control.SignalName.MouseEntered, Callable.From(OnMouseEntered));
            Connect(Control.SignalName.MouseExited, Callable.From(OnMouseExited));
        }

        private bool UsesCombatBottomChrome()
        {
            if (ActionDefinition != null)
                return false;
            return Definition?.Style is ModCardPileUiStyle.BottomLeft or ModCardPileUiStyle.BottomRight;
        }

        private void BuildTopBarDeckLayout()
        {
            _iconHost = new()
            {
                Name = "Control",
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorRight = 1f,
                AnchorBottom = 1f,
            };
            AddChild(_iconHost);

            var texture = ResolveIconTexture();
            var textureRect = new TextureRect
            {
                Name = "Icon",
                Texture = texture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new(TopBarIconSize, TopBarIconSize),
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -TopBarIconSize * 0.5f,
                OffsetTop = -TopBarIconSize * 0.5f,
                OffsetRight = TopBarIconSize * 0.5f,
                OffsetBottom = TopBarIconSize * 0.5f,
                PivotOffset = new(TopBarIconSize * 0.5f, TopBarIconSize * 0.5f - 2f),
            };
            _icon = textureRect;
            _iconHost.AddChild(_icon);

            _countLabel = new()
            {
                Name = "Count",
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                AnchorLeft = 1f,
                AnchorTop = 1f,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                OffsetLeft = -32f,
                OffsetTop = -36f,
                GrowHorizontal = GrowDirection.Begin,
                GrowVertical = GrowDirection.Begin,
                PivotOffset = new(14f, 18f),
            };
            EnsureProceduralCountLabelHasThemeFont(_countLabel);
            _countLabel.SetTextAutoSize("0");
            AddChild(_countLabel);
        }

        private void BuildCombatPileLayout()
        {
            _iconHost = new()
            {
                Name = "Control",
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorLeft = 0f,
                AnchorTop = 0f,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                OffsetLeft = 0f,
                OffsetTop = 0f,
                OffsetRight = 0f,
                OffsetBottom = 0f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
            };
            AddChild(_iconHost);

            var texture = ResolveIconTexture();
            var expand = Definition?.Style == ModCardPileUiStyle.BottomRight
                ? (TextureRect.ExpandModeEnum)1
                : (TextureRect.ExpandModeEnum)2;
            var textureRect = new TextureRect
            {
                Name = "Icon",
                Texture = texture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = expand,
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorLeft = 0f,
                AnchorTop = 0f,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(DefaultButtonWidth * 0.5f, DefaultButtonHeight * 0.5f),
            };
            _icon = textureRect;
            _iconHost.AddChild(_icon);

            _countLabel = new()
            {
                Name = "Count",
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -24f,
                OffsetTop = -24f,
                OffsetRight = 24f,
                OffsetBottom = 24f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(24f, 24f),
            };
            EnsureProceduralCountLabelHasCombatStyleFont(_countLabel);
            _countLabel.SetTextAutoSize("0");
            AddChild(_countLabel);
        }

        private static void EnsureProceduralCountLabelHasThemeFont(MegaLabel countLabel)
        {
            var vanilla = NRun.Instance?.GlobalUi?.TopBar?.Deck?.GetNodeOrNull<MegaLabel>("DeckCardCount");
            var font = vanilla?.GetThemeFont(ThemeConstants.Label.Font, LabelThemeType);
            if (font != null)
            {
                countLabel.AddThemeFontOverride(ThemeConstants.Label.Font, font);
                if (vanilla != null)
                    countLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize,
                        vanilla.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType));
                return;
            }

            var fallback = ThemeDB.FallbackFont;
            if (fallback != null)
            {
                countLabel.AddThemeFontOverride(ThemeConstants.Label.Font, fallback);
                countLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 28);
                return;
            }

            countLabel.AddThemeFontOverride(ThemeConstants.Label.Font, new SystemFont());
            countLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 28);
        }

        private static void EnsureProceduralCountLabelHasCombatStyleFont(MegaLabel countLabel)
        {
            var vanilla = NCombatRoom.Instance?.Ui?.DrawPile?.GetNodeOrNull<MegaLabel>("CountContainer/Count");
            var font = vanilla?.GetThemeFont(ThemeConstants.Label.Font, LabelThemeType);
            if (font != null)
            {
                countLabel.AddThemeFontOverride(ThemeConstants.Label.Font, font);
                if (vanilla != null)
                    countLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize,
                        vanilla.GetThemeFontSize(ThemeConstants.Label.FontSize, LabelThemeType));
                return;
            }

            EnsureProceduralCountLabelHasThemeFont(countLabel);
        }

        private Texture2D? ResolveIconTexture()
        {
            // Both modes use the same single-line rule: if an IconPath was provided AND it resolves on
            // disk we load it; otherwise we return null and rely on the action-mode Initialize() to try
            // borrowing the vanilla %Deck icon as a last resort. Pile-mode buttons keep the legacy
            // behaviour of "no texture" when nothing is provided — that's consistent with how old code
            // behaved before this refactor and avoids surprising existing mods.
            var path = Definition?.IconPath ?? ActionDefinition?.IconPath;
            if (!string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
                return ResourceLoader.Load<Texture2D>(path);
            return null;
        }

        /// <summary>
        ///     Replaces our procedurally-created <see cref="TextureRect" /> icon with a deep clone of the
        ///     vanilla <c>%Deck</c> button's <c>Control/Icon</c> subtree. This is the fidelity version of
        ///     the old "just copy the texture" fallback — it preserves the exact node hierarchy, shader
        ///     materials, and child sprites the scene designer set up for the deck icon, so bare
        ///     action-mode buttons look <i>indistinguishable</i> from the deck button's icon. Safely no-ops
        ///     (leaving our TextureRect in place) if the top bar isn't ready yet or the deck hasn't been
        ///     constructed — e.g. when registration fires from the main menu.
        /// </summary>
        private void TryReplaceIconWithVanillaDeckClone()
        {
            try
            {
                var deck = NRun.Instance?.GlobalUi?.TopBar?.Deck;
                var vanillaIcon = deck?.GetNodeOrNull<Control>("Control/Icon");
                if (vanillaIcon == null)
                    return;

                // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
                // Duplicate scripts + signals + groups so the clone is fully self-contained — without
                // this flag set Godot strips the ShaderMaterial binding that drives the deck icon's
                // HSV shader.
                var clone = vanillaIcon.Duplicate((int)(DuplicateFlags.Scripts
                                                        | DuplicateFlags.Signals
                                                        | DuplicateFlags.Groups));
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
                if (clone is not Control control)
                {
                    clone.QueueFree();
                    return;
                }

                // Drop our procedural Icon placeholder and plug the clone into the same "Control" host
                // node, at the same "Icon" name — keeps the vanilla path `Control/Icon` valid on our
                // button so any future code that looks it up by path (mirroring
                // `NTopBarButton._Ready`) keeps working. The clone already carries the correct 72x72
                // centered layout from the scene, so we do NOT overwrite its anchors / offsets here.
                var host = _icon.GetParent();
                _icon.QueueFree();
                _icon = control;
                host.AddChild(_icon);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModCardPileButton] Could not clone vanilla %Deck icon for action button: {ex.Message}");
            }
        }

        /// <summary>
        ///     Replaces our procedural <see cref="MegaLabel" /> count with a deep clone of the vanilla
        ///     <c>%Deck</c>'s <c>DeckCardCount</c> label. The scene designer configured it with a
        ///     specific <see cref="FontVariation" />, outline colour, outline_size=12, shadow offsets,
        ///     and font_size=28 — procedural labels have none of those by default, which is why our
        ///     old count label looked flat next to the deck's chiselled-looking digits. Silently leaves
        ///     the placeholder in place if the deck isn't constructed yet (e.g. we're bound before the
        ///     top bar exists) so we degrade gracefully rather than crashing.
        /// </summary>
        private void TryReplaceCountLabelWithVanillaDeckClone()
        {
            try
            {
                var deck = NRun.Instance?.GlobalUi?.TopBar?.Deck;
                var vanillaCount = deck?.GetNodeOrNull<MegaLabel>("DeckCardCount");
                if (vanillaCount == null)
                    return;

                // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
                var clone = vanillaCount.Duplicate((int)(DuplicateFlags.Scripts
                                                         | DuplicateFlags.Signals
                                                         | DuplicateFlags.Groups));
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
                if (clone is not MegaLabel cloneLabel)
                {
                    clone.QueueFree();
                    return;
                }

                // Preserve the clone's theme overrides (font / outline / shadow) but re-apply the
                // identity / positioning / mouse-filter flags our code expects. Anchors and offsets
                // come from the scene already — matching vanilla — so we don't touch those.
                var text = _countLabel.Text;
                var visible = _countLabel.Visible;
                _countLabel.QueueFree();
                cloneLabel.Name = "Count";
                cloneLabel.MouseFilter = MouseFilterEnum.Ignore;
                cloneLabel.Visible = visible;
                cloneLabel.SetTextAutoSize(string.IsNullOrEmpty(text) ? "0" : text);
                _countLabel = cloneLabel;
                AddChild(_countLabel);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModCardPileButton] Could not clone vanilla %Deck count label: {ex.Message}");
            }
        }

        private void TryReplaceCountLabelWithVanillaCombatPileTemplate()
        {
            var text = _countLabel.Text;
            var visible = _countLabel.Visible;
            _countContainer?.QueueFree();
            _countContainer = null;

            try
            {
                _countLabel.QueueFree();
                var source = ResolveVanillaCombatPileRootForCountTemplate();
                var vanillaCc = source?.GetNodeOrNull<Control>("CountContainer");
                if (vanillaCc == null)
                {
                    RestoreCombatFallbackCountLabel(text, visible);
                    return;
                }

                // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
                var dup = vanillaCc.Duplicate((int)(DuplicateFlags.Scripts
                                                    | DuplicateFlags.Signals
                                                    | DuplicateFlags.Groups));
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
                if (dup is not Control container)
                {
                    dup.QueueFree();
                    RestoreCombatFallbackCountLabel(text, visible);
                    return;
                }

                _countContainer = container;
                _countContainer.Name = "CountContainer";
                AddChild(_countContainer);
                MoveChild(_countContainer, _iconHost.GetIndex() + 1);
                _countLabel = _countContainer.GetNode<MegaLabel>("Count");
                _countLabel.MouseFilter = MouseFilterEnum.Ignore;
                _countLabel.Visible = visible;
                _countLabel.SetTextAutoSize(string.IsNullOrEmpty(text) ? "0" : text);
            }
            catch (Exception ex)
            {
                _countContainer?.QueueFree();
                _countContainer = null;
                RitsuLibFramework.Logger.Warn(
                    $"[ModCardPileButton] Could not clone vanilla combat CountContainer: {ex.Message}");
                RestoreCombatFallbackCountLabel(text, visible);
            }
        }

        private void RestoreCombatFallbackCountLabel(string text, bool visible)
        {
            _countContainer?.QueueFree();
            _countContainer = null;
            if (IsInstanceValid(_countLabel))
                _countLabel.QueueFree();

            _countLabel = new()
            {
                Name = "Count",
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -24f,
                OffsetTop = -24f,
                OffsetRight = 24f,
                OffsetBottom = 24f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = new(24f, 24f),
                Visible = visible,
            };
            EnsureProceduralCountLabelHasCombatStyleFont(_countLabel);
            _countLabel.SetTextAutoSize(string.IsNullOrEmpty(text) ? "0" : text);
            AddChild(_countLabel);
            MoveChild(_countLabel, _iconHost.GetIndex() + 1);
        }

        private Control? ResolveVanillaCombatPileRootForCountTemplate()
        {
            if (Definition is not { } def)
                return null;
            var ui = NCombatRoom.Instance?.Ui;
            if (ui == null)
                return null;

            return def.Style switch
            {
                ModCardPileUiStyle.BottomLeft when def.Anchor.Kind == ModCardPileAnchorKind.BottomLeftSecondary => ui
                    .DiscardPile,
                ModCardPileUiStyle.BottomLeft => ui.DrawPile,
                ModCardPileUiStyle.BottomRight => ui.ExhaustPile,
                _ => null,
            };
        }

        private void AttachPile(ModCardPile? pile)
        {
            if (ReferenceEquals(_pile, pile))
                return;

            DetachPile();
            _pile = pile;
            if (_pile == null || Definition == null)
                return;

            _pile.CardAddFinished += OnCardAddFinished;
            _pile.CardRemoveFinished += OnCardRemoveFinished;
            _currentCount = _pile.Cards.Count;
            _countLabel.SetTextAutoSize(_currentCount.ToString());
            _hoverTip = ModCardPileHoverTipFactory.Create(Definition);
        }

        /// <summary>
        ///     Refreshes the count label from <see cref="ModTopBarButtonDefinition.CountProvider" />. When
        ///     the provider is null — or returns a negative number — the label is hidden entirely; action
        ///     buttons that don't track a count then look like a plain icon button, matching the vanilla
        ///     <c>%Map</c> / <c>%Pause</c> feel while keeping the card-pile click hit-box. A non-negative
        ///     return value shows the badge and triggers the bump animation on increase.
        /// </summary>
        private void PollActionCount(bool force)
        {
            if (ActionDefinition is not { } def)
                return;

            if (def.CountProvider is null)
            {
                if (_countLabel.Visible)
                    _countLabel.Visible = false;
                return;
            }

            int count;
            try
            {
                count = def.CountProvider(new(def, _player, this));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[TopBar] CountProvider for '{def.Id}' threw: {ex.Message}; using last known count.");
                return;
            }

            if (count < 0)
            {
                if (_countLabel.Visible)
                    _countLabel.Visible = false;
                _actionLastKnownCount = -1;
                return;
            }

            if (!force && count == _actionLastKnownCount)
                return;

            var increased = count > _actionLastKnownCount && _actionLastKnownCount >= 0;
            _actionLastKnownCount = count;
            _currentCount = count;
            _countLabel.Visible = true;
            _countLabel.SetTextAutoSize(count.ToString());
            _countLabel.PivotOffset = _countLabel.Size * 0.5f;

            if (!increased)
                return;

            // Small "count went up" bump, mirroring the pile-mode CardAddFinished animation so action
            // buttons feel just as responsive when the number they track jumps.
            _bumpTween?.Kill();
            _bumpTween = CreateTween().SetParallel();
            _countLabel.Scale = _pileHoverScale;
            _bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }

        private void RefreshActionVisibility()
        {
            if (ActionDefinition is not { } def)
                return;

            bool visible;
            if (def.VisibleWhen is null)
                visible = true;
            else
                try
                {
                    visible = def.VisibleWhen(new(def, _player, this));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[TopBar] VisibleWhen predicate for '{def.Id}' threw: {ex.Message}; hiding button.");
                    visible = false;
                }

            if (Visible == visible)
                return;

            Visible = visible;
            MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            if (!visible)
                NHoverTipSet.Remove(this);
        }

        private void RefreshPileButtonVisibility()
        {
            if (Definition is not { } def)
                return;

            if (def.VisibleWhen is null)
                return;

            bool visible;
            try
            {
                visible = def.VisibleWhen(new(def, _player, this, _pile));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[CardPile] VisibleWhen predicate for '{def.Id}' threw: {ex.Message}; hiding button.");
                visible = false;
            }

            if (Visible == visible)
                return;

            Visible = visible;
            MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            if (!visible)
                NHoverTipSet.Remove(this);
        }

        private void DetachPile()
        {
            if (_pile == null)
                return;

            _pile.CardAddFinished -= OnCardAddFinished;
            _pile.CardRemoveFinished -= OnCardRemoveFinished;
            _pile = null;
        }

        private void OnCardAddFinished()
        {
            if (_pile == null)
                return;

            _currentCount = _pile.Cards.Count;
            _countLabel.SetTextAutoSize(_currentCount.ToString());
            _countLabel.PivotOffset = _countLabel.Size * 0.5f;
            _bumpTween?.Kill();
            _bumpTween = CreateTween().SetParallel();
            _icon.Scale = _pileHoverScale;
            _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            _countLabel.Scale = _pileHoverScale;
            _bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }

        private void OnCardRemoveFinished()
        {
            if (_pile == null)
                return;
            _currentCount = _pile.Cards.Count;
            _countLabel.SetTextAutoSize(_currentCount.ToString());
            _countLabel.PivotOffset = _countLabel.Size * 0.5f;
        }

        private void OnMouseEntered()
        {
            _hovered = true;
            _bumpTween?.Kill();
            _bumpTween = CreateTween();
            _bumpTween.TweenProperty(_icon, "scale", _pileHoverScale, 0.05);

            ShowHoverTipAnchored();
        }

        /// <summary>
        ///     Positions the hover tip. Top-bar and action buttons mirror <c>NTopBarDeckButton.OnFocus</c>
        ///     (right-aligned under the hit target). Combat bottom-row piles instead mirror
        ///     <c>NCombatCardPile.OnFocus</c>, which uses pile-specific offsets — the old single formula
        ///     placed tips below the 80×80 control like the deck button, which reads wrong at the bottom
        ///     of the screen next to vanilla draw / discard / exhaust.
        /// </summary>
        private void ShowHoverTipAnchored()
        {
            if (_hoverTip == null)
                return;
            var tipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
            if (tipSet == null)
                return;
            var desired = ResolveHoverTipGlobalPosition(tipSet);
            tipSet.GlobalPosition = ModCardPileHoverTipViewport.ClampTipTopLeft(tipSet, desired);
        }

        private Vector2 ResolveHoverTipGlobalPosition(NHoverTipSet tipSet)
        {
            if (ActionDefinition != null)
                return TopBarStyleTipBelowRight(_iconHost.GetGlobalRect(), tipSet);

            if (Definition == null)
                return TopBarStyleTipBelowRight(GetGlobalRect(), tipSet);

            if (Definition.HoverTipPlacement != ModCardPileHoverTipPlacement.Auto)
                return ResolveHoverTipByPlacement(Definition.HoverTipPlacement, tipSet) +
                       Definition.HoverTipScreenOffset;

            var basePos = Definition.Anchor.Kind == ModCardPileAnchorKind.Custom
                ? ResolveCustomAnchorHoverTipBase(tipSet)
                : Definition.Style switch
                {
                    ModCardPileUiStyle.BottomLeft when Definition.Anchor.Kind ==
                                                       ModCardPileAnchorKind.BottomLeftSecondary
                        =>
                        GlobalPosition + new Vector2(-320f, -370f),
                    ModCardPileUiStyle.BottomLeft => GlobalPosition + new Vector2(14f, -375f),
                    ModCardPileUiStyle.BottomRight => GlobalPosition + new Vector2(-320f, -125f),
                    ModCardPileUiStyle.TopBarDeck => TopBarStyleTipBelowRight(_iconHost.GetGlobalRect(), tipSet),
                    _ => TopBarStyleTipBelowRight(GetGlobalRect(), tipSet),
                };

            return basePos + Definition.HoverTipScreenOffset;
        }

        private Vector2 ResolveHoverTipByPlacement(ModCardPileHoverTipPlacement placement, NHoverTipSet tipSet)
        {
            var rect = PileHoverTipAnchorRect();
            return placement switch
            {
                ModCardPileHoverTipPlacement.BelowButtonTrailingEdge => TopBarStyleTipBelowRight(rect, tipSet),
                ModCardPileHoverTipPlacement.AboveButtonCentered => TipAboveCentered(rect, tipSet),
                ModCardPileHoverTipPlacement.BelowButtonCentered => TipBelowCentered(rect, tipSet),
                _ => TipAboveCentered(rect, tipSet),
            };
        }

        private Rect2 PileHoverTipAnchorRect()
        {
            return Definition?.Style == ModCardPileUiStyle.TopBarDeck ? _iconHost.GetGlobalRect() : GetGlobalRect();
        }

        private Vector2 ResolveCustomAnchorHoverTipBase(NHoverTipSet tipSet)
        {
            return Definition?.Style == ModCardPileUiStyle.TopBarDeck
                ? TopBarStyleTipBelowRight(_iconHost.GetGlobalRect(), tipSet)
                : TipAboveCentered(GetGlobalRect(), tipSet);
        }

        private static Vector2 TipAboveCentered(Rect2 anchor, NHoverTipSet tipSet)
        {
            const float gap = 20f;
            return new(
                anchor.Position.X + anchor.Size.X * 0.5f - tipSet.Size.X * 0.5f,
                anchor.Position.Y - tipSet.Size.Y - gap);
        }

        private static Vector2 TipBelowCentered(Rect2 anchor, NHoverTipSet tipSet)
        {
            const float gap = 20f;
            return new(
                anchor.Position.X + anchor.Size.X * 0.5f - tipSet.Size.X * 0.5f,
                anchor.Position.Y + anchor.Size.Y + gap);
        }

        private static Vector2 TopBarStyleTipBelowRight(Rect2 anchor, NHoverTipSet tipSet)
        {
            return anchor.Position + new Vector2(anchor.Size.X - tipSet.Size.X, anchor.Size.Y + 20f);
        }

        private void OnMouseExited()
        {
            _hovered = false;
            NHoverTipSet.Remove(this);
            _bumpTween?.Kill();
            _bumpTween = CreateTween().SetParallel();
            _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            _bumpTween.TweenProperty(_icon, "modulate", Colors.White, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }

        private void OnPress()
        {
            _bumpTween?.Kill();
            _bumpTween = CreateTween().SetParallel();
            _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.25)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _bumpTween.TweenProperty(_icon, "modulate", Colors.DarkGray, 0.25)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        private void OnRelease()
        {
            _bumpTween?.Kill();
            _bumpTween = CreateTween();
            _bumpTween.TweenProperty(_icon, "scale", _hovered ? _pileHoverScale : Vector2.One, 0.05);
            _bumpTween.TweenProperty(_icon, "modulate", Colors.White, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);

            if (ActionDefinition is { } actionDef)
            {
                if (actionDef.OnClick is not { } handler)
                    return;
                try
                {
                    handler(new(actionDef, _player, this));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error(
                        $"[TopBar] OnClick handler for '{actionDef.Id}' threw: {ex}");
                }

                return;
            }

            if (_pile == null || _player == null || Definition == null)
                return;

            var inCombat = CombatManager.Instance.IsInProgress;
            if (inCombat && _pile.IsEmpty)
            {
                var instance = NCapstoneContainer.Instance;
                if (instance is { InUse: true })
                    NCapstoneContainer.Instance?.Close();

                var message = Definition.EmptyPileMessage.GetFormattedText();
                var thought = NThoughtBubbleVfx.Create(message, _player.Creature, 2.0);
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(thought);
                return;
            }

            var capstone = NCapstoneContainer.Instance;
            if (capstone is { CurrentCapstoneScreen: NCardPileScreen screen }
                && screen.Pile == _pile)
            {
                capstone.Close();
                return;
            }

            if (Definition.OnOpen is { } onOpen)
            {
                var context = new ModCardPileOpenContext(Definition, _pile, _player, this);
                onOpen(context);
                return;
            }

            NCardPileScreen.ShowScreen(_pile, Definition.Hotkeys ?? []);
        }

        internal void ApplyVisualOffset(Vector2 offset)
        {
            _iconHost.Position = offset;
            CountOffsetTarget.Position = offset;
        }

        /// <summary>
        ///     Programmatically triggers the same open logic the button runs on pointer release (runs
        ///     <see cref="ModCardPileDefinition.OnOpen" /> if set, otherwise the default
        ///     <see cref="NCardPileScreen" />). Intended for hotkey bindings or scripted flows.
        /// </summary>
        public void TriggerOpen()
        {
            OnRelease();
        }
    }
}
