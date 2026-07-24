using STS2RitsuLib.Data;

namespace STS2RitsuLib.Updates
{
    internal static class AutomaticUpdateCheckScheduler
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<string, ScheduledCheck> Checks = new(StringComparer.Ordinal);
        private static int _cycleRunning;
        private static int? _deferredFromOrder;
        private static int _nextOrder;
        private static bool _initialized;
        private static bool _loopStarted;

        internal static IDisposable Register(
            string id,
            string displayName,
            Func<bool> isEnabled,
            Func<CancellationToken, Task> checkAsync)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentNullException.ThrowIfNull(isEnabled);
            ArgumentNullException.ThrowIfNull(checkAsync);

            Initialize();

            var check = new ScheduledCheck(
                id.Trim(),
                displayName.Trim(),
                isEnabled,
                checkAsync,
                Interlocked.Increment(ref _nextOrder));

            lock (SyncRoot)
            {
                Checks[check.Id] = check;
            }

            return new Registration(check.Id);
        }

        private static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                _initialized = true;
                UpdateCheckSessionState.Initialize();
                RitsuLibFramework.SubscribeLifecycle<EssentialInitializationStartingEvent>(_ =>
                    StartLoop("essential initialization starting"));
                RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ =>
                    StartLoop("first main menu fallback"));
                RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ => TryRunDeferredCycle());
                RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(_ => TryRunDeferredCycle());
                RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => TryRunDeferredCycle());
            }
        }

        internal static void StartForGameStartupError()
        {
            StartLoop("game startup error");
        }

        private static void StartLoop(string trigger)
        {
            lock (SyncRoot)
            {
                if (_loopStarted)
                    return;

                _loopStarted = true;
            }

            RitsuLibFramework.Logger.Info(
                $"[UpdateCheck] Automatic scheduler started: {trigger}.");
            _ = Task.Run(RunLoopAsync);
        }

        private static async Task RunLoopAsync()
        {
            while (true)
            {
                await TryRunCycleAsync("scheduled").ConfigureAwait(false);

                var interval = RitsuLibSettingsStore.GetUpdateCheckInterval();
                try
                {
                    await Task.Delay(interval).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[UpdateCheck] Automatic scheduler delay failed: {ex.Message}");
                }
            }
        }

        private static async Task TryRunCycleAsync(string source)
        {
            if (ShouldDeferForCombat())
            {
                RequestDeferredCycle($"{source} cycle reached a combat room", 0);
                return;
            }

            if (Interlocked.Exchange(ref _cycleRunning, 1) != 0)
            {
                RequestDeferredCycle($"{source} cycle overlapped another update check cycle", 0);
                return;
            }

            try
            {
                await RunCycleCoreAsync(0).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Exchange(ref _cycleRunning, 0);
                TryRunDeferredCycle();
            }
        }

        private static async Task RunCycleCoreAsync(int resumeFromOrder)
        {
            var deferInCombat = RitsuLibSettingsStore.ShouldDeferUpdateChecksInCombat();
            ScheduledCheck[] checks;
            lock (SyncRoot)
            {
                checks =
                [
                    .. Checks.Values
                        .Where(check => check.Order >= resumeFromOrder)
                        .OrderBy(check => check.Order),
                ];
            }

            foreach (var check in checks)
            {
                if (deferInCombat && UpdateCheckSessionState.IsCombatRoomActive)
                {
                    RequestDeferredCycle("entered a combat room during update check cycle", check.Order);
                    return;
                }

                if (!check.IsEnabled())
                    continue;

                try
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[UpdateCheck] Running automatic check: {check.DisplayName}.");
                    await check.CheckAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[UpdateCheck] Automatic check '{check.DisplayName}' failed: {ex.Message}");
                }
            }
        }

        private static bool ShouldDeferForCombat()
        {
            return RitsuLibSettingsStore.ShouldDeferUpdateChecksInCombat() &&
                   UpdateCheckSessionState.IsCombatRoomActive;
        }

        private static void RequestDeferredCycle(string reason, int resumeFromOrder)
        {
            lock (SyncRoot)
            {
                _deferredFromOrder = _deferredFromOrder is { } existing
                    ? Math.Min(existing, resumeFromOrder)
                    : resumeFromOrder;
            }

            RitsuLibFramework.Logger.Debug($"[UpdateCheck] Automatic check cycle deferred: {reason}.");
        }

        private static void TryRunDeferredCycle()
        {
            int resumeFromOrder;
            lock (SyncRoot)
            {
                if (_deferredFromOrder is not { } deferredFromOrder)
                    return;

                resumeFromOrder = deferredFromOrder;
            }

            if (ShouldDeferForCombat())
                return;

            if (Interlocked.CompareExchange(ref _cycleRunning, 1, 0) != 0)
                return;

            lock (SyncRoot)
            {
                if (_deferredFromOrder == resumeFromOrder)
                    _deferredFromOrder = null;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[UpdateCheck] Resuming deferred automatic check cycle from order {resumeFromOrder}.");
                    await RunCycleCoreAsync(resumeFromOrder).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Exchange(ref _cycleRunning, 0);
                    TryRunDeferredCycle();
                }
            });
        }

        private sealed class Registration(string id) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                lock (SyncRoot)
                {
                    Checks.Remove(id);
                }
            }
        }

        private sealed record ScheduledCheck(
            string Id,
            string DisplayName,
            Func<bool> IsEnabled,
            Func<CancellationToken, Task> CheckAsync,
            int Order);
    }
}
