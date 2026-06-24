using System.Text.Json;
using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryJson
    {
        internal static JsonSerializerOptions Options { get; } = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = false,
            Converters = { new JsonStringEnumConverter() },
        };
    }
}
