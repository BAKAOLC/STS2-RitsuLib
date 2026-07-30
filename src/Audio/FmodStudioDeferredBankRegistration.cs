namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Coordinates thread-safe, deduplicated loading of FMOD Studio banks and GUID mapping files after <see cref="DeferredInitializationCompletedEvent" />.</para>
    ///     <para xml:lang="zh-CN">协调在 <see cref="DeferredInitializationCompletedEvent" /> 后加载 FMOD Studio 音频库和 GUID 映射文件，并提供线程安全的去重处理。</para>
    /// </summary>
    public static class FmodStudioDeferredBankRegistration
    {
        private static readonly Lock Gate = new();
        private static readonly Lock FlushGate = new();
        private static readonly HashSet<string> PendingBanks = new(StringComparer.Ordinal);
        private static readonly HashSet<string> PendingGuidFiles = new(StringComparer.Ordinal);
        private static bool _flushHookRegistered;

        /// <summary>
        ///     <para xml:lang="en">Registers a bank path for the next deferred flush, ignoring blank paths and duplicate trimmed values.</para>
        ///     <para xml:lang="zh-CN">注册音频库路径以供下一次延迟刷新加载；空白路径和去除首尾空白后的重复值会被忽略。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The Godot resource path of the FMOD Studio bank.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 音频库的 Godot 资源路径。</para>
        /// </param>
        /// <remarks>
        ///     <para xml:lang="en">If deferred initialization has already completed, lifecycle replay starts the flush synchronously. A failed path remains pending until a later registration triggers another flush.</para>
        ///     <para xml:lang="zh-CN">如果延迟初始化已经完成，生命周期回放会同步开始刷新。加载失败的路径会继续保留，直到后续注册再次触发刷新。</para>
        /// </remarks>
        public static void RegisterBank(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            lock (Gate)
                PendingBanks.Add(resourcePath.Trim());

            EnsureFlushHookRegistered();
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a GUID mapping file for <see cref="FmodStudioServer.TryLoadStudioGuidMappings" /> during the next deferred flush.</para>
        ///     <para xml:lang="zh-CN">注册 GUID 映射文件，以便在下一次延迟刷新中通过 <see cref="FmodStudioServer.TryLoadStudioGuidMappings" /> 加载。</para>
        /// </summary>
        /// <param name="guidMapResourcePath">
        ///     <para xml:lang="en">The Godot resource path of a <c>GUIDs.txt</c>-style mapping file; blank paths and duplicate trimmed values are ignored.</para>
        ///     <para xml:lang="zh-CN"><c>GUIDs.txt</c> 格式映射文件的 Godot 资源路径；空白路径和去除首尾空白后的重复值会被忽略。</para>
        /// </param>
        /// <remarks>
        ///     <para xml:lang="en">If deferred initialization has already completed, lifecycle replay starts the flush synchronously. A failed path remains pending until a later registration triggers another flush.</para>
        ///     <para xml:lang="zh-CN">如果延迟初始化已经完成，生命周期回放会同步开始刷新。加载失败的路径会继续保留，直到后续注册再次触发刷新。</para>
        /// </remarks>
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
