using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    internal sealed class TelemetryIdentityDocument
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("anonymous_install_id")]
        public string AnonymousInstallId { get; set; } = Guid.NewGuid().ToString("N");
    }
}
