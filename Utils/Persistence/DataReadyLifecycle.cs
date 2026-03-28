using STS2RitsuLib.Data;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Profile data lifecycle hub:
    ///     - ProfileDataReady: profile data is safe to read/write
    ///     - ProfileDataChanged: profile switched after being ready
    ///     - ProfileDataInvalidated: current ready profile became invalid
    /// </summary>
    public static class DataReadyLifecycle
    {
        private static readonly Lock SyncRoot = new();

        private static ProfileDataReadyEvent? _lastReadyEvent;

        /// <summary>
        ///     True when profile path initialization completed and data is considered safe to use.
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        ///     Profile id associated with the last ready notification, or <c>-1</c> when not ready.
        /// </summary>
        public static int ReadyProfileId { get; private set; } = -1;

        /// <summary>
        ///     Derived lifecycle state from <see cref="IsReady" />.
        /// </summary>
        public static DataLifecycleState State =>
            IsReady ? DataLifecycleState.Ready : DataLifecycleState.WaitingForProfile;

        /// <summary>
        ///     Refreshes the current profile, ensures profile services, reloads data if paths changed, and raises
        ///     lifecycle events when appropriate.
        /// </summary>
        /// <param name="source">Diagnostic label for log and event payloads.</param>
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
