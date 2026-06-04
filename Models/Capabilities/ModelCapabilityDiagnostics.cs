using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Controls optional model capability diagnostic logging.
    ///     控制可选的模型能力诊断日志。
    /// </summary>
    public enum ModelCapabilityConflictLogMode
    {
        /// <summary>
        ///     Do not log capability surface conflicts.
        ///     不记录能力 surface 冲突。
        /// </summary>
        Off,

        /// <summary>
        ///     Log each distinct conflict once.
        ///     每个不同冲突只记录一次。
        /// </summary>
        WarnOnce,

        /// <summary>
        ///     Log every observed conflict.
        ///     每次观察到冲突都记录。
        /// </summary>
        WarnEveryTime,
    }

    /// <summary>
    ///     Runtime diagnostics for model capabilities.
    ///     模型能力运行时诊断。
    /// </summary>
    public static class ModelCapabilityDiagnostics
    {
        private static readonly Lock ConflictGate = new();
        private static readonly HashSet<string> SeenConflicts = new(StringComparer.Ordinal);

        /// <summary>
        ///     Optional conflict logging for single-winner surfaces such as card type, rarity, target type, and
        ///     result pile. Defaults to <see cref="ModelCapabilityConflictLogMode.Off" /> to avoid hot-path log spam.
        ///     单一胜者 surface 的可选冲突日志；默认为关闭，避免热路径刷屏。
        /// </summary>
        public static ModelCapabilityConflictLogMode ConflictLogs { get; set; } =
            ModelCapabilityConflictLogMode.Off;

        internal static bool ShouldInspectConflicts => ConflictLogs != ModelCapabilityConflictLogMode.Off;

        /// <summary>
        ///     Clears the warn-once conflict cache.
        ///     清空 warn-once 冲突缓存。
        /// </summary>
        public static void ClearConflictLogCache()
        {
            lock (ConflictGate)
            {
                SeenConflicts.Clear();
            }
        }

        internal static void WarnFailure(
            string surface,
            AbstractModel model,
            object source,
            Exception exception)
        {
            RitsuLibFramework.Logger.Warn(
                $"[ModelCapabilities] Surface='{surface}' failed. " +
                $"{FormatModel(model)} {FormatSource(source)} Error='{exception.Message}'");
        }

        internal static void WarnSurfaceConflict(
            string surface,
            AbstractModel model,
            object winningSource,
            object? winningValue,
            object ignoredSource,
            object? ignoredValue)
        {
            if (ConflictLogs == ModelCapabilityConflictLogMode.Off)
                return;

            var key = string.Join("|",
                surface,
                model.GetType().FullName,
                model.Id,
                FormatSourceKey(winningSource),
                FormatSourceKey(ignoredSource),
                winningValue?.ToString() ?? "<null>",
                ignoredValue?.ToString() ?? "<null>");

            if (ConflictLogs == ModelCapabilityConflictLogMode.WarnOnce)
                lock (ConflictGate)
                {
                    if (!SeenConflicts.Add(key))
                        return;
                }

            RitsuLibFramework.Logger.Warn(
                $"[ModelCapabilities] Surface='{surface}' conflict. " +
                $"{FormatModel(model)} First=({FormatSource(winningSource)}, Value='{winningValue}') " +
                $"Later=({FormatSource(ignoredSource)}, Value='{ignoredValue}')");
        }

        private static string FormatModel(AbstractModel model)
        {
            return $"ModelId='{model.Id}' OwnerType='{model.GetType().FullName}'";
        }

        private static string FormatSource(object source)
        {
            if (source is IModelCapability capability)
                return $"CapabilityId='{capability.CapabilityId}' CapabilityType='{source.GetType().FullName}'";

            return $"SourceType='{source.GetType().FullName}'";
        }

        private static string FormatSourceKey(object source)
        {
            return source is IModelCapability capability
                ? $"{capability.CapabilityId}:{source.GetType().FullName}"
                : source.GetType().FullName ?? "<unknown>";
        }
    }
}
