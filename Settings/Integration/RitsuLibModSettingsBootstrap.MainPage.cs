using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Updates;
using STS2RitsuLib.Utils.Persistence;

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
                    .AddSubpage(
                        "content_source_hover_tips_open",
                        T("ritsulib.modSourceHoverTips.pageLink.label", "Content source display"),
                        "content-source-hover-tips",
                        T("button.open", "Open"),
                        T("ritsulib.modSourceHoverTips.pageLink.description",
                            "Choose which content groups show source hover tips."))
                    .AddSubpage(
                        "content_mod_load_order_open",
                        T("ritsulib.contentModLoadOrder.pageLink.label", "Content mod load order"),
                        "content-mod-load-order",
                        T("button.open", "Open"),
                        T("ritsulib.contentModLoadOrder.pageLink.description",
                            "Sort, copy, or apply the saved load order for content-affecting mods and their dependencies."))
                    .AddChoice(
                        "modeldb_deterministic_sort_mode",
                        T("ritsulib.modelDbDeterministicSort.label", "Enable ModelDb deterministic sorting"),
                        ui.ModelDbDeterministicSortMode,
                        [
                            new("off", T("ritsulib.modelDbDeterministicSort.option.off", "Off")),
                            new("auto",
                                T("ritsulib.modelDbDeterministicSort.option.auto",
                                    "When RitsuLib-related registered content is detected")),
                            new("force", T("ritsulib.modelDbDeterministicSort.option.force", "Force enabled")),
                        ],
                        T("ritsulib.modelDbDeterministicSort.description",
                            "Controls whether ModelIdSerializationCache is rebuilt from deterministic final ModelDb content during initialization."),
                        ModSettingsChoicePresentation.Dropdown)
                    .AddButton(
                        "modeldb_deterministic_sort_now",
                        T("ritsulib.modelDbDeterministicSort.now.label", "Manual ModelDb deterministic sort"),
                        T("ritsulib.modelDbDeterministicSort.now.button", "Sort now"),
                        TryRebuildModelDbDeterministicCacheFromSettings,
                        ModSettingsButtonTone.Normal,
                        T("ritsulib.modelDbDeterministicSort.now.description",
                            "Rebuilds the current session's ModelIdSerializationCache immediately."))
                    .AddSubpage(
                        "toast_open",
                        T("ritsulib.toast.pageLink.label", "Toast notifications"),
                        "toast",
                        T("button.open", "Open"),
                        T("ritsulib.toast.pageLink.description",
                            "Configure stack placement, queue limits, and animation for global toast notifications."))
                    .AddChoice(
                        "ui_shell_theme_id",
                        T("ritsulib.uiShellTheme.label", "Interface theme"),
                        ui.UiShellThemeId,
                        RitsuShellThemeCatalog.RegisteredThemeIds
                            .Select(id => new ModSettingsChoiceOption<string>(id,
                                T($"ritsulib.uiShellTheme.option.{id}", id)))
                            .ToArray(),
                        T("ritsulib.uiShellTheme.description",
                            "Applies a built-in color theme to the Ritsu settings UI shell (sidebars, rows, and modals)."),
                        ModSettingsChoicePresentation.Dropdown)
                    .AddButton(
                        "ui_shell_theme_reset_file",
                        T("ritsulib.uiShellTheme.resetFile.label", "Reset existing theme files"),
                        T("ritsulib.uiShellTheme.resetFile.button", "Reset theme files"),
                        TryResetExistingThemeFiles,
                        ModSettingsButtonTone.Normal,
                        T("ritsulib.uiShellTheme.resetFile.description",
                            "Overwrite existing built-in theme files on disk with their embedded default versions."))
                    .AddToggle(
                        "update_check_enabled",
                        T("ritsulib.updateCheck.enabled.label", "Check for RitsuLib updates"),
                        ui.UpdateCheckEnabled,
                        T("ritsulib.updateCheck.enabled.description",
                            "Checks for RitsuLib updates periodically. Update notifications are shown on the main menu."))
                    .AddSlider(
                        "update_check_interval_minutes",
                        T("ritsulib.updateCheck.interval.label", "Automatic check interval (minutes)"),
                        ui.UpdateCheckIntervalMinutes,
                        5d,
                        360d,
                        5d,
                        value => value.ToString("0"),
                        T("ritsulib.updateCheck.interval.description",
                            "Controls how often automatic RitsuLib, Workshop, and registered mod update checks run."))
                    .AddToggle(
                        "update_check_skip_in_combat",
                        T("ritsulib.updateCheck.skipInCombat.label", "Defer automatic checks in combat rooms"),
                        ui.UpdateCheckSkipInCombat,
                        T("ritsulib.updateCheck.skipInCombat.description",
                            "When enabled, automatic update checks pause while the run is in a combat room and resume after leaving it."))
                    .AddToggle(
                        "main_menu_mod_settings_button_enabled",
                        T("ritsulib.mainMenuModSettingsButton.enabled.label", "Show main menu settings shortcut"),
                        ui.MainMenuModSettingsButtonEnabled,
                        T("ritsulib.mainMenuModSettingsButton.enabled.description",
                            "Shows a RitsuLib settings shortcut under the patch notes button on the main menu."))
                    .AddButton(
                        "update_check_now",
                        T("ritsulib.updateCheck.now.label", "Check now"),
                        T("ritsulib.updateCheck.now.button", "Check for updates"),
                        RitsuLibUpdateCheckService.CheckNowFromSettings,
                        ModSettingsButtonTone.Normal,
                        T("ritsulib.updateCheck.now.description",
                            "Checks the bundled RitsuLib update source immediately and shows a toast with the result."))
                    .AddToggle(
                        "debug_compatibility_mode",
                        T("ritsulib.debugCompatibility.label", "Debug compatibility mode"),
                        ui.DebugCompatibility,
                        T("ritsulib.debugCompatibility.description",
                            "Enable compatibility fallbacks for localization, unlock, and ancient-dialogue edge cases. Sub-toggles default to on.")))
                // Keep debug_compat_shims immediately after general (master toggle); do not insert sections between them.
                .AddSection(
                    "debug_compat_shims",
                    section => section
                        .WithVisibleWhen(RitsuLibSettingsStore.IsDebugCompatibilityMasterEnabled)
                        .WithTitle(T("ritsulib.section.debugCompatShims.title", "Compatibility fallbacks"))
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
                    .WithEnabledWhen(ModDataCloudHost.CanUseModDataCloud)
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
                .AddSection("steam_workshop_updates", section => section
                    .WithTitle(T("ritsulib.section.steamWorkshopUpdates.title", "Steam Workshop updates"))
                    .Collapsible()
                    .AddToggle(
                        "steam_workshop_auto_update_check_enabled",
                        T("ritsulib.steamWorkshop.autoUpdateCheck.label", "Auto-check Workshop updates"),
                        ui.SteamWorkshopAutoUpdateCheckEnabled,
                        T("ritsulib.steamWorkshop.autoUpdateCheck.description",
                            "Periodically checks subscribed Workshop items and asks Steam to download any items with available updates. Notifications are shown on the main menu."))
                    .AddButton(
                        "steam_workshop_check_now",
                        T("ritsulib.steamWorkshop.checkNow.label", "Check Workshop updates now"),
                        T("ritsulib.steamWorkshop.checkNow.button", "Check Workshop"),
                        SteamWorkshopUpdateCoordinator.CheckNowFromSettings,
                        ModSettingsButtonTone.Normal,
                        T("ritsulib.steamWorkshop.checkNow.description",
                            "Checks subscribed Workshop item states immediately and asks Steam to download any items with available updates.")))
                .AddSection("dev_debug_tools", section => section
                    .WithTitle(T("ritsulib.section.devDebugTools.title", "Developer debug tools"))
                    .Collapsible()
                    .AddToggle(
                        "dev_console_history_navigation_patch_enabled",
                        T("ritsulib.devConsole.historyNavigation.label", "Console history navigation fix"),
                        ui.DevConsoleHistoryNavigationPatchEnabled,
                        T("ritsulib.devConsole.historyNavigation.description",
                            "When enabled, RitsuLib replaces vanilla dev-console up/down history navigation with draft-preserving behavior."))
                    .AddToggle(
                        "dev_console_clear_input_on_visibility_change",
                        T("ritsulib.devConsole.clearInputOnVisibilityChange.label",
                            "Clear console input when hidden"),
                        ui.DevConsoleClearInputOnVisibilityChange,
                        T("ritsulib.devConsole.clearInputOnVisibilityChange.description",
                            "When enabled, hiding or showing the dev console clears the current input buffer. Off by default."))
                    .AddToggle(
                        "dev_console_autocomplete_enhancements_enabled",
                        T("ritsulib.devConsole.autocompleteEnhancements.label", "Console autocomplete enhancements"),
                        ui.DevConsoleAutocompleteEnhancementsEnabled,
                        T("ritsulib.devConsole.autocompleteEnhancements.description",
                            "When enabled, RitsuLib enhances dev-console autocomplete matching, candidate display, and extra command argument sources."))
                    .AddSubpage(
                        "debug_log_viewer_open",
                        T("ritsulib.debugLogViewer.pageLink.label", "Debug log viewer"),
                        "debug-log-viewer",
                        T("button.open", "Open"),
                        T("ritsulib.debugLogViewer.pageLink.description",
                            "Configure and open the browser-based live debug log viewer."))
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
                            "Inspect currently registered runtime hotkeys and their active bindings."))));
        }

        private static void TryResetExistingThemeFiles()
        {
            if (!RitsuShellThemeCatalog.TryRestoreAllExistingDiskThemesFromEmbedded(out _))
                return;
            RitsuShellThemeRuntime.ReapplyActiveTheme(true);
        }

        private static void TryRebuildModelDbDeterministicCacheFromSettings()
        {
            var title = L("ritsulib.modelDbDeterministicSort.toast.title", "ModelDb deterministic sort");
            if (!CanManuallyRebuildModelDbDeterministicCache())
            {
                var body = L(
                    "ritsulib.modelDbDeterministicSort.toast.blockedActiveSession",
                    "ModelDb deterministic sort can only be applied before a run, lobby, or network session starts.");
                RitsuLibFramework.Logger.Warn(
                    "[ModelIdSerializationCache] Manual deterministic rebuild blocked: active run, lobby, or network session.");
                RitsuToastService.ShowWarning(body, title);
                return;
            }

            var result = ModelIdSerializationCacheDynamicContentPatch.RebuildDeterministicCacheForSettings();
            if (!result.Applied)
            {
                var reason = result.Reason ?? L("ritsulib.modelDbDeterministicSort.toast.notAppliedUnknown",
                    "The cache could not be rebuilt.");
                RitsuLibFramework.Logger.Warn(
                    $"[ModelIdSerializationCache] Manual deterministic rebuild skipped: {reason}");
                RitsuToastService.ShowWarning(
                    string.Format(
                        L("ritsulib.modelDbDeterministicSort.toast.notApplied",
                            "ModelDb deterministic sort was not applied: {0}"),
                        reason),
                    title);
                return;
            }

            RitsuLibFramework.Logger.Info(
                $"[ModelIdSerializationCache] Manual deterministic rebuild applied. Hash: {result.InitialHash} -> {result.FinalHash}.");
            RitsuToastService.ShowInfo(
                string.Format(
                    L("ritsulib.modelDbDeterministicSort.toast.applied",
                        "ModelDb deterministic sort applied. Hash: {0} -> {1}."),
                    result.InitialHash,
                    result.FinalHash),
                title);
        }

        private static bool CanManuallyRebuildModelDbDeterministicCache()
        {
            try
            {
                var runManager = RunManager.Instance;
                return runManager is
                {
                    IsInProgress: false,
                    RunLobby: null,
                    NetService.IsConnected: false,
                };
            }
            catch
            {
                return false;
            }
        }
    }
}
