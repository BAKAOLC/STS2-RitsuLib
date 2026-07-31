using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">The active profile ID has been initialized; this event is replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">当前活动档案 ID 已初始化；此事件会向新订阅者重放。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Current profile id.</para>
    ///     <para xml:lang="zh-CN">当前档案 ID。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProfileIdInitializedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A profile switch is about to begin.</para>
    ///     <para xml:lang="zh-CN">档案切换即将开始。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="PreviousProfileId">
    ///     <para xml:lang="en">Prior profile id, if any.</para>
    ///     <para xml:lang="zh-CN">之前的档案 ID（如果存在）。</para>
    /// </param>
    /// <param name="NextProfileId">
    ///     <para xml:lang="en">Target profile id.</para>
    ///     <para xml:lang="zh-CN">目标档案 ID。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProfileSwitchingEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int NextProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A profile switch has completed; this event is replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">档案切换已完成；此事件会向新订阅者重放。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="PreviousProfileId">
    ///     <para xml:lang="en">Prior profile id, if any.</para>
    ///     <para xml:lang="zh-CN">之前的档案 ID（如果存在）。</para>
    /// </param>
    /// <param name="CurrentProfileId">
    ///     <para xml:lang="en">New active profile id.</para>
    ///     <para xml:lang="zh-CN">新的当前活动档案 ID。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProfileSwitchedEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int CurrentProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A run save is about to be written.</para>
    ///     <para xml:lang="zh-CN">局内存档即将写入。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="PreFinishedRoom">
    ///     <para xml:lang="en">Room snapshot before completion, when applicable.</para>
    ///     <para xml:lang="zh-CN">适用时为完成前的房间快照。</para>
    /// </param>
    /// <param name="SaveProgress">
    ///     <para xml:lang="en">Whether progress should be persisted.</para>
    ///     <para xml:lang="zh-CN">是否应持久化进度。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RunSavingEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A run save has been written.</para>
    ///     <para xml:lang="zh-CN">局内存档已写入。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="PreFinishedRoom">
    ///     <para xml:lang="en">Room snapshot before completion, when applicable.</para>
    ///     <para xml:lang="zh-CN">适用时为完成前的房间快照。</para>
    /// </param>
    /// <param name="SaveProgress">
    ///     <para xml:lang="en">Whether progress was persisted.</para>
    ///     <para xml:lang="zh-CN">是否已持久化进度。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RunSavedEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A progress save is about to begin.</para>
    ///     <para xml:lang="zh-CN">进度保存即将开始。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Profile being saved, when scoped.</para>
    ///     <para xml:lang="zh-CN">有作用域时为正在保存的档案。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProgressSavingEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A progress save has completed.</para>
    ///     <para xml:lang="zh-CN">进度保存已完成。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Profile that was saved, when scoped.</para>
    ///     <para xml:lang="zh-CN">有作用域时为已保存的档案。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProgressSavedEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A profile is about to be deleted.</para>
    ///     <para xml:lang="zh-CN">一个档案即将被删除。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Profile slated for deletion.</para>
    ///     <para xml:lang="zh-CN">计划删除的档案。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProfileDeletingEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A profile has been deleted.</para>
    ///     <para xml:lang="zh-CN">一个档案已被删除。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="ProfileId">
    ///     <para xml:lang="en">Profile that was deleted.</para>
    ///     <para xml:lang="zh-CN">已删除的档案。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ProfileDeletedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
