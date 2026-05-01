using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Settings
{
    internal sealed class RitsuLibModSettingsUiBindings
    {
        private RitsuLibModSettingsUiBindings()
        {
        }

        public IModSettingsValueBinding<bool> SyncModDataSteamCloud { get; private init; } = null!;
        public IModSettingsValueBinding<bool> DebugCompatibility { get; private init; } = null!;
        public IModSettingsValueBinding<bool> DebugCompatLocTable { get; private init; } = null!;
        public IModSettingsValueBinding<bool> DebugCompatUnlockEpoch { get; private init; } = null!;

        public IModSettingsValueBinding<bool> DebugCompatAncientArchitect { get; private init; } =
            null!;

        public IModSettingsValueBinding<string> HarmonyPatchDumpOutputPath { get; private init; } =
            null!;

        public IModSettingsValueBinding<bool> HarmonyPatchDumpOnFirstMainMenu { get; private init; } =
            null!;

        public IModSettingsValueBinding<string> SelfCheckOutputFolder { get; private init; } = null!;
        public IModSettingsValueBinding<bool> SelfCheckOnFirstMainMenu { get; private init; } = null!;
        public IModSettingsValueBinding<string> UiShellThemeId { get; private init; } = null!;
        public IModSettingsValueBinding<bool> ToastEnabled { get; private init; } = null!;
        public IModSettingsValueBinding<string> ToastAnchor { get; private init; } = null!;
        public IModSettingsValueBinding<double> ToastOffsetX { get; private init; } = null!;
        public IModSettingsValueBinding<double> ToastOffsetY { get; private init; } = null!;
        public IModSettingsValueBinding<int> ToastMaxVisible { get; private init; } = null!;
        public IModSettingsValueBinding<double> ToastDurationSeconds { get; private init; } = null!;
        public IModSettingsValueBinding<string> ToastAnimation { get; private init; } = null!;
        public IModSettingsValueBinding<string> CardPngExportOutputPath { get; private init; } = null!;
        public IModSettingsValueBinding<bool> CardPngExportIncludeHover { get; private init; } = null!;

        public IModSettingsValueBinding<bool> CardPngExportIncludeUpgrades { get; private init; } =
            null!;

        public IModSettingsValueBinding<double> CardPngExportScale { get; private init; } = null!;
        public IModSettingsValueBinding<string> CardPngExportIdFilter { get; private init; } = null!;

        public IModSettingsValueBinding<bool> CardPngExportIncludeHiddenFromLibrary { get; private init; } =
            null!;

        public IModSettingsValueBinding<string> RelicDetailPngExportOutputPath { get; private init; } =
            null!;

        public IModSettingsValueBinding<double> RelicDetailPngExportScale { get; private init; } =
            null!;

        public IModSettingsValueBinding<string> RelicDetailPngExportIdFilter { get; private init; } =
            null!;

        public IModSettingsValueBinding<bool> RelicDetailPngExportIncludeHover { get; private init; } =
            null!;

        public IModSettingsValueBinding<string>
            PotionDetailPngExportOutputPath { get; private init; } =
            null!;

        public IModSettingsValueBinding<double> PotionDetailPngExportScale { get; private init; } =
            null!;

        public IModSettingsValueBinding<string> PotionDetailPngExportIdFilter { get; private init; } =
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
        public IModSettingsValueBinding<int> PreviewActionCount { get; private init; } = null!;
        public IModSettingsValueBinding<string> PreviewHotkey { get; private init; } = null!;
        public IModSettingsValueBinding<List<string>> PreviewHotkeyMulti { get; private init; } = null!;

        public IModSettingsValueBinding<List<ModSettingsDebugShowcaseListItem>> PreviewList { get; private init; } =
            null!;

        public IModSettingsValueBinding<bool> HostSurfaceCombatReadOnlyDemo { get; private init; } = null!;

        public static RitsuLibModSettingsUiBindings Create()
        {
            var debugShowcase = new ModSettingsDebugShowcaseState();
            var defaults = new RitsuLibSettings();
            return new()
            {
                SyncModDataSteamCloud = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.SyncModDataToSteamCloud,
                        (settings, value) => settings.SyncModDataToSteamCloud = value),
                    () => defaults.SyncModDataToSteamCloud),
                DebugCompatibility = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.DebugCompatibilityMode,
                        (settings, value) => settings.DebugCompatibilityMode = value),
                    () => defaults.DebugCompatibilityMode),
                DebugCompatLocTable = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.DebugCompatLocTable,
                        (settings, value) => settings.DebugCompatLocTable = value),
                    () => defaults.DebugCompatLocTable),
                DebugCompatUnlockEpoch = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.DebugCompatUnlockEpoch,
                        (settings, value) => settings.DebugCompatUnlockEpoch = value),
                    () => defaults.DebugCompatUnlockEpoch),
                DebugCompatAncientArchitect = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.DebugCompatAncientArchitect,
                        (settings, value) => settings.DebugCompatAncientArchitect = value),
                    () => defaults.DebugCompatAncientArchitect),
                HarmonyPatchDumpOutputPath = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.HarmonyPatchDumpOutputPath,
                        (settings, value) => settings.HarmonyPatchDumpOutputPath = value),
                    () => defaults.HarmonyPatchDumpOutputPath),
                HarmonyPatchDumpOnFirstMainMenu = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.HarmonyPatchDumpOnFirstMainMenu,
                        (settings, value) => settings.HarmonyPatchDumpOnFirstMainMenu = value),
                    () => defaults.HarmonyPatchDumpOnFirstMainMenu),
                SelfCheckOutputFolder = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.SelfCheckOutputFolderPath,
                        (settings, value) => settings.SelfCheckOutputFolderPath = value),
                    () => defaults.SelfCheckOutputFolderPath),
                SelfCheckOnFirstMainMenu = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.SelfCheckOnFirstMainMenu,
                        (settings, value) => settings.SelfCheckOnFirstMainMenu = value),
                    () => defaults.SelfCheckOnFirstMainMenu),
                UiShellThemeId = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
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
                    () => defaults.UiShellThemeId),
                ToastEnabled = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.ToastEnabled,
                        (settings, value) =>
                        {
                            settings.ToastEnabled = value;
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => true),
                ToastAnchor = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => NormalizeToastAnchor(settings.ToastAnchor),
                        (settings, value) =>
                        {
                            settings.ToastAnchor = NormalizeToastAnchor(value);
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => "topright"),
                ToastOffsetX = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.ToastOffsetX,
                        (settings, value) =>
                        {
                            settings.ToastOffsetX = value;
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => -24d),
                ToastOffsetY = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.ToastOffsetY,
                        (settings, value) =>
                        {
                            settings.ToastOffsetY = value;
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => 24d),
                ToastMaxVisible = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, int>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => Math.Clamp(settings.ToastMaxVisible, 1, 8),
                        (settings, value) =>
                        {
                            settings.ToastMaxVisible = Math.Clamp(value, 1, 8);
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => 3),
                ToastDurationSeconds = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => Math.Clamp(settings.ToastDurationSeconds, 0.5d, 30d),
                        (settings, value) =>
                        {
                            settings.ToastDurationSeconds = Math.Clamp(value, 0.5d, 30d);
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => 3.5d),
                ToastAnimation = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => NormalizeToastAnimation(settings.ToastAnimation),
                        (settings, value) =>
                        {
                            settings.ToastAnimation = NormalizeToastAnimation(value);
                            RitsuToastService.RefreshSettingsFromStore();
                        }),
                    () => "fadeslide"),
                CardPngExportOutputPath = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportOutputPath,
                        (settings, value) => settings.CardPngExportOutputPath = value),
                    () => defaults.CardPngExportOutputPath),
                CardPngExportIncludeHover = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportIncludeHover,
                        (settings, value) => settings.CardPngExportIncludeHover = value),
                    () => defaults.CardPngExportIncludeHover),
                CardPngExportIncludeUpgrades = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportIncludeUpgrades,
                        (settings, value) => settings.CardPngExportIncludeUpgrades = value),
                    () => defaults.CardPngExportIncludeUpgrades),
                CardPngExportScale = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportScale,
                        (settings, value) => settings.CardPngExportScale = value),
                    () => defaults.CardPngExportScale),
                CardPngExportIdFilter = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportIdFilter,
                        (settings, value) => settings.CardPngExportIdFilter = value),
                    () => defaults.CardPngExportIdFilter),
                CardPngExportIncludeHiddenFromLibrary = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.CardPngExportIncludeHiddenFromLibrary,
                        (settings, value) => settings.CardPngExportIncludeHiddenFromLibrary = value),
                    () => defaults.CardPngExportIncludeHiddenFromLibrary),
                RelicDetailPngExportOutputPath = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.RelicDetailPngExportOutputPath,
                        (settings, value) => settings.RelicDetailPngExportOutputPath = value),
                    () => defaults.RelicDetailPngExportOutputPath),
                RelicDetailPngExportScale = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.RelicDetailPngExportScale,
                        (settings, value) => settings.RelicDetailPngExportScale = value),
                    () => defaults.RelicDetailPngExportScale),
                RelicDetailPngExportIdFilter = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.RelicDetailPngExportIdFilter,
                        (settings, value) => settings.RelicDetailPngExportIdFilter = value),
                    () => defaults.RelicDetailPngExportIdFilter),
                RelicDetailPngExportIncludeHover = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, bool>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.RelicDetailPngExportIncludeHover,
                        (settings, value) => settings.RelicDetailPngExportIncludeHover = value),
                    () => defaults.RelicDetailPngExportIncludeHover),
                PotionDetailPngExportOutputPath = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.PotionDetailPngExportOutputPath,
                        (settings, value) => settings.PotionDetailPngExportOutputPath = value),
                    () => defaults.PotionDetailPngExportOutputPath),
                PotionDetailPngExportScale = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, double>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.PotionDetailPngExportScale,
                        (settings, value) => settings.PotionDetailPngExportScale = value),
                    () => defaults.PotionDetailPngExportScale),
                PotionDetailPngExportIdFilter = ModSettingsBindings.WithDefault(
                    ModSettingsBindings.Global<RitsuLibSettings, string>(
                        Const.ModId,
                        Const.SettingsKey,
                        settings => settings.PotionDetailPngExportIdFilter,
                        (settings, value) => settings.PotionDetailPngExportIdFilter = value),
                    () => defaults.PotionDetailPngExportIdFilter),
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
                PreviewActionCount =
                    ModSettingsBindings.InMemory(Const.ModId, "preview_action_count", debugShowcase.ActionCount),
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

        private static string NormalizeToastAnchor(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "topleft" => "topleft",
                "topcenter" => "topcenter",
                "topright" => "topright",
                "middleleft" => "middleleft",
                "middlecenter" => "middlecenter",
                "middleright" => "middleright",
                "bottomleft" => "bottomleft",
                "bottomcenter" => "bottomcenter",
                "bottomright" => "bottomright",
                _ => "topright",
            };
        }

        private static string NormalizeToastAnimation(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "fade" => "fade",
                "fadescale" => "fadescale",
                _ => "fadeslide",
            };
        }
    }
}
