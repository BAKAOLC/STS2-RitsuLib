using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterMainSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
                .WithModDisplayName(T("ritsulib.mod.displayName", "RitsuLib"))
                .WithModSidebarOrder(-10_000)
                .WithTitle(T("ritsulib.page.title", "Settings"))
                .WithDescription(T("ritsulib.page.description",
                    "Framework settings and settings UI reference entries."))
                .WithSortOrder(-1000)
                .AddSection("general", section => section
                    .WithTitle(T("ritsulib.section.general.title", "General"))
                    .WithDescription(T("ritsulib.section.general.description",
                        "Persisted framework settings exposed to players."))
                    .AddToggle(
                        "debug_compatibility_mode",
                        T("ritsulib.debugCompatibility.label", "Debug compatibility mode"),
                        ui.DebugCompatibility,
                        T("ritsulib.debugCompatibility.description",
                            "Enable compatibility fallbacks for localization, unlock, and ancient-dialogue edge cases. Sub-toggles default to on.")))
                .AddSection(
                    "debug_compat_shims",
                    section => section
                        .WithVisibleWhen(RitsuLibSettingsStore.IsDebugCompatibilityMasterEnabled)
                        .WithTitle(T("ritsulib.section.debugCompatShims.title", "Compatibility fallbacks"))
                        .WithDescription(T("ritsulib.section.debugCompatShims.description",
                            "Shown only when debug compatibility mode is enabled. Each toggle controls one fallback."))
                        .Collapsible()
                        .AddToggle(
                            "debug_compat_loc_table",
                            T("ritsulib.debugCompatLocTable.label", "LocTable missing keys"),
                            ui.DebugCompatLocTable,
                            T("ritsulib.debugCompatLocTable.description",
                                "Resolve missing keys to placeholder LocString values and log one [Localization][DebugCompat] warning per key."))
                        .AddToggle(
                            "debug_compat_unlock_epoch",
                            T("ritsulib.debugCompatUnlockEpoch.label", "Invalid unlock Epochs"),
                            ui.DebugCompatUnlockEpoch,
                            T("ritsulib.debugCompatUnlockEpoch.description",
                                "Skip invalid epoch grants on RitsuLib-registered unlock paths and log one [Unlocks][DebugCompat] warning per stable key."))
                        .AddToggle(
                            "debug_compat_ancient_architect",
                            T("ritsulib.debugCompatAncientArchitect.label", "THE_ARCHITECT missing dialogue"),
                            ui.DebugCompatAncientArchitect,
                            T("ritsulib.debugCompatAncientArchitect.description",
                                "Inject empty Lines entries for ModContentRegistry ancients when vanilla provides no dialogue.")))
                .AddSection("steam_cloud_mod_data", section => section
                    .WithTitle(T("ritsulib.section.steamCloudModData.title", "Steam Cloud (mod data)"))
                    .WithDescription(T("ritsulib.section.steamCloudModData.description",
                        "Manage syncing mod data with Steam Cloud here."))
                    .Collapsible()
                    .AddToggle(
                        "sync_mod_data_to_steam_cloud",
                        T("ritsulib.syncModDataSteamCloud.label", "Sync mod data to Steam Cloud"),
                        ui.SyncModDataSteamCloud,
                        T("ritsulib.syncModDataSteamCloud.description",
                            "When Steam Cloud is active, syncs mod data after saves and on profile load. Off keeps it local-only."))
                    .AddButton(
                        "mod_cloud_push_now",
                        T("ritsulib.modCloud.pushNow.label", "Upload to Steam now"),
                        T("ritsulib.modCloud.pushNow.button", "Push to cloud"),
                        ModDataCloudManualCoordinator.TryManualPushFromSettings,
                        ModSettingsButtonTone.Normal,
                        T("ritsulib.modCloud.pushNow.description",
                            "Uploads local mod data, overwriting the cloud copy."))
                    .AddButton(
                        "mod_cloud_pull_now",
                        T("ritsulib.modCloud.pullNow.label", "Download from Steam now"),
                        T("ritsulib.modCloud.pullNow.button", "Pull from cloud"),
                        ModDataCloudManualCoordinator.TryManualPullFromSettings,
                        ModSettingsButtonTone.Accent,
                        T("ritsulib.modCloud.pullNow.description",
                            "Downloads mod data from the cloud over the local copy, then reloads from disk."))
                    .AddButton(
                        "mod_cloud_clear_registered",
                        T("ritsulib.modCloud.clear.label", "Clear mod data from Steam Cloud"),
                        T("ritsulib.modCloud.clear.button", "Clear cloud…"),
                        ModDataCloudManualCoordinator.TryClearRegisteredModDataFromSettings,
                        ModSettingsButtonTone.Danger,
                        T("ritsulib.modCloud.clear.description",
                            "Deletes mod data from Steam Cloud for this profile. Local files are not removed. Requires confirmation.")))
                .AddSection("dev_debug_tools", section => section
                    .WithTitle(T("ritsulib.section.devDebugTools.title", "Developer debug tools"))
                    .Collapsible(true)
                    .AddSubpage(
                        "harmony_patch_dump_open",
                        T("ritsulib.section.harmonyDump.title", "Harmony patch dump"),
                        "harmony-patch-dump",
                        T("button.open", "Open"),
                        T("ritsulib.section.harmonyDump.description",
                            "Export a text report of patched methods (prefix/postfix/transpiler/finalizer) for debugging mod interactions."))
                    .AddSubpage(
                        "self_check_open",
                        T("ritsulib.section.selfCheck.title", "Self-check mode"),
                        "self-check",
                        T("button.open", "Open"),
                        T("ritsulib.section.selfCheck.description",
                            "Run framework self-checks, export logs and Harmony dump into one folder, then pack them into a zip."))
                    .AddSubpage(
                        "image_png_export_open",
                        T("ritsulib.section.imagePngExport.title", "Image PNG export (dev)"),
                        "image-png-export",
                        T("button.open", "Open"),
                        T("ritsulib.section.imagePngExport.description",
                            "Card, relic, and potion image exports.")))
                .AddSection("reference", section => section
                    .WithTitle(T("ritsulib.section.reference.title", "Reference"))
                    .WithDescription(T("ritsulib.section.reference.description",
                        "Reference controls available in the settings UI."))
                    .Collapsible()
                    .AddParagraph(
                        "reference_intro",
                        T("ritsulib.reference.intro",
                            "Open the control preview page to inspect available controls and layout behavior."))
                    .AddSubpage(
                        "reference_gallery",
                        T("ritsulib.reference.gallery.label", "Control preview"),
                        "debug-showcase",
                        T("button.open", "Open"),
                        T("ritsulib.reference.gallery.description",
                            "Reference page only. Values on this page are not persisted."))
                    .AddSubpage(
                        "reference_runtime_hotkeys",
                        T("ritsulib.reference.runtimeHotkeys.label", "Registered hotkeys"),
                        "runtime-hotkeys",
                        T("button.open", "Open"),
                        T("ritsulib.reference.runtimeHotkeys.description",
                            "Inspect currently registered runtime hotkeys and their active bindings.")))
                .AddSection("host_surface_reference", section => section
                    .WithTitle(T("ritsulib.section.hostSurfaceReference.title", "Settings host surfaces"))
                    .WithDescription(T("ritsulib.section.hostSurfaceReference.description",
                        "Main menu vs run pause vs combat pause: visibility and read-only masks for mod settings pages."))
                    .Collapsible()
                    .AddParagraph(
                        "host_surface_intro",
                        T("ritsulib.hostSurface.intro",
                            "Use WithVisibleOnHostSurfaces / WithReadOnlyOnHostSurfaces on pages and sections. Defaults keep classic global/profile controls editable on every surface."))
                    .AddParagraph(
                        "host_surface_tiers",
                        T("ritsulib.hostSurface.tiers",
                            "Preset ideas: (1) leave defaults for UI aids and hotkeys; (2) add WithReadOnlyOnHostSurfaces(CombatPause) for run-locked difficulty fields; (3) WithVisibleOnHostSurfaces(CombatPause) for in-fight debug tools. Combine WithVisibleWhen when a value may be reapplied from menus while a run is already in progress."))
                    .AddToggle(
                        "host_surface_combat_readonly_demo",
                        T("ritsulib.hostSurface.demoToggle.label", "Combat pause read-only demo"),
                        ui.HostSurfaceCombatReadOnlyDemo,
                        T("ritsulib.hostSurface.demoToggle.description",
                            "Read-only while paused mid-combat; editable on the main menu or when paused outside combat."))
                    .WithReadOnlyOnHostSurfaces(ModSettingsHostSurface.CombatPause))
                .AddSection("host_surface_combat_only_demo", section => section
                    .WithTitle(T("ritsulib.section.hostSurfaceCombatOnly.title", "Combat-only visibility demo"))
                    .WithVisibleOnHostSurfaces(ModSettingsHostSurface.CombatPause)
                    .AddParagraph(
                        "host_surface_combat_only_body",
                        T("ritsulib.hostSurface.combatOnly.body",
                            "This block is hidden unless you open mod settings from a pause while a fight is active."))));
        }
    }
}
