using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the health-bar edge from which a forecast segment extends.</para>
    ///     <para xml:lang="zh-CN">指定生命条预测片段从哪一侧边缘延伸。</para>
    /// </summary>
    public enum HealthBarForecastGrowthDirection
    {
        /// <summary>
        ///     <para xml:lang="en">Extends inward from the current-HP edge, like Poison.</para>
        ///     <para xml:lang="zh-CN">从当前生命值边缘向内延伸，与“中毒”相同。</para>
        /// </summary>
        FromRight = 0,

        /// <summary>
        ///     <para xml:lang="en">Extends inward from the empty edge, like Doom.</para>
        ///     <para xml:lang="zh-CN">从空白侧边缘向内延伸，与“灾厄”相同。</para>
        /// </summary>
        FromLeft = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how <see cref="HealthBarForecastGrowthDirection.FromLeft" /> segments share the empty-edge
    ///         origin.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定 <see cref="HealthBarForecastGrowthDirection.FromLeft" /> 片段如何共享空白侧边缘的起点。
    ///     </para>
    /// </summary>
    public enum HealthBarForecastLeftOriginLayout
    {
        /// <summary>
        ///     <para xml:lang="en">Connects segments end to end from the empty edge.</para>
        ///     <para xml:lang="zh-CN">从空白侧边缘开始首尾相接地排列片段。</para>
        /// </summary>
        Chained = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts every segment at the empty edge and gives it its own amount-capped width. Longer segments are
        ///         drawn behind shorter ones; equal widths use segment order and then registration order, with later
        ///         entries on top.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每个片段都从空白侧边缘开始，并按自身数值计算不超过剩余生命值的宽度。较长片段绘制在较短片段
        ///         后方；宽度相同时依次按片段顺序和注册顺序排列，较晚的项位于上层。
        ///     </para>
        /// </summary>
        OverlapFromOrigin = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">Describes one forecast segment on a creature's health bar.</para>
    ///     <para xml:lang="zh-CN">描述生物生命条上的一个预测片段。</para>
    /// </summary>
    /// <param name="Amount">
    ///     <para xml:lang="en">The HP amount represented by the segment.</para>
    ///     <para xml:lang="zh-CN">该片段表示的生命值数值。</para>
    /// </param>
    /// <param name="Color">
    ///     <para xml:lang="en">
    ///         The lethal HP-label color. When <paramref name="OverlaySelfModulate" /> is <see langword="null" />, this
    ///         also becomes the forecast overlay's <see cref="CanvasItem.SelfModulate" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         预测致命时的生命值文本颜色。当 <paramref name="OverlaySelfModulate" /> 为
    ///         <see langword="null" /> 时，也会作为预测覆盖层的 <see cref="CanvasItem.SelfModulate" />。
    ///     </para>
    /// </param>
    /// <param name="Direction">
    ///     <para xml:lang="en">The edge from which the segment extends.</para>
    ///     <para xml:lang="zh-CN">片段延伸的起始边缘。</para>
    /// </param>
    /// <param name="Order">
    ///     <para xml:lang="en">
    ///         The primary render order. Lower values render earlier and remain closer to the selected origin edge.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         主要渲染顺序。数值较低的片段更早渲染，并更靠近所选的起始边缘。
    ///     </para>
    /// </param>
    /// <param name="OverlayMaterial">
    ///     <para xml:lang="en">
    ///         The optional Godot material. When <see langword="null" />, only the modulation color is applied.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的 Godot 材质。为 <see langword="null" /> 时只应用调制色。
    ///     </para>
    /// </param>
    /// <param name="OverlaySelfModulate">
    ///     <para xml:lang="en">
    ///         The optional overlay <see cref="CanvasItem.SelfModulate" />. This does not change the lethal HP-label
    ///         color supplied by <paramref name="Color" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         覆盖层可选的 <see cref="CanvasItem.SelfModulate" />。该值不会改变由
    ///         <paramref name="Color" /> 提供的致命生命值文本颜色。
    ///     </para>
    /// </param>
    /// <param name="LeftOriginLayout">
    ///     <para xml:lang="en">
    ///         The empty-edge layout for <see cref="HealthBarForecastGrowthDirection.FromLeft" /> segments. Ignored
    ///         for <see cref="HealthBarForecastGrowthDirection.FromRight" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="HealthBarForecastGrowthDirection.FromLeft" /> 片段使用的空白侧布局。
    ///         对 <see cref="HealthBarForecastGrowthDirection.FromRight" /> 片段忽略。
    ///     </para>
    /// </param>
    /// <param name="LeftExclusiveZGroup">
    ///     <para xml:lang="en">
    ///         The exclusive Z group for overlapping empty-edge segments. Larger groups draw above smaller groups.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         空白侧重叠片段使用的互斥 Z 组。数值较大的组绘制在数值较小的组之上。
    ///     </para>
    /// </param>
    /// <param name="AffectsHpLabel">
    ///     <para xml:lang="en">Whether this segment can recolor the HP label when its forecast becomes lethal.</para>
    ///     <para xml:lang="zh-CN">该片段的预测致命时是否可以改变生命值文本颜色。</para>
    /// </param>
    public readonly record struct HealthBarForecastSegment(
        int Amount,
        Color Color,
        HealthBarForecastGrowthDirection Direction,
        int Order,
        Material? OverlayMaterial,
        Color? OverlaySelfModulate = null,
        HealthBarForecastLeftOriginLayout LeftOriginLayout = HealthBarForecastLeftOriginLayout.Chained,
        int LeftExclusiveZGroup = 0,
        bool AffectsHpLabel = true)
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a segment without an overlay material or separate overlay color.</para>
        ///     <para xml:lang="zh-CN">初始化不带覆盖材质或独立覆盖层颜色的片段。</para>
        /// </summary>
        public HealthBarForecastSegment(int amount, Color color, HealthBarForecastGrowthDirection direction,
            int order = 0)
            : this(amount, color, direction, order, null, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a segment with an optional overlay material and the default overlay color.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始化带可选覆盖材质并使用默认覆盖层颜色的片段。</para>
        /// </summary>
        public HealthBarForecastSegment(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial)
            : this(amount, color, direction, order, overlayMaterial, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a segment with an optional overlay color and the default empty-edge layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始化带可选覆盖层颜色并使用默认空白侧布局的片段。</para>
        /// </summary>
        public HealthBarForecastSegment(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate)
            : this(amount, color, direction, order, overlayMaterial, overlaySelfModulate,
                HealthBarForecastLeftOriginLayout.Chained)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a segment with an empty-edge layout and the default exclusive Z group.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始化带空白侧布局并使用默认互斥 Z 组的片段。</para>
        /// </summary>
        public HealthBarForecastSegment(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate,
            HealthBarForecastLeftOriginLayout leftOriginLayout)
            : this(amount, color, direction, order, overlayMaterial, overlaySelfModulate, leftOriginLayout, 0)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a segment with an explicit empty-edge layout and exclusive Z group.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始化显式指定空白侧布局和互斥 Z 组的片段。</para>
        /// </summary>
        public HealthBarForecastSegment(
            int amount,
            Color color,
            HealthBarForecastGrowthDirection direction,
            int order,
            Material? overlayMaterial,
            Color? overlaySelfModulate,
            HealthBarForecastLeftOriginLayout leftOriginLayout,
            int leftExclusiveZGroup)
            : this(amount, color, direction, order, overlayMaterial, overlaySelfModulate, leftOriginLayout,
                leftExclusiveZGroup, true)
        {
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides forecast ordering keys relative to the active turn.</para>
    ///     <para xml:lang="zh-CN">提供相对于当前行动回合的预测排序键。</para>
    /// </summary>
    public static class HealthBarForecastOrder
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the ordering key for an effect that triggers at the start of <paramref name="triggerSide" />'s
        ///         turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在 <paramref name="triggerSide" /> 一方回合开始时触发的效果所用的排序键。
        ///     </para>
        /// </summary>
        /// <param name="creature">
        ///     <para xml:lang="en">The creature whose combat state determines the active side.</para>
        ///     <para xml:lang="zh-CN">使用其战斗状态判断当前行动方的生物。</para>
        /// </param>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-start effect is being ordered.</para>
        ///     <para xml:lang="zh-CN">待排序的回合开始效果所属的一方。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <c>1</c> while <paramref name="triggerSide" /> is active; otherwise, <c>0</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="triggerSide" /> 为当前行动方时返回 <c>1</c>；否则返回 <c>0</c>。
        ///     </para>
        /// </returns>
        public static int ForSideTurnStart(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 1 : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the ordering key for an effect that triggers at the end of <paramref name="triggerSide" />'s
        ///         turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在 <paramref name="triggerSide" /> 一方回合结束时触发的效果所用的排序键。
        ///     </para>
        /// </summary>
        /// <param name="creature">
        ///     <para xml:lang="en">The creature whose combat state determines the active side.</para>
        ///     <para xml:lang="zh-CN">使用其战斗状态判断当前行动方的生物。</para>
        /// </param>
        /// <param name="triggerSide">
        ///     <para xml:lang="en">The side whose turn-end effect is being ordered.</para>
        ///     <para xml:lang="zh-CN">待排序的回合结束效果所属的一方。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <c>0</c> while <paramref name="triggerSide" /> is active; otherwise, <c>1</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="triggerSide" /> 为当前行动方时返回 <c>0</c>；否则返回 <c>1</c>。
        ///     </para>
        /// </returns>
        public static int ForSideTurnEnd(Creature creature, CombatSide triggerSide)
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.CombatState?.CurrentSide == triggerSide ? 0 : 1;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides the global registry for mod-defined health-bar forecast sources.</para>
    ///     <para xml:lang="zh-CN">提供模组自定义生命条预测来源的全局注册表。</para>
    /// </summary>
    public static class HealthBarForecastRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<(string ModId, string ProviderId), ProviderEntry> Providers =
            new(HealthBarProviderKeyComparer.Instance);
        private static long _nextRegistrationOrder;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates and registers a forecast source, replacing the source with the same mod and source
        ///         identifiers.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建并注册预测来源，同时替换模组标识符和来源标识符均相同的来源。</para>
        /// </summary>
        /// <typeparam name="TSource">
        ///     <para xml:lang="en">
        ///         The concrete <see cref="IHealthBarForecastSource" /> type with a parameterless constructor.
        ///     </para>
        ///     <para xml:lang="zh-CN">带无参构造函数的具体 <see cref="IHealthBarForecastSource" /> 类型。</para>
        /// </typeparam>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod identifier. Surrounding whitespace is ignored.</para>
        ///     <para xml:lang="zh-CN">所属模组的标识符；忽略首尾空白。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">
        ///         The optional source identifier within the mod. Defaults to the source type's full name.
        ///     </para>
        ///     <para xml:lang="zh-CN">来源在该模组内的可选标识符；默认使用来源类型的完整名称。</para>
        /// </param>
        public static void Register<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a forecast source instance, replacing the source with the same mod and source identifiers.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册预测来源实例，同时替换模组标识符和来源标识符均相同的来源。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod identifier. Surrounding whitespace is ignored.</para>
        ///     <para xml:lang="zh-CN">所属模组的标识符；忽略首尾空白。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">The source identifier within the mod. Surrounding whitespace is ignored.</para>
        ///     <para xml:lang="zh-CN">来源在该模组内的标识符；忽略首尾空白。</para>
        /// </param>
        /// <param name="source">
        ///     <para xml:lang="en">The forecast source instance.</para>
        ///     <para xml:lang="zh-CN">预测来源实例。</para>
        /// </param>
        public static void Register(
            string modId,
            string sourceId,
            IHealthBarForecastSource source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
            ArgumentNullException.ThrowIfNull(source);

            var normalizedModId = modId.Trim();
            var normalizedSourceId = sourceId.Trim();
            lock (SyncRoot)
            {
                var key = (normalizedModId, normalizedSourceId);
                var registrationOrder = Providers.TryGetValue(key, out var existing)
                    ? existing.RegistrationOrder
                    : _nextRegistrationOrder++;

                Providers[key] = new(normalizedModId, normalizedSourceId, source, registrationOrder);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Removes a registered forecast source.</para>
        ///     <para xml:lang="zh-CN">移除已注册的预测来源。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The mod identifier used during registration. Surrounding whitespace is ignored.</para>
        ///     <para xml:lang="zh-CN">注册时使用的模组标识符；忽略首尾空白。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">
        ///         The source identifier used during registration. Surrounding whitespace is ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册时使用的来源标识符；忽略首尾空白。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if a source was removed; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">成功移除来源时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool Unregister(string modId, string sourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

            lock (SyncRoot)
            {
                return Providers.Remove((modId.Trim(), sourceId.Trim()));
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Collects segments from the creature's powers that implement <see cref="IHealthBarForecastSource" />
        ///         and from globally registered sources.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从该生物实现 <see cref="IHealthBarForecastSource" /> 的能力和全局注册来源中收集片段。
        ///     </para>
        /// </summary>
        /// <param name="creature">
        ///     <para xml:lang="en">The creature whose health bar is being evaluated.</para>
        ///     <para xml:lang="zh-CN">待评估生命条的生物。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The positive forecast segments in stable source order.</para>
        ///     <para xml:lang="zh-CN">按稳定来源顺序排列的正数预测片段。</para>
        /// </returns>
        internal static IReadOnlyList<RegisteredHealthBarForecastSegment> GetSegments(Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            var context = new HealthBarForecastContext(creature);
            List<RegisteredHealthBarForecastSegment> segments = [];

            var powerSequenceOrder = 0L;
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
                snapshot = [.. Providers.Values.OrderBy(entry => entry.RegistrationOrder)];
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
                var snapshot = (from segment in providedSegments
                    where segment.Amount > 0
                    select new RegisteredHealthBarForecastSegment(segment, sequenceOrder)).ToArray();
                segments.AddRange(snapshot);
            }
            catch (Exception ex)
            {
                var ownerText = modId == null ? "runtime source" : $"mod '{modId}'";
                RitsuLibFramework.Logger.Warn(
                    $"[HealthBarForecast] Source '{sourceId}' from {ownerText} failed for creature '{context.Creature}': {ex}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Associates a segment with the stable source key used when
        ///         <see cref="HealthBarForecastSegment.Order" /> values tie.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将片段与 <see cref="HealthBarForecastSegment.Order" /> 相同时使用的稳定来源排序键关联。
        ///     </para>
        /// </summary>
        /// <param name="Segment">
        ///     <para xml:lang="en">The forecast segment.</para>
        ///     <para xml:lang="zh-CN">预测片段。</para>
        /// </param>
        /// <param name="SequenceOrder">
        ///     <para xml:lang="en">
        ///         The monotonic source key, with creature powers ordered before globally registered sources.
        ///     </para>
        ///     <para xml:lang="zh-CN">单调递增的来源排序键；生物的能力排在全局注册来源之前。</para>
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

    internal sealed class HealthBarProviderKeyComparer :
        IEqualityComparer<(string ModId, string SourceId)>
    {
        internal static HealthBarProviderKeyComparer Instance { get; } = new();

        public bool Equals(
            (string ModId, string SourceId) x,
            (string ModId, string SourceId) y)
        {
            return string.Equals(x.ModId, y.ModId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.SourceId, y.SourceId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string ModId, string SourceId) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ModId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SourceId));
        }
    }
}
