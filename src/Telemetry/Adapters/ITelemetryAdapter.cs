namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Sends one applicant's authorized telemetry events to that applicant's fixed backend.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将一个申请方已获授权的遥测事件发送到该申请方的固定后端。
    ///     </para>
    /// </summary>
    public interface ITelemetryAdapter
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the stable adapter ID, such as <c>http_json</c> or <c>posthog</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取稳定的适配器 ID，例如 <c>http_json</c> 或 <c>posthog</c>。
        ///     </para>
        /// </summary>
        string AdapterId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the human-readable endpoint description shown in settings.</para>
        ///     <para xml:lang="zh-CN">获取设置界面中显示的端点说明。</para>
        /// </summary>
        string EndpointDescription { get; }

        /// <summary>
        ///     <para xml:lang="en">Sends one event batch for <paramref name="applicant" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="applicant" /> 发送一个事件批次。</para>
        /// </summary>
        ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     <para xml:lang="en">Represents the result of a telemetry adapter's send attempt.</para>
    ///     <para xml:lang="zh-CN">表示遥测适配器一次发送尝试的结果。</para>
    /// </summary>
    public readonly record struct TelemetrySendResult
    {
        /// <summary>
        ///     <para xml:lang="en">Initializes a send result.</para>
        ///     <para xml:lang="zh-CN">初始化发送结果。</para>
        /// </summary>
        public TelemetrySendResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the adapter accepted the batch.</para>
        ///     <para xml:lang="zh-CN">获取适配器是否接受了该批次。</para>
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the error message when <see cref="Success" /> is <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="Success" /> 为 <see langword="false" /> 时的错误信息。
        ///     </para>
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        ///     <para xml:lang="en">Creates a successful send result.</para>
        ///     <para xml:lang="zh-CN">创建表示发送成功的结果。</para>
        /// </summary>
        public static TelemetrySendResult Ok()
        {
            return new(true);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a failed send result with an error message.</para>
        ///     <para xml:lang="zh-CN">创建带错误信息的发送失败结果。</para>
        /// </summary>
        public static TelemetrySendResult Fail(string errorMessage)
        {
            return new(false, errorMessage);
        }
    }
}
