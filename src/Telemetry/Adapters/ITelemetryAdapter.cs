namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Sends authorized telemetry events for one applicant to that applicant's fixed backend.
    ///     将某个申请方已授权的 telemetry 事件发送到该申请方固定的后端。
    /// </summary>
    public interface ITelemetryAdapter
    {
        /// <summary>
        ///     Stable adapter id, such as <c>http_json</c> or <c>posthog</c>.
        ///     稳定 adapter ID，例如 <c>http_json</c> 或 <c>posthog</c>。
        /// </summary>
        string AdapterId { get; }

        /// <summary>
        ///     Human-readable endpoint description shown in settings.
        ///     设置界面中显示的可读 endpoint 说明。
        /// </summary>
        string EndpointDescription { get; }

        /// <summary>
        ///     Sends one batch of events for <paramref name="applicant" />.
        ///     为 <paramref name="applicant" /> 发送一批事件。
        /// </summary>
        ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Result returned by a telemetry adapter send attempt.
    ///     telemetry adapter 发送尝试的结果。
    /// </summary>
    public readonly record struct TelemetrySendResult
    {
        /// <summary>
        ///     Initializes a send result.
        ///     初始化发送结果。
        /// </summary>
        public TelemetrySendResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        ///     Whether the adapter accepted the batch.
        ///     adapter 是否接受了该批次。
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     Error message when <see cref="Success" /> is false.
        ///     <see cref="Success" /> 为 false 时的错误信息。
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        ///     Creates a successful send result.
        ///     创建成功发送结果。
        /// </summary>
        public static TelemetrySendResult Ok()
        {
            return new(true);
        }

        /// <summary>
        ///     Creates a failed send result with an error message.
        ///     创建带错误信息的失败发送结果。
        /// </summary>
        public static TelemetrySendResult Fail(string errorMessage)
        {
            return new(false, errorMessage);
        }
    }
}
