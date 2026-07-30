namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Indicates whether profile-scoped mod data is ready for safe access.</para>
    ///     <para xml:lang="zh-CN">指示档案作用域的模组数据是否已可安全访问。</para>
    /// </summary>
    public enum DataLifecycleState
    {
        /// <summary>
        ///     <para xml:lang="en">No active profile context is ready for mod-data persistence.</para>
        ///     <para xml:lang="zh-CN">尚无可用于持久化模组数据的活动档案上下文。</para>
        /// </summary>
        WaitingForProfile = 0,

        /// <summary>
        ///     <para xml:lang="en">The profile path is initialized and mod-data operations are expected to be valid.</para>
        ///     <para xml:lang="zh-CN">档案路径已初始化，模组数据操作预期有效。</para>
        /// </summary>
        Ready = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">Published when profile-scoped mod data becomes readable and writable after initialization or reload.</para>
    ///     <para xml:lang="zh-CN">初始化或重新加载后，档案作用域的模组数据变为可读可写时发布。</para>
    /// </summary>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Active profile identifier.</para>
    ///     <para xml:lang="zh-CN">活动档案标识符。</para>
    /// </param>
    /// <param name="Source">
    ///     <para xml:lang="en">Subsystem that triggered the notification.</para>
    ///     <para xml:lang="zh-CN">触发通知的子系统。</para>
    /// </param>
    /// <param name="IsInitialReady">
    ///     <para xml:lang="en"><see langword="true" /> when this notification transitions the lifecycle from not ready to ready.</para>
    ///     <para xml:lang="zh-CN">此次通知使生命周期从未就绪转换为就绪时为 <see langword="true" />。</para>
    /// </param>
    /// <param name="IsProfileSwitch">
    ///     <para xml:lang="en"><see langword="true" /> when the ready profile ID differs from the preceding ready state.</para>
    ///     <para xml:lang="zh-CN">就绪档案 ID 与上一个就绪状态不同时为 <see langword="true" />。</para>
    /// </param>
    /// <param name="DataReloaded">
    ///     <para xml:lang="en"><see langword="true" /> when mod data was reloaded because its path or profile changed.</para>
    ///     <para xml:lang="zh-CN">模组数据因路径或档案变化而重新加载时为 <see langword="true" />。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Timestamp in UTC.</para>
    ///     <para xml:lang="zh-CN">UTC 时间戳。</para>
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
    ///     <para xml:lang="en">Published when the active profile ID changes while RitsuLib considers profile data ready.</para>
    ///     <para xml:lang="zh-CN">RitsuLib 认为档案数据已就绪时，活动档案 ID 发生变化会发布此事件。</para>
    /// </summary>
    /// <param name="OldProfileId">
    ///     <para xml:lang="en">Previous profile identifier.</para>
    ///     <para xml:lang="zh-CN">上一个档案标识符。</para>
    /// </param>
    /// <param name="NewProfileId">
    ///     <para xml:lang="en">New profile identifier.</para>
    ///     <para xml:lang="zh-CN">新档案标识符。</para>
    /// </param>
    /// <param name="Source">
    ///     <para xml:lang="en">Subsystem that triggered the notification.</para>
    ///     <para xml:lang="zh-CN">触发通知的子系统。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Timestamp in UTC.</para>
    ///     <para xml:lang="zh-CN">UTC 时间戳。</para>
    /// </param>
    public readonly record struct ProfileDataChangedEvent(
        int OldProfileId,
        int NewProfileId,
        string Source,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Published when the currently ready profile context becomes invalid, such as after its profile is deleted.</para>
    ///     <para xml:lang="zh-CN">当前已就绪的档案上下文失效时发布，例如对应档案被删除后。</para>
    /// </summary>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Profile that was invalidated.</para>
    ///     <para xml:lang="zh-CN">已失效的档案。</para>
    /// </param>
    /// <param name="Reason">
    ///     <para xml:lang="en">Short diagnostic label.</para>
    ///     <para xml:lang="zh-CN">简短诊断标签。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Timestamp in UTC.</para>
    ///     <para xml:lang="zh-CN">UTC 时间戳。</para>
    /// </param>
    public readonly record struct ProfileDataInvalidatedEvent(
        int ProfileId,
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
