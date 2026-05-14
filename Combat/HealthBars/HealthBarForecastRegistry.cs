using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Which side of the health bar a forecast segment should grow from.
    ///     forecast 片段应从生命条的哪一侧增长。
    /// </summary>
    public enum HealthBarForecastGrowthDirection
    {
        /// <summary>
        ///     Grows inward from the current HP edge, like poison.
        ///     从当前 HP 边缘向内增长，类似 poison。
        /// </summary>
        FromRight = 0,

        /// <summary>
        ///     Grows outward from the empty side, like doom.
        ///     从空白侧向外增长，类似 doom。
        /// </summary>
        FromLeft = 1,
    }

    /// <summary>
    ///     How <see cref="HealthBarForecastGrowthDirection.FromLeft" /> segments share the empty-edge origin.
    ///     <see cref="HealthBarForecastGrowthDirection.FromLeft" /> 片段如何共享空白边缘原点。
    /// </summary>
    public enum HealthBarForecastLeftOriginLayout
    {
        /// <summary>
        ///     Segments connect end-to-end from the empty edge (legacy layout).
        ///     片段从空白边缘首尾相接（旧版布局）。
        /// </summary>
        Chained = 0,

        /// <summary>
        ///     Each segment spans from the empty edge by its own <c>Amount</c> (capped to remaining HP). Larger
        ///     <c>Amount</c> is drawn behind; smaller in front. Equal widths rotate front/back on a timer.
        ///     每个片段从空白边缘开始，按自身 <c>Amount</c> 跨越（限制为剩余 HP）。较大的
        ///     <c>Amount</c> 绘制在后方；较小的绘制在前方。宽度相等时按计时器轮换前后顺序。
        /// </summary>
        OverlapFromOrigin = 1,
    }

    /// <summary>
    ///     One forecast overlay segment for a creature health bar.
    ///     生物生命条上的一个 forecast 覆盖片段。
    /// </summary>
    /// <param name="Amount">
    ///     HP amount represented by this segment.
    ///     此片段表示的 HP 数量。
    /// </param>
    /// <param name="Color">
    ///     Lethal HP label theming; also used as the forecast nine-patch <see cref="CanvasItem.SelfModulate" /> when
    ///     <see cref="OverlaySelfModulate" /> is null.
    ///     致命 HP 标签主题色；当 <see cref="OverlaySelfModulate" /> 为 null 时，也用作 forecast 九宫格
    ///     <see cref="CanvasItem.SelfModulate" />。
    /// </param>
    /// <param name="Direction">
    ///     Which edge the segment grows from.
    ///     片段从哪条边增长。
    /// </param>
    /// <param name="Order">
    ///     Lower values are rendered earlier in the chain.
    ///     For <see cref="HealthBarForecastGrowthDirection.FromRight" />, earlier segments stay closer to the current HP
    ///     edge; for <see cref="HealthBarForecastGrowthDirection.FromLeft" />, earlier segments stay closer to the empty
    ///     edge.
    ///     数值较低者在链中更早渲染。
    ///     对 <see cref="HealthBarForecastGrowthDirection.FromRight" />，较早片段更靠近当前 HP
    ///     边缘；对 <see cref="HealthBarForecastGrowthDirection.FromLeft" />，较早片段更靠近空白
    ///     边缘。
    /// </param>
    /// <param name="OverlayMaterial">
    ///     Optional Godot material (e.g. shader like vanilla doom). When null, only <see cref="Color" /> tint applies.
    ///     可选 Godot material（例如类似原版 doom 的 shader）。为 null 时仅应用 <see cref="Color" /> 染色。
    /// </param>
    /// <param name="OverlaySelfModulate">
    ///     Optional <see cref="CanvasItem.SelfModulate" /> for the forecast nine-patch. When null, <see cref="Color" /> is
    ///     used
    ///     used
    ///     for both overlay tint and lethal HP label; when set, <see cref="Color" /> is still used for lethal label theming.
    ///     forecast 九宫格的可选 <see cref="CanvasItem.SelfModulate" />。为 null 时，<see cref="Color" /> 会
    ///     用于
    ///     用于
    ///     覆盖层染色和致命 HP 标签；设置后，<see cref="Color" /> 仍用于致命标签主题。
    /// </param>
    /// <param name="LeftOriginLayout">
    ///     For <see cref="HealthBarForecastGrowthDirection.FromLeft" /> only:
    ///     <see cref="HealthBarForecastLeftOriginLayout.Chained" />
    ///     or <see cref="HealthBarForecastLeftOriginLayout.OverlapFromOrigin" />. Ignored for
    ///     <see cref="HealthBarForecastGrowthDirection.FromRight" />.
    ///     仅用于 <see cref="HealthBarForecastGrowthDirection.FromLeft" />：
    ///     <see cref="HealthBarForecastLeftOriginLayout.Chained" />
    ///     或 <see cref="HealthBarForecastLeftOriginLayout.OverlapFromOrigin" />。对
    ///     <see cref="HealthBarForecastGrowthDirection.FromRight" /> 忽略。
    /// </param>
    /// <param name="LeftExclusiveZGroup">
    ///     For <see cref="HealthBarForecastLeftOriginLayout.OverlapFromOrigin" />: larger values draw above smaller values.
    ///     Within the same group, longer strips sit behind shorter strips; equal widths rotate.
    ///     对 <see cref="HealthBarForecastLeftOriginLayout.OverlapFromOrigin" />：较大值绘制在较小值上方。
    ///     同一组内，较长条位于较短条后方；宽度相等时轮换。
    /// </param>
    public readonly record struct HealthBarForecastSegment(
        int Amount,
        Color Color,
        HealthBarForecastGrowthDirection Direction,
        int Order,
        Material? OverlayMaterial,
        Color? OverlaySelfModulate = null,
        HealthBarForecastLeftOriginLayout LeftOriginLayout = HealthBarForecastLeftOriginLayout.Chained,
        int LeftExclusiveZGroup = 0)
    {
        /// <summary>
        ///     Initializes a segment without overlay material or separate overlay modulate.
        ///     初始化一个没有覆盖 material 或单独覆盖 modulate 的片段。
        /// </summary>
        public HealthBarForecastSegment(int amount, Color color, HealthBarForecastGrowthDirection direction,
            int order = 0)
            : this(amount, color, direction, order, null, null)
        {
        }

        /// <summary>
        ///     Initializes a segment with an optional <see cref="OverlayMaterial" /> and default overlay modulate.
        ///     初始化一个带可选 <see cref="OverlayMaterial" /> 和默认覆盖 modulate 的片段。
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
    ///     常见回合相对 forecast 片段排序的辅助方法。
    /// </summary>
    public static class HealthBarForecastOrder
    {
        /// <summary>
        ///     Returns an order key for effects that trigger at the start of <paramref name="triggerSide" />'s turn.
        ///     返回在 <paramref name="triggerSide" /> 回合开始时触发的效果所用的 order key。
        /// </summary>
        public static int ForSideTurnStart(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 1 : 0;
        }

        /// <summary>
        ///     Returns an order key for effects that trigger at the end of <paramref name="triggerSide" />'s turn.
        ///     返回在 <paramref name="triggerSide" /> 回合结束时触发的效果所用的 order key。
        /// </summary>
        public static int ForSideTurnEnd(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 0 : 1;
        }
    }

    /// <summary>
    ///     Global registry of health bar forecast providers contributed by mods.
    ///     由 mod 提供的生命条 forecast provider 全局注册表。
    /// </summary>
    public static class HealthBarForecastRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<(string ModId, string ProviderId), ProviderEntry> Providers = [];
        private static long _nextRegistrationOrder;

        /// <summary>
        ///     Registers or replaces a forecast provider for <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 注册或替换 forecast provider。
        /// </summary>
        /// <typeparam name="TSource">
        ///     Concrete <see cref="IHealthBarForecastSource" /> with a parameterless constructor.
        ///     带无参构造函数的具体 <see cref="IHealthBarForecastSource" />。
        /// </typeparam>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 mod 标识符。
        /// </param>
        /// <param name="sourceId">
        ///     Optional unique id; defaults to the type full name.
        ///     可选唯一 id；默认使用类型全名。
        /// </param>
        public static void Register<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
        }

        /// <summary>
        ///     Registers or replaces a forecast source instance for <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 注册或替换 forecast source 实例。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 mod 标识符。
        /// </param>
        /// <param name="sourceId">
        ///     Unique id for this source within the mod.
        ///     此 source 在 mod 内的唯一 id。
        /// </param>
        /// <param name="source">
        ///     Provider instance.
        ///     Provider 实例。
        /// </param>
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
        ///     移除先前注册的 provider。
        /// </summary>
        /// <param name="modId">
        ///     Mod identifier used at registration.
        ///     注册时使用的 mod 标识符。
        /// </param>
        /// <param name="sourceId">
        ///     Source id used at registration.
        ///     注册时使用的 source id。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an entry was removed.
        ///     如果移除了条目，则为 <see langword="true" />。
        /// </returns>
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
        ///     从实现 <see cref="IHealthBarForecastSource" /> 的能力和已注册 provider 收集片段。
        /// </summary>
        /// <param name="creature">
        ///     Creature whose bar is being evaluated.
        ///     正在评估生命条的生物。
        /// </param>
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
        ///     片段加序列 key，用于在 <see cref="HealthBarForecastSegment.Order" /> 相同时稳定排序。
        /// </summary>
        /// <param name="Segment">
        ///     Forecast data.
        ///     Forecast 数据。
        /// </param>
        /// <param name="SequenceOrder">
        ///     Monotonic key (powers first, then registered sources).
        ///     单调 key（先能力，后注册 source）。
        /// </param>
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
