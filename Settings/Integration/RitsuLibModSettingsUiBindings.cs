using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    internal sealed class RitsuLibModSettingsUiBindings
    {
        private RitsuLibModSettingsUiBindings()
        {
        }

        public ModSettingsValueBinding<RitsuLibSettings, bool> SyncModDataSteamCloud { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, bool> DebugCompatibility { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, bool> DebugCompatLocTable { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, bool> DebugCompatUnlockEpoch { get; private init; } = null!;

        public ModSettingsValueBinding<RitsuLibSettings, bool> DebugCompatAncientArchitect { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string> HarmonyPatchDumpOutputPath { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, bool> HarmonyPatchDumpOnFirstMainMenu { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string> SelfCheckOutputFolder { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, bool> SelfCheckOnFirstMainMenu { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, string> UiShellThemeId { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, string> CardPngExportOutputPath { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, bool> CardPngExportIncludeHover { get; private init; } = null!;

        public ModSettingsValueBinding<RitsuLibSettings, bool> CardPngExportIncludeUpgrades { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, double> CardPngExportScale { get; private init; } = null!;
        public ModSettingsValueBinding<RitsuLibSettings, string> CardPngExportIdFilter { get; private init; } = null!;

        public ModSettingsValueBinding<RitsuLibSettings, bool> CardPngExportIncludeHiddenFromLibrary
        {
            get;
            private init;
        } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string> RelicDetailPngExportOutputPath { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, double> RelicDetailPngExportScale { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string> RelicDetailPngExportIdFilter { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, bool> RelicDetailPngExportIncludeHover { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string>
            PotionDetailPngExportOutputPath { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, double> PotionDetailPngExportScale { get; private init; } =
            null!;

        public ModSettingsValueBinding<RitsuLibSettings, string> PotionDetailPngExportIdFilter { get; private init; } =
            null!;

        public ModSettingsDebugShowcaseState DebugShowcase { get; private init; } = null!;
        public IModSettingsValueBinding<bool> PreviewToggle { get; private init; } = null!;
        public IModSettingsValueBinding<double> PreviewSlider { get; private init; } = null!;
        public IModSettingsValueBinding<int> PreviewIntSlider { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewChoice { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewChoiceDropdown { get; private init; } = null!;
        public IModSettingsValueBinding<ModSettingsDebugShowcaseMode> PreviewMode { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewString { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewStringMulti { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewHotkey { get; private init; } = null!;
        public IModSettingsValueBinding<List<string>> PreviewHotkeyMulti { get; private init; } = null!;

        public IModSettingsValueBinding<List<ModSettingsDebugShowcaseListItem>> PreviewList { get; private init; } =
            null!;

        public IModSettingsValueBinding<bool> HostSurfaceCombatReadOnlyDemo { get; private init; } = null!;

        public static RitsuLibModSettingsUiBindings Create()
        {
            var debugShowcase = new ModSettingsDebugShowcaseState();
            return new()
            {
                SyncModDataSteamCloud = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.SyncModDataToSteamCloud,
                    (settings, value) => settings.SyncModDataToSteamCloud = value),
                DebugCompatibility = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatibilityMode,
                    (settings, value) => settings.DebugCompatibilityMode = value),
                DebugCompatLocTable = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatLocTable,
                    (settings, value) => settings.DebugCompatLocTable = value),
                DebugCompatUnlockEpoch = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatUnlockEpoch,
                    (settings, value) => settings.DebugCompatUnlockEpoch = value),
                DebugCompatAncientArchitect = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.DebugCompatAncientArchitect,
                    (settings, value) => settings.DebugCompatAncientArchitect = value),
                HarmonyPatchDumpOutputPath = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.HarmonyPatchDumpOutputPath,
                    (settings, value) => settings.HarmonyPatchDumpOutputPath = value),
                HarmonyPatchDumpOnFirstMainMenu = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.HarmonyPatchDumpOnFirstMainMenu,
                    (settings, value) => settings.HarmonyPatchDumpOnFirstMainMenu = value),
                SelfCheckOutputFolder = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.SelfCheckOutputFolderPath,
                    (settings, value) => settings.SelfCheckOutputFolderPath = value),
                SelfCheckOnFirstMainMenu = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.SelfCheckOnFirstMainMenu,
                    (settings, value) => settings.SelfCheckOnFirstMainMenu = value),
                UiShellThemeId = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => string.IsNullOrWhiteSpace(settings.UiShellThemeId)
                        ? "default"
                        : settings.UiShellThemeId.Trim().ToLowerInvariant(),
                    (settings, value) =>
                    {
                        var n = string.IsNullOrWhiteSpace(value) ? "default" : value.Trim().ToLowerInvariant();
                        settings.UiShellThemeId = n;
                        RitsuShellThemeRuntime.ApplyThemeId(n);
                    }),
                CardPngExportOutputPath = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportOutputPath,
                    (settings, value) => settings.CardPngExportOutputPath = value),
                CardPngExportIncludeHover = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeHover,
                    (settings, value) => settings.CardPngExportIncludeHover = value),
                CardPngExportIncludeUpgrades = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeUpgrades,
                    (settings, value) => settings.CardPngExportIncludeUpgrades = value),
                CardPngExportScale = ModSettingsBindings.Global<RitsuLibSettings, double>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportScale,
                    (settings, value) => settings.CardPngExportScale = value),
                CardPngExportIdFilter = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIdFilter,
                    (settings, value) => settings.CardPngExportIdFilter = value),
                CardPngExportIncludeHiddenFromLibrary = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.CardPngExportIncludeHiddenFromLibrary,
                    (settings, value) => settings.CardPngExportIncludeHiddenFromLibrary = value),
                RelicDetailPngExportOutputPath = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.RelicDetailPngExportOutputPath,
                    (settings, value) => settings.RelicDetailPngExportOutputPath = value),
                RelicDetailPngExportScale = ModSettingsBindings.Global<RitsuLibSettings, double>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.RelicDetailPngExportScale,
                    (settings, value) => settings.RelicDetailPngExportScale = value),
                RelicDetailPngExportIdFilter = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.RelicDetailPngExportIdFilter,
                    (settings, value) => settings.RelicDetailPngExportIdFilter = value),
                RelicDetailPngExportIncludeHover = ModSettingsBindings.Global<RitsuLibSettings, bool>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.RelicDetailPngExportIncludeHover,
                    (settings, value) => settings.RelicDetailPngExportIncludeHover = value),
                PotionDetailPngExportOutputPath = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.PotionDetailPngExportOutputPath,
                    (settings, value) => settings.PotionDetailPngExportOutputPath = value),
                PotionDetailPngExportScale = ModSettingsBindings.Global<RitsuLibSettings, double>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.PotionDetailPngExportScale,
                    (settings, value) => settings.PotionDetailPngExportScale = value),
                PotionDetailPngExportIdFilter = ModSettingsBindings.Global<RitsuLibSettings, string>(
                    Const.ModId,
                    Const.SettingsKey,
                    settings => settings.PotionDetailPngExportIdFilter,
                    (settings, value) => settings.PotionDetailPngExportIdFilter = value),
                DebugShowcase = debugShowcase,
                PreviewToggle =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_toggle", debugShowcase.ToggleValue),
                PreviewSlider =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_slider", debugShowcase.SliderValue),
                PreviewIntSlider =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_int_slider", debugShowcase.IntSliderValue),
                PreviewChoice =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_choice", debugShowcase.ChoiceValue),
                PreviewChoiceDropdown = ModSettingsBindings.InMemory(
                    Const.ModId,
                    "preview_choice_dropdown",
                    debugShowcase.ChoiceDropdownValue),
                PreviewMode =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_mode", debugShowcase.ModeValue),
                PreviewString =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string", debugShowcase.StringValue),
                PreviewStringMulti =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_string_multi", debugShowcase.StringMultiValue),
                PreviewHotkey = ModSettingsBindings.InMemory(Const.ModId, "preview_hotkey", "Ctrl+K"),
                PreviewHotkeyMulti = ModSettingsBindings.WithAdapter(
                    ModSettingsBindings.InMemory(Const.ModId, "preview_hotkey_multi",
                        new List<string> { "Ctrl+K", "Shift+F1" }),
                    ModSettingsStructuredData.List<string>()),
                PreviewList =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_list", debugShowcase.ListItems.ToList()),
                HostSurfaceCombatReadOnlyDemo =
                    ModSettingsBindings.InMemory(Const.ModId, "host_surface_ro_demo", false),
            };
        }
    }
}
