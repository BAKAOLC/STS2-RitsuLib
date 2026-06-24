using System.Text.Json;

namespace STS2RitsuLib.RunData
{
    internal static class RunSavedDataJson
    {
        internal static JsonSerializerOptions Options { get; } = new()
        {
            WriteIndented = false,
            IncludeFields = false,
        };
    }
}
