using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a registered mod card pile and the presentation and interaction options captured when
    ///         it was registered.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述已注册的模组卡牌牌堆，以及注册时保存的展示与交互选项。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="Title" />, <see cref="Description" />, and <see cref="EmptyPileMessage" /> use the
    ///         registered pile ID with the <c>.title</c>, <c>.description</c>, and <c>.empty</c> suffixes in
    ///         <see cref="ModCardPileSpec.HoverTipLocTable" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="Title" />、<see cref="Description" /> 和 <see cref="EmptyPileMessage" /> 使用已注册
    ///         牌堆 ID，并分别附加 <c>.title</c>、<c>.description</c> 和 <c>.empty</c> 后缀，从
    ///         <see cref="ModCardPileSpec.HoverTipLocTable" /> 中读取本地化文本。
    ///     </para>
    /// </remarks>
    public sealed record ModCardPileDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a card-pile definition and its optional presentation capabilities.</para>
        ///     <para xml:lang="zh-CN">初始化卡牌牌堆定义及其可选展示能力。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The ID of the mod that owns the registry.</para>
        ///     <para xml:lang="zh-CN">拥有该注册表的模组 ID。</para>
        /// </param>
        /// <param name="id">
        ///     <para xml:lang="en">The normalized, globally qualified pile ID.</para>
        ///     <para xml:lang="zh-CN">规范化后的全局限定牌堆 ID。</para>
        /// </param>
        /// <param name="pileType">
        ///     <para xml:lang="en">The runtime <see cref="PileType" /> assigned to the pile.</para>
        ///     <para xml:lang="zh-CN">分配给该牌堆的运行时 <see cref="PileType" />。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The lifetime scope of the pile.</para>
        ///     <para xml:lang="zh-CN">牌堆的生命周期作用域。</para>
        /// </param>
        /// <param name="style">
        ///     <para xml:lang="en">The UI style used to present the pile.</para>
        ///     <para xml:lang="zh-CN">用于展示牌堆的界面样式。</para>
        /// </param>
        /// <param name="anchor">
        ///     <para xml:lang="en">The anchor used to place the pile control.</para>
        ///     <para xml:lang="zh-CN">用于放置牌堆控件的锚点。</para>
        /// </param>
        /// <param name="iconPath">
        ///     <para xml:lang="en">The optional Godot resource path of the pile icon.</para>
        ///     <para xml:lang="zh-CN">牌堆图标的可选 Godot 资源路径。</para>
        /// </param>
        /// <param name="hotkeys">
        ///     <para xml:lang="en">The optional input-action IDs forwarded to the default pile screen.</para>
        ///     <para xml:lang="zh-CN">转发给默认牌堆界面的可选输入动作 ID。</para>
        /// </param>
        /// <param name="cardShouldBeVisible">
        ///     <para xml:lang="en">
        ///         Whether cards in an extra-hand pile should be represented by visible card nodes.
        ///     </para>
        ///     <para xml:lang="zh-CN">额外手牌牌堆中的卡牌是否应显示为可见的卡牌节点。</para>
        /// </param>
        /// <param name="onOpen">
        ///     <para xml:lang="en">The optional callback invoked when the pile is opened.</para>
        ///     <para xml:lang="zh-CN">打开牌堆时调用的可选回调。</para>
        /// </param>
        /// <param name="hoverTipScreenOffset">
        ///     <para xml:lang="en">The screen-space offset added to the resolved hover-tip position.</para>
        ///     <para xml:lang="zh-CN">添加到悬停提示最终位置的屏幕空间偏移量。</para>
        /// </param>
        /// <param name="hoverTipPlacement">
        ///     <para xml:lang="en">The placement rule for the pile's hover tip.</para>
        ///     <para xml:lang="zh-CN">牌堆悬停提示的放置规则。</para>
        /// </param>
        /// <param name="visibleWhen">
        ///     <para xml:lang="en">
        ///         The optional predicate that controls pile-control visibility; <see langword="null" /> does
        ///         not add a per-pile visibility restriction.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         控制牌堆控件可见性的可选谓词；<see langword="null" /> 表示不额外限制该牌堆控件的可见性。
        ///     </para>
        /// </param>
        /// <param name="flightTargetPositionResolver">
        ///     <para xml:lang="en">The optional resolver for card-flight target positions.</para>
        ///     <para xml:lang="zh-CN">用于解析卡牌飞行动画目标位置的可选解析器。</para>
        /// </param>
        /// <param name="flightStartPositionResolver">
        ///     <para xml:lang="en">The optional resolver for shuffle-flight start positions.</para>
        ///     <para xml:lang="zh-CN">用于解析洗牌飞行动画起始位置的可选解析器。</para>
        /// </param>
        /// <param name="view">
        ///     <para xml:lang="en">Optional capabilities for the default pile screen.</para>
        ///     <para xml:lang="zh-CN">默认牌堆界面的可选扩展能力。</para>
        /// </param>
        /// <param name="extraHand">
        ///     <para xml:lang="en">Optional extra-hand presentation settings.</para>
        ///     <para xml:lang="zh-CN">可选的额外手牌展示设置。</para>
        /// </param>
        public ModCardPileDefinition(
            string modId,
            string id,
            PileType pileType,
            ModCardPileScope scope,
            ModCardPileUiStyle style,
            ModCardPileAnchor anchor,
            string? iconPath,
            string[]? hotkeys,
            bool cardShouldBeVisible,
            Action<ModCardPileOpenContext>? onOpen = null,
            Vector2 hoverTipScreenOffset = default,
            ModCardPileHoverTipPlacement hoverTipPlacement = ModCardPileHoverTipPlacement.Auto,
            Func<ModCardPileVisibilityContext, bool>? visibleWhen = null,
            Func<ModCardPileFlightTargetContext, Vector2?>? flightTargetPositionResolver = null,
            Func<ModCardPileFlightStartContext, Vector2?>? flightStartPositionResolver = null,
            ModCardPileViewSpec? view = null,
            ModCardPileExtraHandSpec? extraHand = null)
        {
            ModId = modId;
            Id = id;
            PileType = pileType;
            Scope = scope;
            Style = style;
            Anchor = anchor;
            IconPath = iconPath;
            Hotkeys = hotkeys;
            CardShouldBeVisible = cardShouldBeVisible;
            OnOpen = onOpen;
            View = view;
            HoverTipScreenOffset = hoverTipScreenOffset;
            HoverTipPlacement = hoverTipPlacement;
            VisibleWhen = visibleWhen;
            FlightTargetPositionResolver = flightTargetPositionResolver;
            FlightStartPositionResolver = flightStartPositionResolver;
            ExtraHand = extraHand ?? new();
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns the registry.</para>
        ///     <para xml:lang="zh-CN">获取拥有该注册表的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the normalized, globally qualified pile ID.</para>
        ///     <para xml:lang="zh-CN">获取规范化后的全局限定牌堆 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the runtime <see cref="PileType" /> assigned to the pile.</para>
        ///     <para xml:lang="zh-CN">获取分配给该牌堆的运行时 <see cref="PileType" />。</para>
        /// </summary>
        public PileType PileType { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the pile's registered lifetime scope.</para>
        ///     <para xml:lang="zh-CN">获取该牌堆注册时声明的生命周期作用域。</para>
        /// </summary>
        public ModCardPileScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the UI style used to present the pile.</para>
        ///     <para xml:lang="zh-CN">获取用于展示该牌堆的界面样式。</para>
        /// </summary>
        public ModCardPileUiStyle Style { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the anchor used to place the pile control.</para>
        ///     <para xml:lang="zh-CN">获取用于放置该牌堆控件的锚点。</para>
        /// </summary>
        public ModCardPileAnchor Anchor { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional Godot resource path of the pile icon.</para>
        ///     <para xml:lang="zh-CN">获取牌堆图标的可选 Godot 资源路径。</para>
        /// </summary>
        public string? IconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the localized hover-tip title at <c>{Id}.title</c> in
        ///         <see cref="ModCardPileSpec.HoverTipLocTable" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileSpec.HoverTipLocTable" /> 中键为 <c>{Id}.title</c> 的本地化
        ///         悬停提示标题。
        ///     </para>
        /// </summary>
        public LocString Title => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.title");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the localized hover-tip description at <c>{Id}.description</c> in
        ///         <see cref="ModCardPileSpec.HoverTipLocTable" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileSpec.HoverTipLocTable" /> 中键为 <c>{Id}.description</c> 的
        ///         本地化悬停提示描述。
        ///     </para>
        /// </summary>
        public LocString Description => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.description");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the localized thought-bubble message at <c>{Id}.empty</c> in
        ///         <see cref="ModCardPileSpec.HoverTipLocTable" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileSpec.HoverTipLocTable" /> 中键为 <c>{Id}.empty</c> 的本地化
        ///         思考气泡文本。
        ///     </para>
        /// </summary>
        public LocString EmptyPileMessage => new(ModCardPileSpec.HoverTipLocTable, $"{Id}.empty");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional input-action IDs forwarded to the default pile screen.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取转发给默认牌堆界面的可选输入动作 ID。</para>
        /// </summary>
        public string[]? Hotkeys { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether an <see cref="ModCardPileUiStyle.ExtraHand" /> pile should represent its cards
        ///         with visible card nodes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆是否应将其中的卡牌显示为可见的卡牌节点。
        ///     </para>
        /// </summary>
        public bool CardShouldBeVisible { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the extra-hand presentation and interaction settings.</para>
        ///     <para xml:lang="zh-CN">获取额外手牌的展示与交互设置。</para>
        /// </summary>
        public ModCardPileExtraHandSpec ExtraHand { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the callback invoked when the pile is opened. <see langword="null" /> selects the
        ///         default pile screen.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取打开牌堆时调用的回调。<see langword="null" /> 表示使用默认牌堆界面。
        ///     </para>
        /// </summary>
        public Action<ModCardPileOpenContext>? OnOpen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional capabilities applied to the default pile screen. <see langword="null" />
        ///         preserves the base-game screen behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取应用于默认牌堆界面的可选扩展能力。<see langword="null" /> 保留游戏原有界面行为。
        ///     </para>
        /// </summary>
        public ModCardPileViewSpec? View { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the screen-space offset added to the resolved hover-tip position.</para>
        ///     <para xml:lang="zh-CN">获取添加到悬停提示最终位置的屏幕空间偏移量。</para>
        /// </summary>
        public Vector2 HoverTipScreenOffset { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the placement rule for the pile's hover tip.</para>
        ///     <para xml:lang="zh-CN">获取牌堆悬停提示的放置规则。</para>
        /// </summary>
        public ModCardPileHoverTipPlacement HoverTipPlacement { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional predicate evaluated while the pile control is active. Returning
        ///         <see langword="false" /> hides the control; an exception is logged and also hides it for that
        ///         evaluation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取牌堆控件处于活动状态时求值的可选谓词。返回 <see langword="false" /> 会隐藏控件；
        ///         抛出异常时会记录异常，并在本次求值中隐藏控件。
        ///     </para>
        /// </summary>
        public Func<ModCardPileVisibilityContext, bool>? VisibleWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional resolver evaluated for each card-flight target request. Returning
        ///         <see langword="null" /> uses the position resolved from the current pile control or anchor.
        ///         Exceptions propagate to the caller.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取每次请求卡牌飞行动画目标位置时求值的可选解析器。返回 <see langword="null" /> 时使用根据
        ///         当前牌堆控件或锚点解析的位置；异常会传播给调用方。
        ///     </para>
        /// </summary>
        public Func<ModCardPileFlightTargetContext, Vector2?>? FlightTargetPositionResolver { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional resolver evaluated when a shuffle-flight visual starts from this pile.
        ///         Returning <see langword="null" /> uses the position resolved from the current pile control or
        ///         anchor. Exceptions propagate to the caller.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取洗牌飞行动画从该牌堆开始时求值的可选解析器。返回 <see langword="null" /> 时使用根据当前
        ///         牌堆控件或锚点解析的位置；异常会传播给调用方。
        ///     </para>
        /// </summary>
        public Func<ModCardPileFlightStartContext, Vector2?>? FlightStartPositionResolver { get; }
    }
}
