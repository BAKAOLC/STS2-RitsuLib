namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     High-level state of whether profile-scoped mod data may be accessed safely.
    /// </summary>
    public enum DataLifecycleState
    {
        /// <summary>
        ///     No active profile context is ready for mod persistence yet.
        /// </summary>
        WaitingForProfile = 0,

        /// <summary>
        ///     Profile path is initialized and mod data operations are expected to be valid.
        /// </summary>
        Ready = 1,
    }

    /// <summary>
    ///     Published when profile-scoped mod data becomes readable/writable after initialization or reload.
    /// </summary>
    /// <param name="ProfileId">Active profile identifier.</param>
    /// <param name="Source">Subsystem that triggered the notification.</param>
    /// <param name="IsInitialReady">True on the first transition to ready.</param>
    /// <param name="IsProfileSwitch">True when the ready profile id changed from a previous ready state.</param>
    /// <param name="DataReloaded">True when mod data was reloaded due to a path or profile change.</param>
    /// <param name="OccurredAtUtc">Timestamp in UTC.</param>
    public readonly record struct ProfileDataReadyEvent(
        int ProfileId,
        string Source,
        bool IsInitialReady,
        bool IsProfileSwitch,
        bool DataReloaded,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Published when the active profile id changes while the framework considered data ready.
    /// </summary>
    /// <param name="OldProfileId">Previous profile identifier.</param>
    /// <param name="NewProfileId">New profile identifier.</param>
    /// <param name="Source">Subsystem that triggered the notification.</param>
    /// <param name="OccurredAtUtc">Timestamp in UTC.</param>
    public readonly record struct ProfileDataChangedEvent(
        int OldProfileId,
        int NewProfileId,
        string Source,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Published when the current ready profile context is no longer valid (e.g. profile deleted).
    /// </summary>
    /// <param name="ProfileId">Profile that was invalidated.</param>
    /// <param name="Reason">Short diagnostic label.</param>
    /// <param name="OccurredAtUtc">Timestamp in UTC.</param>
    public readonly record struct ProfileDataInvalidatedEvent(
        int ProfileId,
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
