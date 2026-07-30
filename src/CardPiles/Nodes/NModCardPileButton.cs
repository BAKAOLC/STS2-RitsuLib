using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.TopBar;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.CardPiles.Nodes
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Procedurally built button shared by registered card piles and standalone top-bar actions.
    ///         Top-bar buttons follow the vanilla deck-button presentation, while
    ///         <see cref="ModCardPileUiStyle.BottomLeft" /> and <see cref="ModCardPileUiStyle.BottomRight" />
    ///         follow <c>NCombatCardPile</c>.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Buttons produced by <see cref="ModCardPileRegistry" /> run in pile mode. They resolve a
    ///         <see cref="ModCardPile" /> and track its card count through events.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Standalone buttons produced by <see cref="ModTopBarButtonRegistry" /> run in action mode.
    ///         Their callbacks and optional count come from <see cref="ModTopBarButtonSpec" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册牌堆和独立顶部栏操作共用的程序化按钮。顶部栏按钮沿用原版牌组按钮的表现形式；
    ///         <see cref="ModCardPileUiStyle.BottomLeft" /> 和 <see cref="ModCardPileUiStyle.BottomRight" />
    ///         则沿用 <c>NCombatCardPile</c> 的表现形式。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ModCardPileRegistry" /> 生成的按钮以牌堆模式运行。它们会解析
    ///         <see cref="ModCardPile" />，并通过事件跟踪卡牌数量。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="ModTopBarButtonRegistry" /> 生成的独立按钮以操作模式运行，其回调和可选数量来自
    ///         <see cref="ModTopBarButtonSpec" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The button handles pointer input, presents a registered <see cref="HoverTip" />, and dispatches
    ///         either pile-opening logic or <see cref="ModTopBarButtonDefinition.OnClick" /> when released.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按钮处理指针输入并显示已注册的 <see cref="HoverTip" />，释放时执行牌堆打开逻辑或
    ///         <see cref="ModTopBarButtonDefinition.OnClick" />。
    ///     </para>
    /// </remarks>
    public sealed partial class NModCardPileButton : Control
    {
        // Matches the vanilla DeckContainer slot in the top bar.
        private const float DefaultButtonWidth = 80f;

        private const float DefaultButtonHeight = 80f;

        private const float TopBarIconSize = 72f;

        private const float TopBarOpenRotation = -(float)Math.PI / 15f;

        private static readonly Vector2 TopBarHoverScale = Vector2.One * 1.1f;

        private static readonly Vector2 CombatPileHoverScale = Vector2.One * 1.25f;

        private static readonly StringName LabelThemeType = "Label";

        // Action-mode fields (null when Definition is set).
        private int _actionLastKnownCount = -1;
        private bool _actionCountProviderFailed;
        private bool _actionIsOpen;
        private bool _actionOpenPredicateFailed;
        private bool _actionVisibilityPredicateFailed;

        // Shared state between the two modes.
        private Tween? _bumpTween;
        private Tween? _openStateTween;

        private Control? _countContainer;
        private MegaLabel _countLabel = null!;
        private int _currentCount;
        private bool _hovered;

        private HoverTip? _hoverTip;

        // Either the procedural TextureRect or a cloned vanilla deck-icon subtree.
        private Control _icon = null!;
        private Control _iconHost = null!;

        // Pile-mode fields (null when ActionDefinition is set).
        private ModCardPile? _pile;
        private bool _pileVisibilityPredicateFailed;

        private Vector2 _pileHoverScale = TopBarHoverScale;
        private Player? _player;
        private bool _pressed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registered pile definition in pile mode; <see langword="null" /> in action mode.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         牌堆模式下的已注册牌堆定义；操作模式下为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public ModCardPileDefinition? Definition { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registered action definition in action mode; <see langword="null" /> in pile mode.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         操作模式下的已注册操作定义；牌堆模式下为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public ModTopBarButtonDefinition? ActionDefinition { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Whether this button runs in action mode without a backing pile.</para>
        ///     <para xml:lang="zh-CN">此按钮是否以不含底层牌堆的操作模式运行。</para>
        /// </summary>
        public bool IsActionMode => ActionDefinition != null;

        private Control CountOffsetTarget => _countContainer ?? _countLabel;

        /// <summary>
        ///     <para xml:lang="en">Creates a pile-mode button bound to <paramref name="definition" />.</para>
        ///     <para xml:lang="zh-CN">创建绑定到 <paramref name="definition" /> 的牌堆模式按钮。</para>
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
        ///     <para xml:lang="en">
        ///         Creates an action-mode button bound to <paramref name="actionDefinition" />. It uses the
        ///         top-bar pile presentation, dispatches <see cref="ModTopBarButtonDefinition.OnClick" />, and
        ///         polls the definition's state providers during <see cref="Node._Process" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建绑定到 <paramref name="actionDefinition" /> 的操作模式按钮。该按钮使用顶部栏牌堆按钮的
        ///         表现形式，调用 <see cref="ModTopBarButtonDefinition.OnClick" />，并在
        ///         <see cref="Node._Process" /> 中轮询定义中的状态提供器。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Binds the button to <paramref name="player" />. Pile mode resolves and observes its
        ///         <see cref="ModCardPile" />; action mode initializes its callback context and displayed state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将按钮绑定到 <paramref name="player" />。牌堆模式会解析并监听对应的
        ///         <see cref="ModCardPile" />；操作模式会初始化回调上下文和显示状态。
        ///     </para>
        /// </summary>
        public void Initialize(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            _player = player;

            if (ActionDefinition != null)
            {
                TryReplaceCountLabelWithVanillaDeckClone();
                // Without a custom icon, reuse the vanilla deck icon's node hierarchy and materials.
                if (string.IsNullOrWhiteSpace(ActionDefinition.IconPath))
                    TryReplaceIconWithVanillaDeckClone();
                _hoverTip = ModTopBarButtonHoverTipFactory.Create(ActionDefinition);
                PollActionCount(true);
                RefreshActionOpenState(true);
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
            _openStateTween?.Kill();
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
                RefreshActionOpenState();
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

            countLabel.AddThemeFontOverride(ThemeConstants.Label.Font,
                RitsuShellThemeValueCoerce.AsFont(null));
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
            // Action mode may later replace a missing texture with a clone of the vanilla deck icon.
            var path = Definition?.IconPath ?? ActionDefinition?.IconPath;
            if (!string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
                return ResourceLoader.Load<Texture2D>(path);
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces the procedural <see cref="TextureRect" /> with a clone of the vanilla deck
        ///         button's <c>Control/Icon</c> subtree, preserving its hierarchy and materials. If the deck
        ///         node is unavailable, the procedural placeholder remains in use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用原版牌组按钮 <c>Control/Icon</c> 子树的克隆替换程序化
        ///         <see cref="TextureRect" />，并保留其节点层级和材质。牌组节点不可用时继续使用程序化占位图标。
        ///     </para>
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
                // Preserve the behavior and group membership of the vanilla subtree.
                var clone = vanillaIcon.Duplicate((int)(DuplicateFlags.Scripts
                                                        | DuplicateFlags.Signals
                                                        | DuplicateFlags.Groups));
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
                if (clone is not Control control)
                {
                    clone.QueueFree();
                    return;
                }

                // Keep the same Control/Icon path and retain the clone's scene-authored layout.
                var host = _icon.GetParent();
                _icon.QueueFree();
                _icon = control;
                host.AddChild(_icon);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModCardPileButton] Could not clone vanilla %Deck icon for action button: {ex}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces the procedural count label with a clone of the vanilla deck button's
        ///         <c>DeckCardCount</c> label, preserving its font, outline, and shadow settings. If the deck
        ///         node is unavailable, the procedural label remains in use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用原版牌组按钮的 <c>DeckCardCount</c> 标签克隆替换程序化数量标签，并保留字体、描边和
        ///         阴影设置。牌组节点不可用时继续使用程序化标签。
        ///     </para>
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

                // Retain scene-authored layout and theme overrides while applying the identity and input
                // settings expected by this button.
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
                    $"[ModCardPileButton] Could not clone vanilla %Deck count label: {ex}");
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
                    $"[ModCardPileButton] Could not clone vanilla combat CountContainer: {ex}");
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

            _pile.ContentsChanged += OnPileContentsChanged;
            _pile.CardAddFinished += OnCardAddFinished;
            _pile.CardRemoveFinished += OnCardRemoveFinished;
            RefreshPileCount();
            _hoverTip = ModCardPileHoverTipFactory.Create(Definition);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes the action count from <see cref="ModTopBarButtonDefinition.CountProvider" />.
        ///         A missing provider or negative result hides the label; an increased non-negative result
        ///         triggers the count-bump animation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <see cref="ModTopBarButtonDefinition.CountProvider" /> 刷新操作按钮数量。
        ///         未提供该函数或返回负数时隐藏标签；非负数增加时触发数量弹起动画。
        ///     </para>
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
                _actionCountProviderFailed = false;
            }
            catch (Exception ex)
            {
                if (!_actionCountProviderFailed)
                    RitsuLibFramework.Logger.Warn(
                        $"[TopBar] CountProvider for '{def.Id}' threw; using last known count: {ex}");
                _actionCountProviderFailed = true;
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

            // Match the pile-mode count-increase animation.
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
                    _actionVisibilityPredicateFailed = false;
                }
                catch (Exception ex)
                {
                    if (!_actionVisibilityPredicateFailed)
                        RitsuLibFramework.Logger.Warn(
                            $"[TopBar] VisibleWhen predicate for '{def.Id}' threw; hiding button: {ex}");
                    _actionVisibilityPredicateFailed = true;
                    visible = false;
                }

            if (Visible == visible)
                return;

            Visible = visible;
            MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            if (!visible)
                NHoverTipSet.Remove(this);
        }

        private void RefreshActionOpenState(bool immediate = false)
        {
            if (ActionDefinition is not { } def)
                return;

            var isOpen = false;
            if (def.IsOpenWhen != null)
                try
                {
                    isOpen = def.IsOpenWhen(new(def, _player, this));
                    _actionOpenPredicateFailed = false;
                }
                catch (Exception ex)
                {
                    if (!_actionOpenPredicateFailed)
                        RitsuLibFramework.Logger.Warn(
                            $"[TopBar] IsOpenWhen predicate for '{def.Id}' threw; using closed state: {ex}");
                    _actionOpenPredicateFailed = true;
                }

            if (!immediate && _actionIsOpen == isOpen)
                return;

            _actionIsOpen = isOpen;
            _openStateTween?.Kill();
            var targetRotation = isOpen ? TopBarOpenRotation : 0f;
            if (immediate)
            {
                _icon.Rotation = targetRotation;
                return;
            }

            _openStateTween = CreateTween();
            _openStateTween.TweenProperty(_icon, "rotation", targetRotation, isOpen ? 0.5 : 1.0)
                .SetTrans(isOpen ? Tween.TransitionType.Back : Tween.TransitionType.Elastic)
                .SetEase(Tween.EaseType.Out);
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
                _pileVisibilityPredicateFailed = false;
            }
            catch (Exception ex)
            {
                if (!_pileVisibilityPredicateFailed)
                    RitsuLibFramework.Logger.Warn(
                        $"[CardPile] VisibleWhen predicate for '{def.Id}' threw; hiding button: {ex}");
                _pileVisibilityPredicateFailed = true;
                visible = false;
            }

            if (Visible == visible)
                return;

            Visible = visible;
            MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            if (!visible)
                NHoverTipSet.Remove(this);
            TryRelayoutCombatRow();
        }

        private void TryRelayoutCombatRow()
        {
            if (GetParent() is NCombatPilesContainer c)
                ModCardPileCombatLayout.Relayout(c);
        }

        private void DetachPile()
        {
            if (_pile == null)
                return;

            _pile.ContentsChanged -= OnPileContentsChanged;
            _pile.CardAddFinished -= OnCardAddFinished;
            _pile.CardRemoveFinished -= OnCardRemoveFinished;
            _pile = null;
        }

        private void OnPileContentsChanged()
        {
            RefreshPileCount();
        }

        private void RefreshPileCount()
        {
            if (_pile == null)
                return;

            _currentCount = _pile.Cards.Count;
            _countLabel.SetTextAutoSize(_currentCount.ToString());
            _countLabel.PivotOffset = _countLabel.Size * 0.5f;
        }

        private void OnCardAddFinished()
        {
            if (_pile == null)
                return;

            RefreshPileCount();
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

            RefreshPileCount();
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
        ///     <para xml:lang="en">
        ///         Shows the hover tip at the placement defined for the current mode, then clamps it to the
        ///         viewport.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按当前模式规定的位置显示悬停提示，并将其限制在视口范围内。
        ///     </para>
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
                try
                {
                    actionDef.OnClick(new(actionDef, _player, this));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
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
        ///     <para xml:lang="en">
        ///         Programmatically runs the same release behavior as a pointer click, including action
        ///         callbacks and pile-opening rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以程序方式执行与指针点击相同的释放行为，包括操作回调和牌堆打开规则。
        ///     </para>
        /// </summary>
        public void TriggerOpen()
        {
            OnRelease();
        }
    }
}
