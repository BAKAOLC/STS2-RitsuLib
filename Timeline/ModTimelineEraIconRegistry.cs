using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Per-era policy registry for timeline axis icons.
    /// </summary>
    public static class ModTimelineEraIconRegistry
    {
        private static readonly Lock Sync = new();
        private static readonly Dictionary<long, EraIconRule> RulesByEra = [];

        /// <summary>
        ///     Configures icon policy for a concrete <see cref="EpochEra" /> value.
        /// </summary>
        public static void Configure(EpochEra era, bool? enabled = null, string? texturePath = null)
        {
            Configure((long)era, enabled, texturePath);
        }

        /// <summary>
        ///     Configures icon policy for an era integer value (supports custom/non-enum eras).
        /// </summary>
        public static void Configure(long eraValue, bool? enabled = null, string? texturePath = null)
        {
            lock (Sync)
            {
                RulesByEra[eraValue] = new(enabled, texturePath);
            }
        }

        /// <summary>
        ///     Clears icon policy for a concrete <see cref="EpochEra" /> value.
        /// </summary>
        public static void Clear(EpochEra era)
        {
            Clear((long)era);
        }

        /// <summary>
        ///     Clears icon policy for an era integer value.
        /// </summary>
        public static void Clear(long eraValue)
        {
            lock (Sync)
            {
                RulesByEra.Remove(eraValue);
            }
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
