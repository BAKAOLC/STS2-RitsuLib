using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    internal sealed class TelemetryQueueDocument
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("events")] public List<TelemetryEnvelope> Events { get; set; } = [];
    }

    internal sealed class TelemetryQueueState
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("last_send_utc")] public DateTimeOffset? LastSendUtc { get; set; }

        [JsonPropertyName("last_error")] public string? LastError { get; set; }

        [JsonPropertyName("failure_count")] public int FailureCount { get; set; }
    }
}
