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
        public const int CurrentSchemaVersion = 5;

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

        /// <summary>
        ///     Output folder for self-check bundles (report + harmony dump + copied godot.log + zip).
        /// </summary>
        [JsonPropertyName("self_check_output_folder_path")]
        public string SelfCheckOutputFolderPath { get; set; } = "user://ritsulib_self_check";

        /// <summary>
        ///     When true, runs one self-check bundle export after the first main-menu load each session.
        /// </summary>
        [JsonPropertyName("self_check_on_first_main_menu")]
        public bool SelfCheckOnFirstMainMenu { get; set; }

        /// <summary>
        ///     Output directory for dev card PNG batch export (absolute path or <c>user://</c>).
        /// </summary>
        [JsonPropertyName("card_png_export_output_path")]
        public string CardPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     When true, export layout includes a right-hand hover-tip style column (approximation, not in-game tooltip
        ///     positioning).
        /// </summary>
        [JsonPropertyName("card_png_export_include_hover")]
        public bool CardPngExportIncludeHover { get; set; }

        /// <summary>
        ///     When true, also writes <c>_upgraded.png</c> for upgradable cards.
        /// </summary>
        [JsonPropertyName("card_png_export_include_upgrades")]
        public bool CardPngExportIncludeUpgrades { get; set; } = true;

        /// <summary>
        ///     Uniform scale for rendered cards (slider domain; clamped when exporting).
        /// </summary>
        [JsonPropertyName("card_png_export_scale")]
        public double CardPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional substring filter on <c>ModelId.Entry</c> (ordinal ignore-case); empty exports all.
        /// </summary>
        [JsonPropertyName("card_png_export_id_filter")]
        public string CardPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     Maximum number of <em>base</em> cards to process; <c>0</c> means no limit.
        /// </summary>
        [JsonPropertyName("card_png_export_max_base_cards")]
        public int CardPngExportMaxBaseCards { get; set; }

        /// <summary>
        ///     When true, export includes cards that are registered but hidden from the in-game card library.
        /// </summary>
        [JsonPropertyName("card_png_export_include_hidden_from_library")]
        public bool CardPngExportIncludeHiddenFromLibrary { get; set; }

        /// <summary>
        ///     Output directory for relic inspect detail PNG export.
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_output_path")]
        public string RelicDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     Render scale for relic detail export.
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_scale")]
        public double RelicDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional <c>ModelId.Entry</c> substring for relic detail export; empty = all.
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_id_filter")]
        public string RelicDetailPngExportIdFilter { get; set; } = "";

        /// <summary>
        ///     When true, relic detail export includes the right-hand hover column.
        /// </summary>
        [JsonPropertyName("relic_detail_png_export_include_hover")]
        public bool RelicDetailPngExportIncludeHover { get; set; } = true;

        /// <summary>
        ///     Output directory for potion lab focus detail PNG export.
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_output_path")]
        public string PotionDetailPngExportOutputPath { get; set; } = "";

        /// <summary>
        ///     Render scale for potion detail export.
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_scale")]
        public double PotionDetailPngExportScale { get; set; } = 1d;

        /// <summary>
        ///     Optional <c>ModelId.Entry</c> substring for potion detail export; empty = all.
        /// </summary>
        [JsonPropertyName("potion_detail_png_export_id_filter")]
        public string PotionDetailPngExportIdFilter { get; set; } = "";
    }
}
