using Godot;
using STS2RitsuLib.CardPiles.Nodes;

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
        private readonly Vector2 _disabledOffset = new(0f, 100f);
        private readonly Color _disabledModulate = new(0.5f, 0.5f, 0.5f);
        private readonly double _disabledTransitionDuration = 0.2;

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
        ///         Gets whether this pile has manual-card-play capability through the base-game targeting, action
        ///         queue, resource payment, card hooks, and destination-pile flow. This value also initializes
        ///         <see cref="NModExtraHand.CardPlayEnabled" /> for each new container. The default is
        ///         <see langword="true" />. Runtime availability can temporarily disable and restore granted
        ///         capability, but cannot enable a definition that disallows card play.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此牌堆是否具备通过游戏原有目标选择、行动队列、资源支付、卡牌钩子与目标牌堆流程手动
        ///         出牌的能力。该值也会初始化每个新容器的 <see cref="NModExtraHand.CardPlayEnabled" />。默认值为
        ///         <see langword="true" />。运行时可用性可以临时禁用并恢复已授予的能力，但不能启用定义中未
        ///         允许出牌的牌堆。
        ///     </para>
        /// </summary>
        public bool AllowCardPlay { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the position offset applied to the complete card layout while this hand is disabled. The
        ///         offset is applied after the built-in layout and <see cref="LayoutResolver" />. The default is
        ///         <c>(0, 100)</c>, matching the base-game hand.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此手牌区禁用时应用于完整卡牌布局的位置偏移。该偏移会在内置布局与
        ///         <see cref="LayoutResolver" /> 之后应用。默认值为 <c>(0, 100)</c>，与游戏原有手牌一致。
        ///     </para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The assigned vector contains a non-finite component.</para>
        ///     <para xml:lang="zh-CN">所赋向量包含非有限分量。</para>
        /// </exception>
        public Vector2 DisabledOffset
        {
            get => _disabledOffset;
            init
            {
                if (!IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(DisabledOffset), value,
                        "The disabled offset must contain only finite components.");
                _disabledOffset = value;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the modulation color applied to the complete card layout while this hand is disabled. The
        ///         default is the base-game hand's gray modulation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此手牌区禁用时应用于完整卡牌布局的调制颜色。默认值为游戏原有手牌使用的灰色调制。
        ///     </para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The assigned color contains a non-finite component.</para>
        ///     <para xml:lang="zh-CN">所赋颜色包含非有限分量。</para>
        /// </exception>
        public Color DisabledModulate
        {
            get => _disabledModulate;
            init
            {
                if (!IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(DisabledModulate), value,
                        "The disabled modulation color must contain only finite components.");
                _disabledModulate = value;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the duration in seconds used to transition between enabled and disabled presentation. The
        ///         default is <c>0.2</c>, matching the base-game hand. Zero applies the presentation immediately.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取启用与禁用表现之间过渡所用的秒数。默认值为 <c>0.2</c>，与游戏原有手牌一致；设为零时
        ///         立即应用目标表现。
        ///     </para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">The assigned duration is negative or non-finite.</para>
        ///     <para xml:lang="zh-CN">所赋时长为负数或非有限值。</para>
        /// </exception>
        public double DisabledTransitionDuration
        {
            get => _disabledTransitionDuration;
            init
            {
                if (!double.IsFinite(value) || value < 0.0)
                    throw new ArgumentOutOfRangeException(nameof(DisabledTransitionDuration), value,
                        "The disabled transition duration must be finite and non-negative.");
                _disabledTransitionDuration = value;
            }
        }

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

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y);
        }

        private static bool IsFinite(Color value)
        {
            return float.IsFinite(value.R)
                   && float.IsFinite(value.G)
                   && float.IsFinite(value.B)
                   && float.IsFinite(value.A);
        }
    }
}
