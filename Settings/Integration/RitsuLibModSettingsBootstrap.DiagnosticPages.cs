using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterHarmonySelfCheckAndCompendiumPages(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-250)
                    .WithTitle(T("ritsulib.page.harmonyDump.title", "Harmony patch dump"))
                    .WithDescription(T("ritsulib.page.harmonyDump.description",
                        "Export a text report of patched methods (prefix/postfix/transpiler/finalizer) for debugging mod interactions."))
                    .AddSection("harmony_patch_dump", section => section
                        .AddString(
                            "harmony_patch_dump_output_path",
                            T("ritsulib.harmonyDump.path.label", "Output file path"),
                            ui.HarmonyPatchDumpOutputPath,
                            T("ritsulib.harmonyDump.path.placeholder",
                                "Absolute path or user://… (e.g. user://ritsulib_harmony_patch_dump.log)"),
                            1024,
                            T("ritsulib.harmonyDump.path.description",
                                "Where to write the patch report. Use Browse to pick a file, or type a full path or Godot user:// path."))
                        .AddToggle(
                            "harmony_patch_dump_on_first_main_menu",
                            T("ritsulib.harmonyDump.auto.label", "Dump when main menu first loads"),
                            ui.HarmonyPatchDumpOnFirstMainMenu,
                            T("ritsulib.harmonyDump.auto.description",
                                "Once per game session, after the main menu finishes loading, write the report if the output path is set."))
                        .AddButton(
                            "harmony_patch_dump_browse",
                            T("ritsulib.harmonyDump.browse.label", "Choose output file"),
                            T("ritsulib.harmonyDump.browse.button", "Browse…"),
                            host => HarmonyPatchDumpSaveDialog.Show(ui.HarmonyPatchDumpOutputPath, host),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.harmonyDump.browse.hint",
                                "Opens a save dialog and fills the output path above."))
                        .AddButton(
                            "harmony_patch_dump_now",
                            T("ritsulib.harmonyDump.now.label", "Write dump now"),
                            T("ritsulib.harmonyDump.now.button", "Dump now"),
                            HarmonyPatchDumpCoordinator.TryManualDumpFromSettings,
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.harmonyDump.now.description",
                                "Generates the report immediately using the output path. Check the log for success or errors."))),
                "harmony-patch-dump");

            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-225)
                    .WithTitle(T("ritsulib.page.selfCheck.title", "Self-check mode"))
                    .WithDescription(T("ritsulib.page.selfCheck.description",
                        "Run built-in framework checks and export all diagnostics in one package."))
                    .AddSection("self_check", section => section
                        .AddString(
                            "self_check_output_folder_path",
                            T("ritsulib.selfCheck.path.label", "Output folder"),
                            ui.SelfCheckOutputFolder,
                            T("ritsulib.selfCheck.path.placeholder",
                                "Absolute path or user://… (e.g. user://ritsulib_self_check)"),
                            1024,
                            T("ritsulib.selfCheck.path.description",
                                "Self-check artifacts are written to a timestamped folder, then zipped in the same parent folder."))
                        .AddToggle(
                            "self_check_on_first_main_menu",
                            T("ritsulib.selfCheck.auto.label", "Run once when main menu first loads"),
                            ui.SelfCheckOnFirstMainMenu,
                            T("ritsulib.selfCheck.auto.description",
                                "Automatically runs one self-check export per game session after the first main menu load."))
                        .AddButton(
                            "self_check_browse",
                            T("ritsulib.selfCheck.browse.label", "Choose output folder"),
                            T("ritsulib.selfCheck.browse.button", "Browse…"),
                            host => ModSettingsOpenFolderDialog.Show(
                                ui.SelfCheckOutputFolder,
                                host,
                                "SelfCheck",
                                "ritsulib.selfCheck.browseTitle",
                                "Choose self-check output folder"),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.selfCheck.browse.hint",
                                "Opens a folder picker and fills the output folder above."))
                        .AddButton(
                            "self_check_open_folder",
                            T("ritsulib.selfCheck.openFolder.label", "Open output folder"),
                            T("ritsulib.selfCheck.openFolder.button", "Open folder"),
                            SelfCheckBundleCoordinator.TryOpenOutputFolderFromSettings,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.selfCheck.openFolder.hint",
                                "Opens the configured output folder in your system file explorer."))
                        .AddButton(
                            "self_check_run_now",
                            T("ritsulib.selfCheck.runNow.label", "Run self-check now"),
                            T("ritsulib.selfCheck.runNow.button", "Run now"),
                            SelfCheckBundleCoordinator.TryManualRunFromSettings,
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.selfCheck.runNow.description",
                                "Exports self-check report, Harmony patch dump, and godot.log copy, then creates a zip package."))),
                "self-check");
        }
    }
}
