namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Adapter used when an applicant is registered but its backend is not configured.
    ///     当申请方已注册但后端未配置时使用的 adapter。
    /// </summary>
    public sealed class DisabledTelemetryAdapter : ITelemetryAdapter
    {
        /// <summary>
        ///     Creates a disabled adapter with a user-visible reason.
        ///     使用面向用户的原因创建禁用 adapter。
        /// </summary>
        public DisabledTelemetryAdapter(string reason)
        {
            EndpointDescription = string.IsNullOrWhiteSpace(reason) ? "Telemetry backend is not configured." : reason;
        }

        /// <inheritdoc />
        public string AdapterId => "disabled";

        /// <inheritdoc />
        public string EndpointDescription { get; }

        /// <inheritdoc />
        public ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(TelemetrySendResult.Fail(EndpointDescription));
        }
    }
}
