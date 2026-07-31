using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     <para xml:lang="en">Provides the creature used to resolve visual health-bar extension metrics.</para>
    ///     <para xml:lang="zh-CN">提供用于解析生命条视觉扩展参数的生物。</para>
    /// </summary>
    /// <param name="Creature">
    ///     <para xml:lang="en">The creature whose health bar is being evaluated.</para>
    ///     <para xml:lang="zh-CN">正在评估生命条的生物。</para>
    /// </param>
    public readonly record struct HealthBarVisualGraftContext(Creature Creature);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes the visual HP length and appearance appended to the current-HP edge for health-bar geometry
    ///         and right-origin forecasts.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述附加到当前生命值边缘的视觉生命值长度和外观，用于生命条布局和右侧起点预测。
    ///     </para>
    /// </summary>
    /// <param name="GraftHp">
    ///     <para xml:lang="en">The additional visual HP units drawn beyond the current-HP edge.</para>
    ///     <para xml:lang="zh-CN">在当前生命值边缘之外绘制的额外视觉生命值单位。</para>
    /// </param>
    /// <param name="GraftSelfModulate">
    ///     <para xml:lang="en">
    ///         The optional modulation color for the extension strip; <see langword="null" /> uses the default color.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         扩展条的可选调制色；为 <see langword="null" /> 时使用默认颜色。
    ///     </para>
    /// </param>
    /// <param name="GraftMaterial">
    ///     <para xml:lang="en">The optional material for the extension strip.</para>
    ///     <para xml:lang="zh-CN">扩展条的可选材质。</para>
    /// </param>
    public readonly record struct HealthBarVisualGraftMetrics(
        int GraftHp,
        Color? GraftSelfModulate,
        Material? GraftMaterial)
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes metrics without a custom color or material.</para>
        ///     <para xml:lang="zh-CN">初始化不带自定义颜色或材质的参数。</para>
        /// </summary>
        /// <param name="graftHp">
        ///     <para xml:lang="en">The additional visual HP units.</para>
        ///     <para xml:lang="zh-CN">额外视觉生命值单位。</para>
        /// </param>
        public HealthBarVisualGraftMetrics(int graftHp)
            : this(graftHp, null, null)
        {
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Supplies visual health-bar extension metrics for a creature.</para>
    ///     <para xml:lang="zh-CN">为生物提供生命条视觉扩展参数。</para>
    /// </summary>
    public interface IHealthBarVisualGraftSource
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets extension metrics for <paramref name="context" />. Return zero
        ///         <see cref="HealthBarVisualGraftMetrics.GraftHp" /> when no extension applies.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="context" /> 的扩展参数。没有适用的扩展时，应令
        ///         <see cref="HealthBarVisualGraftMetrics.GraftHp" /> 为零。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The creature being rendered.</para>
        ///     <para xml:lang="zh-CN">正在渲染的生物上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The visual extension metrics.</para>
        ///     <para xml:lang="zh-CN">视觉扩展参数。</para>
        /// </returns>
        HealthBarVisualGraftMetrics GetHealthBarVisualGraft(HealthBarVisualGraftContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers and aggregates visual health-bar extension sources from mods and creature powers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册并汇总来自模组和生物能力的生命条视觉扩展来源。
    ///     </para>
    /// </summary>
    public static class HealthBarVisualGraftRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<(string ModId, string SourceId), ProviderEntry> Providers =
            new(HealthBarProviderKeyComparer.Instance);

        private static long _nextRegistrationOrder;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces a visual extension source implemented by <typeparamref name="TSource" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册或替换由 <typeparamref name="TSource" /> 实现的视觉扩展来源。
        ///     </para>
        /// </summary>
        /// <typeparam name="TSource">
        ///     <para xml:lang="en">The source type with a parameterless constructor.</para>
        ///     <para xml:lang="zh-CN">具有无参构造函数的来源类型。</para>
        /// </typeparam>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">The optional mod-local source ID; defaults to the source type name.</para>
        ///     <para xml:lang="zh-CN">可选的模组内来源 ID；默认使用来源类型名称。</para>
        /// </param>
        public static void Register<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarVisualGraftSource, new()
        {
            Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
        }

        /// <summary>
        ///     <para xml:lang="en">Registers or replaces a visual extension source instance.</para>
        ///     <para xml:lang="zh-CN">注册或替换视觉扩展来源实例。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">The source ID within the mod.</para>
        ///     <para xml:lang="zh-CN">该来源在模组内的 ID。</para>
        /// </param>
        /// <param name="source">
        ///     <para xml:lang="en">The source instance.</para>
        ///     <para xml:lang="zh-CN">来源实例。</para>
        /// </param>
        public static void Register(string modId, string sourceId, IHealthBarVisualGraftSource source)
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
        ///     <para xml:lang="en">Removes a previously registered visual extension source.</para>
        ///     <para xml:lang="zh-CN">移除先前注册的视觉扩展来源。</para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The mod ID used at registration.</para>
        ///     <para xml:lang="zh-CN">注册时使用的模组 ID。</para>
        /// </param>
        /// <param name="sourceId">
        ///     <para xml:lang="en">The source ID used at registration.</para>
        ///     <para xml:lang="zh-CN">注册时使用的来源 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when a source was removed.</para>
        ///     <para xml:lang="zh-CN">移除来源时为 <see langword="true" />。</para>
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
        ///         Sums positive extension HP from powers and registered sources with saturation at
        ///         <see cref="int.MaxValue" />. The first non-null modulation color and material win independently,
        ///         even when supplied by a source whose HP contribution is not positive.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         汇总能力和已注册来源提供的正数扩展生命值，并在 <see cref="int.MaxValue" /> 处饱和。
        ///         最先出现的非空调制色和材质分别生效，即使提供外观的来源没有提供正数生命值。
        ///     </para>
        /// </summary>
        internal static HealthBarVisualGraftMetrics Aggregate(Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            var sumHp = 0;
            Color? color = null;
            Material? material = null;
            var context = new HealthBarVisualGraftContext(creature);

            foreach (var source in creature.Powers.OfType<IHealthBarVisualGraftSource>())
                try
                {
                    Merge(source.GetHealthBarVisualGraft(context));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[HealthBarGraft] Power '{source.GetType().FullName}' graft failed for '{creature}': {ex}");
                }

            ProviderEntry[] snapshot;
            lock (SyncRoot)
            {
                snapshot = [.. Providers.Values.OrderBy(e => e.RegistrationOrder)];
            }

            foreach (var entry in snapshot)
                try
                {
                    Merge(entry.Source.GetHealthBarVisualGraft(context));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[HealthBarGraft] Source '{entry.SourceId}' from mod '{entry.ModId}' failed for '{creature}': {ex}");
                }

            return new(sumHp, color, material);

            void Merge(HealthBarVisualGraftMetrics metrics)
            {
                sumHp = (int)Math.Min(int.MaxValue, (long)sumHp + Math.Max(0, metrics.GraftHp));
                color ??= metrics.GraftSelfModulate;
                material ??= metrics.GraftMaterial;
            }
        }

        private readonly record struct ProviderEntry(
            string ModId,
            string SourceId,
            IHealthBarVisualGraftSource Source,
            long RegistrationOrder);
    }
}
