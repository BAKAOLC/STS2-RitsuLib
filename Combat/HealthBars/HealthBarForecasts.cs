using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Convenience helpers for building health bar forecast segments.
    /// </summary>
    public static class HealthBarForecasts
    {
        /// <summary>
        ///     Starts a general-purpose sequence builder for <paramref name="context" />.
        /// </summary>
        public static HealthBarForecastSequenceBuilder For(HealthBarForecastContext context)
        {
            return new(context);
        }

        /// <summary>
        ///     Starts a right-growing forecast lane with a fixed <paramref name="color" />.
        /// </summary>
        public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color)
        {
            return FromRight(context, color, null);
        }

        /// <summary>
        ///     Starts a right-growing lane with separate optional <see cref="CanvasItem.SelfModulate" /> for the nine-patch
        ///     overlay (e.g. white when <see cref="Godot.Material" /> carries tint).
        /// </summary>
        /// <param name="context">Forecast context.</param>
        /// <param name="color">Lethal label color and fallback overlay modulate.</param>
        /// <param name="overlaySelfModulate">
        ///     When set, used as overlay <see cref="CanvasItem.SelfModulate" /> instead of
        ///     <paramref name="color" />.
        /// </param>
        public static HealthBarForecastLaneBuilder FromRight(
            HealthBarForecastContext context,
            Color color,
            Color? overlaySelfModulate)
        {
            return new(For(context), color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate);
        }

        /// <summary>
        ///     Starts a left-growing forecast lane with a fixed <paramref name="color" />.
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
            return new(For(context), color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate);
        }

        /// <summary>
        ///     Returns a single segment when <paramref name="amount" /> is positive, with optional material only.
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
        /// </summary>
        /// <param name="amount">HP chunk size.</param>
        /// <param name="color">Lethal label color and fallback modulate.</param>
        /// <param name="direction">Growth direction.</param>
        /// <param name="order">Sort order among segments.</param>
        /// <param name="overlayMaterial">Optional segment material.</param>
        /// <param name="overlaySelfModulate">When set, stored on <see cref="HealthBarForecastSegment.OverlaySelfModulate" />.</param>
        public static IEnumerable<HealthBarForecastSegment> Single(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            if (amount <= 0)
                return [];

            return [new(amount, color, direction, order, overlayMaterial, overlaySelfModulate)];
        }

        /// <summary>
        ///     Returns a single segment when <paramref name="amount" /> is positive, without a custom material.
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
    /// </summary>
    public sealed class HealthBarForecastSequenceBuilder(HealthBarForecastContext context)
    {
        private readonly List<HealthBarForecastSegment> _segments = [];

        /// <summary>
        ///     Forecast context associated with this sequence.
        /// </summary>
        public HealthBarForecastContext Context { get; } = context;

        /// <summary>
        ///     Appends a segment when <paramref name="amount" /> is positive.
        ///     Consecutive segments with identical color, direction, order, material reference, and overlay modulate are merged.
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
        /// </summary>
        /// <param name="amount">HP chunk size.</param>
        /// <param name="color">Lethal label color and fallback modulate.</param>
        /// <param name="direction">Growth direction.</param>
        /// <param name="order">Sort order among segments.</param>
        /// <param name="overlayMaterial">Optional segment material.</param>
        /// <param name="overlaySelfModulate">
        ///     Optional overlay <see cref="CanvasItem.SelfModulate" />; null uses <paramref name="color" />.
        /// </param>
        public HealthBarForecastSequenceBuilder Add(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            if (amount <= 0)
                return this;

            var segment =
                new HealthBarForecastSegment(amount, color, direction, order, overlayMaterial, overlaySelfModulate);
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
        /// </summary>
        /// <param name="amounts">HP chunk sizes.</param>
        /// <param name="color">Lethal label color and fallback modulate.</param>
        /// <param name="direction">Growth direction.</param>
        /// <param name="order">Sort order among segments.</param>
        /// <param name="overlayMaterial">Optional segment material.</param>
        /// <param name="overlaySelfModulate">Optional overlay <see cref="CanvasItem.SelfModulate" /> shared by chunks.</param>
        public HealthBarForecastSequenceBuilder AddRange(
            IEnumerable<int> amounts,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
        {
            ArgumentNullException.ThrowIfNull(amounts);

            foreach (var amount in amounts)
                Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate);

            return this;
        }

        /// <summary>
        ///     Appends all positive amounts as consecutive segments without a custom material.
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
        /// </summary>
        public HealthBarForecastLaneBuilder FromRight(Color color)
        {
            return FromRight(color, null);
        }

        /// <inheritdoc cref="HealthBarForecasts.FromRight(HealthBarForecastContext, Color, Color?)" />
        public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromRight, overlaySelfModulate);
        }

        /// <summary>
        ///     Creates a fixed-color left-growing lane on this sequence.
        /// </summary>
        public HealthBarForecastLaneBuilder FromLeft(Color color)
        {
            return FromLeft(color, null);
        }

        /// <inheritdoc cref="FromRight(Color, Color?)" />
        public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate)
        {
            return new(this, color, HealthBarForecastGrowthDirection.FromLeft, overlaySelfModulate);
        }

        /// <summary>
        ///     Returns the built sequence snapshot.
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
                   ReferenceEquals(left.OverlayMaterial, right.OverlayMaterial);
        }
    }

    /// <summary>
    ///     Convenience wrapper for the common case of one fixed-color forecast lane.
    /// </summary>
    /// <param name="sequence">Parent sequence builder.</param>
    /// <param name="color">Lane label / fallback modulate color.</param>
    /// <param name="direction">Growth edge for this lane.</param>
    /// <param name="overlaySelfModulate">When set, used as <see cref="CanvasItem.SelfModulate" /> for segments in this lane.</param>
    public sealed class HealthBarForecastLaneBuilder(
        HealthBarForecastSequenceBuilder sequence,
        Color color,
        HealthBarForecastGrowthDirection direction,
        Color? overlaySelfModulate = null)
    {
        /// <summary>
        ///     Parent sequence builder.
        /// </summary>
        public HealthBarForecastSequenceBuilder Sequence { get; } = sequence;

        /// <summary>
        ///     Appends a segment with explicit <paramref name="order" /> and optional <paramref name="overlayMaterial" />.
        /// </summary>
        public HealthBarForecastLaneBuilder Add(int amount, int order, Material? overlayMaterial)
        {
            Sequence.Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate);
            return this;
        }

        /// <summary>
        ///     Appends a segment without a custom material.
        /// </summary>
        public HealthBarForecastLaneBuilder Add(int amount, int order = 0)
        {
            return Add(amount, order, null);
        }

        /// <summary>
        ///     Appends multiple segments with the same <paramref name="order" /> and optional <paramref name="overlayMaterial" />.
        /// </summary>
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order, Material? overlayMaterial)
        {
            Sequence.AddRange(amounts, color, direction, order, overlayMaterial, overlaySelfModulate);
            return this;
        }

        /// <summary>
        ///     Appends multiple segments without a custom material.
        /// </summary>
        public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order = 0)
        {
            return AddRange(amounts, order, null);
        }

        /// <summary>
        ///     Appends segments that trigger at the start of <paramref name="triggerSide" />'s turn.
        /// </summary>
        public HealthBarForecastLaneBuilder AtSideTurnStart(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnStart(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate);
            return this;
        }

        /// <summary>
        ///     Appends segments that trigger at the end of <paramref name="triggerSide" />'s turn.
        /// </summary>
        public HealthBarForecastLaneBuilder AtSideTurnEnd(CombatSide triggerSide, params int[] amounts)
        {
            var order = HealthBarForecastOrder.ForSideTurnEnd(Sequence.Context.Creature, triggerSide);
            Sequence.AddRange(amounts, color, direction, order, null, overlaySelfModulate);
            return this;
        }

        /// <summary>
        ///     Starts another right-growing lane on the same parent sequence.
        /// </summary>
        public HealthBarForecastLaneBuilder ThenFromRight(Color nextColor)
        {
            return Sequence.FromRight(nextColor, null);
        }

        /// <summary>
        ///     Starts another left-growing lane on the same parent sequence.
        /// </summary>
        public HealthBarForecastLaneBuilder ThenFromLeft(Color nextColor)
        {
            return Sequence.FromLeft(nextColor, null);
        }

        /// <summary>
        ///     Returns the built segment snapshot.
        /// </summary>
        public IReadOnlyList<HealthBarForecastSegment> Build()
        {
            return Sequence.Build();
        }
    }
}
