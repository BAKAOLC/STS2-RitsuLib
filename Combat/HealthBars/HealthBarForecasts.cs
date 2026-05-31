using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Convenience helpers for building health bar forecast segments.
    ///     用于构建生命条 forecast 片段的便捷辅助方法。
    /// </summary>
    public static class HealthBarForecasts
    {
        /// <summary>
        ///     Starts a general-purpose sequence builder for <paramref name="context" />.
        ///     为 <paramref name="context" /> 启动一个通用序列构建器。
        /// </summary>
        public static HealthBarForecastSequenceBuilder For(HealthBarForecastContext context)
        {
            return new(context);
        }

        /// <summary>
        ///     Starts a right-growing forecast lane with a fixed <paramref name="color" />.
        ///     启动一条固定 <paramref name="color" /> 的向右增长 forecast 轨道。
        /// </summary>
        public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color)
        {
            return FromRight(context, color, null);
        }

        /// <summary>
        ///     Starts a right-growing lane with separate optional <see cref="CanvasItem.SelfModulate" /> for the nine-patch
        ///     overlay (e.g. white when <see cref="Godot.Material" /> carries tint).
        ///     启动一条向右增长轨道，并为九宫格覆盖层提供单独的可选 <see cref="CanvasItem.SelfModulate" />
        ///     （例如当 <see cref="Godot.Material" /> 携带染色时使用白色）。
        /// </summary>
        /// <param name="context">
        ///     Forecast context.
        ///     Forecast 上下文。
        /// </param>
        /// <param name="color">
        ///     Lethal label color and fallback overlay modulate.
        ///     致命标签颜色和后备覆盖 modulate。
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     When set, used as overlay <see cref="CanvasItem.SelfModulate" /> instead of
        ///     <paramref name="color" />.
        ///     设置后，作为覆盖层 <see cref="CanvasItem.SelfModulate" /> 使用，而不是
        ///     <paramref name="color" />。
        /// </param>
        public static HealthBarForecastLaneBuilder FromRight(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate)
        {
            return FromRight(context, color, overlaySelfModulate, true);
        }

        /// <inheritdoc cref="FromRight(HealthBarForecastContext, Color, Color?)" />
        public static HealthBarForecastLaneBuilder FromRight(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            return new(For(context), color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate,
                affectsHpLabel);
        }

        /// <summary>
        ///     Starts a left-growing forecast lane with a fixed <paramref name="color" />.
        ///     启动一条固定 <paramref name="color" /> 的向左增长 forecast 轨道。
        /// </summary>
        public static HealthBarForecastLaneBuilder FromLeft(HealthBarForecastContext context, Color color)
        {
            return FromLeft(context, color, null);
        }

        /// <inheritdoc cref="FromRight(HealthBarForecastContext, Color, Color?)" />
        public static HealthBarForecastLaneBuilder FromLeft(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate)
        {
            return FromLeft(context, color, overlaySelfModulate, true);
        }

        /// <inheritdoc cref="FromRight(HealthBarForecastContext, Color, Color?)" />
        public static HealthBarForecastLaneBuilder FromLeft(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate,
            bool affectsHpLabel)
        {
            return new(For(context), color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate,
                affectsHpLabel);
        }

        /// <summary>
        ///     Returns a single segment when <paramref name="amount" /> is positive, with optional material only.
        ///     当 <paramref name="amount" /> 为正数时，返回一个仅带可选 material 的单片段。
        /// </summary>
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return Single(amount, color, direction, order, overlayMaterial, null);
        }

        /// <summary>
        ///     Returns a single segment when <paramref name="amount" /> is positive, with optional material and overlay
        ///     <see cref="CanvasItem.SelfModulate" />.
        ///     当 <paramref name="amount" /> 为正数时，返回一个带可选 material 和覆盖层
        ///     <see cref="CanvasItem.SelfModulate" /> 的单片段。
        /// </summary>
        /// <param name="amount">
        ///     HP chunk size.
        ///     HP 块大小。
        /// </param>
        /// <param name="color">
        ///     Lethal label color and fallback modulate.
        ///     致命标签颜色和后备 modulate。
        /// </param>
        /// <param name="direction">
        ///     Growth direction.
        ///     增长方向。
        /// </param>
        /// <param name="order">
        ///     Sort order among segments.
        ///     片段之间的排序顺序。
        /// </param>
        /// <param name="overlayMaterial">
        ///     Optional segment material.
        ///     可选片段 material。
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     When set, stored on <see cref="HealthBarForecastSegment.OverlaySelfModulate" />.
        ///     设置后，存储到 <see cref="HealthBarForecastSegment.OverlaySelfModulate" />。
        /// </param>
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

        /// <inheritdoc cref="Single(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?)" />
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

        /// <summary>
        ///     Returns a single segment when <paramref name="amount" /> is positive, without a custom material.
        ///     当 <paramref name="amount" /> 为正数时，返回一个不带自定义 material 的单片段。
        /// </summary>
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
    ///     Mutable builder for one forecast source's ordered segment sequence.
    ///     一个 forecast source 的有序片段序列的可变构建器。
    /// </summary>
    public sealed class HealthBarForecastSequenceBuilder(HealthBarForecastContext context)
    {
        private readonly List<HealthBarForecastSegment> _segments = [];

        /// <summary>
        ///     Forecast context associated with this sequence.
        ///     与此序列关联的 forecast 上下文。
        /// </summary>
        public HealthBarForecastContext Context { get; } = context;

        /// <summary>
        ///     Appends a segment when <paramref name="amount" /> is positive.
        ///     Consecutive segments with identical color, direction, order, material reference, and overlay modulate are merged.
        ///     当 <paramref name="amount" /> 为正数时追加一个片段。
        ///     颜色、方向、顺序、material 引用和覆盖 modulate 均相同的连续片段会被合并。
        /// </summary>
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return Add(amount, color, direction, order, overlayMaterial, null);
        }

        /// <summary>
        ///     Appends a segment when <paramref name="amount" /> is positive, with explicit overlay modulate.
        ///     当 <paramref name="amount" /> 为正数时，追加一个带显式覆盖 modulate 的片段。
        /// </summary>
        /// <param name="amount">
        ///     HP chunk size.
        ///     HP 块大小。
        /// </param>
        /// <param name="color">
        ///     Lethal label color and fallback modulate.
        ///     致命标签颜色和后备 modulate。
        /// </param>
        /// <param name="direction">
        ///     Growth direction.
        ///     增长方向。
        /// </param>
        /// <param name="order">
        ///     Sort order among segments.
        ///     片段之间的排序顺序。
        /// </param>
        /// <param name="overlayMaterial">
        ///     Optional segment material.
        ///     可选片段 material。
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     Optional overlay <see cref="CanvasItem.SelfModulate" />; null uses <paramref name="color" />.
        ///     可选覆盖层 <see cref="CanvasItem.SelfModulate" />；为 null 时使用 <paramref name="color" />。
        /// </param>
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

        /// <inheritdoc cref="Add(int, Color, HealthBarForecastGrowthDirection, int, Material?, Color?)" />
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
                    _segments[^1] = last with { Amount = last.Amount + segment.Amount };
                    return this;
                }
            }

            _segments.Add(segment);
            return this;
        }

        /// <summary>
        ///     Appends a segment without a custom material.
        ///     追加一个不带自定义 material 的片段。
        /// </summary>
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order = 0)
        {
            return Add(amount, color, direction, order, null, null);
        }

        /// <summary>
        ///     Appends all positive amounts as consecutive segments.
        ///     将所有正数数量追加为连续片段。
        /// </summary>
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
        {
            return AddRange(amounts, color, direction, order, overlayMaterial, null);
        }

        /// <summary>
        ///     Appends all positive amounts as consecutive segments with explicit overlay modulate.
        ///     将所有正数数量追加为带显式覆盖 modulate 的连续片段。
        /// </summary>
        /// <param name="amounts">
        ///     HP chunk sizes.
        ///     HP 块大小。
        /// </param>
        /// <param name="color">
        ///     Lethal label color and fallback modulate.
        ///     致命标签颜色和后备 modulate。
        /// </param>
        /// <param name="direction">
        ///     Growth direction.
        ///     增长方向。
        /// </param>
        /// <param name="order">
        ///     Sort order among segments.
        ///     片段之间的排序顺序。
        /// </param>
        /// <param name="overlayMaterial">
        ///     Optional segment material.
        ///     可选片段 material。
        /// </param>
        /// <param name="overlaySelfModulate">
        ///     Optional overlay <see cref="CanvasItem.SelfModulate" /> shared by chunks.
        ///     由各块共享的可选覆盖层 <see cref="CanvasItem.SelfModulate" />。
        /// </param>
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

        /// <inheritdoc cref="AddRange(IEnumerable{int}, Color, HealthBarForecastGrowthDirection, int, Material?, Color?)" />
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

        /// <summary>
        ///     Appends all positive amounts as consecutive segments without a custom material.
        ///     将所有正数数量追加为不带自定义 material 的连续片段。
        /// </summary>
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order = 0)
        {
            return AddRange(amounts, color, direction, order, null, null);
        }

        /// <summary>
        ///     Appends segments that trigger at the start of <paramref name="triggerSide" />'s turn.
        ///     追加在 <paramref name="triggerSide" /> 回合开始时触发的片段。
        /// </summary>
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
        ///     Appends segments that trigger at the end of <paramref name="triggerSide" />'s turn.
        ///     追加在 <paramref name="triggerSide" /> 回合结束时触发的片段。
        /// </summary>
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

        /// <summary>
        ///     Creates a fixed-color right-growing lane on this sequence.
        ///     在此序列上创建一条固定颜色、向右增长的轨道。
        /// </summary>
        public HealthBarForecastLaneBuilder FromRight(Color color)
        {
            return FromRight(color, null);
        }

        /// <inheritdoc cref="HealthBarForecasts.FromRight(HealthBarForecastContext, Color, Color?)" />
        public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate)
        {
            return FromRight(color, overlaySelfModulate, true);
        }

        /// <inheritdoc cref="FromRight(Color, Color?)" />
        public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate, affectsHpLabel);
        }

        /// <summary>
        ///     Creates a fixed-color left-growing lane on this sequence.
        ///     在此序列上创建一条固定颜色、向左增长的轨道。
        /// </summary>
        public HealthBarForecastLaneBuilder FromLeft(Color color)
        {
            return FromLeft(color, null);
        }

        /// <inheritdoc cref="FromRight(Color, Color?)" />
        public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate)
        {
            return FromLeft(color, overlaySelfModulate, true);
        }

        /// <inheritdoc cref="FromRight(Color, Color?)" />
        public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate, affectsHpLabel);
        }

        /// <summary>
        ///     Returns the built sequence snapshot.
        ///     返回已构建的序列快照。
        /// </summary>
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
    ///     Convenience wrapper for the common case of one fixed-color forecast lane.
    ///     常见单条固定颜色 forecast 轨道场景的便捷包装器。
    /// </summary>
    /// <param name="sequence">
    ///     Parent sequence builder.
    ///     父序列构建器。
    /// </param>
    /// <param name="color">
    ///     Lane label / fallback modulate color.
    ///     轨道标签 / 后备 modulate 颜色。
    /// </param>
    /// <param name="direction">
    ///     Growth edge for this lane.
    ///     此轨道的增长边。
    /// </param>
    /// <param name="overlaySelfModulate">
    ///     When set, used as <see cref="CanvasItem.SelfModulate" /> for segments in this lane.
    ///     设置后，用作此轨道中片段的 <see cref="CanvasItem.SelfModulate" />。
    /// </param>
    /// <param name="affectsHpLabel">
    ///     Whether this lane's segments can recolor the HP label at lethal threshold.
    ///     此轨道中的片段达到致命阈值时是否可以重染 HP 标签。
    /// </param>
    public sealed class HealthBarForecastLaneBuilder(
        HealthBarForecastSequenceBuilder sequence,
        Color color,
        HealthBarForecastGrowthDirection direction,
        Color? overlaySelfModulate = null,
        bool affectsHpLabel = true)
    {
        /// <summary>
        ///     Parent sequence builder.
        ///     父序列构建器。
        /// </summary>
        public HealthBarForecastSequenceBuilder Sequence { get; } = sequence;

        /// <summary>
        ///     Appends a segment with explicit <paramref name="order" /> and optional <paramref name="overlayMaterial" />.
        ///     追加带显式 <paramref name="order" /> 和可选 <paramref name="overlayMaterial" /> 的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder Add(int amount, int order, Material? overlayMaterial)
        {
            Sequence.Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     Appends a segment without a custom material.
        ///     追加一个不带自定义 material 的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder Add(int amount, int order = 0)
        {
            return Add(amount, order, null);
        }

        /// <summary>
        ///     Appends multiple segments with the same <paramref name="order" /> and optional <paramref name="overlayMaterial" />.
        ///     追加多个具有相同 <paramref name="order" /> 和可选 <paramref name="overlayMaterial" /> 的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order, Material? overlayMaterial)
        {
            Sequence.AddRange(amounts, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     Appends multiple segments without a custom material.
        ///     追加多个不带自定义 material 的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order = 0)
        {
            return AddRange(amounts, order, null);
        }

        /// <summary>
        ///     Appends segments that trigger at the start of <paramref name="triggerSide" />'s turn.
        ///     追加在 <paramref name="triggerSide" /> 回合开始时触发的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder AtSideTurnStart(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnStart(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     Appends segments that trigger at the end of <paramref name="triggerSide" />'s turn.
        ///     追加在 <paramref name="triggerSide" /> 回合结束时触发的片段。
        /// </summary>
        public HealthBarForecastLaneBuilder AtSideTurnEnd(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnEnd(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate, affectsHpLabel);
            return this;
        }

        /// <summary>
        ///     Starts another right-growing lane on the same parent sequence.
        ///     在同一父序列上启动另一条向右增长轨道。
        /// </summary>
        public HealthBarForecastLaneBuilder ThenFromRight(Color nextColor)
        {
            return Sequence.FromRight(nextColor, null);
        }

        /// <summary>
        ///     Starts another left-growing lane on the same parent sequence.
        ///     在同一父序列上启动另一条向左增长轨道。
        /// </summary>
        public HealthBarForecastLaneBuilder ThenFromLeft(Color nextColor)
        {
            return Sequence.FromLeft(nextColor, null);
        }

        /// <summary>
        ///     Returns the built segment snapshot.
        ///     返回已构建的片段快照。
        /// </summary>
        public IReadOnlyList<HealthBarForecastSegment> Build()
        {
            return Sequence.Build();
        }
    }
}
