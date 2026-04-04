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
        public const int CurrentSchemaVersion = 3;

        /// <summary>
        ///     Persisted schema version used by the migration pipeline
        ///     (<see cref="ModDataVersion.SchemaVersionProperty" />).
        /// </summary>
        [JsonPropertyName(ModDataVersion.SchemaVersionProperty)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        ///     Master switch: when false, sub-flags are ignored and shim logic no-ops so patched targets follow vanilla
        ///     code paths (<c>LocTable</c>, epoch grants, <c>THE_ARCHITECT</c> load, etc.).
        /// </summary>
        [JsonPropertyName("debug_compatibility_mode")]
        public bool DebugCompatibilityMode { get; set; }

        /// <summary>
        ///     When master is on: soft-fail missing <c>LocTable</c> keys with placeholders and one-time
        ///     <c>[Localization][DebugCompat]</c> warnings. Default true (on new installs and after schema migration).
        /// </summary>
        [JsonPropertyName("debug_compat_loc_table")]
        public bool DebugCompatLocTable { get; set; } = true;

        /// <summary>
        ///     When master and this flag are on: skip invalid epoch grants on framework bridges with one-time
        ///     <c>[Unlocks][DebugCompat]</c> warnings. Otherwise invalid ids use the original grant path (vanilla).
        ///     Default true.
        /// </summary>
        [JsonPropertyName("debug_compat_unlock_epoch")]
        public bool DebugCompatUnlockEpoch { get; set; } = true;

        /// <summary>
        ///     When master is on: inject empty-lines <c>THE_ARCHITECT</c> dialogue for <c>ModContentRegistry</c>
        ///     characters when vanilla resolves none. Default true.
        /// </summary>
        [JsonPropertyName("debug_compat_ancient_architect")]
        public bool DebugCompatAncientArchitect { get; set; } = true;

        /// <summary>
        ///     Absolute path or Godot <c>user://</c> path for Harmony patch dump output (text log).
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_output_path")]
        public string HarmonyPatchDumpOutputPath { get; set; } = string.Empty;

        /// <summary>
        ///     When true, writes a dump once when the main menu first finishes loading this session (deferred).
        /// </summary>
        [JsonPropertyName("harmony_patch_dump_on_first_main_menu")]
        public bool HarmonyPatchDumpOnFirstMainMenu { get; set; }
    }
}
