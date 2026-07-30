using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Per-era policy registry for timeline axis icons.
    ///     时间线轴图标的按 era 策略注册表。
    /// </summary>
    public static class ModTimelineEraIconRegistry
    {
        private static readonly Lock Sync = new();
        private static readonly Dictionary<long, EraIconRule> RulesByEra = [];

        /// <summary>
        ///     Configures icon policy for a concrete <see cref="EpochEra" /> value.
        ///     为具体 <see cref="EpochEra" /> 值配置图标策略。
        /// </summary>
        public static void Configure(EpochEra era, bool? enabled = null, string? texturePath = null)
        {
            Configure((long)era, enabled, texturePath);
        }

        /// <summary>
        ///     Configures icon policy for an era integer value (supports custom/non-enum eras).
        ///     为 era 整数值配置图标策略（支持自定义/非枚举 era）。
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
        ///     Clears icon policy for a concrete <see cref="EpochEra" /> value.
        ///     清除具体 <see cref="EpochEra" /> 值的图标策略。
        /// </summary>
        public static void Clear(EpochEra era)
        {
            Clear((long)era);
        }

        /// <summary>
        ///     Clears icon policy for an era integer value.
        ///     清除 era 整数值的图标策略。
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
