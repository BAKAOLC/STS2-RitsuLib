using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Visual style for the simple secondary-resource card-cost display.
    ///     简易次级资源卡牌费用显示节点的视觉样式。
    /// </summary>
    public sealed record SecondaryResourceCardCostUiStyle
    {
        /// <summary>
        ///     Root size for one cost slot.
        ///     单个费用槽的根节点尺寸。
        /// </summary>
        public Vector2 SlotSize { get; init; } = new(48f, 48f);

        /// <summary>
        ///     Icon rectangle size inside one slot.
        ///     单个费用槽内图标矩形尺寸。
        /// </summary>
        public Vector2 IconSize { get; init; } = new(46f, 46f);

        /// <summary>
        ///     Offset applied to the amount label relative to the centered icon rectangle.
        ///     数量标签相对居中图标矩形的偏移。
        /// </summary>
        public Vector2 LabelOffset { get; init; }

        /// <summary>
        ///     Amount-label font size.
        ///     数量标签字号。
        /// </summary>
        public int FontSize { get; init; } = 28;

        /// <summary>
        ///     Amount-label outline size.
        ///     数量标签描边尺寸。
        /// </summary>
        public int OutlineSize { get; init; } = 7;

        /// <summary>
        ///     Cost text color when the card can pay this line.
        ///     卡牌可支付该行费用时的文本颜色。
        /// </summary>
        public Color AffordableColor { get; init; } = StsColors.cream;

        /// <summary>
        ///     Cost text color when the card cannot pay this line.
        ///     卡牌无法支付该行费用时的文本颜色。
        /// </summary>
        public Color UnaffordableColor { get; init; } = StsColors.red;

        /// <summary>
        ///     Cost text color when the resolved cost is higher than the base cost but still playable.
        ///     已解析费用高于基础费用但仍可打出时的文本颜色。
        /// </summary>
        public Color IncreasedColor { get; init; } = StsColors.energyBlue;

        /// <summary>
        ///     Cost text color when the resolved cost is lower than the base cost.
        ///     已解析费用低于基础费用时的文本颜色。
        /// </summary>
        public Color DecreasedColor { get; init; } = StsColors.green;

        /// <summary>
        ///     Cost text color when an optional line is unavailable for this play.
        ///     可选支付行本次不可用时的文本颜色。
        /// </summary>
        public Color? OptionalUnavailableColor { get; init; } = StsColors.gray;

        /// <summary>
        ///     Cost text outline color when the card can pay this line.
        ///     卡牌可支付该行费用时的文本描边颜色。
        /// </summary>
        public Color AffordableOutlineColor { get; init; } = StsColors.defaultStarCostOutline;

        /// <summary>
        ///     Cost text outline color when the card cannot pay this line.
        ///     卡牌无法支付该行费用时的文本描边颜色。
        /// </summary>
        public Color UnaffordableOutlineColor { get; init; } = StsColors.unplayableEnergyCostOutline;

        /// <summary>
        ///     Cost text outline color when the resolved cost is higher than the base cost but still playable.
        ///     已解析费用高于基础费用但仍可打出时的文本描边颜色。
        /// </summary>
        public Color IncreasedOutlineColor { get; init; } = StsColors.energyBlueOutline;

        /// <summary>
        ///     Cost text outline color when the resolved cost is lower than the base cost.
        ///     已解析费用低于基础费用时的文本描边颜色。
        /// </summary>
        public Color DecreasedOutlineColor { get; init; } = StsColors.energyGreenOutline;

        /// <summary>
        ///     Cost text outline color when an optional line is unavailable for this play.
        ///     可选支付行本次不可用时的文本描边颜色。
        /// </summary>
        public Color? OptionalUnavailableOutlineColor { get; init; } = StsColors.defaultStarCostOutline;

        /// <summary>
        ///     Texture expand mode.
        ///     贴图 expand mode。
        /// </summary>
        public TextureRect.ExpandModeEnum ExpandMode { get; init; } = TextureRect.ExpandModeEnum.IgnoreSize;

        /// <summary>
        ///     Texture stretch mode.
        ///     贴图 stretch mode。
        /// </summary>
        public TextureRect.StretchModeEnum StretchMode { get; init; } =
            TextureRect.StretchModeEnum.KeepAspectCentered;

        /// <summary>
        ///     Optional cost formatter. Receives the resolved payment line.
        ///     可选费用格式化器，参数为已解析支付行。
        /// </summary>
        public Func<SecondaryResourcePaymentLine, string>? FormatCost { get; init; }

        /// <summary>
        ///     Shared default style instance.
        ///     共享默认样式实例。
        /// </summary>
        public static SecondaryResourceCardCostUiStyle Default { get; } = new();

        internal string Format(SecondaryResourcePaymentLine line)
        {
            return FormatCost?.Invoke(line) ?? (line.CostsX ? "X" : line.Cost.ToString());
        }
    }

    /// <summary>
    ///     Simple reusable card-cost display for secondary resources.
    ///     简易可复用的次级资源卡牌费用显示节点。
    /// </summary>
    public partial class NSecondaryResourceCardCostUi : Control
    {
        private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";
        private CardModel? _boundCard;
        private SecondaryResourceDefinition? _definition;

        private MegaLabel _label = null!;
        private SecondaryResourcePaymentLine? _line;
        private PileType _pileType = PileType.Hand;
        private SecondaryResourcePaymentPlan? _plan;
        private CardPreviewMode _previewMode = CardPreviewMode.Normal;
        private string? _resourceId;
        private SecondaryResourceCardCostUiStyle _style = SecondaryResourceCardCostUiStyle.Default;
        private TextureRect _texture = null!;
        private string? _useId;

        /// <summary>
        ///     Whether this node refreshes the bound card's resolved payment plan every frame.
        ///     该节点是否每帧刷新已绑定卡牌的支付计划。
        /// </summary>
        public bool AutoRefresh { get; set; } = true;

        /// <summary>
        ///     Creates and configures a card-cost display node for one secondary resource.
        ///     为一个次级资源创建并配置卡牌费用显示节点。
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
        ///     Creates and configures a card-cost display node for one secondary resource.
        ///     为一个次级资源创建并配置卡牌费用显示节点。
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
        ///     Creates and configures a card-cost display node for one play-use id.
        ///     为一个出牌条款 id 创建并配置卡牌费用显示节点。
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
        ///     Creates and configures a card-cost display node for one play-use id.
        ///     为一个出牌条款 id 创建并配置卡牌费用显示节点。
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
        ///     Configures the visual style.
        ///     配置视觉样式。
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
        }

        /// <summary>
        ///     Binds this node to one secondary resource id.
        ///     将该节点绑定到一个次级资源 id。
        /// </summary>
        public void Bind(string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
            _useId = null;
            _resourceId = resourceId.Trim();

            if (ModSecondaryResourceRegistry.TryGet(_resourceId, out var definition))
                Bind(definition);
            else if (IsNodeReady())
                Visible = false;
        }

        /// <summary>
        ///     Binds this node to one secondary resource definition.
        ///     将该节点绑定到一个次级资源定义。
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
        }

        /// <summary>
        ///     Binds this node to one play-use id and its resource id.
        ///     将该节点绑定到一个出牌条款 id 及其资源 id。
        /// </summary>
        public void BindUse(string useId, string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(useId);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            _useId = useId.Trim();
            _resourceId = resourceId.Trim();

            if (ModSecondaryResourceRegistry.TryGet(_resourceId, out var definition))
                BindUse(_useId, definition);
            else if (IsNodeReady())
                Visible = false;
        }

        /// <summary>
        ///     Binds this node to one play-use id and resource definition.
        ///     将该节点绑定到一个出牌条款 id 及其资源定义。
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
        }

        /// <summary>
        ///     Refreshes from a card UI update context.
        ///     从卡牌 UI 更新上下文刷新。
        /// </summary>
        public void Refresh<TParent>(SecondaryResourceCardUiContext<TParent, NSecondaryResourceCardCostUi> context)
            where TParent : Node
        {
            Refresh(context.Card, context.Plan, context.PileType, context.PreviewMode);
        }

        /// <summary>
        ///     Binds and refreshes this node from <paramref name="card" />.
        ///     绑定并根据 <paramref name="card" /> 刷新该节点。
        /// </summary>
        public void Refresh(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            Refresh(card, SecondaryResourcePaymentResolver.Plan(
                card,
                FreePlayBindingRegistry.IsCardFreeForUpcomingPlay(card)));
        }

        /// <summary>
        ///     Refreshes from a resolved payment plan.
        ///     根据已解析支付计划刷新。
        /// </summary>
        public void Refresh(CardModel card, SecondaryResourcePaymentPlan plan)
        {
            Refresh(card, plan, _pileType, _previewMode);
        }

        /// <summary>
        ///     Refreshes from a resolved payment plan and card visual context.
        ///     根据已解析支付计划和卡牌视觉上下文刷新。
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
            _pileType = pileType;
            _previewMode = previewMode;
            if (string.IsNullOrWhiteSpace(_resourceId))
            {
                Visible = false;
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
                Visible = false;
                return;
            }

            Refresh(plan, line);
        }

        /// <summary>
        ///     Refreshes this node from the matching resolved payment line.
        ///     根据匹配的已解析支付行刷新该节点。
        /// </summary>
        public void Refresh(SecondaryResourcePaymentPlan plan, SecondaryResourcePaymentLine line)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(line);

            _plan = plan;
            _line = line;

            if (!IsNodeReady())
                return;

            Visible = true;
            _label.SetTextAutoSize(_style.Format(line));
            var (fontColor, outlineColor) = ResolveLabelColors(line, _pileType, _previewMode);
            _label.AddThemeColorOverride(ThemeConstants.Label.FontColor, fontColor);
            _label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);
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
                Visible = false;
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            if (AutoRefresh && _boundCard != null)
                Refresh(_boundCard);
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
            if (_definition == null || _texture == null)
                return;

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
                    SecondaryResourceCardCostColor.OptionalUnavailable =>
                        (_style.OptionalUnavailableColor!.Value, _style.OptionalUnavailableOutlineColor!.Value),
                    _ => (_style.AffordableColor, _style.AffordableOutlineColor),
                };
        }
    }
}
