using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Updates;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterCategorySettingsPages(RitsuLibModSettingsUiBindings ui)
        {
            RegisterCoreSettingsPage(ui);
            RegisterCompatibilitySettingsPage(ui);
            RegisterUpdateSettingsPage(ui);
            RegisterCloudSettingsPage(ui);
            RegisterDeveloperToolsSettingsPage(ui);
        }

        private static void RegisterCoreSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-990)
                    .WithTitle(T("ritsulib.category.core.label", "Core settings"))
                    .WithDescription(T("ritsulib.category.core.description",
                        "Interface theme and main menu shortcut."))
                    .AddSection("core", section => ConfigureCoreSettingsSection(section, ui)),
                "core");
        }

        private static void ConfigureCoreSettingsSection(
            ModSettingsSectionBuilder section,
            RitsuLibModSettingsUiBindings ui)
        {
            section.WithTitle(T("ritsulib.category.core.label", "Core settings"));
#if !STS2_AT_LEAST_0_108_0
            AddLegacyModelDbDeterministicSortControls(section, ui);
#endif
            section.AddChoice(
                "ui_shell_theme_id",
                T("ritsulib.uiShellTheme.label", "Interface theme"),
                ui.UiShellThemeId,
                RitsuShellThemeCatalog.RegisteredThemeIds
                    .Select(id => new ModSettingsChoiceOption<string>(id,
                        T($"ritsulib.uiShellTheme.option.{id}", id)))
                    .ToArray(),
                T("ritsulib.uiShellTheme.description",
                    "Applies a built-in color theme to the Ritsu settings UI shell (sidebars, rows, and modals)."),
                ModSettingsChoicePresentation.Dropdown);
            section.AddButton(
                "ui_shell_theme_reset_file",
                T("ritsulib.uiShellTheme.resetFile.label", "Reset existing theme files"),
                T("ritsulib.uiShellTheme.resetFile.button", "Reset theme files"),
                TryResetExistingThemeFiles,
                ModSettingsButtonTone.Normal,
                T("ritsulib.uiShellTheme.resetFile.description",
                    "Overwrite existing built-in theme files on disk with their embedded default versions."));
            section.AddChoice(
                "canvas_texture_filter_mode",
                T("ritsulib.canvasTextureFilter.label", "2D texture filtering"),
                ui.CanvasTextureFilterMode,
                [
                    new("nearest", T("ritsulib.canvasTextureFilter.option.nearest", "Nearest")),
                    new("linear", T("ritsulib.canvasTextureFilter.option.linear", "Linear")),
                    new("nearest_mipmap",
                        T("ritsulib.canvasTextureFilter.option.nearestMipmap", "Nearest with mipmaps")),
                    new("linear_mipmap",
                        T("ritsulib.canvasTextureFilter.option.linearMipmap", "Linear with mipmaps")),
                ],
                T("ritsulib.canvasTextureFilter.description",
                    "Sets the default filter inherited by 2D canvas items. Mipmap modes use mip levels when the texture contains them."),
                ModSettingsChoicePresentation.Dropdown);
            section.AddToggle(
                "main_menu_mod_settings_button_enabled",
                T("ritsulib.mainMenuModSettingsButton.enabled.label", "Show main menu settings shortcut"),
                ui.MainMenuModSettingsButtonEnabled,
                T("ritsulib.mainMenuModSettingsButton.enabled.description",
                    "Shows a RitsuLib settings shortcut under the patch notes button on the main menu."));
        }

#if !STS2_AT_LEAST_0_108_0
        private static void AddLegacyModelDbDeterministicSortControls(
            ModSettingsSectionBuilder section,
            RitsuLibModSettingsUiBindings ui)
        {
            section.AddChoice(
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
                ModSettingsChoicePresentation.Dropdown);
            section.AddButton(
                "modeldb_deterministic_sort_now",
                T("ritsulib.modelDbDeterministicSort.now.label", "Manual ModelDb deterministic sort"),
                T("ritsulib.modelDbDeterministicSort.now.button", "Sort now"),
                TryRebuildModelDbDeterministicCacheFromSettings,
                ModSettingsButtonTone.Normal,
                T("ritsulib.modelDbDeterministicSort.now.description",
                    "Rebuilds the current session's ModelIdSerializationCache immediately."));
        }
#endif

        private static void RegisterCompatibilitySettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-700)
                    .WithTitle(T("ritsulib.category.compatibility.label", "Compatibility fallbacks"))
                    .WithDescription(T("ritsulib.category.compatibility.description",
                        "Debug compatibility mode and fallback shims."))
                    .AddSection("debug_compatibility", section => section
                        .WithTitle(T("ritsulib.debugCompatibility.label", "Debug compatibility mode"))
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
                                    "Inject empty Lines entries for ModContentRegistry ancients when vanilla provides no dialogue."))),
                "compatibility");
        }

        private static void RegisterUpdateSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-600)
                    .WithTitle(CreateUpdatesCategoryTitle())
                    .WithDescription(T("ritsulib.category.updates.description",
                        "RitsuLib and Steam Workshop update checks."))
                    .AddSection("steam_workshop_updates", section => section
                        .WithVisibleWhen(SteamWorkshopUpdateCoordinator.CanUseSteamWorkshopUpdates)
                        .WithTitle(T("ritsulib.section.steamWorkshopUpdates.title", "Steam Workshop updates"))
                        .Collapsible()
                        .AddSubpage(
                            "steam_workshop_management_open",
                            T("ritsulib.steamWorkshopManagement.pageLink.label", "Steam Workshop management"),
                            "steam-workshop-management",
                            T("button.open", "Open"),
                            T("ritsulib.steamWorkshopManagement.pageLink.description",
                                "Subscribe, unsubscribe, and inspect Workshop item states."))
                        .AddToggle(
                            "steam_workshop_auto_update_check_enabled",
                            T("ritsulib.steamWorkshop.autoUpdateCheck.label", "Auto-check Workshop updates"),
                            ui.SteamWorkshopAutoUpdateCheckEnabled,
                            T("ritsulib.steamWorkshop.autoUpdateCheck.description",
                                "Periodically checks subscribed Workshop items and asks Steam to queue available updates. Notifications are shown on the main menu."))
                        .AddButton(
                            "steam_workshop_check_now",
                            T("ritsulib.steamWorkshop.checkNow.label", "Check Workshop updates now"),
                            T("ritsulib.steamWorkshop.checkNow.button", "Check Workshop"),
                            SteamWorkshopUpdateCoordinator.CheckNowFromSettings,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.steamWorkshop.checkNow.description",
                                "Checks subscribed Workshop item states immediately and asks Steam to download any items with available updates.")))
                    .AddSection("ritsulib_updates", section => section
                        .WithTitle(T("ritsulib.section.updateChecks.title", "RitsuLib updates"))
                        .Collapsible()
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
                        .AddButton(
                            "update_check_now",
                            T("ritsulib.updateCheck.now.label", "Check now"),
                            T("ritsulib.updateCheck.now.button", "Check for updates"),
                            RitsuLibUpdateCheckService.CheckNowFromSettings,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.updateCheck.now.description",
                                "Checks the bundled RitsuLib update source immediately and shows a toast with the result."))),
                "updates");
        }

        private static ModSettingsText CreateUpdatesCategoryTitle()
        {
            return ModSettingsText.Dynamic(() => SteamWorkshopUpdateCoordinator.CanUseSteamWorkshopUpdates()
                ? L("ritsulib.category.updatesWithWorkshop.label", "Updates & Workshop")
                : L("ritsulib.category.updates.label", "Updates"));
        }

        private static void RegisterCloudSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-500)
                    .WithTitle(T("ritsulib.category.cloud.label", "Steam Cloud"))
                    .WithDescription(T("ritsulib.category.cloud.description",
                        "Mod data sync and manual Steam Cloud actions."))
                    .AddSection("steam_cloud_mod_data", section => section
                        .WithTitle(T("ritsulib.section.steamCloudModData.title", "Steam Cloud (mod data)"))
                        .WithVisibleWhen(ModDataCloudHost.CanUseModDataCloud)
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
                            T("ritsulib.modCloud.clear.button", "Clear cloud..."),
                            ModDataCloudManualCoordinator.TryClearRegisteredModDataFromSettings,
                            ModSettingsButtonTone.Danger,
                            T("ritsulib.modCloud.clear.description",
                                "Deletes mod data from Steam Cloud for this profile. Local files are not removed. Requires confirmation."))),
                "cloud");
        }

        private static void RegisterDeveloperToolsSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-300)
                    .WithTitle(T("ritsulib.category.developerTools.label", "Developer tools"))
                    .WithDescription(T("ritsulib.category.developerTools.description",
                        "Console fixes, diagnostics, self-checks, and export tools."))
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
                            T("ritsulib.devConsole.autocompleteEnhancements.label",
                                "Console autocomplete enhancements"),
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
                                "Run framework self-checks, export logs, saves, crash diagnostics, runtime metadata, and Harmony dump into a zip."))
                        .AddSubpage(
                            "image_png_export_open",
                            T("ritsulib.section.imagePngExport.title", "Image PNG export (dev)"),
                            "image-png-export",
                            T("button.open", "Open"),
                            T("ritsulib.section.imagePngExport.description",
                                "Card, relic, and potion image exports.")))
                    .AddSection("dev_common_folders", section => section
                        .WithTitle(T("ritsulib.section.devCommonFolders.title", "Common folders"))
                        .WithDescription(T("ritsulib.section.devCommonFolders.description",
                            "Open common game data folders in the system file explorer."))
                        .Collapsible()
                        .AddButton(
                            "open_logs_folder",
                            T("ritsulib.commonFolders.logs.label", "Logs folder"),
                            T("ritsulib.commonFolders.logs.button", "Open logs"),
                            () => GameCommonFolderOpener.OpenLogsFromUi(
                                L("ritsulib.commonFolders.toastTitle", "Common folders"),
                                "[Settings][CommonFolders][Logs]"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.commonFolders.logs.description",
                                "Opens the same folder as the open logs console command."))
                        .AddButton(
                            "open_saves_folder",
                            T("ritsulib.commonFolders.saves.label", "Current profile saves folder"),
                            T("ritsulib.commonFolders.saves.button", "Open saves"),
                            () => GameCommonFolderOpener.OpenSavesFromUi(
                                L("ritsulib.commonFolders.toastTitle", "Common folders"),
                                "[Settings][CommonFolders][Saves]"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.commonFolders.saves.description",
                                "Opens the current profile's saves folder, matching the open saves console command."))
                        .AddButton(
                            "open_user_data_root_folder",
                            T("ritsulib.commonFolders.root.label", "User data root"),
                            T("ritsulib.commonFolders.root.button", "Open root"),
                            () => GameCommonFolderOpener.OpenUserDataRootFromUi(
                                L("ritsulib.commonFolders.toastTitle", "Common folders"),
                                "[Settings][CommonFolders][Root]"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.commonFolders.root.description",
                                "Opens the game user data root folder."))
                        .AddButton(
                            "open_build_logs_folder",
                            T("ritsulib.commonFolders.buildLogs.label", "Build logs folder"),
                            T("ritsulib.commonFolders.buildLogs.button", "Open build logs"),
                            () => GameCommonFolderOpener.OpenBuildLogsFromUi(
                                L("ritsulib.commonFolders.toastTitle", "Common folders"),
                                "[Settings][CommonFolders][BuildLogs]"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.commonFolders.buildLogs.description",
                                "Opens Godot Mono build logs."))
                        .AddButton(
                            "open_localization_override_folder",
                            T("ritsulib.commonFolders.locOverride.label", "Localization override folder"),
                            T("ritsulib.commonFolders.locOverride.button", "Open localization"),
                            () => GameCommonFolderOpener.OpenLocalizationOverrideFromUi(
                                L("ritsulib.commonFolders.toastTitle", "Common folders"),
                                "[Settings][CommonFolders][LocOverride]"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.commonFolders.locOverride.description",
                                "Opens the localization_override folder."))),
                "developer-tools");
        }
    }
}
