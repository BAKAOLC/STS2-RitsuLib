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
        private const int MaxChangedItemsInToast = 10;
        private static readonly Lock SyncRoot = new();
        private static readonly Lock AutoDownloadNotificationSyncRoot = new();
        private static readonly Lock SubscriptionMonitorSyncRoot = new();
        private static readonly HashSet<ulong> SubscriptionMonitorItemIds = [];
        private static AutoDownloadNotification? _autoDownloadNotification;
        private static bool _initialized;
        private static int _checkRunning;

        internal static bool CanUseSteamWorkshopUpdates()
        {
            return SteamWorkshopManager.Instance.IsAvailable;
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
                        return CheckAsync(CheckSource.Auto, true, cancellationToken: cancellationToken);
                    });
            }
        }

        internal static void CheckNowFromSettings()
        {
            RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Manual check requested from settings.");
            _ = CheckAsync(CheckSource.Manual, false);
        }

        internal static void CheckRitsuLibNowFromSettings()
        {
            RitsuLibFramework.Logger.Info(
                "[SteamWorkshopUpdate] Manual RitsuLib Workshop item check requested from settings.");
            _ = CheckItemAsync(Const.SteamWorkshopItemId, false);
        }

        internal static RitsuSteamWorkshopActionResult SubscribeItemFromUi(ulong itemId, string? displayName = null)
        {
            var result = SteamWorkshopManager.Instance.SubscribeFromUi(itemId, displayName);
            if (result.Accepted)
                StartSubscriptionDownloadMonitor(itemId, displayName);
            return result;
        }

        internal static RitsuSteamWorkshopActionResult UnsubscribeItemFromUi(ulong itemId, string? displayName = null)
        {
            return SteamWorkshopManager.Instance.UnsubscribeFromUi(itemId, displayName);
        }

        internal static Task CheckItemAsync(
            ulong itemId,
            bool automatic,
            bool deferToastToMainMenu = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfZero(itemId);
            var source = automatic ? CheckSource.Auto : CheckSource.Manual;
            return CheckAsync(source, deferToastToMainMenu, [itemId], cancellationToken);
        }

        private static Task CheckAsync(CheckSource source,
            bool deferToastToMainMenu,
            IReadOnlyCollection<ulong>? itemIds = null,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _checkRunning, 1) == 0)
                // ReSharper disable once MethodSupportsCancellation
#pragma warning disable CA2016
                return Task.Run(async () =>
#pragma warning restore CA2016
                {
                    WorkshopUpdateProgressToast? progressToast = null;
                    try
                    {
                        if (source == CheckSource.Manual)
                            ClearAutoDownloadNotification();

                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Starting {source} check. SteamInitialized={SteamInitializer.Initialized}.");
                        if (ShouldShowProgressToast(source, deferToastToMainMenu))
                        {
                            progressToast = new(source);
                            progressToast.Start();
                        }

                        if (!SteamInitializer.Initialized)
                        {
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] {source} check skipped: Steam is not initialized.");
                            CompleteOrShowResult(
                                RitsuSteamWorkshopUpdateResult.Unavailable(),
                                source,
                                deferToastToMainMenu,
                                progressToast,
                                false);
                            return;
                        }

                        var result = await RitsuSteamWorkshopUpdates
                            .TriggerMissingUpdatesAsync(progressToast, itemIds, cancellationToken)
                            .ConfigureAwait(false);
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] {source} check result: Available={result.Available}, " +
                            $"Inspected={result.InspectedCount}, NeedsUpdate={result.NeedsUpdateCount}, " +
                            $"Triggered={result.TriggeredCount}, AlreadyQueued={result.AlreadyQueuedCount}, " +
                            $"Failed={result.FailedCount}, Error={result.ErrorMessage ?? "<none>"}.");
                        LogCheckSummary(source, result);
                        if (source == CheckSource.Auto && result.MonitorItems is { Count: > 0 })
                        {
                            StartAutoDownloadNotification(result, deferToastToMainMenu);
                            return;
                        }

                        var downloadFinished = await MonitorTriggeredDownloadsAsync(
                                result,
                                progressToast,
                                true,
                                cancellationToken)
                            .ConfigureAwait(false);
                        CompleteOrShowResult(
                            result,
                            source,
                            deferToastToMainMenu,
                            progressToast,
                            downloadFinished);
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn($"[SteamWorkshopUpdate] Check failed: {ex.Message}");
                        CompleteOrShowResult(
                            RitsuSteamWorkshopUpdateResult.Unavailable(ex.Message),
                            source,
                            deferToastToMainMenu,
                            progressToast,
                            false);
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

        private static void CompleteOrShowResult(
            RitsuSteamWorkshopUpdateResult result,
            CheckSource source,
            bool deferToastToMainMenu,
            WorkshopUpdateProgressToast? progressToast,
            bool downloadFinished)
        {
            if (progressToast != null)
            {
                progressToast.Complete(result, downloadFinished);
                return;
            }

            ShowResultToast(result, source, deferToastToMainMenu, downloadFinished);
        }

        private static void ShowResultToast(
            RitsuSteamWorkshopUpdateResult result,
            CheckSource source,
            bool deferToastToMainMenu,
            bool downloadFinished)
        {
            var request = BuildResultToastRequest(result, source, false, downloadFinished);
            if (request != null)
                ShowToast(request, deferToastToMainMenu);
        }

        private static RitsuToastRequest? BuildResultToastRequest(
            RitsuSteamWorkshopUpdateResult result,
            CheckSource source,
            bool forceCompletionToast,
            bool downloadFinished)
        {
            if (!result.Available)
            {
                if (!forceCompletionToast && !ShouldShowCompletionToast(source))
                    return null;
                var body = result.ErrorMessage == null
                    ? L("ritsulib.steamWorkshop.toast.unavailable",
                        "Steam Workshop is not available in this session.")
                    : Format(
                        "ritsulib.steamWorkshop.toast.failed",
                        "Steam Workshop check failed: {0}",
                        result.ErrorMessage);
                return ToastRequest(body, result.ErrorMessage == null ? RitsuToastLevel.Info : RitsuToastLevel.Warning);
            }

            switch (source)
            {
                case CheckSource.Subscription:
                    return BuildSubscriptionDownloadToast(result, downloadFinished);
                case CheckSource.Auto when
                    result is { NeedsUpdateCount: > 0, ChangedItems: not { Count: > 0 }, TriggeredCount: <= 0 }:
                    return null;
            }

            if (result.FailedCount > 0)
                return ToastRequest(
                    AppendChangedItemList(
                        Format(
                            "ritsulib.steamWorkshop.toast.partial",
                            "Found {0} Workshop item(s) with updates. Asked Steam to download {1}; {2} failed. Check Steam Downloads and restart after Steam finishes.",
                            result.NeedsUpdateCount,
                            result.TriggeredCount,
                            result.FailedCount),
                        result.ChangedItems),
                    RitsuToastLevel.Warning);

            if (downloadFinished && result.MonitorItems is { Count: > 0 })
                return BuildAutoDownloadFinishedToast(result);

            if (result.TriggeredCount > 0)
                return ToastRequest(
                    AppendChangedItemList(
                        Format(
                            source == CheckSource.Auto
                                ? "ritsulib.steamWorkshop.toast.autoTriggered"
                                : "ritsulib.steamWorkshop.toast.triggered",
                            source == CheckSource.Auto
                                ? "Workshop updates found for {0} item(s). Steam has been asked to queue the downloads. Check Steam Downloads and restart after Steam finishes."
                                : "Asked Steam to download Workshop updates for {0} item(s). Check Steam Downloads and restart after Steam finishes.",
                            result.TriggeredCount),
                        result.ChangedItems),
                    RitsuToastLevel.Info);

            if (result.AlreadyQueuedCount > 0)
            {
                if (!forceCompletionToast &&
                    !ShouldShowCompletionToast(source) &&
                    !(source == CheckSource.Auto && result.ChangedItems is { Count: > 0 }))
                    return null;
                return ToastRequest(
                    AppendChangedItemList(
                        Format(
                            "ritsulib.steamWorkshop.toast.alreadyQueued",
                            "{0} Workshop item(s) already have downloads queued or running.",
                            result.AlreadyQueuedCount),
                        result.ChangedItems),
                    RitsuToastLevel.Info);
            }

            if (!forceCompletionToast && !ShouldShowCompletionToast(source))
                return null;
            return ToastRequest(
                Format(
                    "ritsulib.steamWorkshop.toast.none",
                    "Checked {0} subscribed Workshop item(s). No missing updates were found.",
                    result.InspectedCount),
                RitsuToastLevel.Info);
        }

        private static RitsuToastRequest ToastRequest(string body, RitsuToastLevel level)
        {
            return new(
                body,
                L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"),
                null,
                level,
                ToastDurationSeconds);
        }

        private static RitsuToastRequest BuildSubscriptionDownloadToast(
            RitsuSteamWorkshopUpdateResult result,
            bool downloadFinished)
        {
            var itemList = FormatDownloadItemList(result.MonitorItems);
            return ToastRequest(
                downloadFinished
                    ? Format(
                        "ritsulib.steamWorkshop.toast.subscribeDownloadFinished",
                        "Steam finished downloading {0}. Restart the game to load the new files.",
                        itemList)
                    : Format(
                        "ritsulib.steamWorkshop.toast.subscribeDownloadPending",
                        "Steam accepted the subscription for {0}. The download is still queued, running, or not reporting progress yet. Check Steam Downloads and restart after it finishes.",
                        itemList),
                RitsuToastLevel.Info);
        }

        private static string FormatDownloadItemList(IReadOnlyList<RitsuSteamWorkshopDownloadItem>? items)
        {
            if (items is not { Count: > 0 })
                return L("ritsulib.steamWorkshop.toast.progress.downloadUnknownItem", "unknown item");

            var names = items
                .Take(MaxChangedItemsInToast)
                .Select(static item => item.DisplayName)
                .ToArray();
            var remaining = items.Count - names.Length;
            return remaining > 0
                ? $"{string.Join(", ", names)} (+{remaining})"
                : string.Join(", ", names);
        }

        private static string AppendChangedItemList(
            string body,
            IReadOnlyList<RitsuSteamWorkshopChangedItem>? changedItems)
        {
            if (changedItems is not { Count: > 0 })
                return body;

            var names = changedItems
                .Take(MaxChangedItemsInToast)
                .Select(static item => item.DisplayName)
                .ToArray();
            var remaining = changedItems.Count - names.Length;
            var list = remaining > 0
                ? $"{string.Join(", ", names)} (+{remaining})"
                : string.Join(", ", names);
            return body + "\n" + Format(
                "ritsulib.steamWorkshop.toast.changedSinceLastCheck",
                "Changed since last check:\n{0}",
                list);
        }

        private static bool ShouldShowCompletionToast(CheckSource source)
        {
            return source == CheckSource.Manual;
        }

        private static bool ShouldShowProgressToast(CheckSource source, bool deferToastToMainMenu)
        {
            if (source != CheckSource.Manual)
                return false;

            return !deferToastToMainMenu || UpdateCheckSessionState.IsMainMenuActive;
        }

        private static async Task<bool> MonitorTriggeredDownloadsAsync(
            RitsuSteamWorkshopUpdateResult result,
            WorkshopUpdateProgressToast? progressToast,
            bool stopWhenIdle,
            CancellationToken cancellationToken)
        {
            if (result.MonitorItems is not { Count: > 0 })
                return false;

            return await RitsuSteamWorkshopUpdates
                .MonitorDownloadsAsync(result.MonitorItems, progressToast, stopWhenIdle, cancellationToken)
                .ConfigureAwait(false);
        }

        private static void StartAutoDownloadNotification(
            RitsuSteamWorkshopUpdateResult result,
            bool deferToastToMainMenu)
        {
            var notification = new AutoDownloadNotification(result);
            lock (AutoDownloadNotificationSyncRoot)
            {
                _autoDownloadNotification = notification;
            }

            if (BuildResultToastRequest(result, CheckSource.Auto, false, false) != null)
                QueueAutoDownloadStatusToast(notification, deferToastToMainMenu);

            _ = MonitorAutoDownloadsAsync(notification);
        }

        private static async Task MonitorAutoDownloadsAsync(AutoDownloadNotification notification)
        {
            var downloadFinished = await MonitorTriggeredDownloadsAsync(
                    notification.Result,
                    null,
                    false,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (!downloadFinished)
                return;

            notification.MarkFinished();
            if (!notification.HasShownInitial || !UpdateCheckSessionState.IsMainMenuActive)
                return;

            if (!notification.TryMarkCompletionShown())
                return;

            PostToMainLoop(() =>
            {
                if (IsCurrentAutoDownloadNotification(notification))
                    ShowToastNow(BuildAutoDownloadFinishedToast(notification.Result));
            });
        }

        private static void StartSubscriptionDownloadMonitor(ulong itemId, string? displayName)
        {
            lock (SubscriptionMonitorSyncRoot)
            {
                if (!SubscriptionMonitorItemIds.Add(itemId))
                    return;
            }

            _ = Task.Run(async () =>
            {
                var item = new RitsuSteamWorkshopDownloadItem(
                    itemId,
                    FormatWorkshopItemName(itemId, displayName));
                var items = new[] { item };
                var result = new RitsuSteamWorkshopUpdateResult(
                    true,
                    1,
                    1,
                    1,
                    0,
                    0,
                    TriggeredItems: items,
                    MonitorItems: items);
                var progressToast = new WorkshopUpdateProgressToast(CheckSource.Subscription);
                var downloadFinished = false;
                try
                {
                    await Task.Delay(1500).ConfigureAwait(false);
                    progressToast.StartDownload(items);
                    downloadFinished = await RitsuSteamWorkshopUpdates
                        .MonitorDownloadsAsync(items, progressToast, true, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Subscription download monitor failed for item {itemId}: {ex.Message}");
                }
                finally
                {
                    lock (SubscriptionMonitorSyncRoot)
                    {
                        SubscriptionMonitorItemIds.Remove(itemId);
                    }
                }

                progressToast.Complete(result, downloadFinished);
            });
        }

        private static void QueueAutoDownloadStatusToast(
            AutoDownloadNotification notification,
            bool deferToastToMainMenu)
        {
            if (!deferToastToMainMenu)
            {
                PostToMainLoop(() => ShowAutoDownloadStatusToast(notification));
                return;
            }

            UpdateCheckNotificationQueue.ShowWhenMainMenu(
                "steam-workshop",
                () => ShowAutoDownloadStatusToast(notification));
        }

        private static void ShowAutoDownloadStatusToast(AutoDownloadNotification notification)
        {
            if (!IsCurrentAutoDownloadNotification(notification) || !notification.TryMarkInitialShown())
                return;

            if (notification.DownloadFinished)
                notification.TryMarkCompletionShown();

            var request = BuildResultToastRequest(
                notification.Result,
                CheckSource.Auto,
                false,
                notification.DownloadFinished);
            if (request != null)
                ShowToastNow(request);
        }

        private static bool IsCurrentAutoDownloadNotification(AutoDownloadNotification notification)
        {
            lock (AutoDownloadNotificationSyncRoot)
            {
                return ReferenceEquals(_autoDownloadNotification, notification);
            }
        }

        private static void ClearAutoDownloadNotification()
        {
            lock (AutoDownloadNotificationSyncRoot)
            {
                _autoDownloadNotification = null;
            }
        }

        private static RitsuToastRequest BuildAutoDownloadFinishedToast(RitsuSteamWorkshopUpdateResult result)
        {
            return ToastRequest(
                AppendChangedItemList(
                    Format(
                        "ritsulib.steamWorkshop.toast.downloadFinished",
                        "Updated {0} Steam Workshop item(s). Restart the game to load the new files.",
                        result.MonitorItems?.Count ?? result.TriggeredCount),
                    result.ChangedItems),
                RitsuToastLevel.Info);
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
                var action = source == CheckSource.Auto
                    ? "asked Steam to queue downloads"
                    : "queued Steam downloads";
                RitsuLibFramework.Logger.Info(
                    $"[SteamWorkshopUpdate] {prefix} detected Workshop item updates and {action}. " +
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
            ShowToast(ToastRequest(body, level), deferToMainMenu);
        }

        private static RitsuSteamWorkshopActionResult RequestSubscriptionAction(
            ulong itemId,
            string? displayName,
            bool subscribe)
        {
            var action = subscribe ? "subscribe" : "unsubscribe";
            RitsuLibFramework.Logger.Info(
                $"[SteamWorkshopUpdate] Workshop {action} requested from UI. Item={itemId}.");
            if (!SteamInitializer.Initialized)
            {
                var result = RitsuSteamWorkshopActionResult.Unavailable(itemId);
                ShowToast(
                    L("ritsulib.steamWorkshop.toast.unavailable",
                        "Steam Workshop is not available in this session."),
                    RitsuToastLevel.Warning,
                    false);
                return result;
            }

            var actionResult = subscribe
                ? RitsuSteamWorkshopUpdates.RequestSubscribe(itemId)
                : RitsuSteamWorkshopUpdates.RequestUnsubscribe(itemId);
            var title = L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates");
            if (!actionResult.Available)
            {
                RitsuToastService.ShowWarning(
                    actionResult.ErrorMessage ??
                    L("ritsulib.steamWorkshop.toast.unavailable",
                        "Steam Workshop is not available in this session."),
                    title);
                return actionResult;
            }

            if (!actionResult.Accepted)
            {
                RitsuToastService.ShowWarning(
                    Format(
                        subscribe
                            ? "ritsulib.steamWorkshop.toast.subscribeFailed"
                            : "ritsulib.steamWorkshop.toast.unsubscribeFailed",
                        subscribe
                            ? "Steam did not accept the subscribe request for {0}."
                            : "Steam did not accept the unsubscribe request for {0}.",
                        FormatWorkshopItemName(itemId, displayName)),
                    title);
                return actionResult;
            }

            RitsuToastService.ShowInfo(
                Format(
                    subscribe
                        ? "ritsulib.steamWorkshop.toast.subscribeRequested"
                        : "ritsulib.steamWorkshop.toast.unsubscribeRequested",
                    subscribe
                        ? "Asked Steam to subscribe to {0}. Check Steam Downloads and restart after it finishes."
                        : "Asked Steam to unsubscribe from {0}. Restart the game after Steam applies the change.",
                    FormatWorkshopItemName(itemId, displayName)),
                title);
            return actionResult;
        }

        private static string FormatWorkshopItemName(ulong itemId, string? displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Workshop item {itemId}"
                : $"{displayName.Trim()} ({itemId})";
        }

        private static void ShowToast(RitsuToastRequest request, bool deferToMainMenu)
        {
            if (deferToMainMenu)
            {
                UpdateCheckNotificationQueue.ShowWhenMainMenu(
                    "steam-workshop",
                    () => ShowToastNow(request));
                return;
            }

            PostToMainLoop(() => ShowToastNow(request));
        }

        private static void ShowToastNow(RitsuToastRequest request)
        {
            RitsuToastService.Show(request);
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
            Subscription,
        }

        private sealed class WorkshopUpdateProgressToast(CheckSource source)
            : IProgress<RitsuSteamWorkshopUpdateProgress>, IProgress<RitsuSteamWorkshopDownloadProgress>
        {
            private static readonly TimeSpan ProgressUpdateMinInterval = TimeSpan.FromMilliseconds(250);
            private readonly Lock _syncRoot = new();
            private bool _completed;
            private RitsuToastHandle? _handle;
            private DateTimeOffset _lastProgressUpdateAt;
            private RitsuSteamWorkshopUpdateProgress? _latestProgress;
            private bool _progressUpdateQueued;

            public void Report(RitsuSteamWorkshopDownloadProgress value)
            {
                lock (_syncRoot)
                {
                    if (_completed)
                        return;
                }

                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        if (_completed)
                            return;
                        _handle?.Update(BuildDownloadProgressRequest(value), false);
                    }
                });
            }

            public void Report(RitsuSteamWorkshopUpdateProgress value)
            {
                TimeSpan delay;
                lock (_syncRoot)
                {
                    if (_completed)
                        return;
                    _latestProgress = value;
                    if (_progressUpdateQueued)
                        return;

                    _progressUpdateQueued = true;
                    delay = ResolveProgressUpdateDelay();
                }

                if (delay > TimeSpan.Zero)
                {
                    _ = DelayProgressUpdateAsync(delay);
                    return;
                }

                PostProgressUpdateToMainLoop();
            }

            private async Task DelayProgressUpdateAsync(TimeSpan delay)
            {
                await Task.Delay(delay).ConfigureAwait(false);
                PostProgressUpdateToMainLoop();
            }

            private TimeSpan ResolveProgressUpdateDelay()
            {
                if (_lastProgressUpdateAt == default)
                    return TimeSpan.Zero;

                var elapsed = DateTimeOffset.UtcNow - _lastProgressUpdateAt;
                return elapsed >= ProgressUpdateMinInterval
                    ? TimeSpan.Zero
                    : ProgressUpdateMinInterval - elapsed;
            }

            private void PostProgressUpdateToMainLoop()
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        if (_completed)
                            return;
                        _progressUpdateQueued = false;
                        _lastProgressUpdateAt = DateTimeOffset.UtcNow;
                        if (_latestProgress is { } latestProgress)
                            UpdateNow(latestProgress);
                    }
                });
            }

            public void Start()
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        if (_completed)
                            return;

                        _handle = RitsuToastService.ShowTracked(BuildProgressRequest(
                            new(RitsuSteamWorkshopUpdateProgressStage.Starting, 0, 1)));
                        if (_latestProgress is { } latest)
                            UpdateNow(latest);
                    }
                });
            }

            public void StartDownload(IReadOnlyList<RitsuSteamWorkshopDownloadItem> items)
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        if (_completed)
                            return;

                        var total = Math.Max(1, items.Count);
                        _handle = RitsuToastService.ShowTracked(BuildDownloadProgressRequest(
                            new(0, total, 0, 0, items.FirstOrDefault()?.DisplayName)));
                    }
                });
            }

            public void Complete(
                RitsuSteamWorkshopUpdateResult result,
                bool downloadFinished)
            {
                PostToMainLoop(() =>
                {
                    lock (_syncRoot)
                    {
                        _completed = true;
                        var request = BuildResultToastRequest(
                            result,
                            source,
                            source == CheckSource.Manual,
                            downloadFinished);
                        if (request == null)
                        {
                            _handle?.Close();
                            return;
                        }

                        request = request.WithProgress(null).Persistent(false);
                        if (_handle?.Update(request) == true)
                            return;

                        RitsuToastService.Show(request);
                    }
                });
            }

            private void UpdateNow(RitsuSteamWorkshopUpdateProgress progress)
            {
                _handle?.Update(BuildProgressRequest(progress), false);
            }

            private static RitsuToastRequest BuildDownloadProgressRequest(RitsuSteamWorkshopDownloadProgress progress)
            {
                var body = progress.BytesTotal > 0
                    ? Format(
                        "ritsulib.steamWorkshop.toast.progress.download",
                        "Downloading Workshop updates: {0}/{1} item(s), {2}/{3}.\nCurrent: {4}",
                        progress.CompletedCount,
                        progress.TotalCount,
                        FormatBytes(progress.BytesDownloaded),
                        FormatBytes(progress.BytesTotal),
                        progress.CurrentItemName ?? L("ritsulib.steamWorkshop.toast.progress.downloadUnknownItem",
                            "unknown item"))
                    : Format(
                        "ritsulib.steamWorkshop.toast.progress.downloadWaiting",
                        "Downloading Workshop updates: {0}/{1} item(s). Waiting for Steam download size...\nCurrent: {2}",
                        progress.CompletedCount,
                        progress.TotalCount,
                        progress.CurrentItemName ?? L("ritsulib.steamWorkshop.toast.progress.downloadUnknownItem",
                            "unknown item"));

                var fraction = progress.BytesTotal > 0
                    ? Mathf.Clamp((float)progress.BytesDownloaded / progress.BytesTotal, 0f, 1f)
                    : Mathf.Clamp((float)progress.CompletedCount / Math.Max(1, progress.TotalCount), 0f, 1f);
                return new RitsuToastRequest(
                        body,
                        L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"))
                    .Persistent()
                    .WithDismissOnClick(false)
                    .WithProgress(fraction);
            }

            private static RitsuToastRequest BuildProgressRequest(RitsuSteamWorkshopUpdateProgress progress)
            {
                var body = progress.Stage switch
                {
                    RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions => Format(
                        "ritsulib.steamWorkshop.toast.progress.subscriptions",
                        "Reading subscribed Workshop items...",
                        progress.CompletedCount,
                        progress.TotalCount),
                    RitsuSteamWorkshopUpdateProgressStage.RefreshingDetails => Format(
                        "ritsulib.steamWorkshop.toast.progress.details",
                        "Refreshing Workshop details: {0}/{1}",
                        progress.CompletedCount,
                        progress.TotalCount),
                    RitsuSteamWorkshopUpdateProgressStage.InspectingItems => Format(
                        "ritsulib.steamWorkshop.toast.progress.inspecting",
                        "Checking Workshop items: {0}/{1}. Updates: {2}; queued: {3}; failed: {4}.",
                        progress.CompletedCount,
                        progress.TotalCount,
                        progress.NeedsUpdateCount,
                        progress.QueuedCount + progress.AlreadyQueuedCount,
                        progress.FailedCount),
                    _ => L(
                        "ritsulib.steamWorkshop.toast.progress.starting",
                        "Starting Steam Workshop update check..."),
                };

                var total = Math.Max(1, progress.TotalCount);
                var fraction = Mathf.Clamp((float)progress.CompletedCount / total, 0f, 1f);
                return new RitsuToastRequest(
                        body,
                        L("ritsulib.steamWorkshop.toast.title", "Steam Workshop updates"))
                    .Persistent()
                    .WithDismissOnClick(false)
                    .WithProgress(fraction);
            }

            private static string FormatBytes(ulong bytes)
            {
                string[] units = ["B", "KB", "MB", "GB"];
                var value = (double)bytes;
                var unit = 0;
                while (value >= 1024d && unit < units.Length - 1)
                {
                    value /= 1024d;
                    unit++;
                }

                return unit == 0
                    ? $"{bytes} {units[unit]}"
                    : $"{value:0.0} {units[unit]}";
            }
        }

        private sealed class AutoDownloadNotification(RitsuSteamWorkshopUpdateResult result)
        {
            private int _completionShown;
            private int _downloadFinished;
            private int _initialShown;

            public RitsuSteamWorkshopUpdateResult Result { get; } = result;

            public bool DownloadFinished => Volatile.Read(ref _downloadFinished) != 0;

            public bool HasShownInitial => Volatile.Read(ref _initialShown) != 0;

            public void MarkFinished()
            {
                Volatile.Write(ref _downloadFinished, 1);
            }

            public bool TryMarkInitialShown()
            {
                return Interlocked.Exchange(ref _initialShown, 1) == 0;
            }

            public bool TryMarkCompletionShown()
            {
                return Interlocked.Exchange(ref _completionShown, 1) == 0;
            }
        }
    }
}
