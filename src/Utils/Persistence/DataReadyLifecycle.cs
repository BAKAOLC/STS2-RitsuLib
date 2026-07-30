using STS2RitsuLib.Data;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates profile-data readiness and publishes <see cref="ProfileDataReadyEvent" />,
    ///         <see cref="ProfileDataChangedEvent" />, and <see cref="ProfileDataInvalidatedEvent" /> lifecycle events.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         协调档案数据的就绪状态，并发布 <see cref="ProfileDataReadyEvent" />、<see cref="ProfileDataChangedEvent" />
    ///         和 <see cref="ProfileDataInvalidatedEvent" /> 生命周期事件。
    ///     </para>
    /// </summary>
    public static class DataReadyLifecycle
    {
        private static readonly Lock SyncRoot = new();

        private static ProfileDataReadyEvent? _lastReadyEvent;

        /// <summary>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when profile-path initialization has completed and the data is
        ///         considered safe to use.
        ///     </para>
        ///     <para xml:lang="zh-CN">档案路径初始化完成且数据可安全使用时为 <see langword="true" />。</para>
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Profile ID associated with the last ready notification, or <c>-1</c> when not ready.</para>
        ///     <para xml:lang="zh-CN">与最近一次就绪通知关联的档案 ID；未就绪时为 <c>-1</c>。</para>
        /// </summary>
        public static int ReadyProfileId { get; private set; } = -1;

        /// <summary>
        ///     <para xml:lang="en">Lifecycle state derived from <see cref="IsReady" />.</para>
        ///     <para xml:lang="zh-CN">从 <see cref="IsReady" /> 派生的生命周期状态。</para>
        /// </summary>
        public static DataLifecycleState State =>
            IsReady ? DataLifecycleState.Ready : DataLifecycleState.WaitingForProfile;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes the current profile, ensures profile services, reloads data after path changes, and
        ///         publishes lifecycle events when appropriate.
        ///     </para>
        ///     <para xml:lang="zh-CN">刷新当前档案，确保档案服务可用，在路径变化后重新加载数据，并在适当时发布生命周期事件。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">Diagnostic label for log and event payloads.</para>
        ///     <para xml:lang="zh-CN">用于日志和事件载荷的诊断标签。</para>
        /// </param>
        public static void NotifyPotentialReady(string source)
        {
            try
            {
                ProfileManager.Instance.RefreshCurrentProfile();

                var modDataInteropRegistered = ModDataRuntimeInterop.TryRegisterAll();
                if (modDataInteropRegistered > 0)
                    RitsuLibFramework.Logger.Debug(
                        $"ModData runtime interop: registered {modDataInteropRegistered} provider schema(s) during data-ready refresh.");

                RitsuLibFramework.EnsureProfileServicesInitialized();

                var dataReloaded = ModDataStore.ReloadAllIfPathChanged();
                if (dataReloaded)
                    ModDataRuntimeInterop.PushLoadedDataToAllProviders();

                var profileId = ProfileManager.Instance.CurrentProfileId;
                bool isInitialReady;
                int previousProfileId;
                bool isProfileSwitch;
                ProfileDataReadyEvent readyEvent;

                lock (SyncRoot)
                {
                    isInitialReady = !IsReady;
                    previousProfileId = ReadyProfileId;
                    isProfileSwitch = !isInitialReady && previousProfileId != profileId;

                    IsReady = true;
                    ReadyProfileId = profileId;

                    readyEvent = new(
                        profileId,
                        source,
                        isInitialReady,
                        isProfileSwitch,
                        dataReloaded,
                        DateTimeOffset.UtcNow
                    );

                    _lastReadyEvent = readyEvent;
                }

                if (!isInitialReady && !isProfileSwitch)
                    return;

                if (isProfileSwitch)
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileDataChangedEvent(
                            previousProfileId,
                            profileId,
                            source,
                            DateTimeOffset.UtcNow
                        ),
                        nameof(ProfileDataChangedEvent)
                    );

                if (ModDataStore.HasAnyProfileScopedEntries)
                    RitsuLibFramework.Logger.Info($"Data ready for profile {profileId} ({source})");

                RitsuLibFramework.PublishLifecycleEvent(readyEvent, nameof(ProfileDataReadyEvent));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Persistence] Failed to notify data ready lifecycle from '{source}': {ex.Message}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invalidates the given profile and publishes <see cref="ProfileDataInvalidatedEvent" /> if it
        ///         was the active ready profile.
        ///     </para>
        ///     <para xml:lang="zh-CN">使指定档案失效；如果它是当前已就绪的活动档案，则发布 <see cref="ProfileDataInvalidatedEvent" />。</para>
        /// </summary>
        public static void NotifyProfileInvalidated(int profileId, string reason)
        {
            if (profileId < 0)
                return;

            var shouldRaise = false;

            lock (SyncRoot)
            {
                if (IsReady && ReadyProfileId == profileId)
                {
                    IsReady = false;
                    ReadyProfileId = -1;
                    _lastReadyEvent = null;
                    shouldRaise = true;
                }
            }

            if (!shouldRaise)
                return;

            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileDataInvalidatedEvent(profileId, reason, DateTimeOffset.UtcNow),
                nameof(ProfileDataInvalidatedEvent)
            );
        }
    }
}
