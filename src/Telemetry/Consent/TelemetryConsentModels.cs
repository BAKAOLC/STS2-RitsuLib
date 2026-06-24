using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    internal sealed class TelemetryConsentDocument
    {
        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("applicants")]
        public Dictionary<string, TelemetryApplicantConsent> Applicants { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class TelemetryApplicantConsent
    {
        [JsonPropertyName("consent")]
        public TelemetryConsentState Consent { get; set; } = TelemetryConsentState.Unknown;

        [JsonPropertyName("granted_requests")]
        public HashSet<string> GrantedRequests { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("shared_contribution_sources")]
        public Dictionary<string, HashSet<string>> SharedContributionSources { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
