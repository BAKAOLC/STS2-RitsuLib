namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a disabled telemetry destination for an applicant whose backend is not configured.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示遥测后端尚未配置时为申请方使用的禁用遥测目标。
    ///     </para>
    /// </summary>
    public sealed class DisabledTelemetryAdapter : ITelemetryAdapter
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a disabled adapter with a user-visible reason.</para>
        ///     <para xml:lang="zh-CN">创建禁用的适配器，并提供向用户显示的原因。</para>
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
