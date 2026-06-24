using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Serialized telemetry event after consent, routing, and payload assembly.
    ///     经过授权、路由和数据组装后的 telemetry 事件。
    /// </summary>
    public sealed class TelemetryEnvelope
    {
        /// <summary>
        ///     Envelope schema id.
        ///     envelope schema ID。
        /// </summary>
        public string Schema { get; init; } = TelemetrySchemas.EventV1;

        /// <summary>
        ///     Applicant that owns the destination adapter for this event.
        ///     拥有此事件目标 adapter 的申请方。
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     Stable event name.
        ///     稳定事件名。
        /// </summary>
        public required string EventName { get; init; }

        /// <summary>
        ///     Request id whose consent allowed this event.
        ///     授权此事件的申请 ID。
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     Data category for this event.
        ///     此事件的数据类别。
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     Event creation time in UTC.
        ///     事件创建的 UTC 时间。
        /// </summary>
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        ///     Flat metadata sent with the event.
        ///     随事件发送的扁平元数据。
        /// </summary>
        public Dictionary<string, object?> Properties { get; init; } = [];

        /// <summary>
        ///     Structured payload, usually split into base payload, private/shared contributions, and applicant payload.
        ///     结构化数据，通常分为基础数据、私有/共享 contributions 和申请方数据。
        /// </summary>
        public JsonNode? Payload { get; init; }
    }
}
