namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Queues FMOD Studio bank and GUID mapping paths until <see cref="DeferredInitializationCompletedEvent" />,
    ///     then loads them in one batch with a single <see cref="FmodStudioServer.TryWaitForAllLoads" />.
    ///     将 FMOD Studio bank 和 GUID 映射路径排队到 <see cref="DeferredInitializationCompletedEvent" />，
    ///     然后通过单次 <see cref="FmodStudioServer.TryWaitForAllLoads" /> 批量加载它们。
    /// </summary>
    public static class FmodStudioDeferredBankRegistration
    {
        private static readonly Lock Gate = new();
        private static readonly Lock FlushGate = new();
        private static readonly HashSet<string> PendingBanks = new(StringComparer.Ordinal);
        private static readonly HashSet<string> PendingGuidFiles = new(StringComparer.Ordinal);
        private static bool _flushHookRegistered;

        /// <summary>
        ///     Queues a bank path to load after deferred initialization (deduplicated).
        ///     将 bank 路径排队，等待延迟初始化后加载（去重）。
        /// </summary>
        public static void RegisterBank(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            lock (Gate)
                PendingBanks.Add(resourcePath.Trim());

            EnsureFlushHookRegistered();
        }

        /// <summary>
        ///     Queues a GUID mapping file for <see cref="FmodStudioServer.TryLoadStudioGuidMappings" /> after deferred
        ///     initialization (deduplicated).
        ///     将用于 <see cref="FmodStudioServer.TryLoadStudioGuidMappings" /> 的 GUID 映射文件排队，等待延迟
        ///     初始化后加载（去重）。
        /// </summary>
        public static void RegisterStudioGuidMappings(string guidMapResourcePath)
        {
            if (string.IsNullOrWhiteSpace(guidMapResourcePath))
                return;

            lock (Gate)
                PendingGuidFiles.Add(guidMapResourcePath.Trim());

            EnsureFlushHookRegistered();
        }

        private static void EnsureFlushHookRegistered()
        {
            lock (Gate)
            {
                if (_flushHookRegistered)
                    return;

                _flushHookRegistered = true;
            }

            try
            {
                RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ =>
                {
                    lock (Gate)
                        _flushHookRegistered = false;

                    FlushPending();
                });
            }
            catch
            {
                lock (Gate)
                    _flushHookRegistered = false;

                throw;
            }
        }

        private static void FlushPending()
        {
            lock (FlushGate)
                FlushPendingCore();
        }

        private static void FlushPendingCore()
        {
            if (FmodStudioServer.TryGet() is null)
            {
                RitsuLibFramework.Logger.Warn(
                    "[Audio] deferred FMOD: FmodServer singleton missing; pending banks/GUID files kept for a later flush."
                );
                return;
            }

            List<string> banks;
            List<string> guids;

            lock (Gate)
            {
                banks = [.. PendingBanks];
                guids = [.. PendingGuidFiles];
                PendingBanks.Clear();
                PendingGuidFiles.Clear();
            }

            if (banks.Count == 0 && guids.Count == 0)
                return;

            var failedBanks = new List<string>();
            var failedGuids = new List<string>();

            foreach (var path in banks)
                if (!FmodStudioServer.TryLoadBank(path))
                    failedBanks.Add(path);

            foreach (var path in guids)
                if (!FmodStudioServer.TryLoadStudioGuidMappings(path))
                    failedGuids.Add(path);

            if (failedBanks.Count < banks.Count || failedGuids.Count < guids.Count)
                FmodStudioServer.TryWaitForAllLoads();

            if (failedBanks.Count > 0 || failedGuids.Count > 0)
            {
                lock (Gate)
                {
                    PendingBanks.UnionWith(failedBanks);
                    PendingGuidFiles.UnionWith(failedGuids);
                }
            }

            RitsuLibFramework.Logger.Info(
                $"[Audio] deferred FMOD flush complete " +
                $"(banks={banks.Count - failedBanks.Count}/{banks.Count}, " +
                $"guid files={guids.Count - failedGuids.Count}/{guids.Count})."
            );

            if (failedBanks.Count > 0 || failedGuids.Count > 0)
                RitsuLibFramework.Logger.Warn(
                    $"[Audio] deferred FMOD flush retained {failedBanks.Count} bank(s) and " +
                    $"{failedGuids.Count} GUID file(s) for retry."
                );
        }
    }
}
