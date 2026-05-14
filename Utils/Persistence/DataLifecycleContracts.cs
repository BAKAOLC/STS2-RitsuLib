namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     High-level state of whether profile-scoped mod data may be accessed safely.
    ///     档案作用域 mod 数据是否可安全访问的高层状态。
    /// </summary>
    public enum DataLifecycleState
    {
        /// <summary>
        ///     No active profile context is ready for mod persistence yet.
        ///     尚无活动档案上下文可用于 mod 持久化。
        /// </summary>
        WaitingForProfile = 0,

        /// <summary>
        ///     Profile path is initialized and mod data operations are expected to be valid.
        ///     档案路径已初始化，mod 数据操作预期有效。
        /// </summary>
        Ready = 1,
    }

    /// <summary>
    ///     Published when profile-scoped mod data becomes readable/writable after initialization or reload.
    ///     初始化或重新加载后，当档案作用域 mod 数据变为可读 / 可写时发布。
    /// </summary>
    /// <param name="ProfileId">
    ///     Active profile identifier.
    ///     活动档案标识符。
    /// </param>
    /// <param name="Source">
    ///     Subsystem that triggered the notification.
    ///     触发通知的子系统。
    /// </param>
    /// <param name="IsInitialReady">
    ///     True on the first transition to ready.
    ///     首次转换为 ready 时为 true。
    /// </param>
    /// <param name="IsProfileSwitch">
    ///     True when the ready profile id changed from a previous ready state.
    ///     当 ready 档案 id 相比上一个 ready 状态发生变化时为 true。
    /// </param>
    /// <param name="DataReloaded">
    ///     True when mod data was reloaded due to a path or profile change.
    ///     当 mod 数据因路径或档案变化而重新加载时为 true。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     Timestamp in UTC.
    ///     UTC 时间戳。
    /// </param>
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
    ///     当框架认为数据已 ready 时活动档案 id 发生变化，会发布此事件。
    /// </summary>
    /// <param name="OldProfileId">
    ///     Previous profile identifier.
    ///     上一个档案标识符。
    /// </param>
    /// <param name="NewProfileId">
    ///     New profile identifier.
    ///     新档案标识符。
    /// </param>
    /// <param name="Source">
    ///     Subsystem that triggered the notification.
    ///     触发通知的子系统。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     Timestamp in UTC.
    ///     UTC 时间戳。
    /// </param>
    public readonly record struct ProfileDataChangedEvent(
        int OldProfileId,
        int NewProfileId,
        string Source,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Published when the current ready profile context is no longer valid (e.g. profile deleted).
    ///     当前 ready 档案上下文不再有效时发布（例如档案被删除）。
    /// </summary>
    /// <param name="ProfileId">
    ///     Profile that was invalidated.
    ///     被失效的档案。
    /// </param>
    /// <param name="Reason">
    ///     Short diagnostic label.
    ///     简短诊断标签。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     Timestamp in UTC.
    ///     UTC 时间戳。
    /// </param>
    public readonly record struct ProfileDataInvalidatedEvent(
        int ProfileId,
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
