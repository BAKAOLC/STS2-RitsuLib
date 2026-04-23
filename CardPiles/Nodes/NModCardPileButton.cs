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
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     Procedurally built top-bar-style button reused by BOTH systems that want the "card pile with
    ///     icon + count" look:
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

        // Matches `top_bar_deck_button.tscn`: the inner Icon is a 72x72 TextureRect centred in the
        // 80x80 slot. Recreating the same numbers keeps the rendered glyph the same size as the deck's.
        private const float IconSize = 72f;
        private static readonly Vector2 HoverScale = Vector2.One * 1.1f;

        // Action-mode fields (null when Definition is set).
        private int _actionLastKnownCount = -1;

        // Shared state between the two modes.
        private Tween? _bumpTween;
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

            // Regardless of mode, adopt the vanilla %Deck's `DeckCardCount` label — this gives us the
            // exact font, outline colour / size, shadow offsets and font size that the scene designer
            // configured on the real deck button (see `top_bar_deck_button.tscn` lines 69-93).
            // Procedural MegaLabels render without an outline and drift whenever the game updates.
            TryReplaceCountLabelWithVanillaDeckClone();

            if (ActionDefinition != null)
            {
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

            if (Definition != null)
                AttachPile(ModCardPileStorage.Resolve(Definition.PileType, player));
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
            if (ActionDefinition == null)
                return;

            // Action-mode bookkeeping: visibility and count are polled here because there is no pile to
            // subscribe to. Both predicates are best kept cheap per their docs.
            RefreshActionVisibility();
            PollActionCount(false);
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
            // Mirrors the vanilla `top_bar_deck_button.tscn` hierarchy exactly:
            //   Root (80x80 Control, mouse_filter=Stop — the click target)
            //   └── Control (fills parent, mouse_filter=Ignore — holds the icon)
            //       └── Icon (72x72 TextureRect, anchored centre, mouse_filter=Ignore)
            //   └── Count (MegaLabel, anchored bottom-right with vanilla offsets)
            // Keeping the node names identical means tooling / scene inspection / future vanilla
            // lookups by path (`Control/Icon`) work on our buttons too.
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
                CustomMinimumSize = new(IconSize, IconSize),
                // Anchor preset 8 (centre) with explicit ±IconSize/2 offsets — same math the vanilla
                // scene encodes as "offset_left = -36, offset_top = -36, offset_right = 36,
                // offset_bottom = 36" on a 72x72 icon.
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -IconSize * 0.5f,
                OffsetTop = -IconSize * 0.5f,
                OffsetRight = IconSize * 0.5f,
                OffsetBottom = IconSize * 0.5f,
                PivotOffset = new(IconSize * 0.5f, IconSize * 0.5f - 2f),
            };
            _icon = textureRect;
            _iconHost.AddChild(_icon);

            // A placeholder count label — this will be swapped in Initialize() for a deep clone of the
            // vanilla %Deck's `DeckCardCount` MegaLabel, so we inherit the exact font / outline size /
            // shadow offsets / font size (set by the scene designer in top_bar_deck_button.tscn). A
            // procedural label would drift from vanilla on every visual update the game ships.
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

            Connect(Control.SignalName.MouseEntered, Callable.From(OnMouseEntered));
            Connect(Control.SignalName.MouseExited, Callable.From(OnMouseExited));
        }

        private static void EnsureProceduralCountLabelHasThemeFont(MegaLabel countLabel)
        {
            var vanilla = NRun.Instance?.GlobalUi?.TopBar?.Deck?.GetNodeOrNull<MegaLabel>("DeckCardCount");
            if (vanilla != null)
            {
                var font = RitsuThemeLookupCompat.GetThemeFont(vanilla, RitsuMegaLabelThemeNames.Font);
                if (font != null)
                {
                    countLabel.AddThemeFontOverride(RitsuMegaLabelThemeNames.Font, font);
                    countLabel.AddThemeFontSizeOverride(RitsuMegaLabelThemeNames.FontSize,
                        RitsuThemeLookupCompat.GetThemeFontSize(vanilla, RitsuMegaLabelThemeNames.FontSize));
                    return;
                }
            }

            var fallback = ThemeDB.FallbackFont;
            if (fallback != null)
            {
                countLabel.AddThemeFontOverride(RitsuMegaLabelThemeNames.Font, fallback);
                countLabel.AddThemeFontSizeOverride(RitsuMegaLabelThemeNames.FontSize, 28);
                return;
            }

            countLabel.AddThemeFontOverride(RitsuMegaLabelThemeNames.Font, new SystemFont());
            countLabel.AddThemeFontSizeOverride(RitsuMegaLabelThemeNames.FontSize, 28);
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
            _countLabel.Scale = HoverScale;
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
            _icon.Scale = HoverScale;
            _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            _countLabel.Scale = HoverScale;
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
            _bumpTween.TweenProperty(_icon, "scale", HoverScale, 0.05);

            ShowHoverTipAnchored();
        }

        /// <summary>
        ///     Shows our hover tip anchored the same way vanilla <c>NTopBarDeckButton.OnFocus</c> does it:
        ///     right-aligned under the button with a 20 px gap. <c>NHoverTipSet.CreateAndShow</c> with
        ///     <see cref="HoverTipAlignment.None" /> doesn't position the tip at all, so without this
        ///     step our tip rendered at the <c>HoverTipsContainer</c>'s origin (top-left of the
        ///     viewport) — the "weird location" the user reported.
        /// </summary>
        private void ShowHoverTipAnchored()
        {
            if (_hoverTip == null)
                return;
            var tipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
            // `ResetSize()` inside Init already sized the text container; sampling Size here gives the
            // real tip bounds so right-alignment is exact. Matches NTopBarDeckButton.OnFocus verbatim.
            tipSet.GlobalPosition = GlobalPosition
                                    + new Vector2(Size.X - tipSet.Size.X, Size.Y + 20f);
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
            _bumpTween.TweenProperty(_icon, "scale", _hovered ? HoverScale : Vector2.One, 0.05);
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
            _countLabel.Position = offset;
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
