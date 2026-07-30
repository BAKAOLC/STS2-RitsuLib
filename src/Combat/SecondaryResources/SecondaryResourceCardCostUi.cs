using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Cards.FreePlay;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Configures the appearance of <see cref="NSecondaryResourceCardCostUi" />.</para>
    ///     <para xml:lang="zh-CN">配置 <see cref="NSecondaryResourceCardCostUi" /> 的外观。</para>
    /// </summary>
    public sealed record SecondaryResourceCardCostUiStyle
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the size of one cost slot.</para>
        ///     <para xml:lang="zh-CN">获取一个费用槽的尺寸。</para>
        /// </summary>
        public Vector2 SlotSize { get; init; } = new(48f, 48f);

        /// <summary>
        ///     <para xml:lang="en">Gets the icon rectangle size within the slot.</para>
        ///     <para xml:lang="zh-CN">获取费用槽内图标矩形的尺寸。</para>
        /// </summary>
        public Vector2 IconSize { get; init; } = new(46f, 46f);

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label offset from the centered icon rectangle.</para>
        ///     <para xml:lang="zh-CN">获取数量标签相对于居中图标矩形的偏移。</para>
        /// </summary>
        public Vector2 LabelOffset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether this display occupies the game's Stars-cost slot and therefore reserves its
        ///         enchantment-tab layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取该显示是否占用游戏的辉星费用槽，并因此预留其附魔标签布局。</para>
        /// </summary>
        public bool ReserveVanillaStarCostSlot { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label font size.</para>
        ///     <para xml:lang="zh-CN">获取数量标签的字号。</para>
        /// </summary>
        public int FontSize { get; init; } = 28;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label outline size.</para>
        ///     <para xml:lang="zh-CN">获取数量标签的描边尺寸。</para>
        /// </summary>
        public int OutlineSize { get; init; } = 7;

        /// <summary>
        ///     <para xml:lang="en">Gets the cost-text color for an entry that permits card play.</para>
        ///     <para xml:lang="zh-CN">获取支付条目允许出牌时的费用文本颜色。</para>
        /// </summary>
        public Color AffordableColor { get; init; } = StsColors.cream;

        /// <summary>
        ///     <para xml:lang="en">Gets the cost-text color for an entry that prevents card play.</para>
        ///     <para xml:lang="zh-CN">获取支付条目阻止出牌时的费用文本颜色。</para>
        /// </summary>
        public Color UnaffordableColor { get; init; } = StsColors.red;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the cost-text color when an insufficient-payment policy permits a required shortfall.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取资源不足支付策略允许必需费用缺口时的费用文本颜色。</para>
        /// </summary>
        public Color ShortfallPlayableColor { get; init; } = StsColors.energyBlue;

        /// <summary>
        ///     <para xml:lang="en">Gets the text color for a playable cost above its base value.</para>
        ///     <para xml:lang="zh-CN">获取仍可支付但高于基础值的费用文本颜色。</para>
        /// </summary>
        public Color IncreasedColor { get; init; } = StsColors.energyBlue;

        /// <summary>
        ///     <para xml:lang="en">Gets the text color for a cost below its base value.</para>
        ///     <para xml:lang="zh-CN">获取低于基础值的费用文本颜色。</para>
        /// </summary>
        public Color DecreasedColor { get; init; } = StsColors.green;

        /// <summary>
        ///     <para xml:lang="en">Gets the text color for an unavailable optional payment.</para>
        ///     <para xml:lang="zh-CN">获取不可用的可选支付所使用的文本颜色。</para>
        /// </summary>
        public Color? OptionalUnavailableColor { get; init; } = StsColors.gray;

        /// <summary>
        ///     <para xml:lang="en">Gets the text-outline color for an entry that permits card play.</para>
        ///     <para xml:lang="zh-CN">获取支付条目允许出牌时的文本描边颜色。</para>
        /// </summary>
        public Color AffordableOutlineColor { get; init; } = StsColors.defaultStarCostOutline;

        /// <summary>
        ///     <para xml:lang="en">Gets the text-outline color for an entry that prevents card play.</para>
        ///     <para xml:lang="zh-CN">获取支付条目阻止出牌时的文本描边颜色。</para>
        /// </summary>
        public Color UnaffordableOutlineColor { get; init; } = StsColors.unplayableEnergyCostOutline;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the text-outline color when an insufficient-payment policy permits a required shortfall.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取资源不足支付策略允许必需费用缺口时的文本描边颜色。</para>
        /// </summary>
        public Color ShortfallPlayableOutlineColor { get; init; } = StsColors.energyBlueOutline;

        /// <summary>
        ///     <para xml:lang="en">Gets the text-outline color for a playable cost above its base value.</para>
        ///     <para xml:lang="zh-CN">获取仍可支付但高于基础值的费用文本描边颜色。</para>
        /// </summary>
        public Color IncreasedOutlineColor { get; init; } = StsColors.energyBlueOutline;

        /// <summary>
        ///     <para xml:lang="en">Gets the text-outline color for a cost below its base value.</para>
        ///     <para xml:lang="zh-CN">获取低于基础值的费用文本描边颜色。</para>
        /// </summary>
        public Color DecreasedOutlineColor { get; init; } = StsColors.energyGreenOutline;

        /// <summary>
        ///     <para xml:lang="en">Gets the text-outline color for an unavailable optional payment.</para>
        ///     <para xml:lang="zh-CN">获取不可用的可选支付所使用的文本描边颜色。</para>
        /// </summary>
        public Color? OptionalUnavailableOutlineColor { get; init; } = StsColors.defaultStarCostOutline;

        /// <summary>
        ///     <para xml:lang="en">Gets the icon texture's size-expansion mode.</para>
        ///     <para xml:lang="zh-CN">获取图标贴图的尺寸扩展模式。</para>
        /// </summary>
        public TextureRect.ExpandModeEnum ExpandMode { get; init; } = TextureRect.ExpandModeEnum.IgnoreSize;

        /// <summary>
        ///     <para xml:lang="en">Gets the icon texture's stretch mode.</para>
        ///     <para xml:lang="zh-CN">获取图标贴图的拉伸模式。</para>
        /// </summary>
        public TextureRect.StretchModeEnum StretchMode { get; init; } =
            TextureRect.StretchModeEnum.KeepAspectCentered;

        /// <summary>
        ///     <para xml:lang="en">Gets an optional formatter for the resolved payment entry's cost text.</para>
        ///     <para xml:lang="zh-CN">获取用于生成已解析支付条目费用文本的可选格式化器。</para>
        /// </summary>
        public Func<SecondaryResourcePaymentLine, string>? FormatCost { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared default style.</para>
        ///     <para xml:lang="zh-CN">获取共享的默认样式。</para>
        /// </summary>
        public static SecondaryResourceCardCostUiStyle Default { get; } = new();

        internal string Format(SecondaryResourcePaymentLine line)
        {
            return FormatCost?.Invoke(line) ?? (line.CostsX ? "X" : line.Cost.ToString());
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Displays one reusable secondary-resource card-cost entry.</para>
    ///     <para xml:lang="zh-CN">显示一项可复用的次级资源卡牌费用条目。</para>
    /// </summary>
    public partial class NSecondaryResourceCardCostUi : Control
    {
        private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";
        private bool _autoRefresh = true;
        private CardModel? _boundCard;
        private SecondaryResourceState? _boundState;
        private SecondaryResourceDefinition? _definition;
        private bool _hasLastFontColor;
        private bool _hasLastOutlineColor;

        private MegaLabel _label = null!;
        private Color _lastFontColor;
        private Color _lastOutlineColor;
        private string? _lastText;
        private SecondaryResourcePaymentLine? _line;
        private PileType _pileType = PileType.Hand;
        private SecondaryResourcePaymentPlan? _plan;
        private CardPreviewMode _previewMode = CardPreviewMode.Normal;
        private string? _resourceId;
        private SecondaryResourceCardCostUiStyle _style = SecondaryResourceCardCostUiStyle.Default;
        private TextureRect _texture = null!;
        private string? _useId;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the display refreshes when the bound card owner's secondary-resource state
        ///         changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置卡牌所有者的次级资源状态变化时是否自动刷新显示。</para>
        /// </summary>
        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (_autoRefresh == value)
                    return;
                _autoRefresh = value;
                UpdateStateSubscription();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a card-cost display bound to one resource identifier.</para>
        ///     <para xml:lang="zh-CN">创建绑定到一个资源标识符的卡牌费用显示节点。</para>
        /// </summary>
        public static NSecondaryResourceCardCostUi Create(
            string resourceId,
            SecondaryResourceCardCostUiStyle? style = null)
        {
            var node = new NSecondaryResourceCardCostUi();
            node.Configure(style);
            node.Bind(resourceId);
            return node;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a card-cost display bound to one resource definition.</para>
        ///     <para xml:lang="zh-CN">创建绑定到一个资源定义的卡牌费用显示节点。</para>
        /// </summary>
        public static NSecondaryResourceCardCostUi Create(
            SecondaryResourceDefinition definition,
            SecondaryResourceCardCostUiStyle? style = null)
        {
            var node = new NSecondaryResourceCardCostUi();
            node.Configure(style);
            node.Bind(definition);
            return node;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a card-cost display bound to one payment-use and resource identifier.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建绑定到一项支付条款及资源标识符的卡牌费用显示节点。</para>
        /// </summary>
        public static NSecondaryResourceCardCostUi CreateForUse(
            string useId,
            string resourceId,
            SecondaryResourceCardCostUiStyle? style = null)
        {
            var node = new NSecondaryResourceCardCostUi();
            node.Configure(style);
            node.BindUse(useId, resourceId);
            return node;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a card-cost display bound to one payment use and resource definition.</para>
        ///     <para xml:lang="zh-CN">创建绑定到一项支付条款及资源定义的卡牌费用显示节点。</para>
        /// </summary>
        public static NSecondaryResourceCardCostUi CreateForUse(
            string useId,
            SecondaryResourceDefinition definition,
            SecondaryResourceCardCostUiStyle? style = null)
        {
            var node = new NSecondaryResourceCardCostUi();
            node.Configure(style);
            node.BindUse(useId, definition);
            return node;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies a visual style and refreshes any resolved entry already displayed.</para>
        ///     <para xml:lang="zh-CN">应用视觉样式，并刷新已在显示的已解析条目。</para>
        /// </summary>
        public void Configure(SecondaryResourceCardCostUiStyle? style = null)
        {
            ApplyStyle(style ?? SecondaryResourceCardCostUiStyle.Default);
        }

        private void ApplyStyle(SecondaryResourceCardCostUiStyle style)
        {
            ArgumentNullException.ThrowIfNull(style);

            _style = style;
            CustomMinimumSize = _style.SlotSize;
            Size = _style.SlotSize;

            if (!IsNodeReady())
                return;

            ApplyLayout();
            ApplyLabelTheme();
            if (_plan != null && _line != null)
                Refresh(_plan, _line);
        }

        /// <summary>
        ///     <para xml:lang="en">Binds the display to one secondary-resource identifier.</para>
        ///     <para xml:lang="zh-CN">将显示节点绑定到一个次级资源标识符。</para>
        /// </summary>
        public void Bind(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            _useId = null;
            _resourceId = resourceId.Trim();
            _definition = null;

            if (ModSecondaryResourceRegistry.TryGet(_resourceId, out var definition))
                Bind(definition);
            else if (IsNodeReady())
            {
                ApplyDefinition();
                UpdateVisibility(false);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Binds the display to one secondary-resource definition.</para>
        ///     <para xml:lang="zh-CN">将显示节点绑定到一个次级资源定义。</para>
        /// </summary>
        public void Bind(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _useId = null;
            _resourceId = definition.Id;
            _definition = definition;

            if (!IsNodeReady())
                return;

            ApplyDefinition();
            if (_boundCard != null)
                Refresh(_boundCard);
        }

        /// <summary>
        ///     <para xml:lang="en">Binds the display to one payment-use and resource identifier.</para>
        ///     <para xml:lang="zh-CN">将显示节点绑定到一项支付条款及其资源标识符。</para>
        /// </summary>
        public void BindUse(string useId, string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            _useId = useId.Trim();
            _resourceId = resourceId.Trim();
            _definition = null;

            if (ModSecondaryResourceRegistry.TryGet(_resourceId, out var definition))
                BindUse(_useId, definition);
            else if (IsNodeReady())
            {
                ApplyDefinition();
                UpdateVisibility(false);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Binds the display to one payment use and resource definition.</para>
        ///     <para xml:lang="zh-CN">将显示节点绑定到一项支付条款及其资源定义。</para>
        /// </summary>
        public void BindUse(string useId, SecondaryResourceDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            ArgumentNullException.ThrowIfNull(definition);

            _useId = useId.Trim();
            _resourceId = definition.Id;
            _definition = definition;

            if (!IsNodeReady())
                return;

            ApplyDefinition();
            if (_boundCard != null)
                Refresh(_boundCard);
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes from a card UI update context and reserves any configured Stars slot.</para>
        ///     <para xml:lang="zh-CN">根据卡牌界面更新上下文刷新，并按配置预留辉星费用槽。</para>
        /// </summary>
        public void Refresh<TParent>(SecondaryResourceCardUiContext<TParent, NSecondaryResourceCardCostUi> context)
            where TParent : Node
        {
            Refresh(context.Card, context.Plan, context.PileType, context.PreviewMode);
            if (Visible &&
                _style.ReserveVanillaStarCostSlot &&
                context.Parent is NCard card)
                SecondaryResourceCardUiLayout.ReserveVanillaStarCostSlot(card);
        }

        /// <summary>
        ///     <para xml:lang="en">Binds and refreshes the display from <paramref name="card" />.</para>
        ///     <para xml:lang="zh-CN">绑定并根据 <paramref name="card" /> 刷新显示节点。</para>
        /// </summary>
        public void Refresh(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            Refresh(card, SecondaryResourcePaymentResolver.Plan(
                card,
                SecondaryResourcePaymentFreeMode.FromCardCostScope(
                    FreePlayBindingRegistry.ResolveCardCostScopeForUpcomingPlay(card))));
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes the bound card from a resolved payment plan.</para>
        ///     <para xml:lang="zh-CN">根据已解析支付计划刷新已绑定卡牌的显示。</para>
        /// </summary>
        public void Refresh(CardModel card, SecondaryResourcePaymentPlan plan)
        {
            Refresh(card, plan, _pileType, _previewMode);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes from a resolved payment plan, pile type, and card preview mode.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据已解析支付计划、牌堆类型及卡牌预览模式刷新显示。</para>
        /// </summary>
        public void Refresh(
            CardModel card,
            SecondaryResourcePaymentPlan plan,
            PileType pileType,
            CardPreviewMode previewMode)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(plan);

            _boundCard = card;
            UpdateStateSubscription();
            _pileType = pileType;
            _previewMode = previewMode;
            if (string.IsNullOrWhiteSpace(_resourceId))
            {
                UpdateVisibility(false);
                return;
            }

            if (_definition == null && ModSecondaryResourceRegistry.TryGet(_resourceId, out var definition))
            {
                _definition = definition;
                if (IsNodeReady())
                    ApplyDefinition();
            }

            var line = FindLine(plan);
            if (line == null)
            {
                UpdateVisibility(false);
                return;
            }

            Refresh(plan, line);
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes the display from a matching resolved payment entry.</para>
        ///     <para xml:lang="zh-CN">根据匹配的已解析支付条目刷新显示。</para>
        /// </summary>
        public void Refresh(SecondaryResourcePaymentPlan plan, SecondaryResourcePaymentLine line)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(line);

            _plan = plan;
            _line = line;

            if (!IsNodeReady())
                return;

            UpdateVisibility(true);
            var text = _style.Format(line);
            if (!string.Equals(_lastText, text, StringComparison.Ordinal))
            {
                _label.SetTextAutoSize(text);
                _lastText = text;
            }

            var (fontColor, outlineColor) = ResolveLabelColors(line, _pileType, _previewMode);
            if (!_hasLastFontColor || _lastFontColor != fontColor)
            {
                _label.AddThemeColorOverride(ThemeConstants.Label.FontColor, fontColor);
                _lastFontColor = fontColor;
                _hasLastFontColor = true;
            }

            // ReSharper disable once InvertIf
            if (!_hasLastOutlineColor || _lastOutlineColor != outlineColor)
            {
                _label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);
                _lastOutlineColor = outlineColor;
                _hasLastOutlineColor = true;
            }
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            UpdateStateSubscription();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            CustomMinimumSize = _style.SlotSize;
            Size = _style.SlotSize;

            _texture = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(_texture);

            _label = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutoSizeEnabled = true,
                MinFontSize = Math.Max(8, _style.FontSize - 10),
                MaxFontSize = _style.FontSize,
            };

            ApplyLayout();
            ApplyLabelTheme();
            AddChild(_label);

            ApplyDefinition();

            if (_plan != null && _line != null)
                Refresh(_plan, _line);
            else if (_boundCard != null)
                Refresh(_boundCard);
            else
                UpdateVisibility(false);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            SetBoundState(null);
        }

        private void ApplyLayout()
        {
            var iconPosition = (_style.SlotSize - _style.IconSize) * 0.5f;
            _texture.Position = iconPosition;
            _texture.CustomMinimumSize = _style.IconSize;
            _texture.Size = _style.IconSize;
            _texture.ExpandMode = _style.ExpandMode;
            _texture.StretchMode = _style.StretchMode;

            _label.Position = iconPosition + _style.LabelOffset;
            _label.CustomMinimumSize = _style.IconSize;
            _label.Size = _style.IconSize;
            _label.MinFontSize = Math.Max(8, _style.FontSize - 10);
            _label.MaxFontSize = _style.FontSize;
        }

        private void ApplyDefinition()
        {
            if (_texture == null)
                return;

            if (_definition == null)
            {
                _texture.Texture = null;
                return;
            }

            var path = _definition.LargeIconPath ?? _definition.SmallIconPath;
            _texture.Texture = string.IsNullOrWhiteSpace(path) ? null : ResourceLoader.Load<Texture2D>(path);
        }

        private SecondaryResourcePaymentLine? FindLine(SecondaryResourcePaymentPlan plan)
        {
            if (!string.IsNullOrWhiteSpace(_useId))
                return plan.Lines.FirstOrDefault(line =>
                    string.Equals(line.UseId, _useId, StringComparison.OrdinalIgnoreCase));

            return plan.Lines.FirstOrDefault(line =>
                string.Equals(line.ResourceId, _resourceId, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyLabelTheme()
        {
            var font = PreloadManager.Cache.GetAsset<Font>(DefaultLabelFontPath);
            _label.AddThemeFontOverride(ThemeConstants.Label.Font, font);
            _label.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, _style.FontSize);
            _label.AddThemeColorOverride(ThemeConstants.Label.FontColor, _style.AffordableColor);
            _label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, _style.AffordableOutlineColor);
            _label.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, _style.OutlineSize);
            _lastFontColor = _style.AffordableColor;
            _lastOutlineColor = _style.AffordableOutlineColor;
            _hasLastFontColor = true;
            _hasLastOutlineColor = true;
        }

        private bool HasVisibleCanvasAncestor()
        {
            for (var ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
                if (ancestor is CanvasItem canvasItem)
                    return canvasItem.IsVisibleInTree();

            return true;
        }

        private void UpdateStateSubscription()
        {
            var state = _autoRefresh && ModSecondaryResourceRegistry.HasAny &&
                        _boundCard is
                        {
                            IsCanonical: false,
                            Owner: { PlayerCombatState: not null } player,
                        }
                ? SecondaryResourceStateStore.Get(player)
                : null;
            SetBoundState(state);
        }

        private void SetBoundState(SecondaryResourceState? state)
        {
            if (ReferenceEquals(_boundState, state))
                return;
            if (_boundState != null)
                _boundState.Changed -= OnSecondaryResourceChanged;
            _boundState = state;
            if (_boundState != null)
                _boundState.Changed += OnSecondaryResourceChanged;
        }

        private void OnSecondaryResourceChanged(SecondaryResourceChangedEvent change)
        {
            if (_boundCard == null || !HasVisibleCanvasAncestor())
                return;
            Refresh(_boundCard);
        }

        private void UpdateVisibility(bool visible)
        {
            if (Visible != visible)
                Visible = visible;
        }

        private (Color FontColor, Color OutlineColor) ResolveLabelColors(
            SecondaryResourcePaymentLine line,
            PileType pileType,
            CardPreviewMode previewMode)
        {
            var useOptionalUnavailable =
                _style.OptionalUnavailableColor.HasValue &&
                _style.OptionalUnavailableOutlineColor.HasValue;
            return SecondaryResourceCardCostHelper.GetCostColor(
                    line,
                    pileType,
                    previewMode,
                    includeOptionalUnavailable: useOptionalUnavailable) switch
                {
                    SecondaryResourceCardCostColor.Increased => (_style.IncreasedColor, _style.IncreasedOutlineColor),
                    SecondaryResourceCardCostColor.Decreased => (_style.DecreasedColor, _style.DecreasedOutlineColor),
                    SecondaryResourceCardCostColor.InsufficientResources =>
                        (_style.UnaffordableColor, _style.UnaffordableOutlineColor),
                    SecondaryResourceCardCostColor.ShortfallPlayable =>
                        (_style.ShortfallPlayableColor, _style.ShortfallPlayableOutlineColor),
                    SecondaryResourceCardCostColor.OptionalUnavailable =>
                        (_style.OptionalUnavailableColor!.Value, _style.OptionalUnavailableOutlineColor!.Value),
                    _ => (_style.AffordableColor, _style.AffordableOutlineColor),
                };
        }
    }
}
