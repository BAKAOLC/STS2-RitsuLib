using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     <para xml:lang="en">Provides convenience methods for building health-bar forecast segments.</para>
    ///     <para xml:lang="zh-CN">提供用于构建生命条预测片段的便捷方法。</para>
    /// </summary>
    public static class HealthBarForecasts
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a general-purpose sequence builder for <paramref name="context" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="context" /> 创建通用序列构建器。</para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The forecast context, which must contain a creature.</para>
        ///     <para xml:lang="zh-CN">预测上下文，其中必须包含生物。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new sequence builder.</para>
        ///     <para xml:lang="zh-CN">新建的序列构建器。</para>
        /// </returns>
        public static HealthBarForecastSequenceBuilder For(HealthBarForecastContext context)
        {
            ArgumentNullException.ThrowIfNull(context.Creature);
            return new(context);
        }

        /// <inheritdoc cref="FromRight(HealthBarForecastContext, Color, Color?, bool)" />
        public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color)
        {
            return FromRight(context, color, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a fixed-color forecast lane that extends inward from the current-HP edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建从当前生命值边缘向内延伸的固定颜色预测轨道。</para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The forecast context, which must contain a creature.</para>
        ///     <para xml:lang="zh-CN">预测上下文，其中必须包含生物。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. When omitted,
        ///         <paramref name="color" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />；省略时使用 <paramref name="color" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A lane builder whose segments can recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">其片段可以改变生命值文本颜色的轨道构建器。</para>
        /// </returns>
        public static HealthBarForecastLaneBuilder FromRight(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate)
        {
            return FromRight(context, color, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a fixed-color forecast lane that extends inward from the current-HP edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建从当前生命值边缘向内延伸的固定颜色预测轨道。</para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The forecast context, which must contain a creature.</para>
        ///     <para xml:lang="zh-CN">预测上下文，其中必须包含生物。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. When omitted,
        ///         <paramref name="color" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />；省略时使用 <paramref name="color" />。
        ///     </para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether lethal forecasts in this lane may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该轨道的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new lane builder.</para>
        ///     <para xml:lang="zh-CN">新建的轨道构建器。</para>
        /// </returns>
        public static HealthBarForecastLaneBuilder FromRight(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            return new(For(context), color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate,
                affectsHpLabel);
        }

        /// <inheritdoc cref="FromLeft(HealthBarForecastContext, Color, Color?, bool)" />
        public static HealthBarForecastLaneBuilder FromLeft(HealthBarForecastContext context, Color color)
        {
            return FromLeft(context, color, null);
        }

        /// <inheritdoc cref="FromLeft(HealthBarForecastContext, Color, Color?, bool)" />
        public static HealthBarForecastLaneBuilder FromLeft(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate)
        {
            return FromLeft(context, color, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a fixed-color forecast lane that extends inward from the empty edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建从生命条空白侧边缘向内延伸的固定颜色预测轨道。</para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The forecast context, which must contain a creature.</para>
        ///     <para xml:lang="zh-CN">预测上下文，其中必须包含生物。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. When omitted,
        ///         <paramref name="color" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />；省略时使用 <paramref name="color" />。
        ///     </para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether lethal forecasts in this lane may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该轨道的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new lane builder.</para>
        ///     <para xml:lang="zh-CN">新建的轨道构建器。</para>
        /// </returns>
        public static HealthBarForecastLaneBuilder FromLeft(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            return new(For(context), color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate,
                affectsHpLabel);
        }

        /// <inheritdoc cref="Single(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return Single(amount, color, direction, order, overlayMaterial, null);
        }

        /// <inheritdoc cref="Single(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            return Single(amount, color, direction, order, overlayMaterial, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates one forecast segment when <paramref name="amount" /> is positive; otherwise, returns an empty
        ///         sequence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="amount" /> 为正数时创建一个预测片段；否则返回空序列。
        ///     </para>
        /// </summary>
        /// <param name="amount">
        ///     <para xml:lang="en">The HP amount represented by the segment.</para>
        ///     <para xml:lang="zh-CN">片段表示的生命值数值。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="direction">
        ///     <para xml:lang="en">The edge from which the segment extends.</para>
        ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
        /// </param>
        /// <param name="order">
        ///     <para xml:lang="en">The primary render order.</para>
        ///     <para xml:lang="zh-CN">主要渲染顺序。</para>
        /// </param>
        /// <param name="overlayMaterial">
        ///     <para xml:lang="en">The optional overlay material.</para>
        ///     <para xml:lang="zh-CN">可选的覆盖材质。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. When omitted,
        ///         <paramref name="color" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />；省略时使用 <paramref name="color" />。
        ///     </para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether a lethal forecast from this segment may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该片段的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A one-segment sequence, or an empty sequence for a nonpositive amount.</para>
        ///     <para xml:lang="zh-CN">包含一个片段的序列；数值非正时为空序列。</para>
        /// </returns>
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            if (amount <= 0)
                return [];

            return
            [
                new(amount, color, direction, order, overlayMaterial, overlaySelfModulate,
                    AffectsHpLabel: affectsHpLabel),
            ];
        }

        /// <inheritdoc cref="Single(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order = 0)
        {
            return Single(amount, color, direction, order, null, null);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Builds an ordered sequence of segments for one forecast source.</para>
    ///     <para xml:lang="zh-CN">为单个预测来源构建有序片段序列。</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">The forecast context, which must contain a creature.</para>
    ///     <para xml:lang="zh-CN">预测上下文，其中必须包含生物。</para>
    /// </param>
    public sealed class HealthBarForecastSequenceBuilder(HealthBarForecastContext context)
    {
        private readonly List<HealthBarForecastSegment> _segments = [];

        /// <summary>
        ///     <para xml:lang="en">Gets the forecast context associated with this sequence.</para>
        ///     <para xml:lang="zh-CN">获取与该序列关联的预测上下文。</para>
        /// </summary>
        public HealthBarForecastContext Context { get; } =
            context.Creature == null
                ? throw new ArgumentException("The forecast context must contain a creature.", nameof(context))
                : context;

        /// <inheritdoc cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return Add(amount, color, direction, order, overlayMaterial, null);
        }

        /// <inheritdoc cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            return Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends a segment when <paramref name="amount" /> is positive. Consecutive compatible segments are
        ///         merged, with their combined amount capped at <see cref="int.MaxValue" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="amount" /> 为正数时追加片段。相邻且所有显示与排序属性均兼容的片段会合并，
        ///         合计数值不超过 <see cref="int.MaxValue" />。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Compatibility requires equal colors, direction, order, overlay material reference, overlay
        ///         modulation, empty-edge layout, exclusive Z group, and HP-label behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         兼容要求颜色、方向、顺序、覆盖材质引用、覆盖层调制色、空白侧布局、互斥 Z 组和生命值文本
        ///         行为均相同。
        ///     </para>
        /// </remarks>
        /// <param name="amount">
        ///     <para xml:lang="en">The HP amount represented by the segment.</para>
        ///     <para xml:lang="zh-CN">片段表示的生命值数值。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="direction">
        ///     <para xml:lang="en">The edge from which the segment extends.</para>
        ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
        /// </param>
        /// <param name="order">
        ///     <para xml:lang="en">The primary render order.</para>
        ///     <para xml:lang="zh-CN">主要渲染顺序。</para>
        /// </param>
        /// <param name="overlayMaterial">
        ///     <para xml:lang="en">The optional overlay material.</para>
        ///     <para xml:lang="zh-CN">可选的覆盖材质。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. When omitted,
        ///         <paramref name="color" /> is used.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />；省略时使用 <paramref name="color" />。
        ///     </para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether a lethal forecast from this segment may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该片段的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This builder.</para>
        ///     <para xml:lang="zh-CN">当前构建器。</para>
        /// </returns>
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            if (amount <= 0)
                return this;

            var segment =
                new HealthBarForecastSegment(amount, color, direction, order, overlayMaterial, overlaySelfModulate,
                    AffectsHpLabel: affectsHpLabel);
            if (_segments.Count > 0)
            {
                var last = _segments[^1];
                if (CanMerge(last, segment))
                {
                    _segments[^1] = last with
                    {
                        Amount = (int)Math.Min(int.MaxValue, (long)last.Amount + segment.Amount),
                    };
                    return this;
                }
            }

            _segments.Add(segment);
            return this;
        }

        /// <inheritdoc cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order = 0)
        {
            return Add(amount, color, direction, order, null, null);
        }

        /// <inheritdoc cref="AddRange(IEnumerable{int}, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return AddRange(amounts, color, direction, order, overlayMaterial, null);
        }

        /// <inheritdoc cref="AddRange(IEnumerable{int}, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            return AddRange(amounts, color, direction, order, overlayMaterial, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Passes each amount to
        ///         <see cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" /> in
        ///         enumeration order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按枚举顺序将每个数值传给
        ///         <see cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />。
        ///     </para>
        /// </summary>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append. Nonpositive values are ignored.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值；忽略非正值。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="direction">
        ///     <para xml:lang="en">The edge from which the segments extend.</para>
        ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
        /// </param>
        /// <param name="order">
        ///     <para xml:lang="en">The primary render order.</para>
        ///     <para xml:lang="zh-CN">主要渲染顺序。</para>
        /// </param>
        /// <param name="overlayMaterial">
        ///     <para xml:lang="en">The optional overlay material.</para>
        ///     <para xml:lang="zh-CN">可选的覆盖材质。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">
        ///         The optional overlay <see cref="CanvasItem.SelfModulate" /> shared by the segments.
        ///     </para>
        ///     <para xml:lang="zh-CN">所有片段共用的可选覆盖层 <see cref="CanvasItem.SelfModulate" />。</para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether lethal forecasts from these segments may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">这些片段的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This builder.</para>
        ///     <para xml:lang="zh-CN">当前构建器。</para>
        /// </returns>
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            ArgumentNullException.ThrowIfNull(amounts);

            foreach (var amount in amounts)
                Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);

            return this;
        }

        /// <inheritdoc cref="AddRange(IEnumerable{int}, Color, HealthBarForecastGrowthDirection, int, Material?, Color?, bool)" />
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order = 0)
        {
            return AddRange(amounts, color, direction, order, null, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends segments without a custom material, ordered for the start of
        ///         <paramref name="triggerSide" />'s turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         追加不带自定义材质并按 <paramref name="triggerSide" /> 一方回合开始时机排序的片段。
        ///     </para>
        /// </summary>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-start timing determines the order.</para>
        ///     <para xml:lang="zh-CN">以其回合开始时机决定顺序的一方。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The segment color.</para>
        ///     <para xml:lang="zh-CN">片段颜色。</para>
        /// </param>
        /// <param name="direction">
        ///     <para xml:lang="en">The edge from which the segments extend.</para>
        ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
        /// </param>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This builder.</para>
        ///     <para xml:lang="zh-CN">当前构建器。</para>
        /// </returns>
        public HealthBarForecastSequenceBuilder AddSideTurnStart(
            CombatSide triggerSide,
            Color color,
            HealthBarForecastGrowthDirection direction,
            params int[] amounts)
        {
            return AddRange(
                amounts,
                color,
                direction,
                HealthBarForecastOrder.ForSideTurnStart(Context.Creature, triggerSide));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends segments without a custom material, ordered for the end of
        ///         <paramref name="triggerSide" />'s turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         追加不带自定义材质并按 <paramref name="triggerSide" /> 一方回合结束时机排序的片段。
        ///     </para>
        /// </summary>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-end timing determines the order.</para>
        ///     <para xml:lang="zh-CN">以其回合结束时机决定顺序的一方。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">The segment color.</para>
        ///     <para xml:lang="zh-CN">片段颜色。</para>
        /// </param>
        /// <param name="direction">
        ///     <para xml:lang="en">The edge from which the segments extend.</para>
        ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
        /// </param>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This builder.</para>
        ///     <para xml:lang="zh-CN">当前构建器。</para>
        /// </returns>
        public HealthBarForecastSequenceBuilder AddSideTurnEnd(
            CombatSide triggerSide,
            Color color,
            HealthBarForecastGrowthDirection direction,
            params int[] amounts)
        {
            return AddRange(
                amounts,
                color,
                direction,
                HealthBarForecastOrder.ForSideTurnEnd(Context.Creature, triggerSide));
        }

        /// <inheritdoc cref="FromRight(Color, Color?, bool)" />
        public HealthBarForecastLaneBuilder FromRight(Color color)
        {
            return FromRight(color, null);
        }

        /// <inheritdoc cref="FromRight(Color, Color?, bool)" />
        public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate)
        {
            return FromRight(color, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a fixed-color lane on this sequence that extends inward from the current-HP edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">在该序列上创建从当前生命值边缘向内延伸的固定颜色轨道。</para>
        /// </summary>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">The optional independent overlay color.</para>
        ///     <para xml:lang="zh-CN">可选的独立覆盖层颜色。</para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether lethal forecasts in this lane may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该轨道的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new lane builder backed by this sequence.</para>
        ///     <para xml:lang="zh-CN">以该序列为基础的新轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate, affectsHpLabel);
        }

        /// <inheritdoc cref="FromLeft(Color, Color?, bool)" />
        public HealthBarForecastLaneBuilder FromLeft(Color color)
        {
            return FromLeft(color, null);
        }

        /// <inheritdoc cref="FromLeft(Color, Color?, bool)" />
        public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate)
        {
            return FromLeft(color, overlaySelfModulate, true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a fixed-color lane on this sequence that extends inward from the empty edge.
        ///     </para>
        ///     <para xml:lang="zh-CN">在该序列上创建从生命条空白侧边缘向内延伸的固定颜色轨道。</para>
        /// </summary>
        /// <param name="color">
        ///     <para xml:lang="en">The lethal HP-label color and fallback overlay color.</para>
        ///     <para xml:lang="zh-CN">致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     <para xml:lang="en">The optional independent overlay color.</para>
        ///     <para xml:lang="zh-CN">可选的独立覆盖层颜色。</para>
        /// </param>
        /// <param name="affectsHpLabel">
        ///     <para xml:lang="en">Whether lethal forecasts in this lane may recolor the HP label.</para>
        ///     <para xml:lang="zh-CN">该轨道的预测致命时是否可以改变生命值文本颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new lane builder backed by this sequence.</para>
        ///     <para xml:lang="zh-CN">以该序列为基础的新轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate, affectsHpLabel);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a snapshot of the built segment sequence.</para>
        ///     <para xml:lang="zh-CN">返回已构建片段序列的快照。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">An immutable view backed by a new array, or an empty collection.</para>
        ///     <para xml:lang="zh-CN">由新数组承载的只读视图，或空集合。</para>
        /// </returns>
        public IReadOnlyList<HealthBarForecastSegment> Build()
        {
            return _segments.Count == 0 ? [] : _segments.ToArray();
        }

        private static bool CanMerge(HealthBarForecastSegment left, HealthBarForecastSegment right)
        {
            return left.Color == right.Color &&
                   left.Direction == right.Direction &&
                   left.Order == right.Order &&
                   left.OverlaySelfModulate == right.OverlaySelfModulate &&
                   left.LeftOriginLayout == right.LeftOriginLayout &&
                   left.LeftExclusiveZGroup == right.LeftExclusiveZGroup &&
                   left.AffectsHpLabel == right.AffectsHpLabel &&
                   ReferenceEquals(left.OverlayMaterial, right.OverlayMaterial);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Builds a fixed-color forecast lane on a parent sequence.</para>
    ///     <para xml:lang="zh-CN">在父序列上构建固定颜色的预测轨道。</para>
    /// </summary>
    /// <param name="sequence">
    ///     <para xml:lang="en">The parent sequence builder.</para>
    ///     <para xml:lang="zh-CN">父序列构建器。</para>
    /// </param>
    /// <param name="color">
    ///     <para xml:lang="en">The lane's lethal HP-label color and fallback overlay color.</para>
    ///     <para xml:lang="zh-CN">轨道致命时的生命值文本颜色，也是覆盖层的回退颜色。</para>
    /// </param>
    /// <param name="direction">
    ///     <para xml:lang="en">The edge from which this lane extends.</para>
    ///     <para xml:lang="zh-CN">该轨道延伸的起始边缘。</para>
    /// </param>
    /// <param name="overlaySelfModulate">
    ///     <para xml:lang="en">
    ///         The optional overlay <see cref="CanvasItem.SelfModulate" /> shared by this lane's segments.
    ///     </para>
    ///     <para xml:lang="zh-CN">该轨道所有片段共用的可选覆盖层 <see cref="CanvasItem.SelfModulate" />。</para>
    /// </param>
    /// <param name="affectsHpLabel">
    ///     <para xml:lang="en">Whether lethal forecasts in this lane may recolor the HP label.</para>
    ///     <para xml:lang="zh-CN">该轨道的预测致命时是否可以改变生命值文本颜色。</para>
    /// </param>
    public sealed class HealthBarForecastLaneBuilder(
        HealthBarForecastSequenceBuilder sequence,
        Color color,
        HealthBarForecastGrowthDirection direction,
        Color? overlaySelfModulate = null,
        bool affectsHpLabel = true)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the parent sequence builder.</para>
        ///     <para xml:lang="zh-CN">获取父序列构建器。</para>
        /// </summary>
        public HealthBarForecastSequenceBuilder Sequence { get; } =
            sequence ?? throw new ArgumentNullException(nameof(sequence));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends a positive segment using this lane's appearance and direction.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用该轨道的外观和方向追加数值为正的片段。</para>
        /// </summary>
        /// <param name="amount">
        ///     <para xml:lang="en">The HP amount represented by the segment.</para>
        ///     <para xml:lang="zh-CN">片段表示的生命值数值。</para>
        /// </param>
        /// <param name="order">
        ///     <para xml:lang="en">The primary render order.</para>
        ///     <para xml:lang="zh-CN">主要渲染顺序。</para>
        /// </param>
        /// <param name="overlayMaterial">
        ///     <para xml:lang="en">The optional overlay material.</para>
        ///     <para xml:lang="zh-CN">可选的覆盖材质。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This lane builder.</para>
        ///     <para xml:lang="zh-CN">当前轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder Add(int amount, int order, Material? overlayMaterial)
        {
            Sequence.Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <inheritdoc cref="Add(int, int, Material?)" />
        public HealthBarForecastLaneBuilder Add(int amount, int order = 0)
        {
            return Add(amount, order, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends positive amounts using this lane's appearance and direction.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用该轨道的外观和方向追加数值为正的片段。</para>
        /// </summary>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append. Nonpositive values are ignored.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值；忽略非正值。</para>
        /// </param>
        /// <param name="order">
        ///     <para xml:lang="en">The primary render order shared by the segments.</para>
        ///     <para xml:lang="zh-CN">所有片段共用的主要渲染顺序。</para>
        /// </param>
        /// <param name="overlayMaterial">
        ///     <para xml:lang="en">The optional overlay material shared by the segments.</para>
        ///     <para xml:lang="zh-CN">所有片段共用的可选覆盖材质。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This lane builder.</para>
        ///     <para xml:lang="zh-CN">当前轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order, Material? overlayMaterial)
        {
            Sequence.AddRange(amounts, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <inheritdoc cref="AddRange(IEnumerable{int}, int, Material?)" />
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order = 0)
        {
            return AddRange(amounts, order, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends segments without a custom material, ordered for the start of
        ///         <paramref name="triggerSide" />'s turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         追加不带自定义材质并按 <paramref name="triggerSide" /> 一方回合开始时机排序的片段。
        ///     </para>
        /// </summary>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-start timing determines the order.</para>
        ///     <para xml:lang="zh-CN">以其回合开始时机决定顺序的一方。</para>
        /// </param>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This lane builder.</para>
        ///     <para xml:lang="zh-CN">当前轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder AtSideTurnStart(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnStart(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends segments without a custom material, ordered for the end of
        ///         <paramref name="triggerSide" />'s turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         追加不带自定义材质并按 <paramref name="triggerSide" /> 一方回合结束时机排序的片段。
        ///     </para>
        /// </summary>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-end timing determines the order.</para>
        ///     <para xml:lang="zh-CN">以其回合结束时机决定顺序的一方。</para>
        /// </param>
        /// <param name="amounts">
        ///     <para xml:lang="en">The HP amounts to append.</para>
        ///     <para xml:lang="zh-CN">待追加的生命值数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">This lane builder.</para>
        ///     <para xml:lang="zh-CN">当前轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder AtSideTurnEnd(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnEnd(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates another lane from the current-HP edge on the same parent sequence.</para>
        ///     <para xml:lang="zh-CN">在同一父序列上创建另一条从当前生命值边缘开始的轨道。</para>
        /// </summary>
        /// <param name="nextColor">
        ///     <para xml:lang="en">The next lane's color.</para>
        ///     <para xml:lang="zh-CN">下一条轨道的颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The new lane builder.</para>
        ///     <para xml:lang="zh-CN">新建的轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder ThenFromRight(Color nextColor)
        {
            return Sequence.FromRight(nextColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates another lane from the empty edge on the same parent sequence.</para>
        ///     <para xml:lang="zh-CN">在同一父序列上创建另一条从生命条空白侧边缘开始的轨道。</para>
        /// </summary>
        /// <param name="nextColor">
        ///     <para xml:lang="en">The next lane's color.</para>
        ///     <para xml:lang="zh-CN">下一条轨道的颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The new lane builder.</para>
        ///     <para xml:lang="zh-CN">新建的轨道构建器。</para>
        /// </returns>
        public HealthBarForecastLaneBuilder ThenFromLeft(Color nextColor)
        {
            return Sequence.FromLeft(nextColor, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a snapshot of the parent sequence's built segments.</para>
        ///     <para xml:lang="zh-CN">返回父序列中已构建片段的快照。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The built segment snapshot.</para>
        ///     <para xml:lang="zh-CN">已构建的片段快照。</para>
        /// </returns>
        public IReadOnlyList<HealthBarForecastSegment> Build()
        {
            return Sequence.Build();
        }
    }
}
