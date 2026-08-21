namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one telemetry event before RitsuLib builds contributions or queues the event.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述一项尚未由 RitsuLib 构建数据贡献或加入队列的遥测事件。
    ///     </para>
    /// </summary>
    public readonly record struct TelemetryCaptureContext
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a telemetry capture context.</para>
        ///     <para xml:lang="zh-CN">创建遥测采集上下文。</para>
        /// </summary>
        /// <param name="eventName">
        ///     <para xml:lang="en">Stable telemetry event name.</para>
        ///     <para xml:lang="zh-CN">稳定的遥测事件名称。</para>
        /// </param>
        /// <param name="requestId">
        ///     <para xml:lang="en">Request ID whose consent authorizes the event.</para>
        ///     <para xml:lang="zh-CN">授权该事件的申请项 ID。</para>
        /// </param>
        /// <param name="category">
        ///     <para xml:lang="en">Data category associated with the request.</para>
        ///     <para xml:lang="zh-CN">申请项对应的数据类别。</para>
        /// </param>
        /// <param name="source">
        ///     <para xml:lang="en">RitsuLib capture-source identifier.</para>
        ///     <para xml:lang="zh-CN">RitsuLib 采集源标识符。</para>
        /// </param>
        /// <param name="sourceData">
        ///     <para xml:lang="en">
        ///         Optional source object available before payload generation, such as an exception or
        ///         <see cref="RunEndedEvent" />. The filter must not mutate it or retain it after returning.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         负载生成前可用的可选来源对象，例如异常或 <see cref="RunEndedEvent" />。筛选器不得修改该对象，
        ///         也不得在返回后继续持有它。
        ///     </para>
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         <paramref name="eventName" />, <paramref name="requestId" />, or <paramref name="source" /> is
        ///         empty, or <paramref name="category" /> is unsupported.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="eventName" />、<paramref name="requestId" /> 或 <paramref name="source" /> 为空，
        ///         或 <paramref name="category" /> 不受支持。
        ///     </para>
        /// </exception>
        public TelemetryCaptureContext(
            string eventName,
            string requestId,
            TelemetryDataCategory category,
            string source,
            object? sourceData = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            if (category == TelemetryDataCategory.None || !Enum.IsDefined(category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported telemetry category.");
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            EventName = eventName;
            RequestId = requestId;
            Category = category;
            Source = source;
            SourceData = sourceData;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable telemetry event name.</para>
        ///     <para xml:lang="zh-CN">获取稳定的遥测事件名称。</para>
        /// </summary>
        public string EventName { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the request ID whose consent authorizes this event.</para>
        ///     <para xml:lang="zh-CN">获取授权此事件的申请项 ID。</para>
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the event's data category.</para>
        ///     <para xml:lang="zh-CN">获取事件的数据类别。</para>
        /// </summary>
        public TelemetryDataCategory Category { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the capture-source identifier. Automatic sources use RitsuLib-defined values; direct client
        ///         captures use <c>applicant</c> unless a more specific source is supplied internally.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取采集源标识符。自动采集源使用 RitsuLib 定义的值；客户端直接采集在未由内部提供更具体
        ///         来源时使用 <c>applicant</c>。
        ///     </para>
        /// </summary>
        public string Source { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets optional source data available before payload generation. Filters must treat it as borrowed,
        ///         read-only data and must not retain it after returning.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取负载生成前可用的可选来源数据。筛选器必须将其视为借用的只读数据，且不得在返回后继续
        ///         持有它。
        ///     </para>
        /// </summary>
        public object? SourceData { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the exception for an exception event, or <see langword="null" /> for another event type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取异常事件对应的异常；其他事件类型则为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public Exception? Exception => SourceData as Exception;
    }
}
