using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Configures the presentation and interaction behavior of an
    ///         <see cref="ModCardPileUiStyle.ExtraHand" /> pile.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         配置 <see cref="ModCardPileUiStyle.ExtraHand" /> 牌堆的展示与交互行为。
    ///     </para>
    /// </summary>
    public sealed record ModCardPileExtraHandSpec
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the built-in card arrangement. The default uses the base-game hand layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取内置卡牌排列方式。默认使用游戏原有的手牌布局。</para>
        /// </summary>
        public ModExtraHandLayoutDirection Direction { get; init; } = ModExtraHandLayoutDirection.VanillaHand;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the distance in pixels between adjacent card-holder origins in horizontal and vertical
        ///         layouts. The default is <c>110</c>; the base-game hand layout ignores this value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取水平和垂直布局中相邻卡牌容器原点之间的像素距离。默认值为 <c>110</c>；游戏原有的
        ///         手牌布局会忽略该值。
        ///     </para>
        /// </summary>
        public float Spacing { get; init; } = 110f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the normal card scale for horizontal and vertical layouts. Both axes default to
        ///         <c>0.65</c>; the base-game hand layout ignores this value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取水平和垂直布局中的卡牌常态缩放比例。两轴默认均为 <c>0.65</c>；游戏原有的手牌布局
        ///         会忽略该值。
        ///     </para>
        /// </summary>
        public Vector2 CardScale { get; init; } = Vector2.One * 0.65f;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the focused-card scale for horizontal and vertical layouts. The default is
        ///         <see cref="Vector2.One" />; the base-game hand layout ignores this value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取水平和垂直布局中卡牌获得焦点时的缩放比例。默认值为 <see cref="Vector2.One" />；
        ///         游戏原有的手牌布局会忽略该值。
        ///     </para>
        /// </summary>
        public Vector2 HoverScale { get; init; } = Vector2.One;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether mounted cards show the base-game playable highlight. The default is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已挂载卡牌是否显示游戏原有的可打出高亮。默认值为 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool ShowPlayableGlow { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether cards can be manually played through the base-game targeting, action queue,
        ///         resource payment, card hooks, and destination-pile flow. The default is
        ///         <see langword="true" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取卡牌是否可通过游戏原有的目标选择、行动队列、资源支付、卡牌钩子与目标牌堆流程手动打出。
        ///         默认值为 <see langword="true" />。
        ///     </para>
        /// </summary>
        public bool AllowCardPlay { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional per-card layout resolver. Returning <see langword="null" /> keeps the
        ///         built-in transform; exceptions propagate to the layout caller.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的逐卡布局解析器。返回 <see langword="null" /> 时保留内置变换；异常会传播给布局调用方。
        ///     </para>
        /// </summary>
        public Func<ModExtraHandCardContext, ModExtraHandCardTransform?>? LayoutResolver { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional callback invoked after an interactive card visual is mounted. Exceptions
        ///         propagate to the mounting caller.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取交互式卡牌视觉节点挂载后调用的可选回调。异常会传播给执行挂载的调用方。
        ///     </para>
        /// </summary>
        public Action<ModExtraHandCardContext>? OnCardVisualCreated { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional callback invoked after the matching card's hand-entry motion settles into
        ///         the current layout. Adds that skip individual visuals or use an aggregate shuffle visual do
        ///         not invoke it. Exceptions propagate from the arrival callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取对应卡牌的手牌入场动画稳定到当前布局后调用的可选回调。跳过逐卡动画或使用聚合洗牌
        ///         动画的加牌操作不会调用该回调；回调异常会从到达回调中传播。
        ///     </para>
        /// </summary>
        public Action<ModExtraHandCardContext>? OnCardArrived { get; init; }
    }
}
