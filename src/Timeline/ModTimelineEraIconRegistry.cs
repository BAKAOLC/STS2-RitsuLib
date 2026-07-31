using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     <para xml:lang="en">Registers timeline-axis icon policies by era.</para>
    ///     <para xml:lang="zh-CN">按时代注册时间线坐标轴图标策略。</para>
    /// </summary>
    public static class ModTimelineEraIconRegistry
    {
        private static readonly Lock Sync = new();
        private static readonly Dictionary<long, EraIconRule> RulesByEra = [];

        /// <summary>
        ///     <para xml:lang="en">Configures the icon policy for an <see cref="EpochEra" /> value.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="EpochEra" /> 值配置图标策略。</para>
        /// </summary>
        public static void Configure(EpochEra era, bool? enabled = null, string? texturePath = null)
        {
            Configure((long)era, enabled, texturePath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the icon policy for an era's integer value, including custom values absent from the enum.
        ///     </para>
        ///     <para xml:lang="zh-CN">为时代的整数值配置图标策略，也支持未在枚举中定义的自定义值。</para>
        /// </summary>
        public static void Configure(long eraValue, bool? enabled = null, string? texturePath = null)
        {
            ValidateEraValue(eraValue);

            lock (Sync)
            {
                RulesByEra[eraValue] = new(enabled,
                    string.IsNullOrWhiteSpace(texturePath) ? null : texturePath.Trim());
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Clears the icon policy for an <see cref="EpochEra" /> value.</para>
        ///     <para xml:lang="zh-CN">清除 <see cref="EpochEra" /> 值的图标策略。</para>
        /// </summary>
        public static void Clear(EpochEra era)
        {
            Clear((long)era);
        }

        /// <summary>
        ///     <para xml:lang="en">Clears the icon policy for an era's integer value.</para>
        ///     <para xml:lang="zh-CN">清除时代整数值的图标策略。</para>
        /// </summary>
        public static void Clear(long eraValue)
        {
            ValidateEraValue(eraValue);

            lock (Sync)
            {
                RulesByEra.Remove(eraValue);
            }
        }

        private static void ValidateEraValue(long eraValue)
        {
            if (eraValue is < int.MinValue or > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(eraValue),
                    eraValue,
                    $"{nameof(EpochEra)} uses 32-bit integer values.");
        }

        internal static bool TryResolve(EpochEra era, out bool? enabled, out string? texturePath)
        {
            lock (Sync)
            {
                if (!RulesByEra.TryGetValue((long)era, out var rule))
                {
                    enabled = null;
                    texturePath = null;
                    return false;
                }

                enabled = rule.Enabled;
                texturePath = rule.TexturePath;
                return true;
            }
        }

        private readonly record struct EraIconRule(bool? Enabled, string? TexturePath);
    }
}
