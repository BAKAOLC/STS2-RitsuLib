using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Describes one user-visible telemetry request made by an applicant.</para>
    ///     <para xml:lang="zh-CN">描述申请方向用户展示的一个遥测数据申请项。</para>
    /// </summary>
    public sealed class TelemetryRequest
    {
        private readonly Func<TelemetryCaptureContext, bool>? _captureFilter;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private Func<RunEndedEvent, bool>? _runHistoryCaptureFilter;

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
        ///         Gets the legacy predicate for automatic run-history capture. Use <see cref="CaptureFilter" /> and
        ///         inspect <see cref="TelemetryCaptureContext.SourceData" /> as a <see cref="RunEndedEvent" /> instead.
        ///         RitsuLib exposes this predicate only through the common capture-filter path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取自动采集游戏历史记录时使用的旧版谓词。请改用 <see cref="CaptureFilter" />，并将
        ///         <see cref="TelemetryCaptureContext.SourceData" /> 作为 <see cref="RunEndedEvent" /> 检查。
        ///         RitsuLib 只会通过通用采集筛选路径使用此谓词。
        ///     </para>
        /// </summary>
        [Obsolete("Use CaptureFilter and inspect TelemetryCaptureContext.SourceData as RunEndedEvent.")]
        public Func<RunEndedEvent, bool>? RunHistoryCaptureFilter
        {
            get => _runHistoryCaptureFilter;
            init
            {
                _runHistoryCaptureFilter = value;
                _captureFilter = WrapRunHistoryCaptureFilter(value);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional predicate applied to every event authorized by this request. RitsuLib invokes it
        ///         synchronously before building contributions or queuing the event. Automatic collectors also invoke
        ///         it before expensive source payload generation. Returning <see langword="false" />, or throwing an
        ///         exception, rejects that capture for this applicant. The predicate runs on the capture source's
        ///         current thread, may run concurrently, and must be fast, non-blocking, and reentrant.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取应用于此申请项所授权每个事件的可选谓词。RitsuLib 会在构建数据贡献或将事件加入队列
        ///         前同步调用；自动采集器还会在生成开销较大的来源负载前调用。返回 <see langword="false" /> 或
        ///         抛出异常时，会为此申请方拒绝该次采集。谓词在采集源的当前线程执行，可能被并发调用，因此
        ///         必须快速、非阻塞且可重入。
        ///     </para>
        /// </summary>
        public Func<TelemetryCaptureContext, bool>? CaptureFilter
        {
            get => _captureFilter;
            init
            {
                _captureFilter = value;
                _runHistoryCaptureFilter = null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a copy of this request with the supplied common event capture filter. This allows every
        ///         built-in and custom request factory to opt into per-event filtering without making registered
        ///         requests mutable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建此申请项的副本并设置所提供的通用事件采集筛选器。借此，每个内置或自定义申请项工厂
        ///         都能启用逐事件筛选，同时不会使已注册的申请项变为可变对象。
        ///     </para>
        /// </summary>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Fast, non-blocking, reentrant event predicate.</para>
        ///     <para xml:lang="zh-CN">快速、非阻塞且可重入的事件谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A new request carrying the supplied capture filter.</para>
        ///     <para xml:lang="zh-CN">包含所提供采集筛选器的新申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="captureFilter" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="captureFilter" /> 为 null。</para>
        /// </exception>
        public TelemetryRequest WithCaptureFilter(Func<TelemetryCaptureContext, bool> captureFilter)
        {
            ArgumentNullException.ThrowIfNull(captureFilter);
            return new()
            {
                RequestId = RequestId,
                Category = Category,
                Description = Description,
                DescriptionText = DescriptionText,
                ContributionSubscriptions = ContributionSubscriptions,
                CaptureFilter = captureFilter,
            };
        }

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
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "run_history",
                Category = TelemetryDataCategory.RunHistory,
                Description = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the built-in run-history request.</para>
        ///     <para xml:lang="zh-CN">创建内置的游戏历史记录申请项。</para>
        /// </summary>
        public static TelemetryRequest RunHistory(
            ModSettingsText description,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "run_history",
                Category = TelemetryDataCategory.RunHistory,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a run-history request with the legacy strongly typed capture predicate.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建带旧版强类型采集谓词的游戏历史记录申请项。</para>
        /// </summary>
        [Obsolete("Use RunHistoryFiltered with TelemetryCaptureContext.SourceData.")]
        public static TelemetryRequest RunHistory(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions,
            Func<RunEndedEvent, bool>? captureFilter)
        {
            ArgumentNullException.ThrowIfNull(description);
            if (captureFilter == null)
                return RunHistory(description, sharedContributionSubscriptions);

            var request = RunHistoryFiltered(
                description,
                WrapRunHistoryCaptureFilter(captureFilter)!,
                sharedContributionSubscriptions);
            request._runHistoryCaptureFilter = captureFilter;
            return request;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a run-history request with the legacy strongly typed capture predicate.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建带旧版强类型采集谓词的游戏历史记录申请项。</para>
        /// </summary>
        [Obsolete("Use RunHistoryFiltered with TelemetryCaptureContext.SourceData.")]
        public static TelemetryRequest RunHistory(
            ModSettingsText description,
            IReadOnlyList<string>? sharedContributionSubscriptions,
            Func<RunEndedEvent, bool>? captureFilter)
        {
            ArgumentNullException.ThrowIfNull(description);
            if (captureFilter == null)
                return RunHistory(description, sharedContributionSubscriptions);

            var request = RunHistoryFiltered(
                description,
                WrapRunHistoryCaptureFilter(captureFilter)!,
                sharedContributionSubscriptions);
            request._runHistoryCaptureFilter = captureFilter;
            return request;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a run-history request using the common event capture filter. For automatic run completion,
        ///         <see cref="TelemetryCaptureContext.SourceData" /> is a <see cref="RunEndedEvent" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建使用通用事件采集筛选器的游戏历史记录申请项。自动采集局终记录时，
        ///         <see cref="TelemetryCaptureContext.SourceData" /> 为 <see cref="RunEndedEvent" />。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Human-readable consent description.</para>
        ///     <para xml:lang="zh-CN">向用户显示的授权说明。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Fast synchronous event predicate.</para>
        ///     <para xml:lang="zh-CN">快速同步事件谓词。</para>
        /// </param>
        /// <param name="sharedContributionSubscriptions">
        ///     <para xml:lang="en">Optional private or explicitly authorized shared contribution subscriptions.</para>
        ///     <para xml:lang="zh-CN">可选的私有数据贡献或已明确授权的共享数据贡献订阅。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A filtered run-history telemetry request.</para>
        ///     <para xml:lang="zh-CN">带筛选器的游戏历史记录遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> or <paramref name="captureFilter" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 或 <paramref name="captureFilter" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest RunHistoryFiltered(
            string description,
            Func<TelemetryCaptureContext, bool> captureFilter,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(captureFilter);
            return RunHistory(description, sharedContributionSubscriptions).WithCaptureFilter(captureFilter);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a localized run-history request using the common event capture filter. For automatic run
        ///         completion, <see cref="TelemetryCaptureContext.SourceData" /> is a <see cref="RunEndedEvent" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建使用通用事件采集筛选器的本地化游戏历史记录申请项。自动采集局终记录时，
        ///         <see cref="TelemetryCaptureContext.SourceData" /> 为 <see cref="RunEndedEvent" />。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Localized consent description.</para>
        ///     <para xml:lang="zh-CN">本地化授权说明。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Fast synchronous event predicate.</para>
        ///     <para xml:lang="zh-CN">快速同步事件谓词。</para>
        /// </param>
        /// <param name="sharedContributionSubscriptions">
        ///     <para xml:lang="en">Optional private or explicitly authorized shared contribution subscriptions.</para>
        ///     <para xml:lang="zh-CN">可选的私有数据贡献或已明确授权的共享数据贡献订阅。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A filtered localized run-history telemetry request.</para>
        ///     <para xml:lang="zh-CN">带筛选器的本地化游戏历史记录遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> or <paramref name="captureFilter" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 或 <paramref name="captureFilter" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest RunHistoryFiltered(
            ModSettingsText description,
            Func<TelemetryCaptureContext, bool> captureFilter,
            IReadOnlyList<string>? sharedContributionSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(captureFilter);
            return RunHistory(description, sharedContributionSubscriptions).WithCaptureFilter(captureFilter);
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
        ///     <para xml:lang="en">
        ///         Creates the built-in diagnostics request with a predicate that filters automatic diagnostics before
        ///         their payloads are built or queued. A predicate exception rejects that capture.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带筛选谓词的内置诊断信息申请项；该谓词会在自动诊断负载构建或进入队列前执行，谓词抛出
        ///         异常时会拒绝该次采集。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Human-readable consent description.</para>
        ///     <para xml:lang="zh-CN">向用户显示的授权说明。</para>
        /// </param>
        /// <param name="sharedContributionSubscriptions">
        ///     <para xml:lang="en">Optional private or explicitly authorized shared contribution subscriptions.</para>
        ///     <para xml:lang="zh-CN">可选的私有数据贡献或已明确授权的共享数据贡献订阅。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Fast synchronous predicate invoked before automatic diagnostics payload creation.</para>
        ///     <para xml:lang="zh-CN">在自动诊断负载创建前调用的快速同步谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A diagnostics telemetry request carrying the supplied filter.</para>
        ///     <para xml:lang="zh-CN">包含所提供筛选器的诊断遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> or <paramref name="captureFilter" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 或 <paramref name="captureFilter" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest Diagnostics(
            string description,
            IReadOnlyList<string>? sharedContributionSubscriptions,
            Func<TelemetryCaptureContext, bool> captureFilter)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(captureFilter);
            return new()
            {
                RequestId = "diagnostics",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
                CaptureFilter = captureFilter,
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
        ///     <para xml:lang="en">
        ///         Creates the built-in diagnostics request with a localized description and a predicate that filters
        ///         automatic diagnostics before their payloads are built or queued. A predicate exception rejects that
        ///         capture.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带本地化说明和筛选谓词的内置诊断信息申请项；该谓词会在自动诊断负载构建或进入队列前
        ///         执行，谓词抛出异常时会拒绝该次采集。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Localized consent description.</para>
        ///     <para xml:lang="zh-CN">本地化授权说明。</para>
        /// </param>
        /// <param name="sharedContributionSubscriptions">
        ///     <para xml:lang="en">Optional private or explicitly authorized shared contribution subscriptions.</para>
        ///     <para xml:lang="zh-CN">可选的私有数据贡献或已明确授权的共享数据贡献订阅。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Fast synchronous predicate invoked before automatic diagnostics payload creation.</para>
        ///     <para xml:lang="zh-CN">在自动诊断负载创建前调用的快速同步谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A diagnostics telemetry request carrying the supplied filter.</para>
        ///     <para xml:lang="zh-CN">包含所提供筛选器的诊断遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> or <paramref name="captureFilter" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 或 <paramref name="captureFilter" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest Diagnostics(
            ModSettingsText description,
            IReadOnlyList<string>? sharedContributionSubscriptions,
            Func<TelemetryCaptureContext, bool> captureFilter)
        {
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(captureFilter);
            return new()
            {
                RequestId = "diagnostics",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
                ContributionSubscriptions = sharedContributionSubscriptions ?? [],
                CaptureFilter = captureFilter,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the built-in multiplayer state-divergence bundle request. The optional predicate runs
        ///         before RitsuLib reads, sanitizes, or queues the bundle.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建内置的多人游戏状态分歧诊断包申请项。可选谓词会在 RitsuLib 读取、清理或将诊断包加入
        ///         队列之前执行。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Human-readable consent description.</para>
        ///     <para xml:lang="zh-CN">向用户显示的授权说明。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Optional fast synchronous predicate for automatic bundle capture.</para>
        ///     <para xml:lang="zh-CN">用于自动诊断包采集的可选快速同步谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A separately consented state-divergence telemetry request.</para>
        ///     <para xml:lang="zh-CN">需要单独授权的状态分歧遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest StateDivergence(
            string description,
            Func<TelemetryCaptureContext, bool>? captureFilter = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "state_divergence",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description,
                CaptureFilter = captureFilter,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the built-in multiplayer state-divergence bundle request with a localized description. The
        ///         optional predicate runs before RitsuLib reads, sanitizes, or queues the bundle.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带本地化说明的内置多人游戏状态分歧诊断包申请项。可选谓词会在 RitsuLib 读取、清理或
        ///         将诊断包加入队列之前执行。
        ///     </para>
        /// </summary>
        /// <param name="description">
        ///     <para xml:lang="en">Localized consent description.</para>
        ///     <para xml:lang="zh-CN">本地化授权说明。</para>
        /// </param>
        /// <param name="captureFilter">
        ///     <para xml:lang="en">Optional fast synchronous predicate for automatic bundle capture.</para>
        ///     <para xml:lang="zh-CN">用于自动诊断包采集的可选快速同步谓词。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A separately consented state-divergence telemetry request.</para>
        ///     <para xml:lang="zh-CN">需要单独授权的状态分歧遥测申请项。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en"><paramref name="description" /> is null.</para>
        ///     <para xml:lang="zh-CN"><paramref name="description" /> 为 null。</para>
        /// </exception>
        public static TelemetryRequest StateDivergence(
            ModSettingsText description,
            Func<TelemetryCaptureContext, bool>? captureFilter = null)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new()
            {
                RequestId = "state_divergence",
                Category = TelemetryDataCategory.Diagnostics,
                Description = description.FallbackText ?? string.Empty,
                DescriptionText = description,
                CaptureFilter = captureFilter,
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

        private static Func<TelemetryCaptureContext, bool>? WrapRunHistoryCaptureFilter(
            Func<RunEndedEvent, bool>? captureFilter)
        {
            return captureFilter == null
                ? null
                : context => context.SourceData is RunEndedEvent runEndedEvent && captureFilter(runEndedEvent);
        }
    }
}
