using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Which side of the health bar a forecast segment should grow from.
    /// </summary>
    public enum HealthBarForecastGrowthDirection
    {
        /// <summary>
        ///     Grows inward from the current HP edge, like poison.
        /// </summary>
        FromRight = 0,

        /// <summary>
        ///     Grows outward from the empty side, like doom.
        /// </summary>
        FromLeft = 1,
    }

    /// <summary>
    ///     One forecast overlay segment for a creature health bar.
    /// </summary>
    /// <param name="Amount">HP amount represented by this segment.</param>
    /// <param name="Color">
    ///     Lethal HP label theming; also used as the forecast nine-patch <see cref="CanvasItem.SelfModulate" /> when
    ///     <see cref="OverlaySelfModulate" /> is null.
    /// </param>
    /// <param name="Direction">Which edge the segment grows from.</param>
    /// <param name="Order">
    ///     Lower values are rendered earlier in the chain.
    ///     For <see cref="HealthBarForecastGrowthDirection.FromRight" />, earlier segments stay closer to the current HP
    ///     edge; for <see cref="HealthBarForecastGrowthDirection.FromLeft" />, earlier segments stay closer to the empty
    ///     edge.
    /// </param>
    /// <param name="OverlayMaterial">
    ///     Optional Godot material (e.g. shader like vanilla doom). When null, only <see cref="Color" /> tint applies.
    /// </param>
    /// <param name="OverlaySelfModulate">
    ///     Optional <see cref="CanvasItem.SelfModulate" /> for the forecast nine-patch. When null, <see cref="Color" /> is
    ///     used
    ///     for both overlay tint and lethal HP label; when set, <see cref="Color" /> is still used for lethal label theming.
    /// </param>
    public readonly record struct HealthBarForecastSegment(
        int Amount,
        Color Color,
        HealthBarForecastGrowthDirection Direction,
        int Order,
        Material? OverlayMaterial,
        Color? OverlaySelfModulate = null)
    {
        /// <summary>
        ///     Initializes a segment without overlay material or separate overlay modulate.
        /// </summary>
        public HealthBarForecastSegment(int amount, Color color, HealthBarForecastGrowthDirection direction,
            int order = 0)
            : this(amount, color, direction, order, null, null)
        {
        }

        /// <summary>
        ///     Initializes a segment with an optional <see cref="OverlayMaterial" /> and default overlay modulate.
        /// </summary>
        // ReSharper disable once RedundantOverload.Global
        public HealthBarForecastSegment(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
            : this(amount, color, direction, order, overlayMaterial, null)
        {
        }
    }

    /// <summary>
    ///     Helpers for common turn-relative ordering of forecast segments.
    /// </summary>
    public static class HealthBarForecastOrder
    {
        /// <summary>
        ///     Returns an order key for effects that trigger at the start of <paramref name="triggerSide" />'s turn.
        /// </summary>
        public static int ForSideTurnStart(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 1 : 0;
        }

        /// <summary>
        ///     Returns an order key for effects that trigger at the end of <paramref name="triggerSide" />'s turn.
        /// </summary>
        public static int ForSideTurnEnd(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 0 : 1;
        }
    }

    /// <summary>
    ///     Global registry of health bar forecast providers contributed by mods.
    /// </summary>
    public static class HealthBarForecastRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<(string ModId, string ProviderId), ProviderEntry> Providers = [];
        private static long _nextRegistrationOrder;

        /// <summary>
        ///     Registers or replaces a forecast provider for <paramref name="modId" />.
        /// </summary>
        /// <typeparam name="TSource">Concrete <see cref="IHealthBarForecastSource" /> with a parameterless constructor.</typeparam>
        /// <param name="modId">Owning mod identifier.</param>
        /// <param name="sourceId">Optional unique id; defaults to the type full name.</param>
        public static void Register<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
        }

        /// <summary>
        ///     Registers or replaces a forecast source instance for <paramref name="modId" />.
        /// </summary>
        /// <param name="modId">Owning mod identifier.</param>
        /// <param name="sourceId">Unique id for this source within the mod.</param>
        /// <param name="source">Provider instance.</param>
        public static void Register(
            string modId,
            string sourceId,
            IHealthBarForecastSource source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
            ArgumentNullException.ThrowIfNull(source);

            lock (SyncRoot)
            {
                var key = (modId, sourceId);
                var registrationOrder = Providers.TryGetValue(key, out var existing)
                    ? existing.RegistrationOrder
                    : _nextRegistrationOrder++;

                Providers[key] = new(modId, sourceId, source, registrationOrder);
            }
        }

        /// <summary>
        ///     Removes a previously registered provider.
        /// </summary>
        /// <param name="modId">Mod identifier used at registration.</param>
        /// <param name="sourceId">Source id used at registration.</param>
        /// <returns><see langword="true" /> if an entry was removed.</returns>
        public static bool Unregister(string modId, string sourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

            lock (SyncRoot)
            {
                return Providers.Remove((modId, sourceId));
            }
        }

        /// <summary>
        ///     Collects segments from powers implementing <see cref="IHealthBarForecastSource" /> and registered providers.
        /// </summary>
        /// <param name="creature">Creature whose bar is being evaluated.</param>
        internal static IReadOnlyList<RegisteredHealthBarForecastSegment> GetSegments(Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            var context = new HealthBarForecastContext(creature);
            List<RegisteredHealthBarForecastSegment> segments = [];

            var powerSequenceOrder = 0L;
            // ReSharper disable once SuspiciousTypeConversion.Global
            foreach (var source in creature.Powers.OfType<IHealthBarForecastSource>())
                AppendSegments(
                    source,
                    source.GetType().FullName ?? source.GetType().Name,
                    context,
                    powerSequenceOrder++,
                    segments);

            ProviderEntry[] snapshot;
            lock (SyncRoot)
            {
                snapshot = Providers.Values.OrderBy(entry => entry.RegistrationOrder).ToArray();
            }

            const long externalSourceOrderOffset = 1_000_000L;
            foreach (var entry in snapshot)
                AppendSegments(
                    entry.Source,
                    entry.SourceId,
                    context,
                    externalSourceOrderOffset + entry.RegistrationOrder,
                    segments,
                    entry.ModId);

            return segments;
        }

        private static void AppendSegments(
            IHealthBarForecastSource source,
            string sourceId,
            HealthBarForecastContext context,
            long sequenceOrder,
            List<RegisteredHealthBarForecastSegment> segments,
            string? modId = null)
        {
            try
            {
                var providedSegments = source.GetHealthBarForecastSegments(context);
                segments.AddRange(from segment in providedSegments
                    where segment.Amount > 0
                    select new RegisteredHealthBarForecastSegment(segment, sequenceOrder));
            }
            catch (Exception ex)
            {
                var ownerText = modId == null ? "runtime source" : $"mod '{modId}'";
                RitsuLibFramework.Logger.Warn(
                    $"[HealthBarForecast] Source '{sourceId}' from {ownerText} failed for creature '{context.Creature}': {ex}");
            }
        }

        /// <summary>
        ///     Segment plus a sequence key for stable ordering when <see cref="HealthBarForecastSegment.Order" /> ties.
        /// </summary>
        /// <param name="Segment">Forecast data.</param>
        /// <param name="SequenceOrder">Monotonic key (powers first, then registered sources).</param>
        internal readonly record struct RegisteredHealthBarForecastSegment(
            HealthBarForecastSegment Segment,
            long SequenceOrder);

        private readonly record struct ProviderEntry(
            string ModId,
            string SourceId,
            IHealthBarForecastSource Source,
            long RegistrationOrder);
    }
}
