using STS2RitsuLib.Data.Migrations;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Data
{
    internal static class RitsuLibSettingsStore
    {
        private static readonly ModDataStore Store = ModDataStore.For(Const.ModId);

        private static readonly Lock InitLock = new();
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                using (RitsuLibFramework.BeginModDataRegistration(Const.ModId, false))
                {
                    Store.Register<RitsuLibSettings>(
                        Const.SettingsKey,
                        Const.SettingsFileName,
                        SaveScope.Global,
                        () => new(),
                        true,
                        new()
                        {
                            CurrentDataVersion = RitsuLibSettings.CurrentSchemaVersion,
                            MinimumSupportedDataVersion = 0,
                        },
                        [
                            new RitsuLibSettingsV0Or1ToV2Migration(),
                            new RitsuLibSettingsV2ToV4Migration(),
                            new RitsuLibSettingsV4ToV5Migration(),
                            new RitsuLibSettingsV5ToV6Migration(),
                            new RitsuLibSettingsV6ToV7Migration(),
                            new RitsuLibSettingsV7ToV8Migration(),
                        ]);
                }

                _initialized = true;
                RitsuShellThemeRuntime.ApplyThemeId(GetSettings().UiShellThemeId);
                LogConfigSnapshot();
            }
        }

        private static void LogConfigSnapshot()
        {
            var s = GetSettings();
            var master = s.DebugCompatibilityMode;
            RitsuLibFramework.Logger.Info(
                $"[Config] Debug compatibility master is {(master ? "enabled" : "disabled")}. " +
                $"Sub-flags (only when master on): LocTable={s.DebugCompatLocTable}, UnlockEpoch={s.DebugCompatUnlockEpoch}, AncientArchitect={s.DebugCompatAncientArchitect}. " +
                $"Mod Steam cloud mirror: {(s.SyncModDataToSteamCloud ? "enabled" : "disabled")}. " +
                $"Config file: {ProfileManager.GetFilePath(Const.SettingsFileName, SaveScope.Global, 0, Const.ModId)}");
        }

        /// <summary>
        ///     Master debug-compatibility switch. When false, no RitsuLib soft-fail shims run.
        /// </summary>
        internal static bool IsDebugCompatibilityMasterEnabled()
        {
            Initialize();
            return GetSettings().DebugCompatibilityMode;
        }

        /// <summary>
        ///     <c>LocTable</c> missing-key placeholders + warnings.
        /// </summary>
        internal static bool IsLocTableCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatLocTable: true };
        }

        /// <summary>
        ///     Skip invalid epoch grants with warnings instead of throwing.
        /// </summary>
        internal static bool IsUnlockEpochCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatUnlockEpoch: true };
        }

        /// <summary>
        ///     <c>THE_ARCHITECT</c> empty dialogue stub for registry characters.
        /// </summary>
        internal static bool IsAncientArchitectCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatAncientArchitect: true };
        }

        private static RitsuLibSettings GetSettings()
        {
            return Store.Get<RitsuLibSettings>(Const.SettingsKey);
        }

        /// <summary>
        ///     Harmony patch dump UI / lifecycle reads paths and flags without exposing the store surface publicly.
        /// </summary>
        internal static (string OutputPath, bool DumpOnFirstMainMenu) GetHarmonyPatchDumpOptions()
        {
            Initialize();
            var s = GetSettings();
            return (s.HarmonyPatchDumpOutputPath, s.HarmonyPatchDumpOnFirstMainMenu);
        }

        internal static (string OutputFolder, bool RunOnFirstMainMenu) GetSelfCheckOptions()
        {
            Initialize();
            var s = GetSettings();
            return (s.SelfCheckOutputFolderPath, s.SelfCheckOnFirstMainMenu);
        }

        internal static bool IsSyncModDataToCloudEnabled()
        {
            Initialize();
            return GetSettings().SyncModDataToSteamCloud;
        }

        internal static RitsuToastSettings GetToastSettings()
        {
            Initialize();
            var s = GetSettings();
            var anchor = ParseAnchor(s.ToastAnchor);
            var animation = ParseAnimation(s.ToastAnimation);
            var maxVisible = Math.Clamp(s.ToastMaxVisible, 1, 8);
            var duration = Math.Clamp(s.ToastDurationSeconds, 0.5d, 30d);
            return new(
                s.ToastEnabled,
                new(anchor, new((float)s.ToastOffsetX, (float)s.ToastOffsetY)),
                new(maxVisible, 12f),
                duration,
                animation);
        }

        private static RitsuToastAnchor ParseAnchor(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "topleft" => RitsuToastAnchor.TopLeft,
                "topcenter" => RitsuToastAnchor.TopCenter,
                "topright" => RitsuToastAnchor.TopRight,
                "middleleft" => RitsuToastAnchor.MiddleLeft,
                "middlecenter" => RitsuToastAnchor.MiddleCenter,
                "middleright" => RitsuToastAnchor.MiddleRight,
                "bottomleft" => RitsuToastAnchor.BottomLeft,
                "bottomcenter" => RitsuToastAnchor.BottomCenter,
                "bottomright" => RitsuToastAnchor.BottomRight,
                _ => RitsuToastAnchor.TopRight,
            };
        }

        private static RitsuToastAnimationPreset ParseAnimation(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "fade" => RitsuToastAnimationPreset.Fade,
                "fadescale" => RitsuToastAnimationPreset.FadeScale,
                _ => RitsuToastAnimationPreset.FadeSlide,
            };
        }
    }
}
