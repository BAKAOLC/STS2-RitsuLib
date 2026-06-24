using STS2RitsuLib.Data;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Profile data lifecycle hub:
    ///     - ProfileDataReady: profile data is safe to read/write
    ///     - ProfileDataChanged: profile switched after being ready
    ///     - ProfileDataInvalidated: current ready profile became invalid
    ///     档案数据生命周期枢纽：
    ///     - ProfileDataReady：档案数据可安全读写
    ///     - ProfileDataChanged：ready 后档案发生切换
    ///     - ProfileDataInvalidated：当前 ready 档案变为无效
    /// </summary>
    public static class DataReadyLifecycle
    {
        private static readonly Lock SyncRoot = new();

        private static ProfileDataReadyEvent? _lastReadyEvent;

        /// <summary>
        ///     True when profile path initialization completed and data is considered safe to use.
        ///     当档案路径初始化完成且数据被认为可安全使用时为 true。
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        ///     Profile id associated with the last ready notification, or <c>-1</c> when not ready.
        ///     与最近一次 ready 通知关联的档案 id；未 ready 时为 <c>-1</c>。
        /// </summary>
        public static int ReadyProfileId { get; private set; } = -1;

        /// <summary>
        ///     Derived lifecycle state from <see cref="IsReady" />.
        ///     从 <see cref="IsReady" /> 派生的生命周期状态。
        /// </summary>
        public static DataLifecycleState State =>
            IsReady ? DataLifecycleState.Ready : DataLifecycleState.WaitingForProfile;

        /// <summary>
        ///     Refreshes the current profile, ensures profile services, reloads data if paths changed, and raises
        ///     lifecycle events when appropriate.
        ///     刷新当前档案，确保档案服务可用，在路径变化时重新加载数据，并在适当时触发
        ///     生命周期事件。
        /// </summary>
        /// <param name="source">
        ///     Diagnostic label for log and event payloads.
        ///     用于日志和事件载荷的诊断标签。
        /// </param>
        public static void NotifyPotentialReady(string source)
        {
            try
            {
                ProfileManager.Instance.RefreshCurrentProfile();

                RitsuLibFramework.EnsureProfileServicesInitialized();

                var dataReloaded = ModDataStore.ReloadAllIfPathChanged();

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
        ///     Marks the given profile as invalid and raises
        ///     <see cref="STS2RitsuLib.Utils.Persistence.ProfileDataInvalidatedEvent" /> when it was the active ready
        ///     profile.
        ///     将给定档案标记为无效，并在它是活动 ready
        ///     档案时触发 <see cref="STS2RitsuLib.Utils.Persistence.ProfileDataInvalidatedEvent" />。
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
