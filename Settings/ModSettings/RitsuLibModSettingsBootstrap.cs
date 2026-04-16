using System.Globalization;
using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuLibModSettingsBootstrap
    {
        private static readonly Lock InitLock = new();
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                var debugCompatibilityBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatibilityMode,
                    (settings, value) => settings.DebugCompatibilityMode = value);

                var debugCompatLocTableBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatLocTable,
                    (settings, value) => settings.DebugCompatLocTable = value);

                var debugCompatUnlockEpochBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatUnlockEpoch,
                    (settings, value) => settings.DebugCompatUnlockEpoch = value);

                var debugCompatAncientArchitectBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatAncientArchitect,
                    (settings, value) => settings.DebugCompatAncientArchitect = value);

                var harmonyPatchDumpPathBinding = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.HarmonyPatchDumpOutputPath,
                    (settings, value) => settings.HarmonyPatchDumpOutputPath = value);

                var harmonyPatchDumpOnFirstMainMenuBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.HarmonyPatchDumpOnFirstMainMenu,
                    (settings, value) => settings.HarmonyPatchDumpOnFirstMainMenu = value);

                var selfCheckOutputFolderBinding = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.SelfCheckOutputFolderPath,
                    (settings, value) => settings.SelfCheckOutputFolderPath = value);

                var selfCheckOnFirstMainMenuBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.SelfCheckOnFirstMainMenu,
                    (settings, value) => settings.SelfCheckOnFirstMainMenu = value);

                var cardPngExportPathBinding = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportOutputPath,
                    (settings, value) => settings.CardPngExportOutputPath = value);

                var cardPngExportIncludeHoverBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeHover,
                    (settings, value) => settings.CardPngExportIncludeHover = value);

                var cardPngExportIncludeUpgradesBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeUpgrades,
                    (settings, value) => settings.CardPngExportIncludeUpgrades = value);

                var cardPngExportScaleBinding = ModSettingsBindings.Global<RitsuLibSettings, double>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportScale,
                    (settings, value) => settings.CardPngExportScale = value);

                var cardPngExportFilterBinding = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIdFilter,
                    (settings, value) => settings.CardPngExportIdFilter = value);

                var cardPngExportIncludeHiddenBinding = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeHiddenFromLibrary,
                    (settings, value) => settings.CardPngExportIncludeHiddenFromLibrary = value);

                var showcaseState = new DebugShowcaseState();
                var previewToggleBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_toggle", showcaseState.ToggleValue);
                var runtimeHotkeyCategoryOrder = new[]
                {
                    "Gameplay",
                    "UI",
                    "Debug",
                    "Developer tools",
                    "Other",
                };
                var previewSliderBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_slider", showcaseState.SliderValue);
                var previewIntSliderBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_int_slider", showcaseState.IntSliderValue);
                var previewChoiceBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_choice", showcaseState.ChoiceValue);
                var previewChoiceDropdownBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_choice_dropdown",
                        showcaseState.ChoiceDropdownValue);
                var previewEnumBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_mode", showcaseState.ModeValue);
                var previewStringBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string", showcaseState.StringValue);
                var previewStringMultiBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string_multi", showcaseState.StringMultiValue);
                var previewListBinding =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_list", showcaseState.ListItems.ToList());

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
                            debugCompatibilityBinding,
                            T("ritsulib.debugCompatibility.description",
                                "Enable compatibility fallbacks for localization, unlock, and ancient-dialogue edge cases. Sub-toggles default to on.")))
                    .AddSection(
                        "debug_compat_shims",
                        section => section
                            .WithVisibleWhen(RitsuLibSettingsStore.IsDebugCompatibilityMasterEnabled)
                            .WithTitle(T("ritsulib.section.debugCompatShims.title", "Compatibility fallbacks"))
                            .WithDescription(T("ritsulib.section.debugCompatShims.description",
                                "Shown only when debug compatibility mode is enabled. Each toggle controls one fallback."))
                            .AddToggle(
                                "debug_compat_loc_table",
                                T("ritsulib.debugCompatLocTable.label", "LocTable missing keys"),
                                debugCompatLocTableBinding,
                                T("ritsulib.debugCompatLocTable.description",
                                    "Resolve missing keys to placeholder LocString values and log one [Localization][DebugCompat] warning per key."))
                            .AddToggle(
                                "debug_compat_unlock_epoch",
                                T("ritsulib.debugCompatUnlockEpoch.label", "Invalid unlock Epochs"),
                                debugCompatUnlockEpochBinding,
                                T("ritsulib.debugCompatUnlockEpoch.description",
                                    "Skip invalid epoch grants on RitsuLib-registered unlock paths and log one [Unlocks][DebugCompat] warning per stable key."))
                            .AddToggle(
                                "debug_compat_ancient_architect",
                                T("ritsulib.debugCompatAncientArchitect.label", "THE_ARCHITECT missing dialogue"),
                                debugCompatAncientArchitectBinding,
                                T("ritsulib.debugCompatAncientArchitect.description",
                                    "Inject empty Lines entries for ModContentRegistry ancients when vanilla provides no dialogue.")))
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
                            "card_png_export_open",
                            T("ritsulib.section.cardPngExport.title", "Card PNG export (dev)"),
                            "card-png-export",
                            T("button.open", "Open"),
                            T("ritsulib.section.cardPngExport.description",
                                "Library card set.")))
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
                                "Inspect currently registered runtime hotkeys and their active bindings."))));

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
                                harmonyPatchDumpPathBinding,
                                T("ritsulib.harmonyDump.path.placeholder",
                                    "Absolute path or user://… (e.g. user://ritsulib_harmony_patch_dump.log)"),
                                1024,
                                T("ritsulib.harmonyDump.path.description",
                                    "Where to write the patch report. Use Browse to pick a file, or type a full path or Godot user:// path."))
                            .AddToggle(
                                "harmony_patch_dump_on_first_main_menu",
                                T("ritsulib.harmonyDump.auto.label", "Dump when main menu first loads"),
                                harmonyPatchDumpOnFirstMainMenuBinding,
                                T("ritsulib.harmonyDump.auto.description",
                                    "Once per game session, after the main menu finishes loading, write the report if the output path is set."))
                            .AddButton(
                                "harmony_patch_dump_browse",
                                T("ritsulib.harmonyDump.browse.label", "Choose output file"),
                                T("ritsulib.harmonyDump.browse.button", "Browse…"),
                                host => HarmonyPatchDumpSaveDialog.Show(harmonyPatchDumpPathBinding, host),
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
                                selfCheckOutputFolderBinding,
                                T("ritsulib.selfCheck.path.placeholder",
                                    "Absolute path or user://… (e.g. user://ritsulib_self_check)"),
                                1024,
                                T("ritsulib.selfCheck.path.description",
                                    "Self-check artifacts are written to a timestamped folder, then zipped in the same parent folder."))
                            .AddToggle(
                                "self_check_on_first_main_menu",
                                T("ritsulib.selfCheck.auto.label", "Run once when main menu first loads"),
                                selfCheckOnFirstMainMenuBinding,
                                T("ritsulib.selfCheck.auto.description",
                                    "Automatically runs one self-check export per game session after the first main menu load."))
                            .AddButton(
                                "self_check_browse",
                                T("ritsulib.selfCheck.browse.label", "Choose output folder"),
                                T("ritsulib.selfCheck.browse.button", "Browse…"),
                                host => SelfCheckExportFolderDialog.Show(selfCheckOutputFolderBinding, host),
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

                RitsuLibFramework.RegisterModSettings(
                    Const.ModId,
                    page => page
                        .AsChildOf(Const.ModId)
                        .WithSortOrder(-200)
                        .WithTitle(T("ritsulib.page.cardPngExport.title", "Card PNG export (dev)"))
                        .WithDescription(T("ritsulib.page.cardPngExport.description",
                            "Same card set as the library."))
                        .AddSection("card_png_export", section => section
                            .AddString(
                                "card_png_export_output_path",
                                T("ritsulib.cardPngExport.path.label", "Output folder"),
                                cardPngExportPathBinding,
                                T("ritsulib.cardPngExport.path.placeholder",
                                    "Absolute path or user://… (e.g. user://ritsu_card_png)"),
                                1024)
                            .AddButton(
                                "card_png_export_browse",
                                T("ritsulib.cardPngExport.browse.label", "Choose output folder"),
                                T("ritsulib.cardPngExport.browse.button", "Browse…"),
                                host => CardPngExportFolderDialog.Show(cardPngExportPathBinding, host))
                            .AddToggle(
                                "card_png_export_include_hover",
                                T("ritsulib.cardPngExport.hover.label", "Include hover-tip panel"),
                                cardPngExportIncludeHoverBinding)
                            .AddToggle(
                                "card_png_export_include_upgrades",
                                T("ritsulib.cardPngExport.upgrades.label", "Export upgraded variants"),
                                cardPngExportIncludeUpgradesBinding)
                            .AddToggle(
                                "card_png_export_include_hidden",
                                T("ritsulib.cardPngExport.hidden.label", "Include non-library cards"),
                                cardPngExportIncludeHiddenBinding)
                            .AddSlider(
                                "card_png_export_scale",
                                T("ritsulib.cardPngExport.scale.label", "Render scale"),
                                cardPngExportScaleBinding,
                                0.25d,
                                4d,
                                0.25d,
                                v => v.ToString("0.##", CultureInfo.InvariantCulture))
                            .AddString(
                                "card_png_export_id_filter",
                                T("ritsulib.cardPngExport.filter.label", "Card id contains (optional)"),
                                cardPngExportFilterBinding,
                                T("ritsulib.cardPngExport.filter.placeholder", "Empty = all cards; e.g. WINE_"),
                                256,
                                T("ritsulib.cardPngExport.filter.description",
                                    "Substring match, case-insensitive."))
                            .AddButton(
                                "card_png_export_start",
                                T("ritsulib.cardPngExport.start.label", "Start export"),
                                T("ritsulib.cardPngExport.start.button", "Start export"),
                                () => CardPngExportSettingsActions.TryBeginFromSettings(
                                    cardPngExportPathBinding,
                                    cardPngExportIncludeHoverBinding,
                                    cardPngExportIncludeUpgradesBinding,
                                    cardPngExportScaleBinding,
                                    cardPngExportFilterBinding,
                                    cardPngExportIncludeHiddenBinding),
                                ModSettingsButtonTone.Accent)),
                    "card-png-export");

                RitsuLibFramework.RegisterModSettings(
                    Const.ModId,
                    page => ConfigureRuntimeHotkeysPage(page, runtimeHotkeyCategoryOrder),
                    "runtime-hotkeys");

                RitsuLibFramework.RegisterModSettings(
                    Const.ModId,
                    page => page
                        .AsChildOf(Const.ModId)
                        .WithTitle(T("ritsulib.showcase.title", "Control Preview"))
                        .WithDescription(T("ritsulib.showcase.description",
                            "Demonstrates supported controls and dynamic descriptions without persisting values."))
                        .AddSection("overview", section => section
                            .WithTitle(T("ritsulib.showcase.overview.title", "Overview"))
                            .AddHeader(
                                "showcase_header",
                                T("ritsulib.showcase.header", "Preview-only controls"),
                                T("ritsulib.showcase.header.description",
                                    "Reference controls backed by preview-only bindings."))
                            .AddParagraph(
                                "showcase_paragraph",
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.summary",
                                            "Toggle: {0} | Double: {1:0.##} | Int: {2} | Choice: {3} | Dropdown: {4} | Mode: {5} | Action Count: {6}"),
                                        showcaseState.ToggleValue,
                                        showcaseState.SliderValue,
                                        showcaseState.IntSliderValue,
                                        showcaseState.ChoiceValue,
                                        showcaseState.ChoiceDropdownValue,
                                        showcaseState.ModeValue,
                                        showcaseState.ActionCount)))
                            .AddImage(
                                "showcase_image",
                                T("ritsulib.showcase.image.label", "Reference image"),
                                () => ModSettingsUiResources.SettingsButtonTexture,
                                120f,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.image.description",
                                            "Image previews can participate in dynamic descriptions. Current mode: {0}"),
                                        showcaseState.ModeValue))))
                        .AddSection("inputs", section => section
                            .WithTitle(T("ritsulib.showcase.inputs.title", "Inputs"))
                            .WithDescription(T("ritsulib.showcase.inputs.description",
                                "Editing these controls updates the preview state only."))
                            .Collapsible()
                            .AddToggle(
                                "preview_toggle",
                                T("ritsulib.showcase.toggle.label", "Preview toggle"),
                                new ShowcaseBinding<bool>(previewToggleBinding,
                                    value => showcaseState.ToggleValue = value),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.toggle.description", "Current value: {0}"),
                                        showcaseState.ToggleValue)))
                            .AddSlider(
                                "preview_slider",
                                T("ritsulib.showcase.slider.label", "Preview slider"),
                                new ShowcaseBinding<double>(previewSliderBinding,
                                    value => showcaseState.SliderValue = value),
                                0d,
                                100d,
                                0.25d,
                                value => value.ToString("0.##"),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.slider.description", "Current double value: {0:0.##}"),
                                        showcaseState.SliderValue)))
                            .AddIntSlider(
                                "preview_int_slider",
                                T("ritsulib.showcase.intSlider.label", "Preview integer slider"),
                                new ShowcaseBinding<int>(previewIntSliderBinding,
                                    value => showcaseState.IntSliderValue = value),
                                0,
                                5,
                                1,
                                value => value.ToString(),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.intSlider.description", "Current integer value: {0}"),
                                        showcaseState.IntSliderValue)))
                            .AddChoice(
                                "preview_choice",
                                T("ritsulib.showcase.choice.label", "Preview choice"),
                                new ShowcaseBinding<string>(previewChoiceBinding,
                                    value => showcaseState.ChoiceValue = value),
                                [
                                    new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                    new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                    new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                                ],
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.choice.description", "Current choice: {0}"),
                                        showcaseState.ChoiceValue)))
                            .AddChoice(
                                "preview_choice_dropdown",
                                T("ritsulib.showcase.choiceDropdown.label", "Preview choice (dropdown)"),
                                new ShowcaseBinding<string>(previewChoiceDropdownBinding,
                                    value => showcaseState.ChoiceDropdownValue = value),
                                [
                                    new("compact", T("ritsulib.showcase.choice.compact", "Compact")),
                                    new("balanced", T("ritsulib.showcase.choice.balanced", "Balanced")),
                                    new("wide", T("ritsulib.showcase.choice.wide", "Wide")),
                                ],
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.choiceDropdown.description",
                                            "Same options as the stepper control, rendered as a dropdown. Current: {0}"),
                                        showcaseState.ChoiceDropdownValue)),
                                ModSettingsChoicePresentation.Dropdown)
                            .AddEnumChoice(
                                "preview_mode",
                                T("ritsulib.showcase.mode.label", "Preview enum choice"),
                                new ShowcaseBinding<ShowcaseMode>(previewEnumBinding,
                                    value => showcaseState.ModeValue = value),
                                mode => T($"ritsulib.showcase.mode.{mode}", mode.ToString()),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.mode.description", "Current mode: {0}"),
                                        showcaseState.ModeValue)))
                            .AddString(
                                "preview_string",
                                T("ritsulib.showcase.string.label", "Preview string field"),
                                new ShowcaseBinding<string>(previewStringBinding,
                                    value => showcaseState.StringValue = value),
                                T("ritsulib.showcase.string.placeholder", "Single-line string binding"),
                                null,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.string.description", "Current text: {0}"),
                                        showcaseState.StringValue)))
                            .AddMultilineString(
                                "preview_string_multi",
                                T("ritsulib.showcase.stringMulti.label", "Preview multiline field"),
                                new ShowcaseBinding<string>(previewStringMultiBinding,
                                    value => showcaseState.StringMultiValue = value),
                                T("ritsulib.showcase.stringMulti.placeholder",
                                    "Multiple lines — Enter inserts a new line."),
                                null,
                                ModSettingsText.Dynamic(() =>
                                {
                                    var t = showcaseState.StringMultiValue ?? string.Empty;
                                    var lineCount = string.IsNullOrEmpty(t) ? 0 : t.Split('\n').Length;
                                    return string.Format(
                                        L("ritsulib.showcase.stringMulti.description",
                                            "{0} characters, {1} lines."),
                                        t.Length,
                                        lineCount);
                                })))
                        .AddSection("actions", section => section
                            .WithTitle(T("ritsulib.showcase.actions.title", "Commands"))
                            .WithDescription(T("ritsulib.showcase.actions.description",
                                "Buttons can mutate preview state and refresh adjacent descriptions."))
                            .Collapsible()
                            .AddButton(
                                "preview_action",
                                T("ritsulib.showcase.action.label", "Preview command button"),
                                T("ritsulib.showcase.action.button", "Trigger"),
                                () => showcaseState.ActionCount++,
                                ModSettingsButtonTone.Accent,
                                ModSettingsText.Dynamic(() =>
                                    string.Format(
                                        L("ritsulib.showcase.action.description", "Command invoked {0} times."),
                                        showcaseState.ActionCount)))
                            .AddButton(
                                "preview_reset",
                                T("ritsulib.showcase.reset.label", "Reset preview bindings"),
                                T("ritsulib.showcase.reset.button", "Reset"),
                                () =>
                                {
                                    showcaseState.ToggleValue = true;
                                    showcaseState.SliderValue = 35d;
                                    showcaseState.IntSliderValue = 2;
                                    showcaseState.ChoiceValue = "balanced";
                                    showcaseState.ChoiceDropdownValue = "wide";
                                    showcaseState.ModeValue = ShowcaseMode.Balanced;
                                    showcaseState.ActionCount = 0;
                                    showcaseState.StringValue = "Single line";
                                    showcaseState.StringMultiValue = "First line\nSecond line";
                                    previewToggleBinding.Write(showcaseState.ToggleValue);
                                    previewSliderBinding.Write(showcaseState.SliderValue);
                                    previewIntSliderBinding.Write(showcaseState.IntSliderValue);
                                    previewChoiceBinding.Write(showcaseState.ChoiceValue);
                                    previewChoiceDropdownBinding.Write(showcaseState.ChoiceDropdownValue);
                                    previewEnumBinding.Write(showcaseState.ModeValue);
                                    previewStringBinding.Write(showcaseState.StringValue);
                                    previewStringMultiBinding.Write(showcaseState.StringMultiValue);
                                },
                                ModSettingsButtonTone.Danger,
                                T("ritsulib.showcase.reset.description",
                                    "Restore all preview bindings to default values without persisting data."))
                            .AddParagraph(
                                "showcase_footer",
                                T("ritsulib.showcase.footer",
                                    "Use this page as a reference when implementing settings pages.")))
                        .AddSection("list", section => section
                            .WithTitle(T("ritsulib.showcase.list.title", "Structured List"))
                            .WithDescription(T("ritsulib.showcase.list.description",
                                "Structured collections can be edited, reordered, added, and removed inside the settings UI."))
                            .Collapsible()
                            .AddList(
                                "preview_list",
                                T("ritsulib.showcase.list.label", "Preview structured collection"),
                                new ShowcaseBinding<List<ShowcaseListItem>>(previewListBinding,
                                    value => showcaseState.ListItems = value.ToList()),
                                showcaseState.CreateListItem,
                                item => ModSettingsText.Literal($"{item.Name} ({item.Weight})"),
                                item => ModSettingsText.Literal(item.Enabled
                                    ? $"Enabled item - tag: {item.Tag} - notes: {item.Details.Count}"
                                    : $"Disabled item - tag: {item.Tag} - notes: {item.Details.Count}"),
                                CreateShowcaseListItemEditor,
                                ModSettingsStructuredData.Json<ShowcaseListItem>(),
                                T("ritsulib.showcase.list.add", "Add Item"),
                                ModSettingsText.Dynamic(() =>
                                    string.Format(L("ritsulib.showcase.list.summary", "Current item count: {0}"),
                                        showcaseState.ListItems.Count)),
                                true,
                                false,
                                null)),
                    "debug-showcase");

                _initialized = true;
            }
        }

        private static ModSettingsPageBuilder ConfigureRuntimeHotkeysPage(
            ModSettingsPageBuilder page,
            IReadOnlyList<string> categoryOrder)
        {
            page
                .AsChildOf(Const.ModId)
                .WithSortOrder(-175)
                .WithTitle(T("ritsulib.page.runtimeHotkeys.title", "Registered hotkeys"))
                .WithDescription(T("ritsulib.page.runtimeHotkeys.description",
                    "Inspect currently registered runtime hotkeys and their active bindings."))
                .AddSection("runtime_hotkeys_overview", section => section
                    .WithTitle(T("ritsulib.section.runtimeHotkeys.title", "Runtime hotkeys"))
                    .WithDescription(T("ritsulib.section.runtimeHotkeys.description",
                        "Lists active runtime hotkey registrations grouped by category."))
                    .AddParagraph(
                        "runtime_hotkeys_summary",
                        ModSettingsText.Dynamic(() =>
                        {
                            var hotkeys = RuntimeHotkeyService.GetRegisteredHotkeys();
                            return hotkeys.Count == 0
                                ? L("ritsulib.runtimeHotkeys.empty",
                                    "No runtime hotkeys are currently registered.")
                                : string.Format(
                                    L("ritsulib.runtimeHotkeys.summary",
                                        "{0} runtime hotkeys are currently registered."), hotkeys.Count);
                        }))
                    .AddParagraph(
                        "runtime_hotkeys_intro",
                        T("ritsulib.runtimeHotkeys.groups.description",
                            "Each entry shows the current binding, display name, optional description, and registration id.")));

            var groups = GetOrderedRuntimeHotkeyGroups(categoryOrder);
            if (groups.Count == 0)
            {
                page.AddSection("runtime_hotkeys_empty", section => section
                    .WithTitle(T("ritsulib.section.runtimeHotkeys.empty.title", "No registered hotkeys"))
                    .AddParagraph(
                        "runtime_hotkeys_empty_text",
                        T("ritsulib.runtimeHotkeys.empty", "No runtime hotkeys are currently registered.")));
                return page;
            }

            foreach (var (category, runtimeHotkeyRegistrationInfos) in groups)
            {
                var sectionId = $"runtime_hotkeys_group_{SanitizeRuntimeHotkeySectionId(category)}";
                page.AddSection(sectionId, section =>
                {
                    section
                        .WithTitle(ModSettingsText.Literal(category))
                        .WithDescription(ModSettingsText.Dynamic(() => string.Format(
                            L("ritsulib.runtimeHotkeys.groupSummary", "{0} hotkeys in this category."),
                            CountRuntimeHotkeysInCategory(category))))
                        .Collapsible(true);

                    for (var i = 0; i < runtimeHotkeyRegistrationInfos.Count; i++)
                    {
                        var (lookupBinding, _, lookupId, lookupDisplayName, _, _, _, _, _, _, _) =
                            runtimeHotkeyRegistrationInfos[i];
                        var entryIdBase =
                            $"{i}_{SanitizeRuntimeHotkeySectionId(lookupId ?? lookupDisplayName ?? lookupBinding)}";
                        section
                            .AddHeader(
                                $"{entryIdBase}_header",
                                ModSettingsText.Literal(lookupDisplayName ?? lookupId ?? lookupBinding),
                                ModSettingsText.Dynamic(() => FormatRuntimeHotkeyHeaderDescription(
                                    lookupId,
                                    lookupBinding,
                                    lookupDisplayName)))
                            .AddParagraph(
                                $"{entryIdBase}_details",
                                ModSettingsText.Dynamic(() => FormatRuntimeHotkeyDetails(
                                    lookupId,
                                    lookupBinding,
                                    lookupDisplayName)));
                    }
                });
            }

            return page;
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsText.I18N(ModSettingsLocalization.Instance, key, fallback);
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static List<RuntimeHotkeyCategoryGroup> GetOrderedRuntimeHotkeyGroups(
            IReadOnlyList<string> categoryOrder)
        {
            var hotkeys = RuntimeHotkeyService.GetRegisteredHotkeys();
            return hotkeys
                .GroupBy(info => string.IsNullOrWhiteSpace(info.Category)
                    ? L("ritsulib.runtimeHotkeys.category.other", "Other")
                    : info.Category!)
                .OrderBy(group =>
                {
                    var index = -1;
                    for (var i = 0; i < categoryOrder.Count; i++)
                    {
                        if (!string.Equals(categoryOrder[i], group.Key, StringComparison.Ordinal))
                            continue;
                        index = i;
                        break;
                    }

                    return index < 0 ? int.MaxValue : index;
                })
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new RuntimeHotkeyCategoryGroup(
                    group.Key,
                    group.OrderBy(info => info.DisplayName ?? info.Id ?? info.CurrentBinding,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList()))
                .ToList();
        }

        private static int CountRuntimeHotkeysInCategory(string category)
        {
            return RuntimeHotkeyService.GetRegisteredHotkeys().Count(info => string.Equals(
                string.IsNullOrWhiteSpace(info.Category)
                    ? L("ritsulib.runtimeHotkeys.category.other", "Other")
                    : info.Category,
                category,
                StringComparison.Ordinal));
        }

        private static string FormatRuntimeHotkeyHeaderDescription(string? id, string binding, string? displayName)
        {
            var hotkey = FindRuntimeHotkey(id, binding, displayName);
            if (hotkey == null)
                return string.Empty;

            var scope = hotkey.IsModifierOnly
                ? L("ritsulib.runtimeHotkeys.scope.modifierOnly", "modifier-only")
                : L("ritsulib.runtimeHotkeys.scope.standard", "standard");
            return string.Format(
                L("ritsulib.runtimeHotkeys.bindingLine", "Binding: {0} ({1})"),
                hotkey.CurrentBinding,
                scope);
        }

        private static string FormatRuntimeHotkeyDetails(string? id, string binding, string? displayName)
        {
            var hotkey = FindRuntimeHotkey(id, binding, displayName);
            if (hotkey == null)
                return string.Empty;

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(hotkey.Description))
                lines.Add(hotkey.Description);
            if (!string.IsNullOrWhiteSpace(hotkey.Id))
                lines.Add(string.Format(L("ritsulib.runtimeHotkeys.idLine", "Id: {0}"), hotkey.Id));
            if (!string.IsNullOrWhiteSpace(hotkey.Purpose))
                lines.Add(string.Format(L("ritsulib.runtimeHotkeys.purposeLine", "Purpose: {0}"), hotkey.Purpose));
            return string.Join("\n", lines);
        }

        private static RuntimeHotkeyRegistrationInfo? FindRuntimeHotkey(string? id, string binding, string? displayName)
        {
            var hotkeys = RuntimeHotkeyService.GetRegisteredHotkeys();
            if (!string.IsNullOrWhiteSpace(id))
                foreach (var info in hotkeys)
                    if (string.Equals(info.Id, id, StringComparison.Ordinal))
                        return info;

            foreach (var info in hotkeys)
            {
                if (!string.Equals(info.CurrentBinding, binding, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(info.DisplayName, displayName, StringComparison.Ordinal))
                    continue;
                return info;
            }

            return hotkeys.FirstOrDefault(info =>
                string.Equals(info.CurrentBinding, binding, StringComparison.Ordinal));
        }

        private static string SanitizeRuntimeHotkeySectionId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "other";

            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')
                .ToArray();
            var sanitized = new string(chars).Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "other" : sanitized;
        }

        private static Control CreateShowcaseListItemEditor(ModSettingsListItemContext<ShowcaseListItem> itemContext)
        {
            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            content.AddThemeConstantOverride("separation", 12);

            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            grid.AddThemeConstantOverride("h_separation", 12);
            grid.AddThemeConstantOverride("v_separation", 12);
            content.AddChild(grid);

            var nameEdit = CreateListField(itemContext.Item.Name, value =>
                itemContext.Update(itemContext.Item with { Name = value }));
            nameEdit.CustomMinimumSize = new(220f, 44f);
            grid.AddChild(CreateListFieldGroup(ModSettingsLocalization.Get("ritsulib.showcase.list.field.name", "Name"),
                nameEdit));

            var tagEdit = CreateListField(itemContext.Item.Tag, value =>
                itemContext.Update(itemContext.Item with { Tag = value }));
            tagEdit.CustomMinimumSize = new(180f, 44f);
            grid.AddChild(CreateListFieldGroup(ModSettingsLocalization.Get("ritsulib.showcase.list.field.tag", "Tag"),
                tagEdit));

            var weightEdit = CreateListField(itemContext.Item.Weight.ToString(), value =>
            {
                if (int.TryParse(value, out var weight))
                    itemContext.Update(itemContext.Item with { Weight = weight });
                else
                    itemContext.RequestRefresh();
            });
            weightEdit.CustomMinimumSize = new(120f, 44f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.list.field.weight", "Weight"), weightEdit));

            var enabledButton = new ModSettingsToggleControl(itemContext.Item.Enabled,
                value => itemContext.Update(itemContext.Item with { Enabled = value }))
            {
                CustomMinimumSize = new(140f, 44f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.list.field.enabled", "Enabled"), enabledButton, false));

            var nestedListBinding = itemContext.Project(
                "details",
                item => item.Details,
                (item, details) => item with { Details = details },
                ModSettingsStructuredData.List(ModSettingsStructuredData.Json<ShowcaseListDetail>()));

            content.AddChild(itemContext.CreateListEditor(
                "details",
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.title",
                    "Detail Notes"),
                nestedListBinding,
                () => new(ModSettingsLocalization.Get("ritsulib.showcase.details.defaultLabel", "New note"), "value"),
                detail => ModSettingsText.Literal(detail.Label),
                detail => ModSettingsText.Literal(detail.Value),
                CreateShowcaseDetailEditor,
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.add", "Add Detail"),
                ModSettingsText.I18N(ModSettingsLocalization.Instance, "ritsulib.showcase.details.description",
                    "Nested structured list editor for each item.")));

            return content;
        }

        private static Control CreateShowcaseDetailEditor(ModSettingsListItemContext<ShowcaseListDetail> itemContext)
        {
            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            grid.AddThemeConstantOverride("h_separation", 12);
            grid.AddThemeConstantOverride("v_separation", 10);

            var labelEdit = CreateListField(itemContext.Item.Label, value =>
                itemContext.Update(itemContext.Item with { Label = value }));
            labelEdit.CustomMinimumSize = new(180f, 42f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.details.field.label", "Label"), labelEdit));

            var valueEdit = CreateListField(itemContext.Item.Value, value =>
                itemContext.Update(itemContext.Item with { Value = value }));
            valueEdit.CustomMinimumSize = new(220f, 42f);
            grid.AddChild(CreateListFieldGroup(
                ModSettingsLocalization.Get("ritsulib.showcase.details.field.value", "Value"), valueEdit));
            return grid;
        }

        private static Control CreateListFieldGroup(string labelText, Control field, bool expand = true)
        {
            var group = new VBoxContainer
            {
                SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            group.AddThemeConstantOverride("separation", 6);

            var label = ModSettingsUiFactory.CreateInlineDescription(labelText);
            group.AddChild(label);

            if (!expand)
            {
                var fieldRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                fieldRow.AddChild(field);
                fieldRow.AddChild(new Control
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore,
                });
                group.AddChild(fieldRow);
            }
            else
            {
                group.AddChild(field);
            }

            return group;
        }

        private static LineEdit CreateListField(string initialValue, Action<string> commit)
        {
            var edit = new LineEdit
            {
                Text = initialValue,
                SelectAllOnFocus = true,
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            edit.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            edit.AddThemeFontSizeOverride("font_size", 18);
            edit.AddThemeColorOverride("font_color", new(1f, 0.964706f, 0.886275f));
            edit.AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            edit.AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            edit.TextSubmitted += value =>
            {
                commit(value);
                edit.ReleaseFocus();
            };
            edit.FocusExited += () => commit(edit.Text);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(edit);
            return edit;
        }

        private sealed record RuntimeHotkeyCategoryGroup(string Category, List<RuntimeHotkeyRegistrationInfo> Hotkeys);

        private sealed class DebugShowcaseState
        {
            private int _nextItemIndex = 4;
            public bool ToggleValue { get; set; } = true;
            public double SliderValue { get; set; } = 35d;
            public int IntSliderValue { get; set; } = 2;
            public string ChoiceValue { get; set; } = "balanced";
            public string ChoiceDropdownValue { get; set; } = "wide";
            public ShowcaseMode ModeValue { get; set; } = ShowcaseMode.Balanced;
            public string StringValue { get; set; } = "Single line";
            public string StringMultiValue { get; set; } = "First line\nSecond line";
            public int ActionCount { get; set; }

            public List<ShowcaseListItem> ListItems { get; set; } =
            [
                new("Sample A", 3, true, "alpha", [new("Author", "Ritsu"), new("Mode", "Default")]),
                new("Sample B", 1, false, "beta", [new("Author", "Debug")]),
                new("Sample C", 5, true, "gamma", [new("Mode", "Experimental"), new("Tier", "Rare")]),
            ];

            public ShowcaseListItem CreateListItem()
            {
                var index = _nextItemIndex++;
                return new($"Sample {index}", index, index % 2 == 0,
                    $"tag-{index}", [new("Author", $"User {index}")]);
            }
        }

        private sealed record ShowcaseListItem(
            string Name,
            int Weight,
            bool Enabled,
            string Tag,
            List<ShowcaseListDetail> Details);

        private sealed record ShowcaseListDetail(string Label, string Value);

        private enum ShowcaseMode
        {
            Compact,
            Balanced,
            Detailed,
        }

        private sealed class ShowcaseBinding<TValue>(IModSettingsValueBinding<TValue> inner, Action<TValue> afterWrite)
            : IModSettingsValueBinding<TValue>, ITransientModSettingsBinding
        {
            public string ModId => inner.ModId;
            public string DataKey => inner.DataKey;
            public SaveScope Scope => inner.Scope;

            public TValue Read()
            {
                return inner.Read();
            }

            public void Write(TValue value)
            {
                inner.Write(value);
                afterWrite(value);
            }

            public void Save()
            {
            }
        }
    }
}
