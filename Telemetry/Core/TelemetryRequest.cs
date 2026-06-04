using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     One user-visible telemetry request made by an applicant.
    ///     申请方向用户展示的一项 telemetry 数据申请。
    /// </summary>
    public sealed class TelemetryRequest
    {
        /// <summary>
        ///     Stable request id within the applicant.
        ///     申请方内部稳定的申请 ID。
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     Data category covered by this request.
        ///     此申请覆盖的数据类别。
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     Human-readable explanation shown to users before consent.
        ///     授权前向用户展示的可读说明。
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        ///     Optional localized explanation shown to users before consent.
        ///     可选本地化说明，用于授权前展示给用户。
        /// </summary>
        public ModSettingsText? DescriptionText { get; init; }

        /// <summary>
        ///     Contribution ids this request wants to attach. Private contributions only attach to their owning
        ///     applicant; shared contributions additionally require explicit source consent and should use
        ///     "contributorModId/contributionId" subscriptions.
        ///     此申请希望附加的 contribution ID。私有 contribution 仅附加到拥有者自己的申请；
        ///     共享 contribution 还需要额外的来源授权，并应使用 "contributorModId/contributionId" 订阅。
        /// </summary>
        public IReadOnlyList<string> ContributionSubscriptions { get; init; } = [];

        /// <summary>
        ///     Obsolete alias for <see cref="ContributionSubscriptions" /> kept for source compatibility.
        ///     为源码兼容保留的 <see cref="ContributionSubscriptions" /> 旧别名。
        /// </summary>
        [Obsolete("Use ContributionSubscriptions.")]
        public IReadOnlyList<string> SharedContributionSubscriptions
        {
            get => ContributionSubscriptions;
            init => ContributionSubscriptions = value;
        }

        /// <summary>
        ///     Optional predicate for automatic run-history capture; when unset, every ended run is eligible.
        ///     自动采集 run-history 时使用的可选谓词；未设置时，每个已结束跑局都符合条件。
        /// </summary>
        public Func<RunEndedEvent, bool>? RunHistoryCaptureFilter { get; init; }

        /// <summary>
        ///     Creates the built-in basic-usage request.
        ///     创建内置基础使用信息申请。
        /// </summary>
        public static TelemetryRequest BasicUsage(string description)
        {
            return new()
            {
                RequestId = "basic_usage",
                Category = TelemetryDataCategory.BasicUsage,
                Description = description,
            };
        }

        /// <summary>
        ///     Creates the built-in basic-usage request.
        ///     创建内置基础使用信息申请。
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
        ///     Creates the built-in loaded-mod inventory request.
        ///     创建内置已加载 mod 清单申请。
        /// </summary>
        public static TelemetryRequest ModInventory(string description)
        {
            return new()
            {
                RequestId = "mod_inventory",
                Category = TelemetryDataCategory.ModInventory,
                Description = description,
            };
        }

        /// <summary>
        ///     Creates the built-in loaded-mod inventory request.
        ///     创建内置已加载 mod 清单申请。
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
        ///     Creates the built-in run-history request.
        ///     创建内置 run-history 申请。
        /// </summary>
        public static TelemetryRequest RunHistory(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null,
            Func<RunEndedEvent, bool>? captureFilter = null)
        {
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
        ///     Creates the built-in run-history request.
        ///     创建内置 run-history 申请。
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
        ///     Creates the built-in diagnostics request.
        ///     创建内置诊断信息申请。
        /// </summary>
        public static TelemetryRequest Diagnostics(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            return new()
            {
                RequestId = "diagnostics",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
            };
        }

        /// <summary>
        ///     Creates the built-in diagnostics request.
        ///     创建内置诊断信息申请。
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
        ///     Creates an applicant-defined custom request.
        ///     创建申请方定义的自定义申请。
        /// </summary>
        public static TelemetryRequest Custom(string requestId, string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            return new()
            {
                RequestId = requestId,
                Category = TelemetryDataCategory.Custom,
                Description = description,
            };
        }

        /// <summary>
        ///     Creates an applicant-defined custom request.
        ///     创建申请方定义的自定义申请。
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
