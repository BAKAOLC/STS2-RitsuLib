using System.Text.Json.Serialization;

namespace STS2RitsuLib.Settings.RunSidecar
{
    internal sealed class ModRunSidecarEnvelope<TModel> where TModel : class, new()
    {
        [JsonPropertyName("envelope_version")] public int EnvelopeVersion { get; set; } = 1;

        [JsonPropertyName("fingerprint")] public ModRunSidecarFingerprintDto? Fingerprint { get; set; }

        [JsonPropertyName("settings")] public TModel Settings { get; set; } = new();
    }
}
