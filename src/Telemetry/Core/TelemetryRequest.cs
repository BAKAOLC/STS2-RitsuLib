using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Describes one user-visible telemetry request made by an applicant.</para>
    ///     <para xml:lang="zh-CN">描述申请方向用户展示的一个遥测数据申请项。</para>
    /// </summary>
    public sealed class TelemetryRequest
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the request's stable ID within the applicant.</para>
        ///     <para xml:lang="zh-CN">获取此申请项在申请方内部的稳定 ID。</para>
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the data category covered by this request.</para>
        ///     <para xml:lang="zh-CN">获取此申请项涵盖的数据类别。</para>
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the human-readable explanation shown before consent.</para>
        ///     <para xml:lang="zh-CN">获取授权前向用户显示的可读说明。</para>
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets an optional localized explanation shown before consent.</para>
        ///     <para xml:lang="zh-CN">获取授权前向用户显示的可选本地化说明。</para>
        /// </summary>
        public ModSettingsText? DescriptionText { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the contribution IDs this request subscribes to. Private contributions are attached only to
        ///         their owning applicant. Shared contributions additionally require explicit source consent and
        ///         should use <c>contributorModId/contributionId</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此申请项订阅的数据贡献 ID。私有数据贡献只会附加到其所属申请方；共享数据贡献还需要针对
        ///         来源的明确授权，并应使用 <c>contributorModId/contributionId</c>。
        ///     </para>
        /// </summary>
        public IReadOnlyList<string> ContributionSubscriptions { get; init; } = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the obsolete alias of <see cref="ContributionSubscriptions" /> retained for source
        ///         compatibility.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取为源代码兼容性保留的 <see cref="ContributionSubscriptions" /> 旧别名。
        ///     </para>
        /// </summary>
        [Obsolete("Use ContributionSubscriptions.")]
        public IReadOnlyList<string> SharedContributionSubscriptions
        {
            get => ContributionSubscriptions;
            init => ContributionSubscriptions = value;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional predicate for automatic run-history capture. When unset, every completed run is
        ///         eligible.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取自动采集游戏历史记录时使用的可选谓词。未设置时，每局已结束的游戏都符合条件。
        ///     </para>
        /// </summary>
        public Func<RunEndedEvent, bool>? RunHistoryCaptureFilter { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in basic-usage request.</para>
        ///     <para xml:lang="zh-CN">创建内置的基础使用信息申请项。</para>
        /// </summary>
        public static TelemetryRequest BasicUsage(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "basic_usage",
                Category = TelemetryDataCategory.BasicUsage,
                Description = description,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in basic-usage request.</para>
        ///     <para xml:lang="zh-CN">创建内置的基础使用信息申请项。</para>
        /// </summary>
        public static TelemetryRequest BasicUsage(ModSettingsText description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "basic_usage",
                Category = TelemetryDataCategory.BasicUsage,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in loaded-mod inventory request.</para>
        ///     <para xml:lang="zh-CN">创建内置的已加载模组清单申请项。</para>
        /// </summary>
        public static TelemetryRequest ModInventory(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "mod_inventory",
                Category = TelemetryDataCategory.ModInventory,
                Description = description,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in loaded-mod inventory request.</para>
        ///     <para xml:lang="zh-CN">创建内置的已加载模组清单申请项。</para>
        /// </summary>
        public static TelemetryRequest ModInventory(ModSettingsText description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "mod_inventory",
                Category = TelemetryDataCategory.ModInventory,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in run-history request.</para>
        ///     <para xml:lang="zh-CN">创建内置的游戏历史记录申请项。</para>
        /// </summary>
        public static TelemetryRequest RunHistory(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null,
            Func<RunEndedEvent, bool>? captureFilter = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "run_history",
                Category = TelemetryDataCategory.RunHistory,
                Description = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
                RunHistoryCaptureFilter = captureFilter,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in run-history request.</para>
        ///     <para xml:lang="zh-CN">创建内置的游戏历史记录申请项。</para>
        /// </summary>
        public static TelemetryRequest RunHistory(
            ModSettingsText description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null,
            Func<RunEndedEvent, bool>? captureFilter = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "run_history",
                Category = TelemetryDataCategory.RunHistory,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
                RunHistoryCaptureFilter = captureFilter,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in diagnostics request.</para>
        ///     <para xml:lang="zh-CN">创建内置的诊断信息申请项。</para>
        /// </summary>
        public static TelemetryRequest Diagnostics(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "diagnostics",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in diagnostics request.</para>
        ///     <para xml:lang="zh-CN">创建内置的诊断信息申请项。</para>
        /// </summary>
        public static TelemetryRequest Diagnostics(
            ModSettingsText description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "diagnostics",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an applicant-defined custom request.</para>
        ///     <para xml:lang="zh-CN">创建由申请方定义的自定义申请项。</para>
        /// </summary>
        public static TelemetryRequest Custom(string requestId, string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = requestId,
                Category = TelemetryDataCategory.Custom,
                Description = description,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an applicant-defined custom request.</para>
        ///     <para xml:lang="zh-CN">创建由申请方定义的自定义申请项。</para>
        /// </summary>
        public static TelemetryRequest Custom(string requestId, ModSettingsText description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = requestId,
                Category = TelemetryDataCategory.Custom,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
            };
        }

        internal string ResolveDescription()
        {
            return DescriptionText?.Resolve() ?? Description;
        }
    }
}
