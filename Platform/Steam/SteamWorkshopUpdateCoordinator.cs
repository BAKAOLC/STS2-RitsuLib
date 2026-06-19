using Godot;
using MegaCrit.Sts2.Core.Platform.Steam;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class SteamWorkshopUpdateCoordinator
    {
        private const double ToastDurationSeconds = 7.0d;
        private static readonly Lock SyncRoot = new();
        private static bool _initialized;
        private static int _checkRunning;

        internal static bool CanUseSteamWorkshopUpdates()
        {
            return SteamInitializer.Initialized && RitsuSteamWorkshopUpdates.IsAvailable;
        }

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                _initialized = true;
                RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Coordinator initialized.");
                RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(evt =>
                {
                    if (!RitsuLibSettingsStore.IsSteamWorkshopUpdateCheckEnabled())
                    {
                        RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Auto check skipped: setting disabled.");
                        return;
                    }

                    RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Auto check requested after first main menu.");
                    _ = CheckAsync(false);
                });
            }
        }

        internal static void CheckNowFromSettings()
        {
            RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Manual check requested from settings.");
            _ = CheckAsync(true);
        }

        private static Task CheckAsync(bool showCompletionToast)
        {
            if (Interlocked.Exchange(ref _checkRunning, 1) == 0)
                return Task.Run(() =>
                {
                    try
                    {
                        var mode = showCompletionToast ? "manual" : "auto";
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Starting {mode} check. SteamInitialized={SteamInitializer.Initialized}.");
                        if (!SteamInitializer.Initialized)
                        {
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] {mode} check skipped: Steam is not initialized.");
                            if (showCompletionToast)
                                PostToMainLoop(() => ShowToast(
                                    L("ritsulib.steamWorkshop.toast.unavailable",
                                        "Steam Workshop is not available in this session."),
                                    RitsuToastLevel.Info));
                            return;
                        }

                        var result = RitsuSteamWorkshopUpdates.TriggerMissingUpdates();
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] {mode} check result: Available={result.Available}, " +
                            $"Inspected={result.InspectedCount}, NeedsUpdate={result.NeedsUpdateCount}, " +
                            $"Triggered={result.TriggeredCount}, AlreadyQueued={result.AlreadyQueuedCount}, " +
                            $"Failed={result.FailedCount}, Error={result.ErrorMessage ?? "<none>"}.");
                        PostToMainLoop(() => ShowResultToast(result, showCompletionToast));
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn($"[SteamWorkshopUpdate] Check failed: {ex.Message}");
                        if (showCompletionToast)
                            PostToMainLoop(() => ShowToast(
                                Format("ritsulib.steamWorkshop.toast.failed", "Steam Workshop check failed: {0}",
                                    ex.Message),
                                RitsuToastLevel.Warning));
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _checkRunning, 0);
                    }
                });
            RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Check skipped: another check is already running.");
            if (showCompletionToast)
                PostToMainLoop(() => ShowToast(
                    L("ritsulib.steamWorkshop.toast.busy",
                        "Another Steam Workshop update check is already running."),
                    RitsuToastLevel.Info));
            return Task.CompletedTask;
        }

        private static void ShowResultToast(RitsuSteamWorkshopUpdateResult result, bool showCompletionToast)
        {
            if (!result.Available)
            {
                if (showCompletionToast)
                    ShowToast(
                        result.ErrorMessage == null
                            ? L("ritsulib.steamWorkshop.toast.unavailable",
                                "Steam Workshop is not available in this session.")
                            : Format(
                                "ritsulib.steamWorkshop.toast.failed",
                                "Steam Workshop check failed: {0}",
                                result.ErrorMessage),
                        result.ErrorMessage == null ? RitsuToastLevel.Info : RitsuToastLevel.Warning);
                return;
            }

            if (result.TriggeredCount > 0)
            {
                ShowToast(
                    Format(
                        "ritsulib.steamWorkshop.toast.triggered",
                        "Queued Steam Workshop update download for {0} item(s). Restart after Steam finishes downloading.",
                        result.TriggeredCount),
                    RitsuToastLevel.Info);
                return;
            }

            if (result.FailedCount > 0)
            {
                ShowToast(
                    Format(
                        "ritsulib.steamWorkshop.toast.partial",
                        "Found {0} stale Workshop item(s), but {1} update trigger(s) failed. See game log for details.",
                        result.NeedsUpdateCount,
                        result.FailedCount),
                    RitsuToastLevel.Warning);
                return;
            }

            if (result.AlreadyQueuedCount > 0)
            {
                if (showCompletionToast)
                    ShowToast(
                        Format(
                            "ritsulib.steamWorkshop.toast.alreadyQueued",
                            "{0} Workshop item(s) already have downloads queued or running.",
                            result.AlreadyQueuedCount),
                        RitsuToastLevel.Info);
                return;
            }

            if (showCompletionToast)
                ShowToast(
                    Format(
                        "ritsulib.steamWorkshop.toast.none",
                        "Checked {0} subscribed Workshop item(s). No missing updates were found.",
                        result.InspectedCount),
                    RitsuToastLevel.Info);
        }

        private static void ShowToast(string body, RitsuToastLevel level)
        {
            RitsuLibFramework.Logger.Info($"[SteamWorkshopUpdate] Toast ({level}): {body}");
            RitsuToastService.Show(new(
                body,
                L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"),
                null,
                level,
                ToastDurationSeconds));
        }

        private static void PostToMainLoop(Action action)
        {
            if (Engine.GetMainLoop() is SceneTree)
            {
                Callable.From(action).CallDeferred();
                return;
            }

            action();
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string Format(string key, string fallback, params object[] args)
        {
            return string.Format(L(key, fallback), args);
        }
    }
}
