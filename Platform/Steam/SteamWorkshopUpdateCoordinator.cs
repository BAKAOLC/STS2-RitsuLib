using Godot;
using MegaCrit.Sts2.Core.Platform.Steam;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Updates;

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
                AutomaticUpdateCheckScheduler.Register(
                    "steam-workshop",
                    "Steam Workshop updates",
                    RitsuLibSettingsStore.IsSteamWorkshopUpdateCheckEnabled,
                    cancellationToken =>
                    {
                        RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Auto check requested.");
                        return CheckAsync(CheckSource.Auto, true, cancellationToken);
                    });
            }
        }

        internal static void CheckNowFromSettings()
        {
            RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Manual check requested from settings.");
            _ = CheckAsync(CheckSource.Manual, false);
        }

        private static Task CheckAsync(
            CheckSource source,
            bool deferToastToMainMenu,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _checkRunning, 1) == 0)
                // ReSharper disable once MethodSupportsCancellation
#pragma warning disable CA2016
                return Task.Run(async () =>
#pragma warning restore CA2016
                {
                    try
                    {
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Starting {source} check. SteamInitialized={SteamInitializer.Initialized}.");
                        if (!SteamInitializer.Initialized)
                        {
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] {source} check skipped: Steam is not initialized.");
                            if (ShouldShowCompletionToast(source))
                                ShowToast(
                                    L("ritsulib.steamWorkshop.toast.unavailable",
                                        "Steam Workshop is not available in this session."),
                                    RitsuToastLevel.Info,
                                    deferToastToMainMenu);
                            return;
                        }

                        var result = await RitsuSteamWorkshopUpdates
                            .TriggerMissingUpdatesAsync(cancellationToken)
                            .ConfigureAwait(false);
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] {source} check result: Available={result.Available}, " +
                            $"Inspected={result.InspectedCount}, NeedsUpdate={result.NeedsUpdateCount}, " +
                            $"Triggered={result.TriggeredCount}, AlreadyQueued={result.AlreadyQueuedCount}, " +
                            $"Failed={result.FailedCount}, Error={result.ErrorMessage ?? "<none>"}.");
                        LogCheckSummary(source, result);
                        ShowResultToast(result, source, deferToastToMainMenu);
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn($"[SteamWorkshopUpdate] Check failed: {ex.Message}");
                        if (ShouldShowCompletionToast(source))
                            ShowToast(
                                Format("ritsulib.steamWorkshop.toast.failed", "Steam Workshop check failed: {0}",
                                    ex.Message),
                                RitsuToastLevel.Warning,
                                deferToastToMainMenu);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _checkRunning, 0);
                    }
                });
            RitsuLibFramework.Logger.Info(
                $"[SteamWorkshopUpdate] {source} check skipped: another check is already running.");
            if (ShouldShowCompletionToast(source))
                ShowToast(
                    L("ritsulib.steamWorkshop.toast.busy",
                        "Another Steam Workshop update check is already running."),
                    RitsuToastLevel.Info,
                    deferToastToMainMenu);
            return Task.CompletedTask;
        }

        private static void ShowResultToast(
            RitsuSteamWorkshopUpdateResult result,
            CheckSource source,
            bool deferToastToMainMenu)
        {
            if (!result.Available)
            {
                if (ShouldShowCompletionToast(source))
                    ShowToast(
                        result.ErrorMessage == null
                            ? L("ritsulib.steamWorkshop.toast.unavailable",
                                "Steam Workshop is not available in this session.")
                            : Format(
                                "ritsulib.steamWorkshop.toast.failed",
                                "Steam Workshop check failed: {0}",
                                result.ErrorMessage),
                        result.ErrorMessage == null ? RitsuToastLevel.Info : RitsuToastLevel.Warning,
                        deferToastToMainMenu);
                return;
            }

            if (result.FailedCount > 0)
            {
                ShowToast(
                    Format(
                        "ritsulib.steamWorkshop.toast.partial",
                        "Found {0} Workshop item(s) with updates. Queued {1}; {2} failed. See game log for details.",
                        result.NeedsUpdateCount,
                        result.TriggeredCount,
                        result.FailedCount),
                    RitsuToastLevel.Warning,
                    deferToastToMainMenu);
                return;
            }

            if (result.TriggeredCount > 0)
            {
                var isAuto = source == CheckSource.Auto;
                ShowToast(
                    Format(
                        isAuto
                            ? "ritsulib.steamWorkshop.toast.autoTriggered"
                            : "ritsulib.steamWorkshop.toast.triggered",
                        isAuto
                            ? "Auto update check found {0} Workshop item(s) with updates and queued Steam downloads. Restart after Steam finishes downloading."
                            : "Queued Steam Workshop update download for {0} item(s). Restart after Steam finishes downloading.",
                        result.TriggeredCount),
                    RitsuToastLevel.Info,
                    deferToastToMainMenu);
                return;
            }

            if (result.AlreadyQueuedCount > 0)
            {
                if (ShouldShowCompletionToast(source))
                    ShowToast(
                        Format(
                            "ritsulib.steamWorkshop.toast.alreadyQueued",
                            "{0} Workshop item(s) already have downloads queued or running.",
                            result.AlreadyQueuedCount),
                        RitsuToastLevel.Info,
                        deferToastToMainMenu);
                return;
            }

            if (ShouldShowCompletionToast(source))
                ShowToast(
                    Format(
                        "ritsulib.steamWorkshop.toast.none",
                        "Checked {0} subscribed Workshop item(s). No missing updates were found.",
                        result.InspectedCount),
                    RitsuToastLevel.Info,
                    deferToastToMainMenu);
        }

        private static bool ShouldShowCompletionToast(CheckSource source)
        {
            return source == CheckSource.Manual;
        }

        private static void LogCheckSummary(CheckSource source, RitsuSteamWorkshopUpdateResult result)
        {
            var prefix = source == CheckSource.Auto ? "Auto update check" : "Manual update check";
            if (!result.Available)
            {
                RitsuLibFramework.Logger.Info(
                    $"[SteamWorkshopUpdate] {prefix} could not inspect Workshop updates. Error={result.ErrorMessage ?? "<none>"}.");
                return;
            }

            if (result.NeedsUpdateCount <= 0)
            {
                RitsuLibFramework.Logger.Info(
                    $"[SteamWorkshopUpdate] {prefix} found no Workshop item updates. Inspected={result.InspectedCount}.");
                return;
            }

            if (result.FailedCount > 0)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SteamWorkshopUpdate] {prefix} detected Workshop item updates but not all downloads were queued. " +
                    $"NeedsUpdate={result.NeedsUpdateCount}, Triggered={result.TriggeredCount}, " +
                    $"AlreadyQueued={result.AlreadyQueuedCount}, Failed={result.FailedCount}.");
                return;
            }

            if (result.TriggeredCount > 0)
            {
                RitsuLibFramework.Logger.Info(
                    $"[SteamWorkshopUpdate] {prefix} detected Workshop item updates and queued Steam downloads. " +
                    $"NeedsUpdate={result.NeedsUpdateCount}, Triggered={result.TriggeredCount}, " +
                    $"AlreadyQueued={result.AlreadyQueuedCount}.");
                return;
            }

            RitsuLibFramework.Logger.Info(
                $"[SteamWorkshopUpdate] {prefix} found Workshop item updates that were already queued or downloading. " +
                $"NeedsUpdate={result.NeedsUpdateCount}, AlreadyQueued={result.AlreadyQueuedCount}.");
        }

        private static void ShowToast(string body, RitsuToastLevel level, bool deferToMainMenu)
        {
            if (deferToMainMenu)
            {
                UpdateCheckNotificationQueue.ShowWhenMainMenu(
                    "steam-workshop",
                    () => ShowToastNow(body, level));
                return;
            }

            PostToMainLoop(() => ShowToastNow(body, level));
        }

        private static void ShowToastNow(string body, RitsuToastLevel level)
        {
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

        private enum CheckSource
        {
            Auto,
            Manual,
        }
    }
}
