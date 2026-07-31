using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Controls optional model capability diagnostic logging.</para>
    ///     <para xml:lang="zh-CN">控制可选的模型能力诊断日志。</para>
    /// </summary>
    public enum ModelCapabilityConflictLogMode
    {
        /// <summary>
        ///     <para xml:lang="en">Does not log conflicts between capability-provided values.</para>
        ///     <para xml:lang="zh-CN">不记录能力所提供值之间的冲突。</para>
        /// </summary>
        Off,

        /// <summary>
        ///     <para xml:lang="en">Logs each distinct conflict once.</para>
        ///     <para xml:lang="zh-CN">每个不同冲突只记录一次。</para>
        /// </summary>
        WarnOnce,

        /// <summary>
        ///     <para xml:lang="en">Logs every observed conflict.</para>
        ///     <para xml:lang="zh-CN">每次观察到冲突都记录。</para>
        /// </summary>
        WarnEveryTime,
    }

    /// <summary>
    ///     <para xml:lang="en">Runtime diagnostics for model capabilities.</para>
    ///     <para xml:lang="zh-CN">模型能力运行时诊断。</para>
    /// </summary>
    public static class ModelCapabilityDiagnostics
    {
        private static readonly Lock ConflictGate = new();
        private static readonly HashSet<string> SeenConflicts = new(StringComparer.Ordinal);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional conflict logging for single-winner values such as card type, rarity, target type, and
        ///         result pile. Defaults to <see cref="ModelCapabilityConflictLogMode.Off" /> to avoid hot-path log spam.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选择记录卡牌类型、稀有度、目标类型及结果牌堆等仅采用一个值的冲突。
        ///         默认为关闭，以免热路径产生大量日志。
        ///     </para>
        /// </summary>
        public static ModelCapabilityConflictLogMode ConflictLogs { get; set; } =
            ModelCapabilityConflictLogMode.Off;

        internal static bool ShouldInspectConflicts => ConflictLogs != ModelCapabilityConflictLogMode.Off;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clears the cache of conflicts already logged by
        ///         <see cref="ModelCapabilityConflictLogMode.WarnOnce" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         清空 <see cref="ModelCapabilityConflictLogMode.WarnOnce" /> 已记录冲突的缓存。
        ///     </para>
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
