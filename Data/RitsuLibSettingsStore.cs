using System.Security.Cryptography;
using STS2RitsuLib.Data.Migrations;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Diagnostics.Logging;
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
        private static volatile bool _initializing;

        internal static void Initialize()
        {
            lock (InitLock)
            {
                if (_initialized)
                    return;

                if (_initializing)
                    return;

                _initializing = true;

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
                            new RitsuLibSettingsV8ToV9Migration(),
                            new RitsuLibSettingsV9ToV10Migration(),
                            new RitsuLibSettingsV10ToV11Migration(),
                            new RitsuLibSettingsV11ToV12Migration(),
                        ]);
                }

                try
                {
                    _initialized = true;
                    RitsuShellThemeRuntime.ApplyThemeId(GetSettings().UiShellThemeId);
                    LogConfigSnapshot();
                }
                finally
                {
                    _initializing = false;
                }
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
        ///     调试兼容性总开关。为 false 时，不运行任何 RitsuLib 软失败 shim。
        /// </summary>
        internal static bool IsDebugCompatibilityMasterEnabled()
        {
            Initialize();
            return GetSettings().DebugCompatibilityMode;
        }

        /// <summary>
        ///     <c>LocTable</c> missing-key placeholders + warnings.
        ///     <c>LocTable</c> 缺失键占位符与警告。
        /// </summary>
        internal static bool IsLocTableCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatLocTable: true };
        }

        /// <summary>
        ///     Skip invalid epoch grants with warnings instead of throwing.
        ///     跳过无效 epoch 授予并输出警告，而不是抛出异常。
        /// </summary>
        internal static bool IsUnlockEpochCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatUnlockEpoch: true };
        }

        /// <summary>
        ///     <c>THE_ARCHITECT</c> empty dialogue stub for registry characters.
        ///     注册表角色使用的 <c>THE_ARCHITECT</c> 空对话桩。
        /// </summary>
        internal static bool IsAncientArchitectCompatEnabled()
        {
            Initialize();
            var s = GetSettings();
            return s is { DebugCompatibilityMode: true, DebugCompatAncientArchitect: true };
        }

        internal static bool IsModSourceHoverTipsEnabled()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsEnabled;
        }

        internal static bool ShouldIncludeVanillaModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsIncludeVanilla;
        }

        internal static bool ShouldIncludeNonDetailModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsIncludeNonDetails;
        }

        internal static bool ShouldShowCardModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsCards;
        }

        internal static bool ShouldShowRelicModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsRelics;
        }

        internal static bool ShouldShowPotionModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsPotions;
        }

        internal static bool ShouldShowPowerModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsPowers;
        }

        internal static bool ShouldShowOrbModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsOrbs;
        }

        internal static bool ShouldShowEnchantmentModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsEnchantments;
        }

        internal static bool ShouldShowAfflictionModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsAfflictions;
        }

        internal static bool ShouldShowKeywordModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsKeywords;
        }

        internal static bool ShouldShowEventModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsEvents;
        }

        internal static bool ShouldShowCreatureModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsCreatures;
        }

        internal static bool ShouldShowGameTermModSourceHoverTips()
        {
            Initialize();
            return GetSettings().ModSourceHoverTipsGameTerms;
        }

        internal static RitsuDebugLogViewerOptions GetDebugLogViewerOptions()
        {
            Initialize();
            var s = GetSettings();
            var commandLine = RitsuDebugLogViewerCommandLine.ParseCurrentProcess();
            var changed = false;
            if (string.IsNullOrWhiteSpace(s.DebugLogViewerAccessToken))
            {
                s.DebugLogViewerAccessToken =
                    Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
                changed = true;
            }

            if (changed)
                Store.Save(Const.SettingsKey);

            foreach (var warning in commandLine.Warnings)
                RitsuLibFramework.Logger.Warn($"[DebugLogViewer] {warning}");

            var port = Math.Clamp(s.DebugLogViewerPort, 1, 65535);
            var portFallbackCount = Math.Clamp(s.DebugLogViewerPortFallbackCount, 0, 100);
            if (commandLine.Port is { } commandLinePort)
            {
                port = commandLinePort;
                portFallbackCount = commandLine.PortFallbackCount ?? 0;
                RitsuLibFramework.Logger.Info(
                    $"[DebugLogViewer] Command line override applied: port={port}, fallbackCount={portFallbackCount}.");
            }
            else if (commandLine.PortFallbackCount is { } commandLinePortFallbackCount)
            {
                portFallbackCount = commandLinePortFallbackCount;
                RitsuLibFramework.Logger.Info(
                    $"[DebugLogViewer] Command line override applied: fallbackCount={portFallbackCount}.");
            }

            return new(
                s.DebugLogViewerEnabled,
                s.DebugLogViewerMirrorGameLogs,
                s.DebugLogViewerAutoOpen,
                s.DebugLogViewerLanAccessEnabled,
                port,
                portFallbackCount,
                s.DebugLogViewerAccessToken,
                s.DebugLogViewerRingBufferCapacity,
                s.DebugLogViewerQueueCapacity);
        }

        internal static bool IsDevConsoleHistoryNavigationPatchEnabled()
        {
            Initialize();
            return GetSettings().DevConsoleHistoryNavigationPatchEnabled;
        }

        internal static bool IsDevConsoleAutocompleteEnhancementsEnabled()
        {
            Initialize();
            return GetSettings().DevConsoleAutocompleteEnhancementsEnabled;
        }

        internal static bool ShouldClearDevConsoleInputOnVisibilityChange()
        {
            Initialize();
            return GetSettings().DevConsoleClearInputOnVisibilityChange;
        }

        private static RitsuLibSettings GetSettings()
        {
            return Store.Get<RitsuLibSettings>(Const.SettingsKey);
        }

        /// <summary>
        ///     Harmony patch dump UI / lifecycle reads paths and flags without exposing the store surface publicly.
        ///     Harmony 补丁转储 UI/生命周期读取路径和标志，同时不公开暴露存储接口。
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
            if (_initializing && !_initialized)
                return false;

            Initialize();
            return GetSettings().SyncModDataToSteamCloud;
        }

        internal static bool IsUpdateCheckEnabled()
        {
            Initialize();
            return GetSettings().UpdateCheckEnabled;
        }

        internal static bool IsMainMenuModSettingsButtonEnabled()
        {
            Initialize();
            return GetSettings().MainMenuModSettingsButtonEnabled;
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
