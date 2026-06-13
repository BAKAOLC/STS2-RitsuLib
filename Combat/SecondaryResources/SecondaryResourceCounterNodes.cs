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
    ///     Hover-tip request for one secondary-resource display node.
    ///     单个次级资源显示节点的悬浮提示请求。
    /// </summary>
    public readonly record struct SecondaryResourceHoverTipRequest(
        SecondaryResourceDefinition Definition,
        int Amount = 1,
        int? MaxAmount = null);

    /// <summary>
    ///     Hover-tip placement context for one secondary-resource display node.
    ///     单个次级资源显示节点的悬浮提示放置上下文。
    /// </summary>
    public readonly record struct SecondaryResourceHoverTipPlacementContext(
        Control Owner,
        NHoverTipSet TipSet,
        SecondaryResourceDefinition Definition,
        int Amount,
        int? MaxAmount);

    /// <summary>
    ///     Hover-tip behavior for secondary-resource display nodes.
    ///     次级资源显示节点的悬浮提示行为。
    /// </summary>
    public sealed record SecondaryResourceHoverTipStyle
    {
        private const float DefaultGap = 20f;

        /// <summary>
        ///     Whether the icon shows a hover tip.
        ///     图标是否显示悬浮提示。
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        ///     Resolver for the hover tip's screen-space top-left position.
        ///     用于决定悬浮提示左上角屏幕空间位置的 resolver。
        /// </summary>
        public Func<SecondaryResourceHoverTipPlacementContext, Vector2> ResolveGlobalPosition { get; init; } =
            ResolveAboveOwner;

        /// <summary>
        ///     Extra screen-space pixels added after custom position resolution.
        ///     自定义位置解析后追加的屏幕空间偏移。
        /// </summary>
        public Vector2 ScreenOffset { get; init; }

        /// <summary>
        ///     Shared default hover-tip style.
        ///     共享默认悬浮提示样式。
        /// </summary>
        public static SecondaryResourceHoverTipStyle Default { get; } = new();

        private static Vector2 ResolveAboveOwner(SecondaryResourceHoverTipPlacementContext context)
        {
            var ownerRect = context.Owner.GetGlobalRect();
            var tipSize = context.TipSet.Size;
            if (tipSize.X < 1f || tipSize.Y < 1f)
                tipSize = context.TipSet.GetCombinedMinimumSize();

            return new(
                ownerRect.Position.X + ownerRect.Size.X * 0.5f - tipSize.X * 0.5f,
                ownerRect.Position.Y - tipSize.Y - DefaultGap);
        }
    }

    /// <summary>
    ///     Visual and hover-tip style for a secondary-resource icon node.
    ///     次级资源图标节点的视觉和悬浮提示样式。
    /// </summary>
    public sealed record SecondaryResourceIconStyle
    {
        /// <summary>
        ///     Root control and icon rectangle size.
        ///     根 control 和图标矩形尺寸。
        /// </summary>
        public Vector2 Size { get; init; } = new(46f, 46f);

        /// <summary>
        ///     Icon offset inside the root control.
        ///     图标在根 control 内的偏移。
        /// </summary>
        public Vector2 IconOffset { get; init; }

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
        ///     Optional hover-tip behavior. Null disables built-in hover-tip wiring.
        ///     可选悬浮提示行为。为 null 时禁用内建悬浮提示绑定。
        /// </summary>
        public SecondaryResourceHoverTipStyle? HoverTip { get; init; } =
            SecondaryResourceHoverTipStyle.Default;

        /// <summary>
        ///     Shared default icon style.
        ///     共享默认图标样式。
        /// </summary>
        public static SecondaryResourceIconStyle Default { get; } = new();
    }

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
        ///     Optional icon-node style. When null, <see cref="IconSize" /> is used with
        ///     <see cref="SecondaryResourceIconStyle.Default" />.
        ///     可选图标节点样式。为 null 时使用 <see cref="IconSize" /> 和
        ///     <see cref="SecondaryResourceIconStyle.Default" />。
        /// </summary>
        public SecondaryResourceIconStyle? IconStyle { get; init; }

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

        private Player? _boundPlayer;
        private SecondaryResourceDefinition? _definition;
        private bool _hasBeenMaterial;
        private NSecondaryResourceIcon _icon = null!;
        private int? _maxAmount;
        private SecondaryResourceCounterStyle _style = SecondaryResourceCounterStyle.Default;

        /// <summary>
        ///     Whether this counter refreshes the bound player's resource every frame.
        ///     该计数器是否每帧刷新已绑定玩家的资源数量。
        /// </summary>
        public bool AutoRefresh { get; set; }

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
        ///     Binds this counter to a player for automatic or manual refreshes.
        ///     将该计数器绑定到一名玩家，用于自动或手动刷新。
        /// </summary>
        public void Bind(Player? player, bool autoRefresh = true)
        {
            if (!ReferenceEquals(_boundPlayer, player))
            {
                _hasBeenMaterial = false;
                Visible = false;
            }

            _boundPlayer = player;
            AutoRefresh = autoRefresh;
            Refresh(_boundPlayer);
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

            var amount = SecondaryResourceCmd.Get(player, _definition.Id);
            var maxAmount = SecondaryResourceCmd.GetMax(player, _definition.Id);
            _hasBeenMaterial = _hasBeenMaterial || amount > _definition.DefaultAmount;
            Visible = _hasBeenMaterial || _definition.IsVisibleInCombatUi(player);
            SetAmount(amount, maxAmount);
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
            _icon?.SetAmount(_amount, _maxAmount);
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
                MouseFilter = MouseFilterEnum.Pass,
                Position = GetIconPosition(),
            };
            if (_definition != null)
                _icon.Configure(_definition, ResolveIconStyle());
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

            ApplyDefinition();
            SetAmount(_amount, _maxAmount);
        }

        /// <inheritdoc />
        public override void _Process(double delta)
        {
            if (AutoRefresh && _boundPlayer != null) Refresh(_boundPlayer);
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
            if (_icon != null)
                NHoverTipSet.Remove(_icon);
        }

        private void ApplyDefinition()
        {
            if (_definition == null || _icon == null)
                return;

            _icon.Configure(_definition, ResolveIconStyle());
            _icon.SetAmount(_amount, _maxAmount);
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

        private SecondaryResourceIconStyle ResolveIconStyle()
        {
            return _style.IconStyle ?? SecondaryResourceIconStyle.Default with
            {
                Size = _style.IconSize,
            };
        }
    }

    /// <summary>
    ///     Built-in secondary-resource icon node with consistent texture setup and optional hover-tip wiring.
    ///     内建次级资源图标节点，统一处理贴图设置和可选悬浮提示绑定。
    /// </summary>
    public partial class NSecondaryResourceIcon : Control
    {
        private int _amount = 1;
        private SecondaryResourceDefinition? _definition;
        private SecondaryResourceHoverTipBinder? _hoverTipBinder;
        private int? _maxAmount;
        private SecondaryResourceIconStyle _style = SecondaryResourceIconStyle.Default;
        private TextureRect _texture = null!;

        /// <summary>
        ///     Creates and configures a secondary-resource icon.
        ///     创建并配置次级资源图标。
        /// </summary>
        public static NSecondaryResourceIcon Create(
            SecondaryResourceDefinition definition,
            SecondaryResourceIconStyle? style = null,
            int amount = 1,
            int? maxAmount = null)
        {
            var icon = new NSecondaryResourceIcon();
            icon.Configure(definition, style);
            icon.SetAmount(amount, maxAmount);
            return icon;
        }

        /// <summary>
        ///     Configures the resource definition and visual style.
        ///     配置资源定义和视觉样式。
        /// </summary>
        public void Configure(SecondaryResourceDefinition definition, SecondaryResourceIconStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _definition = definition;
            _style = style ?? SecondaryResourceIconStyle.Default;
            CustomMinimumSize = _style.Size;
            Size = _style.Size;

            if (!IsNodeReady()) return;
            ApplyStyleAndDefinition();
            RefreshHoverTipBinding();
        }

        /// <summary>
        ///     Updates the amount displayed in this icon's hover tip.
        ///     更新该图标悬浮提示中显示的数量。
        /// </summary>
        public void SetAmount(int amount, int? maxAmount = null)
        {
            _amount = amount;
            _maxAmount = maxAmount;
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            _texture = new()
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(_texture);

            ApplyStyleAndDefinition();
            RefreshHoverTipBinding();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            _hoverTipBinder?.Hide();
        }

        private void ApplyStyleAndDefinition()
        {
            if (_texture == null)
                return;

            CustomMinimumSize = _style.Size;
            Size = _style.Size;
            _texture.Position = _style.IconOffset;
            _texture.CustomMinimumSize = _style.Size;
            _texture.Size = _style.Size;
            _texture.ExpandMode = _style.ExpandMode;
            _texture.StretchMode = _style.StretchMode;

            if (_definition == null)
            {
                _texture.Texture = null;
                return;
            }

            var path = _definition.LargeIconPath ?? _definition.SmallIconPath;
            _texture.Texture = string.IsNullOrWhiteSpace(path) ? null : ResourceLoader.Load<Texture2D>(path);
        }

        private void RefreshHoverTipBinding()
        {
            if (!IsNodeReady())
                return;

            if (_definition == null || _style.HoverTip is not { Enabled: true } hoverTipStyle)
            {
                _hoverTipBinder?.QueueFree();
                _hoverTipBinder = null;
                return;
            }

            if (_hoverTipBinder == null || !IsInstanceValid(_hoverTipBinder))
            {
                _hoverTipBinder = SecondaryResourceHoverTipBinder.Bind(
                    this,
                    CreateHoverTipRequest,
                    hoverTipStyle);
                return;
            }

            _hoverTipBinder.Configure(CreateHoverTipRequest, hoverTipStyle);
        }

        private SecondaryResourceHoverTipRequest? CreateHoverTipRequest()
        {
            return _definition == null ? null : new(_definition, _amount, _maxAmount);
        }
    }

    /// <summary>
    ///     Reusable binder that gives any secondary-resource display <see cref="Control" /> the same
    ///     mouse-enter / mouse-exit hover-tip behavior used by vanilla resource counters.
    ///     可复用绑定器，为任意次级资源显示 <see cref="Control" /> 提供与原版资源计数器一致的
    ///     mouse-enter / mouse-exit 悬浮提示行为。
    /// </summary>
    public partial class SecondaryResourceHoverTipBinder : Node
    {
        private readonly Callable _hideCallable;
        private readonly Callable _showCallable;
        private Control _owner = null!;
        private Func<SecondaryResourceHoverTipRequest?> _requestFactory = null!;
        private SecondaryResourceHoverTipStyle _style = SecondaryResourceHoverTipStyle.Default;

        /// <summary>
        ///     Creates a binder node.
        ///     创建绑定器节点。
        /// </summary>
        public SecondaryResourceHoverTipBinder()
        {
            _showCallable = Callable.From(Show);
            _hideCallable = Callable.From(Hide);
        }

        /// <summary>
        ///     Binds a secondary-resource hover tip to any display control.
        ///     将次级资源悬浮提示绑定到任意显示 control。
        /// </summary>
        public static SecondaryResourceHoverTipBinder Bind(
            Control owner,
            Func<SecondaryResourceHoverTipRequest?> requestFactory,
            SecondaryResourceHoverTipStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(requestFactory);

            var binder = new SecondaryResourceHoverTipBinder
            {
                Name = "RitsuLibSecondaryResourceHoverTipBinder",
                _owner = owner,
            };
            binder.Configure(requestFactory, style);
            owner.AddChild(binder);
            return binder;
        }

        /// <summary>
        ///     Binds a fixed secondary-resource definition with dynamic amount providers.
        ///     使用固定次级资源定义和动态数量 provider 绑定悬浮提示。
        /// </summary>
        public static SecondaryResourceHoverTipBinder Bind(
            Control owner,
            SecondaryResourceDefinition definition,
            Func<int> amount,
            Func<int?>? maxAmount = null,
            SecondaryResourceHoverTipStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(amount);

            return Bind(
                owner,
                () => new(definition, amount(), maxAmount?.Invoke()),
                style);
        }

        /// <summary>
        ///     Updates the request factory and style used by this binder.
        ///     更新该绑定器使用的请求 factory 和样式。
        /// </summary>
        public void Configure(
            Func<SecondaryResourceHoverTipRequest?> requestFactory,
            SecondaryResourceHoverTipStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(requestFactory);
            _requestFactory = requestFactory;
            _style = style ?? SecondaryResourceHoverTipStyle.Default;
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            _owner ??= GetParent<Control>();

            ConnectOwnerSignals();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            Hide();
            DisconnectOwnerSignals();
        }

        /// <summary>
        ///     Shows the current hover tip, if the request factory returns one.
        ///     如果请求 factory 返回内容，则显示当前悬浮提示。
        /// </summary>
        public void Show()
        {
            if (_requestFactory == null || _owner == null || !IsInstanceValid(_owner))
                return;

            var request = _requestFactory();
            if (request == null)
                return;

            SecondaryResourceHoverTipFactory.Show(
                _owner,
                request.Value.Definition,
                request.Value.Amount,
                request.Value.MaxAmount,
                _style);
        }

        /// <summary>
        ///     Removes the active hover tip owned by this binder's control.
        ///     移除此绑定器 control 拥有的当前悬浮提示。
        /// </summary>
        public void Hide()
        {
            if (_owner != null && IsInstanceValid(_owner))
                NHoverTipSet.Remove(_owner);
        }

        private void ConnectOwnerSignals()
        {
            if (_owner == null || !IsInstanceValid(_owner))
                return;

            if (!_owner.IsConnected(Control.SignalName.MouseEntered, _showCallable))
                _owner.Connect(Control.SignalName.MouseEntered, _showCallable);
            if (!_owner.IsConnected(Control.SignalName.MouseExited, _hideCallable))
                _owner.Connect(Control.SignalName.MouseExited, _hideCallable);
        }

        private void DisconnectOwnerSignals()
        {
            if (_owner == null || !IsInstanceValid(_owner))
                return;

            if (_owner.IsConnected(Control.SignalName.MouseEntered, _showCallable))
                _owner.Disconnect(Control.SignalName.MouseEntered, _showCallable);
            if (_owner.IsConnected(Control.SignalName.MouseExited, _hideCallable))
                _owner.Disconnect(Control.SignalName.MouseExited, _hideCallable);
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
            if (!AutoRefresh || _boundDefinitions == null || _boundPlayer == null)
                return;

            foreach (var definition in _boundDefinitions)
                if (_counters.TryGetValue(definition.Id, out var counter))
                    counter.Refresh(_boundPlayer);
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
            var title = SecondaryResourceText.GetTitle(definition, amount, maxAmount);
            var description = SecondaryResourceText.GetDescription(definition, amount, maxAmount);
            var tip = (title, description) switch
            {
                ({ } titleLoc, { } descriptionLoc) => new(titleLoc, descriptionLoc, icon),
                ({ } titleLoc, null) => CreateRaw(definition.Id, titleLoc.GetFormattedText(),
                    ResolveDescription(definition), icon),
                (null, { } descriptionLoc) => CreateRaw(definition.Id, ResolveTitle(definition),
                    descriptionLoc.GetFormattedText(), icon),
                _ => CreateRaw(definition.Id, ResolveTitle(definition), ResolveDescription(definition), icon),
            };
            tip.Id = definition.Id;
            return tip;
        }

        /// <summary>
        ///     Creates and shows a secondary-resource hover tip for an icon owner.
        ///     为图标 owner 创建并显示次级资源悬浮提示。
        /// </summary>
        public static NHoverTipSet? Show(
            Control owner,
            SecondaryResourceDefinition definition,
            int amount,
            int? maxAmount = null,
            SecondaryResourceHoverTipStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(definition);

            var resolvedStyle = style ?? SecondaryResourceHoverTipStyle.Default;
            if (!resolvedStyle.Enabled)
                return null;

            var hoverTip = Create(definition, amount, maxAmount);
            NHoverTipSet.Remove(owner);
            var tipSet = NHoverTipSet.CreateAndShow(owner, hoverTip);
            if (tipSet == null)
                return null;

            var context = new SecondaryResourceHoverTipPlacementContext(
                owner,
                tipSet,
                definition,
                amount,
                maxAmount);
            tipSet.GlobalPosition = resolvedStyle.ResolveGlobalPosition(context) + resolvedStyle.ScreenOffset;

            return tipSet;
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
