using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Data.Models
{
    /// <summary>
    ///     Global JSON settings blob for RitsuLib itself (schema version and debug flags).
    /// </summary>
    public sealed class RitsuLibSettings
    {
        /// <summary>
        ///     Current schema version written by the library when creating or normalizing settings.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        ///     Persisted schema version used by the migration pipeline
        ///     (<see cref="ModDataVersion.SchemaVersionProperty" />).
        /// </summary>
        [JsonPropertyName(ModDataVersion.SchemaVersionProperty)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        ///     When true, enables library debug compatibility behavior (see startup log for file path).
        /// </summary>
        [JsonPropertyName("debug_compatibility_mode")]
        public bool DebugCompatibilityMode { get; set; }
    }
}
