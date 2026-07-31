using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures the lifetime, presentation, and interaction behavior of a mod card pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">配置模组卡牌牌堆的生命周期、展示与交互行为。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Pile text is read from <c>static_hover_tips</c> with the registered pile ID followed by
    ///         <c>.title</c>, <c>.description</c>, or <c>.empty</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         牌堆文本从 <c>static_hover_tips</c> 中读取，键名为已注册牌堆 ID 后接
    ///         <c>.title</c>、<c>.description</c> 或 <c>.empty</c>。
    ///     </para>
    /// </remarks>
    public sealed record ModCardPileSpec
    {
        /// <summary>
        ///     <para xml:lang="en">The localization table used for mod card-pile text.</para>
        ///     <para xml:lang="zh-CN">用于模组卡牌牌堆文本的本地化表。</para>
        /// </summary>
        public const string HoverTipLocTable = "static_hover_tips";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a combat-only, headless card-pile specification.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始化一个仅在战斗中存在且不带界面的卡牌牌堆规范。</para>
        /// </summary>
        public ModCardPileSpec()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the pile lifetime scope. The default is <see cref="ModCardPileScope.CombatOnly" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取牌堆的生命周期作用域。默认为 <see cref="ModCardPileScope.CombatOnly" />。
        ///     </para>
        /// </summary>
        public ModCardPileScope Scope { get; init; } = ModCardPileScope.CombatOnly;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the pile's UI style. The default is <see cref="ModCardPileUiStyle.Headless" />, which
        ///         does not create a pile control.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取牌堆的界面样式。默认为 <see cref="ModCardPileUiStyle.Headless" />，不会创建牌堆控件。
        ///     </para>
        /// </summary>
        public ModCardPileUiStyle Style { get; init; } = ModCardPileUiStyle.Headless;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the placement anchor used with <see cref="Style" />. The default anchor lets the layout
        ///         place piles of the same style by their registered ID order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取与 <see cref="Style" /> 配合使用的放置锚点。默认锚点会让布局按已注册 ID 的顺序放置
        ///         相同样式的牌堆。
        ///     </para>
        /// </summary>
        public ModCardPileAnchor Anchor { get; init; } = ModCardPileAnchor.Default;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional Godot resource path of the pile icon. A missing or invalid path uses the
        ///         placeholder texture.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取牌堆图标的可选 Godot 资源路径。路径缺失或无效时使用占位贴图。
        ///     </para>
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional input-action IDs that open the pile screen.</para>
        ///     <para xml:lang="zh-CN">获取用于打开牌堆界面的可选输入动作 ID。</para>
        /// </summary>
        public string[]? Hotkeys { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether cards in an <see cref="ModCardPileUiStyle.ExtraHand" /> pile are represented by
        ///         card nodes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆中的卡牌是否显示为卡牌节点。
        ///     </para>
        /// </summary>
        public bool CardShouldBeVisible { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the presentation and interaction settings for <see cref="ModCardPileUiStyle.ExtraHand" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileUiStyle.ExtraHand" /> 的展示与交互设置。
        ///     </para>
        /// </summary>
        public ModCardPileExtraHandSpec ExtraHand { get; init; } = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the screen-space offset added to the resolved hover-tip position.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取添加到悬停提示最终位置的屏幕空间偏移量。</para>
        /// </summary>
        public Vector2 HoverTipScreenOffset { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the hover-tip placement relative to the pile control. The default is
        ///         <see cref="ModCardPileHoverTipPlacement.Auto" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取悬停提示相对于牌堆控件的放置方式。默认为
        ///         <see cref="ModCardPileHoverTipPlacement.Auto" />。
        ///     </para>
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; init; } = ModCardPileHoverTipPlacement.Auto;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional predicate evaluated by the pile control to determine its visibility.
        ///         Returning <see langword="false" /> hides the control and removes its active hover tip.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取由牌堆控件求值、用于决定其可见性的可选谓词。返回 <see langword="false" /> 会隐藏控件，
        ///         并移除其当前悬停提示。
        ///     </para>
        /// </summary>
        public Func<ModCardPileVisibilityContext, bool>? VisibleWhen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional default pile-screen capabilities. <see langword="null" /> uses the
        ///         unextended vanilla pile screen.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取默认牌堆界面的可选扩展能力。<see langword="null" /> 表示使用未经扩展的原版牌堆界面。
        ///     </para>
        /// </summary>
        public ModCardPileViewSpec? View { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional callback invoked when a non-empty pile control is released.
        ///         <see langword="null" /> opens the vanilla pile screen.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取释放非空牌堆控件时调用的可选回调。<see langword="null" /> 表示打开原版牌堆界面。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Empty piles show the registered empty-pile message instead of invoking the callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">空牌堆会显示已注册的空牌堆提示，而不会调用该回调。</para>
        /// </remarks>
        public Action<ModCardPileOpenContext>? OnOpen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional resolver for card-flight target positions. Returning <see langword="null" />
        ///         uses the default position.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌飞行动画目标位置的可选解析器。返回 <see langword="null" /> 时使用默认位置。
        ///     </para>
        /// </summary>
        public Func<ModCardPileFlightTargetContext, Vector2?>? FlightTargetPositionResolver { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional resolver for shuffle-flight start positions. Returning
        ///         <see langword="null" /> uses the default position.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取洗牌飞行动画起始位置的可选解析器。返回 <see langword="null" /> 时使用默认位置。
        ///     </para>
        /// </summary>
        public Func<ModCardPileFlightStartContext, Vector2?>? FlightStartPositionResolver { get; init; }
    }
}
