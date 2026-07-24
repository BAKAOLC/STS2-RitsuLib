using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Runtime context for resolving visual graft metrics on a creature health bar.
    ///     用于解析生物生命条上 visual graft 指标的运行时上下文。
    /// </summary>
    /// <param name="Creature">
    ///     Creature whose bar is being evaluated.
    ///     正在评估生命条的生物。
    /// </param>
    public readonly record struct HealthBarVisualGraftContext(Creature Creature);

    /// <summary>
    ///     Extra HP-length grafted onto the right end of the current HP fill for bar geometry and right-side forecasts.
    ///     嫁接到当前 HP 填充右端的额外 HP 长度，用于生命条几何和右侧 forecast。
    /// </summary>
    /// <param name="GraftHp">
    ///     Additional HP units drawn past the current HP edge along the bar.
    ///     沿生命条绘制到当前 HP 边缘之外的额外 HP 单位。
    /// </param>
    /// <param name="GraftSelfModulate">
    ///     Optional tint for the graft strip; null uses a default extension color.
    ///     graft 条的可选染色；null 使用默认扩展颜色。
    /// </param>
    /// <param name="GraftMaterial">
    ///     Optional material for the graft strip.
    ///     graft 条的可选 material。
    /// </param>
    public readonly record struct HealthBarVisualGraftMetrics(
        int GraftHp,
        Color? GraftSelfModulate,
        Material? GraftMaterial)
    {
        /// <summary>
        ///     Initializes metrics with no custom appearance.
        ///     初始化没有自定义外观的指标。
        /// </summary>
        public HealthBarVisualGraftMetrics(int graftHp)
            : this(graftHp, null, null)
        {
        }
    }

    /// <summary>
    ///     Supplies visual graft metrics for a creature (temporary HP bar extension, etc.).
    ///     为生物提供 visual graft 指标（临时 HP 条扩展等）。
    /// </summary>
    public interface IHealthBarVisualGraftSource
    {
        /// <summary>
        ///     Returns graft metrics for <paramref name="context" />; yield zero
        ///     <see cref="HealthBarVisualGraftMetrics.GraftHp" />
        ///     when none apply.
        ///     返回 <paramref name="context" /> 的 graft 指标；不适用时产生零
        ///     <see cref="HealthBarVisualGraftMetrics.GraftHp" />。
        /// </summary>
        HealthBarVisualGraftMetrics GetHealthBarVisualGraft(HealthBarVisualGraftContext context);
    }

    /// <summary>
    ///     Aggregates graft metrics from creature powers and registered providers.
    ///     汇总来自生物能力和已注册 provider 的 graft 指标。
    /// </summary>
    public static class HealthBarVisualGraftRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<(string ModId, string SourceId), ProviderEntry> Providers = [];
        private static long _nextRegistrationOrder;

        /// <summary>
        ///     Registers or replaces a graft source implemented by <typeparamref name="TSource" />.
        ///     注册或替换由 <typeparamref name="TSource" /> 实现的 graft source。
        /// </summary>
        public static void Register<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarVisualGraftSource, new()
        {
            Register(modId, sourceId ?? typeof(TSource).FullName ?? typeof(TSource).Name, new TSource());
        }

        /// <summary>
        ///     Registers or replaces a graft source instance.
        ///     注册或替换 graft source 实例。
        /// </summary>
        public static void Register(string modId, string sourceId, IHealthBarVisualGraftSource source)
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
        ///     Removes a previously registered graft source.
        ///     移除先前注册的 graft source。
        /// </summary>
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
        ///     Sums graft HP from powers and registered providers; first non-null appearance wins for tint/material.
        ///     汇总来自能力和已注册 provider 的 graft HP；第一个非 null 外观决定 tint/material。
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
                    var m = source.GetHealthBarVisualGraft(context);
                    sumHp += Math.Max(0, m.GraftHp);
                    color ??= m.GraftSelfModulate;
                    material ??= m.GraftMaterial;
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
                    var m = entry.Source.GetHealthBarVisualGraft(context);
                    sumHp += Math.Max(0, m.GraftHp);
                    color ??= m.GraftSelfModulate;
                    material ??= m.GraftMaterial;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[HealthBarGraft] Source '{entry.SourceId}' from mod '{entry.ModId}' failed for '{creature}': {ex}");
                }

            return new(sumHp, color, material);
        }

        private readonly record struct ProviderEntry(
            string ModId,
            string SourceId,
            IHealthBarVisualGraftSource Source,
            long RegistrationOrder);
    }
}
