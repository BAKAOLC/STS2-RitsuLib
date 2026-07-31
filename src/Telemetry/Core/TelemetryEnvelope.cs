using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a serialized telemetry event after consent, routing, and payload assembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示经过授权、路由和负载组装后的序列化遥测事件。
    ///     </para>
    /// </summary>
    public sealed class TelemetryEnvelope
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the envelope schema ID.</para>
        ///     <para xml:lang="zh-CN">获取信封格式的架构 ID。</para>
        /// </summary>
        public string Schema { get; init; } = TelemetrySchemas.EventV1;

        /// <summary>
        ///     <para xml:lang="en">Gets the applicant that owns this event's destination adapter.</para>
        ///     <para xml:lang="zh-CN">获取拥有此事件目标适配器的申请方。</para>
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable event name.</para>
        ///     <para xml:lang="zh-CN">获取稳定的事件名称。</para>
        /// </summary>
        public required string EventName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the request whose consent authorized this event.</para>
        ///     <para xml:lang="zh-CN">获取授权此事件的申请项 ID。</para>
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the event's data category.</para>
        ///     <para xml:lang="zh-CN">获取此事件的数据类别。</para>
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the event's creation time in UTC.</para>
        ///     <para xml:lang="zh-CN">获取此事件的 UTC 创建时间。</para>
        /// </summary>
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        ///     <para xml:lang="en">Gets flat metadata sent with the event.</para>
        ///     <para xml:lang="zh-CN">获取随事件发送的扁平元数据。</para>
        /// </summary>
        public Dictionary<string, object?> Properties { get; init; } = [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the structured payload, usually divided into a base payload, private and shared contributions,
        ///         and an applicant payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取结构化负载，通常包含基础负载、私有和共享数据贡献以及申请方负载。
        ///     </para>
        /// </summary>
        public JsonNode? Payload { get; init; }
    }
}
