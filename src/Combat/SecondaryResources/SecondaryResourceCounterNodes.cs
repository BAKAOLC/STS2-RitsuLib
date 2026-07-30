using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Describes a hover-tip request for one secondary-resource display.</para>
    ///     <para xml:lang="zh-CN">描述一个次级资源显示节点的悬浮提示请求。</para>
    /// </summary>
    public readonly record struct SecondaryResourceHoverTipRequest(
        SecondaryResourceDefinition Definition,
        int Amount = 1,
        int? MaxAmount = null);

    /// <summary>
    ///     <para xml:lang="en">Provides placement data for a secondary-resource hover tip.</para>
    ///     <para xml:lang="zh-CN">提供次级资源悬浮提示的放置数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceHoverTipPlacementContext(
        Control Owner,
        NHoverTipSet TipSet,
        SecondaryResourceDefinition Definition,
        int Amount,
        int? MaxAmount);

    /// <summary>
    ///     <para xml:lang="en">Configures hover-tip behavior for secondary-resource displays.</para>
    ///     <para xml:lang="zh-CN">配置次级资源显示节点的悬浮提示行为。</para>
    /// </summary>
    public sealed record SecondaryResourceHoverTipStyle
    {
        private const float DefaultGap = 20f;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the display shows a hover tip.</para>
        ///     <para xml:lang="zh-CN">获取显示节点是否显示悬浮提示。</para>
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the resolver for the hover tip's screen-space top-left position.</para>
        ///     <para xml:lang="zh-CN">获取用于解析悬浮提示左上角屏幕空间位置的函数。</para>
        /// </summary>
        public Func<SecondaryResourceHoverTipPlacementContext, Vector2> ResolveGlobalPosition { get; init; } =
            ResolveAboveOwner;

        /// <summary>
        ///     <para xml:lang="en">Gets the screen-space offset added after position resolution.</para>
        ///     <para xml:lang="zh-CN">获取位置解析完成后追加的屏幕空间偏移。</para>
        /// </summary>
        public Vector2 ScreenOffset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared default hover-tip style.</para>
        ///     <para xml:lang="zh-CN">获取共享的默认悬浮提示样式。</para>
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
    ///     <para xml:lang="en">Configures the appearance and hover tip of a secondary-resource icon.</para>
    ///     <para xml:lang="zh-CN">配置次级资源图标的外观与悬浮提示。</para>
    /// </summary>
    public sealed record SecondaryResourceIconStyle
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the root control and icon rectangle size.</para>
        ///     <para xml:lang="zh-CN">获取根控件及图标矩形的尺寸。</para>
        /// </summary>
        public Vector2 Size { get; init; } = new(46f, 46f);

        /// <summary>
        ///     <para xml:lang="en">Gets the icon offset within the root control.</para>
        ///     <para xml:lang="zh-CN">获取图标在根控件内的偏移。</para>
        /// </summary>
        public Vector2 IconOffset { get; init; }

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
        ///     <para xml:lang="en">Gets the optional hover-tip style; <see langword="null" /> disables hover tips.</para>
        ///     <para xml:lang="zh-CN">获取可选的悬浮提示样式；为 <see langword="null" /> 时禁用悬浮提示。</para>
        /// </summary>
        public SecondaryResourceHoverTipStyle? HoverTip { get; init; } =
            SecondaryResourceHoverTipStyle.Default;

        /// <summary>
        ///     <para xml:lang="en">Gets the shared default icon style.</para>
        ///     <para xml:lang="zh-CN">获取共享的默认图标样式。</para>
        /// </summary>
        public static SecondaryResourceIconStyle Default { get; } = new();
    }

    /// <summary>
    ///     <para xml:lang="en">Configures visual feedback when a secondary-resource counter increases.</para>
    ///     <para xml:lang="zh-CN">配置次级资源计数器增加时的视觉反馈。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterGainFeedback
    {
        private const string IroncladEnergyBackVfxPath =
            "res://scenes/vfx/energy/ironclad/ironclad_energy_vfx_back.tscn";

        private const string IroncladEnergyFrontVfxPath =
            "res://scenes/vfx/energy/ironclad/ironclad_energy_vfx_front.tscn";

        /// <summary>
        ///     <para xml:lang="en">Gets the ordered effects played when the displayed amount increases.</para>
        ///     <para xml:lang="zh-CN">获取显示数量增加时按顺序播放的效果。</para>
        /// </summary>
        public IReadOnlyList<SecondaryResourceCounterGainEffect> Effects { get; init; } = [];

        /// <summary>
        ///     <para xml:lang="en">Gets feedback with no effects.</para>
        ///     <para xml:lang="zh-CN">获取不包含任何效果的反馈。</para>
        /// </summary>
        public static SecondaryResourceCounterGainFeedback None { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets Stars-counter-style gain feedback scaled from the current icon's rendered size.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取根据当前图标实际渲染尺寸缩放的辉星计数器式增加反馈。</para>
        /// </summary>
        public static SecondaryResourceCounterGainFeedback StarCounterLike { get; } = From(
            SecondaryResourceCounterGainEffects.IconBrightnessFlash(),
            SecondaryResourceCounterGainEffects.StarCounterLikeBurst());

        /// <summary>
        ///     <para xml:lang="en">Gets energy-counter-style gain feedback using the game's energy particle scenes.</para>
        ///     <para xml:lang="zh-CN">获取使用游戏能量粒子场景的能量计数器式增加反馈。</para>
        /// </summary>
        public static SecondaryResourceCounterGainFeedback EnergyCounterLike { get; } = From(
            SecondaryResourceCounterGainEffects.EnergyCounterLikeParticles(
                IroncladEnergyBackVfxPath,
                IroncladEnergyFrontVfxPath));

        /// <summary>
        ///     <para xml:lang="en">Creates gain feedback from the supplied non-null effects.</para>
        ///     <para xml:lang="zh-CN">根据提供的非空效果创建增加反馈。</para>
        /// </summary>
        public static SecondaryResourceCounterGainFeedback From(params SecondaryResourceCounterGainEffect[] effects)
        {
            ArgumentNullException.ThrowIfNull(effects);
            return new()
            {
                Effects = [.. effects.Where(static effect => effect != null)],
            };
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines one visual effect played when a secondary-resource counter increases.</para>
    ///     <para xml:lang="zh-CN">定义次级资源计数器增加时播放的一项视觉效果。</para>
    /// </summary>
    public abstract record SecondaryResourceCounterGainEffect;

    /// <summary>
    ///     <para xml:lang="en">Flashes the icon with the game's HSV brightness shader.</para>
    ///     <para xml:lang="zh-CN">使用游戏的 HSV 亮度着色器使图标闪烁。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterIconBrightnessFlashEffect(
        float BrightnessFrom = 2f,
        float BrightnessTo = 1f,
        double DurationSeconds = 0.2) : SecondaryResourceCounterGainEffect;

    /// <summary>
    ///     <para xml:lang="en">Creates a one-shot Stars-counter-style burst from the current resource icon.</para>
    ///     <para xml:lang="zh-CN">根据当前资源图标创建一次性的辉星计数器式爆发效果。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterStarCounterLikeBurstEffect(
        Color Color,
        double DurationSeconds = 1.0) : SecondaryResourceCounterGainEffect;

    /// <summary>
    ///     <para xml:lang="en">Uses the game's energy-effect scenes as energy-counter-style particles.</para>
    ///     <para xml:lang="zh-CN">使用游戏的能量特效场景生成能量计数器式粒子效果。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterEnergyCounterLikeParticlesEffect(
        string BackScenePath,
        string FrontScenePath,
        Vector2 Offset = default,
        Vector2? Scale = null) : SecondaryResourceCounterGainEffect;

    /// <summary>
    ///     <para xml:lang="en">Instantiates a caller-supplied scene as a one-shot gain effect.</para>
    ///     <para xml:lang="zh-CN">将调用方提供的场景实例化为一次性的增加效果。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterSceneBurstEffect(
        string ScenePath,
        Vector2 Offset = default,
        Vector2? Scale = null,
        bool BehindCounter = true) : SecondaryResourceCounterGainEffect;

    /// <summary>
    ///     <para xml:lang="en">Creates built-in secondary-resource counter gain effects.</para>
    ///     <para xml:lang="zh-CN">创建内置的次级资源计数器增加效果。</para>
    /// </summary>
    public static class SecondaryResourceCounterGainEffects
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an icon-brightness flash effect.</para>
        ///     <para xml:lang="zh-CN">创建图标亮度闪烁效果。</para>
        /// </summary>
        public static SecondaryResourceCounterIconBrightnessFlashEffect IconBrightnessFlash(
            float brightnessFrom = 2f,
            float brightnessTo = 1f,
            double durationSeconds = 0.2)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);
            return new(brightnessFrom, brightnessTo, durationSeconds);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a Stars-counter-style burst from the current resource icon.</para>
        ///     <para xml:lang="zh-CN">根据当前资源图标创建辉星计数器式爆发效果。</para>
        /// </summary>
        public static SecondaryResourceCounterStarCounterLikeBurstEffect StarCounterLikeBurst(
            Color? color = null,
            double durationSeconds = 1.0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);
            return new(color ?? new Color(0.77f, 0.93f, 1f),
                durationSeconds);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an energy-counter-style effect from the game's energy particle scenes.</para>
        ///     <para xml:lang="zh-CN">使用游戏的能量粒子场景创建能量计数器式效果。</para>
        /// </summary>
        public static SecondaryResourceCounterEnergyCounterLikeParticlesEffect EnergyCounterLikeParticles(
            string backScenePath,
            string frontScenePath,
            Vector2 offset = default,
            Vector2? scale = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backScenePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(frontScenePath);
            return new(backScenePath, frontScenePath, offset,
                scale);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a burst effect from a caller-supplied scene.</para>
        ///     <para xml:lang="zh-CN">使用调用方提供的场景创建爆发效果。</para>
        /// </summary>
        public static SecondaryResourceCounterSceneBurstEffect SceneBurst(
            string scenePath,
            Vector2 offset = default,
            Vector2? scale = null,
            bool behindCounter = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
            return new(scenePath, offset, scale, behindCounter);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Configures the appearance and gain feedback of built-in resource counters.</para>
    ///     <para xml:lang="zh-CN">配置内置次级资源计数器的外观与增加反馈。</para>
    /// </summary>
    public sealed record SecondaryResourceCounterStyle
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the root control size of one counter.</para>
        ///     <para xml:lang="zh-CN">获取一个计数器根控件的尺寸。</para>
        /// </summary>
        public Vector2 CounterSize { get; init; } = new(48f, 48f);

        /// <summary>
        ///     <para xml:lang="en">Gets the icon rectangle size within one counter.</para>
        ///     <para xml:lang="zh-CN">获取一个计数器内图标矩形的尺寸。</para>
        /// </summary>
        public Vector2 IconSize { get; init; } = new(46f, 46f);

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
        ///     <para xml:lang="en">Gets the amount-label color for a positive resource amount.</para>
        ///     <para xml:lang="zh-CN">获取资源数量为正数时的数量标签颜色。</para>
        /// </summary>
        public Color PositiveColor { get; init; } = StsColors.cream;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label color for a nonpositive resource amount.</para>
        ///     <para xml:lang="zh-CN">获取资源数量小于或等于零时的数量标签颜色。</para>
        /// </summary>
        public Color ZeroColor { get; init; } = StsColors.red;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label outline color.</para>
        ///     <para xml:lang="zh-CN">获取数量标签的描边颜色。</para>
        /// </summary>
        public Color OutlineColor { get; init; } = new(0.16f, 0.08f, 0.04f);

        /// <summary>
        ///     <para xml:lang="en">Gets the amount-label offset from the centered icon rectangle.</para>
        ///     <para xml:lang="zh-CN">获取数量标签相对于居中图标矩形的偏移。</para>
        /// </summary>
        public Vector2 AmountLabelOffset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether increases use the game's Stars-counter-style smoothing.</para>
        ///     <para xml:lang="zh-CN">获取数量增加时是否使用游戏的辉星计数器式平滑动画。</para>
        /// </summary>
        public bool AnimateAmountGain { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the smoothing time for an animated amount increase.</para>
        ///     <para xml:lang="zh-CN">获取数量增加动画的平滑时间。</para>
        /// </summary>
        public float AmountGainSmoothTime { get; init; } = 0.1f;

        /// <summary>
        ///     <para xml:lang="en">Gets the horizontal separation used by <see cref="NSecondaryResourceCounterRow" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="NSecondaryResourceCounterRow" /> 使用的水平间距。</para>
        /// </summary>
        public int RowSeparation { get; init; } = 8;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional icon style. When <see langword="null" />, the default icon style is used with
        ///         <see cref="IconSize" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的图标样式；为 <see langword="null" /> 时，将默认图标样式与
        ///         <see cref="IconSize" /> 配合使用。
        ///     </para>
        /// </summary>
        public SecondaryResourceIconStyle? IconStyle { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional gain feedback; <see langword="null" /> disables gain effects.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的增加反馈；为 <see langword="null" /> 时禁用增加效果。</para>
        /// </summary>
        public SecondaryResourceCounterGainFeedback? GainFeedback { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets an optional formatter for the current and maximum amount.</para>
        ///     <para xml:lang="zh-CN">获取用于格式化当前数量及最大数量的可选函数。</para>
        /// </summary>
        public Func<int, int?, string>? FormatAmount { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared default counter style.</para>
        ///     <para xml:lang="zh-CN">获取共享的默认计数器样式。</para>
        /// </summary>
        public static SecondaryResourceCounterStyle Default { get; } = new();

        internal string Format(int amount, int? maxAmount)
        {
            return FormatAmount?.Invoke(amount, maxAmount) ??
                   (maxAmount.HasValue ? $"{amount}/{maxAmount.Value}" : amount.ToString());
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Displays one secondary resource with an icon, amount label, and hover tip.</para>
    ///     <para xml:lang="zh-CN">使用图标、数量标签及悬浮提示显示一种次级资源。</para>
    /// </summary>
    public partial class NSecondaryResourceCounter : Control
    {
        private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";

        private readonly Dictionary<SecondaryResourceCounterEnergyCounterLikeParticlesEffect, EnergyCounterLikeVfxNodes>
            _energyCounterLikeVfxNodes = new();

        private int _amount;
        private float _amountDisplayVelocity;
        private MegaLabel _amountLabel = null!;
        private bool _autoRefresh;

        private Player? _boundPlayer;
        private SecondaryResourceState? _boundState;
        private SecondaryResourceDefinition? _definition;
        private int _displayedAmount;
        private bool _hasBeenMaterial;
        private bool _hasDisplayedAmount;
        private bool _hasLastAmountColor;
        private NSecondaryResourceIcon _icon = null!;
        private Tween? _iconBrightnessTween;
        private Color _lastAmountColor;
        private string? _lastAmountText;
        private int? _maxAmount;
        private float _smoothDisplayedAmount;
        private SecondaryResourceCounterStyle _style = SecondaryResourceCounterStyle.Default;
        private bool _suppressNextGainFeedback = true;

        internal SecondaryResourceDefinition Definition =>
            _definition ?? throw new InvalidOperationException("The secondary-resource counter is not configured.");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the counter refreshes when the bound player's secondary-resource state changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置已绑定玩家的次级资源状态变化时是否自动刷新计数器。</para>
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
        ///     <para xml:lang="en">Creates and configures a counter for <paramref name="definition" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="definition" /> 创建并配置计数器。</para>
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
        ///     <para xml:lang="en">
        ///         Configures the resource definition and style. Reusing the current definition preserves the
        ///         counter's displayed state.
        ///     </para>
        ///     <para xml:lang="zh-CN">配置资源定义及样式；继续使用当前定义时会保留计数器的显示状态。</para>
        /// </summary>
        public void Configure(SecondaryResourceDefinition definition, SecondaryResourceCounterStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(definition);
            var definitionChanged = !ReferenceEquals(_definition, definition);
            _definition = definition;
            _style = style ?? SecondaryResourceCounterStyle.Default;
            var resetIconBrightness = _iconBrightnessTween != null;
            _iconBrightnessTween?.Kill();
            _iconBrightnessTween = null;
            ClearEnergyCounterLikeVfxNodes();
            if (definitionChanged)
            {
                _amount = 0;
                _displayedAmount = 0;
                _smoothDisplayedAmount = 0f;
                _amountDisplayVelocity = 0f;
                _hasDisplayedAmount = false;
                _hasBeenMaterial = false;
                _lastAmountText = null;
                _hasLastAmountColor = false;
                _maxAmount = null;
                _suppressNextGainFeedback = true;
            }

            CustomMinimumSize = _style.CounterSize;
            Size = _style.CounterSize;

            if (IsNodeReady())
            {
                if (resetIconBrightness)
                    _icon.SetShaderBrightness(1f);
                ApplyStyle();
                ApplyDefinition();
                Refresh(_boundPlayer);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Binds the counter to a player for automatic or manual refresh.</para>
        ///     <para xml:lang="zh-CN">将计数器绑定到一名玩家，以便自动或手动刷新。</para>
        /// </summary>
        public void Bind(Player? player, bool autoRefresh = true)
        {
            if (!ReferenceEquals(_boundPlayer, player))
            {
                _hasBeenMaterial = false;
                _amount = 0;
                _displayedAmount = 0;
                _smoothDisplayedAmount = 0f;
                _amountDisplayVelocity = 0f;
                _hasDisplayedAmount = false;
                _lastAmountText = null;
                _hasLastAmountColor = false;
                _maxAmount = null;
                _suppressNextGainFeedback = true;
                Visible = false;
            }

            _boundPlayer = player;
            AutoRefresh = autoRefresh;
            UpdateStateSubscription();
            Refresh(_boundPlayer);
        }

        /// <summary>
        ///     <para xml:lang="en">Refreshes the displayed amount and visibility from <paramref name="player" />.</para>
        ///     <para xml:lang="zh-CN">根据 <paramref name="player" /> 刷新显示数量及可见性。</para>
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
        ///     <para xml:lang="en">Sets the displayed current and maximum amounts directly.</para>
        ///     <para xml:lang="zh-CN">直接设置显示的当前数量及最大数量。</para>
        /// </summary>
        public void SetAmount(int amount, int? maxAmount = null)
        {
            var oldAmount = _amount;
            _amount = amount;
            _maxAmount = maxAmount;
            if (!IsNodeReady())
                return;

            if (!_hasDisplayedAmount ||
                !_style.AnimateAmountGain ||
                amount <= oldAmount)
            {
                _smoothDisplayedAmount = amount;
                _amountDisplayVelocity = 0f;
                SetDisplayedAmount(amount);
            }
            else
            {
                UpdateAmountLabel(_displayedAmount);
            }

            _icon?.SetAmount(_amount, _maxAmount);

            if (!_suppressNextGainFeedback && amount > oldAmount)
                PlayGainFeedback();

            _suppressNextGainFeedback = false;
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            UpdateStateSubscription();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the child controls and applies the configured definition.</para>
        ///     <para xml:lang="zh-CN">初始化子控件并应用已配置的资源定义。</para>
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
            if (!_style.AnimateAmountGain ||
                !_hasDisplayedAmount ||
                _displayedAmount == _amount)
                return;

            _smoothDisplayedAmount = MathHelper.SmoothDamp(
                _smoothDisplayedAmount,
                _amount,
                ref _amountDisplayVelocity,
                Math.Max(0.001f, _style.AmountGainSmoothTime),
                (float)delta);

            if (Math.Abs(_smoothDisplayedAmount - _amount) < 0.01f)
            {
                _smoothDisplayedAmount = _amount;
                _amountDisplayVelocity = 0f;
            }

            SetDisplayedAmount(Mathf.RoundToInt(_smoothDisplayedAmount));
        }

        private void UpdateStateSubscription()
        {
            var state = _autoRefresh && ModSecondaryResourceRegistry.HasAny &&
                        _boundPlayer is { PlayerCombatState: not null } player
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
            if (_boundPlayer == null || _definition == null ||
                !ReferenceEquals(change.Player, _boundPlayer) ||
                !string.Equals(change.Definition.Id, _definition.Id, StringComparison.OrdinalIgnoreCase))
                return;
            Refresh(_boundPlayer);
        }

        private Vector2 GetIconPosition()
        {
            return (_style.CounterSize - _style.IconSize) * 0.5f;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Releases state subscriptions, gain effects, and the active hover tip when leaving the scene tree.
        ///     </para>
        ///     <para xml:lang="zh-CN">离开场景树时释放状态订阅、增加效果及当前悬浮提示。</para>
        /// </summary>
        public override void _ExitTree()
        {
            SetBoundState(null);
            if (_iconBrightnessTween != null && _icon != null)
                _icon.SetShaderBrightness(1f);
            _iconBrightnessTween?.Kill();
            _iconBrightnessTween = null;
            ClearEnergyCounterLikeVfxNodes();
            if (_icon != null)
                NHoverTipSet.Remove(_icon);
        }

        private void SetDisplayedAmount(int amount)
        {
            _displayedAmount = amount;
            _hasDisplayedAmount = true;
            UpdateAmountLabel(amount);
        }

        private void UpdateAmountLabel(int amount)
        {
            var text = _style.Format(amount, _maxAmount);
            if (!string.Equals(_lastAmountText, text, StringComparison.Ordinal))
            {
                _amountLabel.SetTextAutoSize(text);
                _lastAmountText = text;
            }

            var color = amount <= 0 ? _style.ZeroColor : _style.PositiveColor;
            if (_hasLastAmountColor && _lastAmountColor == color)
                return;
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, color);
            _lastAmountColor = color;
            _hasLastAmountColor = true;
        }

        private void PlayGainFeedback()
        {
            if (_style.GainFeedback is not { Effects.Count: > 0 } feedback || _icon == null)
                return;

            foreach (var effect in feedback.Effects)
                switch (effect)
                {
                    case SecondaryResourceCounterIconBrightnessFlashEffect flash:
                        PlayIconBrightnessFlash(flash);
                        break;
                    case SecondaryResourceCounterStarCounterLikeBurstEffect burst:
                        PlayStarCounterLikeBurst(burst);
                        break;
                    case SecondaryResourceCounterEnergyCounterLikeParticlesEffect particles:
                        RestartEnergyCounterLikeParticles(particles);
                        break;
                    case SecondaryResourceCounterSceneBurstEffect scene:
                        PlaySceneBurst(scene);
                        break;
                }
        }

        private void PlayIconBrightnessFlash(SecondaryResourceCounterIconBrightnessFlashEffect effect)
        {
            _icon.EnsureHsvMaterial();
            _iconBrightnessTween?.Kill();
            _iconBrightnessTween = CreateTween();
            _iconBrightnessTween.TweenMethod(
                Callable.From<float>(_icon.SetShaderBrightness),
                effect.BrightnessFrom,
                effect.BrightnessTo,
                Math.Max(0.0, effect.DurationSeconds));
        }

        private void PlayStarCounterLikeBurst(SecondaryResourceCounterStarCounterLikeBurstEffect effect)
        {
            if (_icon.Texture == null)
                return;

            var rect = _icon.GetRenderedTextureRect();
            var vfx = new NSecondaryResourceStarCounterLikeBurstVfx(
                _icon.Texture,
                rect.Size,
                effect.Color,
                effect.DurationSeconds)
            {
                Position = _icon.Position + rect.Position + rect.Size * 0.5f,
            };
            RitsuGodotTreeCompat.AddChildSafely(this, vfx);
            RitsuGodotTreeCompat.MoveChildSafely(this, vfx, 0);
        }

        private void PlaySceneBurst(SecondaryResourceCounterSceneBurstEffect effect)
        {
            var rect = _icon.GetRenderedTextureRect();
            var scene = PreloadManager.Cache.GetAsset<PackedScene>(effect.ScenePath);
            var node = scene.Instantiate<Node2D>();
            node.Position = _icon.Position + rect.Position + rect.Size * 0.5f + effect.Offset;
            if (effect.Scale is { } scale)
                node.Scale = scale;

            RitsuGodotTreeCompat.AddChildSafely(this, node);
            if (effect.BehindCounter)
                RitsuGodotTreeCompat.MoveChildSafely(this, node, 0);
        }

        private void RestartEnergyCounterLikeParticles(SecondaryResourceCounterEnergyCounterLikeParticlesEffect effect)
        {
            var nodes = GetOrCreateEnergyCounterLikeVfxNodes(effect);
            nodes.Back.Restart();
            nodes.Front.Restart();
        }

        private EnergyCounterLikeVfxNodes GetOrCreateEnergyCounterLikeVfxNodes(
            SecondaryResourceCounterEnergyCounterLikeParticlesEffect effect)
        {
            if (_energyCounterLikeVfxNodes.TryGetValue(effect, out var existing) &&
                IsInstanceValid(existing.Back) &&
                IsInstanceValid(existing.Front))
                return existing;

            var rect = _icon.GetRenderedTextureRect();
            var center = _icon.Position + rect.Position + rect.Size * 0.5f + effect.Offset;
            var scale = effect.Scale ?? Vector2.One * (Math.Min(rect.Size.X, rect.Size.Y) / 128f);

            var back = PreloadManager.Cache.GetAsset<PackedScene>(effect.BackScenePath)
                .Instantiate<NParticlesContainer>();
            back.Position = center;
            back.Scale = scale;
            RitsuGodotTreeCompat.AddChildSafely(this, back);
            RitsuGodotTreeCompat.MoveChildSafely(this, back, 0);

            var front = PreloadManager.Cache.GetAsset<PackedScene>(effect.FrontScenePath)
                .Instantiate<NParticlesContainer>();
            front.Position = center;
            front.Scale = scale;
            RitsuGodotTreeCompat.AddChildSafely(this, front);
            RitsuGodotTreeCompat.MoveChildSafely(this, front, _amountLabel.GetIndex());

            var created = new EnergyCounterLikeVfxNodes(back, front);
            _energyCounterLikeVfxNodes[effect] = created;
            return created;
        }

        private void ClearEnergyCounterLikeVfxNodes()
        {
            foreach (var nodes in _energyCounterLikeVfxNodes.Values)
            {
                if (IsInstanceValid(nodes.Back))
                    nodes.Back.QueueFree();
                if (IsInstanceValid(nodes.Front))
                    nodes.Front.QueueFree();
            }

            _energyCounterLikeVfxNodes.Clear();
        }

        private void ApplyDefinition()
        {
            if (_definition == null || _icon == null)
                return;

            _icon.Configure(_definition, ResolveIconStyle());
            _icon.SetAmount(_amount, _maxAmount);
        }

        private void ApplyStyle()
        {
            CustomMinimumSize = _style.CounterSize;
            Size = _style.CounterSize;
            if (_icon != null)
            {
                _icon.Position = GetIconPosition();
                if (_definition != null)
                    _icon.Configure(_definition, ResolveIconStyle());
            }

            if (_amountLabel == null)
                return;

            _amountLabel.Position = GetIconPosition() + _style.AmountLabelOffset;
            _amountLabel.CustomMinimumSize = _style.IconSize;
            _amountLabel.Size = _style.IconSize;
            _amountLabel.MinFontSize = Math.Max(8, _style.FontSize - 10);
            _amountLabel.MaxFontSize = _style.FontSize;
            ApplyAmountLabelTheme();
            UpdateAmountLabel(_displayedAmount);
        }

        private void ApplyAmountLabelTheme()
        {
            var font = PreloadManager.Cache.GetAsset<Font>(DefaultLabelFontPath);
            _amountLabel.AddThemeFontOverride(ThemeConstants.Label.Font, font);
            _amountLabel.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, _style.FontSize);
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, _style.PositiveColor);
            _amountLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, _style.OutlineColor);
            _amountLabel.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, _style.OutlineSize);
            _lastAmountColor = _style.PositiveColor;
            _hasLastAmountColor = true;
        }

        private SecondaryResourceIconStyle ResolveIconStyle()
        {
            return _style.IconStyle ?? SecondaryResourceIconStyle.Default with
            {
                Size = _style.IconSize,
            };
        }

        private readonly record struct EnergyCounterLikeVfxNodes(
            NParticlesContainer Back,
            NParticlesContainer Front);
    }

    /// <summary>
    ///     <para xml:lang="en">Displays a secondary-resource icon with optional built-in hover-tip behavior.</para>
    ///     <para xml:lang="zh-CN">显示次级资源图标，并可选启用内置悬浮提示行为。</para>
    /// </summary>
    public partial class NSecondaryResourceIcon : Control
    {
        private int _amount = 1;
        private SecondaryResourceDefinition? _definition;
        private SecondaryResourceHoverTipBinder? _hoverTipBinder;
        private ShaderMaterial? _hsvMaterial;
        private int? _maxAmount;
        private SecondaryResourceIconStyle _style = SecondaryResourceIconStyle.Default;
        private TextureRect _texture = null!;

        /// <summary>
        ///     <para xml:lang="en">Gets the texture currently loaded for the icon.</para>
        ///     <para xml:lang="zh-CN">获取图标当前加载的贴图。</para>
        /// </summary>
        public Texture2D? Texture => _texture?.Texture;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures a secondary-resource icon.</para>
        ///     <para xml:lang="zh-CN">创建并配置次级资源图标。</para>
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
        ///     <para xml:lang="en">Configures the resource definition, visual style, and hover-tip binding.</para>
        ///     <para xml:lang="zh-CN">配置资源定义、视觉样式及悬浮提示绑定。</para>
        /// </summary>
        public void Configure(SecondaryResourceDefinition definition, SecondaryResourceIconStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _definition = definition;
            _style = style ?? SecondaryResourceIconStyle.Default;
            CustomMinimumSize = _style.Size;
            Size = _style.Size;

            if (!IsNodeReady())
                return;

            ApplyStyleAndDefinition();
            RefreshHoverTipBinding();
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the current and maximum amounts shown by this icon's hover tip.</para>
        ///     <para xml:lang="zh-CN">更新该图标悬浮提示显示的当前数量及最大数量。</para>
        /// </summary>
        public void SetAmount(int amount, int? maxAmount = null)
        {
            _amount = amount;
            _maxAmount = maxAmount;
            _hoverTipBinder?.Refresh();
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the texture's rendered rectangle relative to this icon node.</para>
        ///     <para xml:lang="zh-CN">返回贴图相对于该图标节点的实际渲染矩形。</para>
        /// </summary>
        public Rect2 GetRenderedTextureRect()
        {
            if (_texture?.Texture == null)
                return new(_style.IconOffset, _style.Size);

            var textureSize = _texture.Texture.GetSize();
            var boxSize = _style.Size;
            if (textureSize.X <= 0f || textureSize.Y <= 0f || boxSize.X <= 0f || boxSize.Y <= 0f)
                return new(_style.IconOffset, boxSize);

            var renderedSize = _style.StretchMode switch
            {
                TextureRect.StretchModeEnum.Keep => textureSize,
                TextureRect.StretchModeEnum.KeepCentered => textureSize,
                TextureRect.StretchModeEnum.KeepAspect => FitSize(textureSize, boxSize),
                TextureRect.StretchModeEnum.KeepAspectCentered => FitSize(textureSize, boxSize),
                TextureRect.StretchModeEnum.KeepAspectCovered => CoverSize(textureSize, boxSize),
                _ => boxSize,
            };

            var offset = _style.StretchMode switch
            {
                TextureRect.StretchModeEnum.KeepCentered => (boxSize - renderedSize) * 0.5f,
                TextureRect.StretchModeEnum.KeepAspectCentered => (boxSize - renderedSize) * 0.5f,
                TextureRect.StretchModeEnum.KeepAspectCovered => (boxSize - renderedSize) * 0.5f,
                _ => Vector2.Zero,
            };

            return new(_style.IconOffset + offset, renderedSize);
        }

        /// <summary>
        ///     <para xml:lang="en">Ensures that the icon texture uses the game's unmodulated HSV material.</para>
        ///     <para xml:lang="zh-CN">确保图标贴图使用游戏的未调制 HSV 材质。</para>
        /// </summary>
        public void EnsureHsvMaterial()
        {
            if (!IsNodeReady() || _texture == null)
                return;

            _hsvMaterial ??= MaterialUtils.CreateUnmodulatedHsvShaderMaterial();
            _texture.Material = _hsvMaterial;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the HSV brightness parameter used by gain feedback.</para>
        ///     <para xml:lang="zh-CN">设置增加反馈使用的 HSV 亮度参数。</para>
        /// </summary>
        public void SetShaderBrightness(float value)
        {
            if (!IsNodeReady())
                return;

            EnsureHsvMaterial();
            _hsvMaterial?.SetShaderParameter("v", value);
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
            if (_hsvMaterial != null)
                _texture.Material = _hsvMaterial;

            if (_definition == null)
            {
                _texture.Texture = null;
                return;
            }

            var path = _definition.LargeIconPath ?? _definition.SmallIconPath;
            _texture.Texture = string.IsNullOrWhiteSpace(path) ? null : ResourceLoader.Load<Texture2D>(path.Trim());
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
            return _definition == null ? null : new SecondaryResourceHoverTipRequest(_definition, _amount, _maxAmount);
        }

        private static Vector2 FitSize(Vector2 textureSize, Vector2 boxSize)
        {
            var scale = Math.Min(boxSize.X / textureSize.X, boxSize.Y / textureSize.Y);
            return textureSize * scale;
        }

        private static Vector2 CoverSize(Vector2 textureSize, Vector2 boxSize)
        {
            var scale = Math.Max(boxSize.X / textureSize.X, boxSize.Y / textureSize.Y);
            return textureSize * scale;
        }
    }

    internal sealed partial class NSecondaryResourceStarCounterLikeBurstVfx : Node2D
    {
        private const float IconStartSizeFactor = 0.7662625f;
        private const float IconEndSizeFactor = 1.25f;
        private const float GlowStartSizeFactor = 1.735392f;
        private const float GlowEndSizeFactor = 2.595368f;

        private readonly Color _color;
        private readonly double _durationSeconds;
        private readonly Vector2 _renderedIconSize;
        private readonly Texture2D? _texture;

        public NSecondaryResourceStarCounterLikeBurstVfx(
            Texture2D texture,
            Vector2 renderedIconSize,
            Color color,
            double durationSeconds)
        {
            _texture = texture;
            _renderedIconSize = renderedIconSize;
            _color = color;
            _durationSeconds = Math.Max(0.0, durationSeconds);
        }

        public NSecondaryResourceStarCounterLikeBurstVfx()
        {
        }

        public override void _Ready()
        {
            var glow = CreateParticle(CreateGlowMaterial());
            var icon = CreateParticle(CreateIconMaterial());
            AddChild(glow);
            AddChild(icon);
            glow.Emitting = true;
            icon.Emitting = true;

            var timer = GetTree().CreateTimer(_durationSeconds + 0.05);
            timer.Timeout += QueueFree;
        }

        private GpuParticles2D CreateParticle(ParticleProcessMaterial material)
        {
            return new()
            {
                Emitting = false,
                Amount = 1,
                Texture = _texture,
                OneShot = true,
                ProcessMaterial = material,
            };
        }

        private ParticleProcessMaterial CreateIconMaterial()
        {
            return new()
            {
                ParticleFlagDisableZ = true,
                AngularVelocityMin = 0.999984f,
                AngularVelocityMax = 0.999984f,
                AngularVelocityCurve = CreateCurveTexture(
                    new(0f, 326.461f),
                    new(1f, 303.241f),
                    minValue: 0f,
                    maxValue: 360f),
                Gravity = Vector3.Zero,
                ScaleCurve = CreateCurveTexture(
                    new(0f, ResolveParticleScale(IconStartSizeFactor)),
                    new(0.75f, ResolveParticleScale(IconEndSizeFactor)),
                    width: 128,
                    minValue: 0f,
                    maxValue: 4f),
                Color = _color,
                AlphaCurve = CreateCurveTexture(
                    new(0.151685f, 1f),
                    new(0.99999f, 0f),
                    new Vector2(1f, 0f)),
            };
        }

        private ParticleProcessMaterial CreateGlowMaterial()
        {
            return new()
            {
                ParticleFlagDisableZ = true,
                AngularVelocityMin = -1.60933e-05f,
                AngularVelocityMax = -1.60933e-05f,
                Gravity = Vector3.Zero,
                ScaleCurve = CreateCurveTexture(
                    new(0f, ResolveParticleScale(GlowStartSizeFactor)),
                    new(1f, ResolveParticleScale(GlowEndSizeFactor)),
                    width: 128),
                Color = _color with { A = 0.592157f },
                AlphaCurve = CreateCurveTexture(
                    new(0.202247f, 1f),
                    new(0.99999f, 0f),
                    new Vector2(1f, 0f)),
            };
        }

        private CurveTexture CreateCurveTexture(
            Vector2 firstPoint,
            Vector2 secondPoint,
            Vector2? thirdPoint = null,
            int width = 64,
            float minValue = 0f,
            float maxValue = 1f)
        {
            var curve = new Curve
            {
                MinValue = minValue,
                MaxValue = maxValue,
            };
            curve.AddPoint(firstPoint);
            curve.AddPoint(secondPoint);
            if (thirdPoint is { } point)
                curve.AddPoint(point);

            return new()
            {
                Width = width,
                Curve = curve,
            };
        }

        private float ResolveParticleScale(float displaySizeFactor)
        {
            if (_texture == null) return displaySizeFactor;
            var textureSize = _texture.GetSize();
            if (textureSize.X <= 0f || textureSize.Y <= 0f ||
                _renderedIconSize.X <= 0f || _renderedIconSize.Y <= 0f)
                return displaySizeFactor;

            var renderedScale = Math.Min(
                _renderedIconSize.X / textureSize.X,
                _renderedIconSize.Y / textureSize.Y);
            return renderedScale * displaySizeFactor;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Gives a secondary-resource <see cref="Control" /> the same pointer hover behavior as the game's
    ///         resource counters.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使次级资源 <see cref="Control" /> 具备与游戏资源计数器一致的指针悬浮行为。
    ///     </para>
    /// </summary>
    public partial class SecondaryResourceHoverTipBinder : Node
    {
        private readonly Callable _hideCallable;
        private readonly Callable _showCallable;
        private Control _owner = null!;
        private Func<SecondaryResourceHoverTipRequest?> _requestFactory = null!;
        private bool _shown;
        private SecondaryResourceHoverTipStyle _style = SecondaryResourceHoverTipStyle.Default;

        /// <summary>
        ///     <para xml:lang="en">Creates a hover-tip binder.</para>
        ///     <para xml:lang="zh-CN">创建悬浮提示绑定器。</para>
        /// </summary>
        public SecondaryResourceHoverTipBinder()
        {
            _showCallable = Callable.From(Show);
            _hideCallable = Callable.From(Hide);
        }

        /// <summary>
        ///     <para xml:lang="en">Binds a request-driven secondary-resource hover tip to a display control.</para>
        ///     <para xml:lang="zh-CN">将由请求驱动的次级资源悬浮提示绑定到显示控件。</para>
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
        ///     <para xml:lang="en">
        ///         Binds a fixed resource definition with providers for its current and maximum amounts.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用固定资源定义以及当前数量和最大数量提供函数绑定悬浮提示。</para>
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
                () => new SecondaryResourceHoverTipRequest(definition, amount(), maxAmount?.Invoke()),
                style);
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the request factory and style, refreshing a tip already shown.</para>
        ///     <para xml:lang="zh-CN">更新请求工厂及样式，并刷新已显示的提示。</para>
        /// </summary>
        public void Configure(
            Func<SecondaryResourceHoverTipRequest?> requestFactory,
            SecondaryResourceHoverTipStyle? style = null)
        {
            ArgumentNullException.ThrowIfNull(requestFactory);
            var refreshShownTip = _shown;
            _requestFactory = requestFactory;
            _style = style ?? SecondaryResourceHoverTipStyle.Default;
            if (refreshShownTip)
                Show();
        }

        /// <inheritdoc />
        public override void _EnterTree()
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
        ///     <para xml:lang="en">Shows the current hover tip when the request factory returns a request.</para>
        ///     <para xml:lang="zh-CN">请求工厂返回有效请求时显示当前悬浮提示。</para>
        /// </summary>
        public void Show()
        {
            if (_requestFactory == null || _owner == null || !IsInstanceValid(_owner))
                return;

            var request = _requestFactory();
            if (request == null)
            {
                Hide();
                return;
            }

            _shown = SecondaryResourceHoverTipFactory.Show(
                _owner,
                request.Value.Definition,
                request.Value.Amount,
                request.Value.MaxAmount,
                _style) != null;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the active hover tip owned by this binder's control.</para>
        ///     <para xml:lang="zh-CN">移除此绑定器所属控件当前拥有的悬浮提示。</para>
        /// </summary>
        public void Hide()
        {
            _shown = false;
            if (_owner != null && IsInstanceValid(_owner))
                NHoverTipSet.Remove(_owner);
        }

        /// <summary>
        ///     <para xml:lang="en">Recreates the hover tip from current request data when it is already visible.</para>
        ///     <para xml:lang="zh-CN">悬浮提示已显示时，根据当前请求数据重新创建提示。</para>
        /// </summary>
        public void Refresh()
        {
            if (_shown)
                Show();
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
    ///     <para xml:lang="en">Displays multiple secondary-resource counters in a built-in horizontal row.</para>
    ///     <para xml:lang="zh-CN">在内置水平行中显示多个次级资源计数器。</para>
    /// </summary>
    public partial class NSecondaryResourceCounterRow : Control
    {
        private readonly Dictionary<string, NSecondaryResourceCounter> _counters =
            new(StringComparer.OrdinalIgnoreCase);

        private bool _autoRefresh;

        private SecondaryResourceDefinition[]? _boundDefinitions;
        private Player? _boundPlayer;
        private SecondaryResourceState? _boundState;
        private SecondaryResourceDefinition[]? _pendingDefinitions;
        private Player? _pendingPlayer;

        private HBoxContainer _row = null!;
        private SecondaryResourceCounterStyle _style = SecondaryResourceCounterStyle.Default;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets whether the row refreshes when the bound player's secondary-resource state changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或设置已绑定玩家的次级资源状态变化时是否自动刷新该行。</para>
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
        ///     <para xml:lang="en">Applies the row style to its layout and existing counters.</para>
        ///     <para xml:lang="zh-CN">将行样式应用于布局及已有计数器。</para>
        /// </summary>
        public void Configure(SecondaryResourceCounterStyle? style = null)
        {
            _style = style ?? SecondaryResourceCounterStyle.Default;
            _row?.AddThemeConstantOverride("separation", _style.RowSeparation);
            foreach (var counter in _counters.Values)
                counter.Configure(counter.Definition, _style);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds the row to a player and resource definitions for automatic or manual refresh.
        ///     </para>
        ///     <para xml:lang="zh-CN">将该行绑定到一名玩家及一组资源定义，以便自动或手动刷新。</para>
        /// </summary>
        public void Bind(
            Player? player,
            IReadOnlyList<SecondaryResourceDefinition> definitions,
            bool autoRefresh = true)
        {
            ArgumentNullException.ThrowIfNull(definitions);

            _boundPlayer = player;
            _boundDefinitions = [.. definitions];
            AutoRefresh = autoRefresh;
            UpdateStateSubscription();
            Refresh(_boundPlayer, _boundDefinitions);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes, deduplicates, and orders the visible counters for the supplied definitions.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据提供的定义刷新可见计数器，并对其去重及排序。</para>
        /// </summary>
        public void Refresh(Player? player, IReadOnlyList<SecondaryResourceDefinition> visibleDefinitions)
        {
            ArgumentNullException.ThrowIfNull(visibleDefinitions);

            if (!IsNodeReady())
            {
                _pendingPlayer = player;
                _pendingDefinitions = [.. visibleDefinitions];
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
            var visibleIndex = 0;
            var seenDefinitions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in visibleDefinitions)
            {
                if (!seenDefinitions.Add(definition.Id))
                    continue;

                var counter = GetOrCreateCounter(definition);
                _row.MoveChild(counter, visibleIndex++);
                counter.Bind(player, false);
                anyVisible |= counter.Visible;
            }

            Visible = anyVisible;
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            UpdateStateSubscription();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the horizontal container used by child counters.</para>
        ///     <para xml:lang="zh-CN">初始化子计数器使用的水平容器。</para>
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

            if (_pendingDefinitions == null)
                return;

            var player = _pendingPlayer;
            var definitions = _pendingDefinitions;
            _pendingPlayer = null;
            _pendingDefinitions = null;
            Refresh(player, definitions);
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            SetBoundState(null);
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

        private void UpdateStateSubscription()
        {
            var state = _autoRefresh && ModSecondaryResourceRegistry.HasAny &&
                        _boundPlayer is { PlayerCombatState: not null } player
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
            if (_boundPlayer == null || _boundDefinitions == null ||
                !ReferenceEquals(change.Player, _boundPlayer))
                return;
            Refresh(_boundPlayer, _boundDefinitions);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Creates and displays hover tips for secondary resources.</para>
    ///     <para xml:lang="zh-CN">创建并显示次级资源的悬浮提示。</para>
    /// </summary>
    public static class SecondaryResourceHoverTipFactory
    {
        private static readonly PropertyInfo TitleProperty = typeof(HoverTip).GetProperty(nameof(HoverTip.Title))!;

        private static readonly PropertyInfo DescriptionProperty =
            typeof(HoverTip).GetProperty(nameof(HoverTip.Description))!;

        private static readonly PropertyInfo IconProperty = typeof(HoverTip).GetProperty(nameof(HoverTip.Icon))!;

        /// <summary>
        ///     <para xml:lang="en">Creates a hover tip for a secondary resource at the supplied amounts.</para>
        ///     <para xml:lang="zh-CN">根据提供的数量为次级资源创建悬浮提示。</para>
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
        ///     <para xml:lang="en">Creates, positions, and shows a secondary-resource hover tip for its owning control.</para>
        ///     <para xml:lang="zh-CN">为所属控件创建、定位并显示次级资源悬浮提示。</para>
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
            {
                NHoverTipSet.Remove(owner);
                return null;
            }

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
            return string.IsNullOrWhiteSpace(path) ? null : ResourceLoader.Load<Texture2D>(path.Trim());
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
