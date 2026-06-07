using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Visual style for the built-in secondary-resource counter nodes.
    ///     内建次级资源计数节点的视觉样式。
    /// </summary>
    public sealed record SecondaryResourceCounterStyle
    {
        /// <summary>
        ///     Root control size for one counter.
        ///     单个计数器根节点尺寸。
        /// </summary>
        public Vector2 CounterSize { get; init; } = new(48f, 48f);

        /// <summary>
        ///     Icon rectangle size inside one counter.
        ///     单个计数器内图标矩形尺寸。
        /// </summary>
        public Vector2 IconSize { get; init; } = new(46f, 46f);

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
        ///     Amount-label color when the resource is positive.
        ///     资源数量为正时的数量标签颜色。
        /// </summary>
        public Color PositiveColor { get; init; } = StsColors.cream;

        /// <summary>
        ///     Amount-label color when the resource is zero.
        ///     资源数量为零时的数量标签颜色。
        /// </summary>
        public Color ZeroColor { get; init; } = StsColors.red;

        /// <summary>
        ///     Amount-label outline color.
        ///     数量标签描边颜色。
        /// </summary>
        public Color OutlineColor { get; init; } = new(0.16f, 0.08f, 0.04f);

        /// <summary>
        ///     Amount-label offset relative to the centered icon rectangle.
        ///     数量标签相对居中图标矩形的偏移。
        /// </summary>
        public Vector2 AmountLabelOffset { get; init; }

        /// <summary>
        ///     Horizontal separation used by <see cref="NSecondaryResourceCounterRow" />.
        ///     <see cref="NSecondaryResourceCounterRow" /> 使用的水平间距。
        /// </summary>
        public int RowSeparation { get; init; } = 8;

        /// <summary>
        ///     Optional amount formatter. Receives current amount and max amount.
        ///     可选数量格式化器，参数为当前数量和最大数量。
        /// </summary>
        public Func<int, int?, string>? FormatAmount { get; init; }

        /// <summary>
        ///     Shared default style instance.
        ///     共享默认样式实例。
        /// </summary>
        public static SecondaryResourceCounterStyle Default { get; } = new();

        internal string Format(int amount, int? maxAmount)
        {
            return FormatAmount?.Invoke(amount, maxAmount) ??
                   (maxAmount.HasValue ? $"{amount}/{maxAmount.Value}" : amount.ToString());
        }
    }

    /// <summary>
    ///     Built-in single secondary-resource counter with icon, amount label, and hover tip.
    ///     内建单个次级资源计数节点，包含图标、数量标签和悬浮提示。
    /// </summary>
    public partial class NSecondaryResourceCounter : Control
    {
        private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";
        private int _amount;
        private MegaLabel _amountLabel = null!;

        private SecondaryResourceDefinition? _definition;
        private TextureRect _icon = null!;
        private int? _maxAmount;
        private SecondaryResourceCounterStyle _style = SecondaryResourceCounterStyle.Default;

        /// <summary>
        ///     Creates and configures a counter for <paramref name="definition" />.
        ///     为 <paramref name="definition" /> 创建并配置计数器。
        /// </summary>
        public static NSecondaryResourceCounter Create(
            SecondaryResourceDefinition definition,
            SecondaryResourceCounterStyle? style = null)
        {
            var counter = new NSecondaryResourceCounter();
            counter.Configure(definition, style);
            return counter;
        }

        /// <summary>
        ///     Configures the counter definition and style.
        ///     配置计数器定义和样式。
        /// </summary>
        public void Configure(SecondaryResourceDefinition definition, SecondaryResourceCounterStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _definition = definition;
            _style = style ?? SecondaryResourceCounterStyle.Default;
            CustomMinimumSize = _style.CounterSize;
            Size = _style.CounterSize;

            if (IsNodeReady())
                ApplyDefinition();
        }

        /// <summary>
        ///     Refreshes the displayed amount from <paramref name="player" />.
        ///     从 <paramref name="player" /> 刷新显示数量。
        /// </summary>
        public void Refresh(Player? player)
        {
            if (_definition == null || player == null)
            {
                Visible = false;
                return;
            }

            SetAmount(
                SecondaryResourceCmd.Get(player, _definition.Id),
                SecondaryResourceCmd.GetMax(player, _definition.Id));
        }

        /// <summary>
        ///     Sets the displayed amount directly.
        ///     直接设置显示数量。
        /// </summary>
        public void SetAmount(int amount, int? maxAmount = null)
        {
            _amount = amount;
            _maxAmount = maxAmount;
            if (!IsNodeReady())
                return;

            _amountLabel.SetTextAutoSize(_style.Format(amount, maxAmount));
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor,
                amount <= 0 ? _style.ZeroColor : _style.PositiveColor);
        }

        /// <summary>
        ///     Initializes child controls and applies the configured definition.
        ///     初始化子控件并应用已配置的资源定义。
        /// </summary>
        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            CustomMinimumSize = _style.CounterSize;
            Size = _style.CounterSize;

            _icon = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = _style.IconSize,
                Size = _style.IconSize,
                Position = GetIconPosition(),
            };
            AddChild(_icon);

            _amountLabel = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = _style.IconSize,
                Size = _style.IconSize,
                Position = GetIconPosition() + _style.AmountLabelOffset,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutoSizeEnabled = true,
                MinFontSize = Math.Max(8, _style.FontSize - 10),
                MaxFontSize = _style.FontSize,
            };
            ApplyAmountLabelTheme();
            AddChild(_amountLabel);

            Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
            Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));

            ApplyDefinition();
            SetAmount(_amount, _maxAmount);
        }

        private Vector2 GetIconPosition()
        {
            return (_style.CounterSize - _style.IconSize) * 0.5f;
        }

        /// <summary>
        ///     Clears the active hover tip when the counter leaves the scene tree.
        ///     当计数器离开场景树时清理当前悬浮提示。
        /// </summary>
        public override void _ExitTree()
        {
            NHoverTipSet.Remove(this);
        }

        private void ApplyDefinition()
        {
            if (_definition == null || _icon == null)
                return;

            var iconPath = _definition.LargeIconPath ?? _definition.SmallIconPath;
            if (!string.IsNullOrWhiteSpace(iconPath))
                _icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
        }

        private void ApplyAmountLabelTheme()
        {
            var font = PreloadManager.Cache.GetAsset<Font>(DefaultLabelFontPath);
            _amountLabel.AddThemeFontOverride(ThemeConstants.Label.Font, font);
            _amountLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, _style.FontSize);
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, _style.PositiveColor);
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, _style.OutlineColor);
            _amountLabel.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, _style.OutlineSize);
        }

        private void OnHovered()
        {
            if (_definition == null)
                return;

            var hoverTip = SecondaryResourceHoverTipFactory.Create(_definition, _amount, _maxAmount);
            NHoverTipSet.CreateAndShow(this, hoverTip)?.SetGlobalPosition(GlobalPosition + new Vector2(Size.X, 0f));
        }

        private void OnUnhovered()
        {
            NHoverTipSet.Remove(this);
        }
    }

    /// <summary>
    ///     Built-in row container for multiple secondary-resource counters.
    ///     内建多次级资源计数器行容器。
    /// </summary>
    public partial class NSecondaryResourceCounterRow : Control
    {
        private readonly Dictionary<string, NSecondaryResourceCounter> _counters =
            new(StringComparer.OrdinalIgnoreCase);

        private SecondaryResourceDefinition[]? _boundDefinitions;
        private Player? _boundPlayer;
        private SecondaryResourceDefinition[]? _pendingDefinitions;
        private Player? _pendingPlayer;

        private HBoxContainer _row = null!;
        private SecondaryResourceCounterStyle _style = SecondaryResourceCounterStyle.Default;

        /// <summary>
        ///     Whether this row refreshes bound definitions every frame.
        ///     该行是否每帧刷新已绑定的资源定义。
        /// </summary>
        public bool AutoRefresh { get; set; }

        /// <summary>
        ///     Configures the row style.
        ///     配置行样式。
        /// </summary>
        public void Configure(SecondaryResourceCounterStyle? style = null)
        {
            _style = style ?? SecondaryResourceCounterStyle.Default;
            _row?.AddThemeConstantOverride("separation", _style.RowSeparation);
        }

        /// <summary>
        ///     Binds this row to a player and definition set for automatic or manual refreshes.
        ///     将该行绑定到一名玩家和一组资源定义，用于自动或手动刷新。
        /// </summary>
        public void Bind(
            Player? player,
            IReadOnlyList<SecondaryResourceDefinition> definitions,
            bool autoRefresh = true)
        {
            ArgumentNullException.ThrowIfNull(definitions);

            _boundPlayer = player;
            _boundDefinitions = definitions.ToArray();
            AutoRefresh = autoRefresh;
            Refresh(_boundPlayer, _boundDefinitions);
        }

        /// <summary>
        ///     Refreshes visible counters for the supplied definitions.
        ///     根据传入定义刷新可见计数器。
        /// </summary>
        public void Refresh(Player? player, IReadOnlyList<SecondaryResourceDefinition> visibleDefinitions)
        {
            ArgumentNullException.ThrowIfNull(visibleDefinitions);

            if (!IsNodeReady())
            {
                _pendingPlayer = player;
                _pendingDefinitions = visibleDefinitions.ToArray();
                return;
            }

            foreach (var counter in _counters.Values)
                counter.Visible = false;

            if (player == null)
            {
                Visible = false;
                return;
            }

            var anyVisible = false;
            foreach (var definition in visibleDefinitions)
            {
                var counter = GetOrCreateCounter(definition);
                counter.Visible = true;
                counter.Refresh(player);
                anyVisible = true;
            }

            Visible = anyVisible;
        }

        /// <summary>
        ///     Initializes the row container used by child counters.
        ///     初始化子计数器使用的行容器。
        /// </summary>
        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _row = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorRight = 1f,
                AnchorBottom = 1f,
            };
            _row.AddThemeConstantOverride("separation", _style.RowSeparation);
            AddChild(_row);

            if (_pendingDefinitions == null) return;
            var player = _pendingPlayer;
            var definitions = _pendingDefinitions;
            _pendingPlayer = null;
            _pendingDefinitions = null;
            Refresh(player, definitions);
        }

        /// <summary>
        ///     Refreshes bound resource definitions when automatic refresh is enabled.
        ///     启用自动刷新时刷新已绑定的资源定义。
        /// </summary>
        public override void _Process(double delta)
        {
            if (AutoRefresh && _boundDefinitions != null)
                Refresh(_boundPlayer, _boundDefinitions);
        }

        private NSecondaryResourceCounter GetOrCreateCounter(SecondaryResourceDefinition definition)
        {
            if (_counters.TryGetValue(definition.Id, out var existing))
                return existing;

            var created = NSecondaryResourceCounter.Create(definition, _style);
            _row.AddChild(created);
            _counters[definition.Id] = created;
            return created;
        }
    }

    /// <summary>
    ///     Hover-tip factory for secondary-resource counters.
    ///     次级资源计数器的悬浮提示工厂。
    /// </summary>
    public static class SecondaryResourceHoverTipFactory
    {
        private static readonly PropertyInfo TitleProperty = typeof(HoverTip).GetProperty(nameof(HoverTip.Title))!;

        private static readonly PropertyInfo DescriptionProperty =
            typeof(HoverTip).GetProperty(nameof(HoverTip.Description))!;

        private static readonly PropertyInfo IconProperty = typeof(HoverTip).GetProperty(nameof(HoverTip.Icon))!;

        /// <summary>
        ///     Creates a hover tip for a secondary resource.
        ///     为次级资源创建悬浮提示。
        /// </summary>
        public static HoverTip Create(
            SecondaryResourceDefinition definition,
            int amount,
            int? maxAmount = null)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var icon = LoadIcon(definition);
            var title = ResolveTitle(definition);
            var description = ResolveDescription(definition);
            var amountText = maxAmount.HasValue ? $"{amount}/{maxAmount.Value}" : amount.ToString();
            description += $"\nAmount: {amountText}";

            return CreateRaw(definition.Id, title, description, icon);
        }

        private static Texture2D? LoadIcon(SecondaryResourceDefinition definition)
        {
            var path = definition.LargeIconPath ?? definition.SmallIconPath;
            return string.IsNullOrWhiteSpace(path) ? null : ResourceLoader.Load<Texture2D>(path);
        }

        private static string ResolveTitle(SecondaryResourceDefinition definition)
        {
            return SecondaryResourceText.GetTitleText(definition);
        }

        private static string ResolveDescription(SecondaryResourceDefinition definition)
        {
            return SecondaryResourceText.GetDescriptionText(definition);
        }

        private static HoverTip CreateRaw(string id, string title, string description, Texture2D? icon)
        {
            object boxed = default(HoverTip);
            TitleProperty.SetValue(boxed, title);
            DescriptionProperty.SetValue(boxed, description);
            IconProperty.SetValue(boxed, icon);

            var tip = (HoverTip)boxed;
            tip.Id = id;
            return tip;
        }
    }
}
