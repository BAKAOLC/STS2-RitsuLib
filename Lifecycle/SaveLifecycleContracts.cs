using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Active profile id is known; replayed to new subscribers.
    ///     已知当前活动档案 ID；会向新订阅者重放。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="ProfileId">
    ///     Current profile id.
    ///     当前档案 ID。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProfileIdInitializedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile switch requested.
    ///     已请求切换档案。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="PreviousProfileId">
    ///     Prior profile id, if any.
    ///     之前的档案 ID（如果存在）。
    /// </param>
    /// <param name="NextProfileId">
    ///     Target profile id.
    ///     目标档案 ID。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProfileSwitchingEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int NextProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile switch completed; replayed to new subscribers.
    ///     档案切换已完成；会向新订阅者重放。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="PreviousProfileId">
    ///     Prior profile id, if any.
    ///     之前的档案 ID（如果存在）。
    /// </param>
    /// <param name="CurrentProfileId">
    ///     New active profile id.
    ///     新的当前活动档案 ID。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProfileSwitchedEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int CurrentProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Run save is about to be written.
    ///     跑局存档即将写入。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="PreFinishedRoom">
    ///     Room snapshot before completion, when applicable.
    ///     适用时为完成前的房间快照。
    /// </param>
    /// <param name="SaveProgress">
    ///     Whether progress should be persisted.
    ///     是否应持久化进度。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RunSavingEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Run save completed.
    ///     跑局存档已完成。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="PreFinishedRoom">
    ///     Room snapshot before completion, when applicable.
    ///     适用时为完成前的房间快照。
    /// </param>
    /// <param name="SaveProgress">
    ///     Whether progress was persisted.
    ///     是否已持久化进度。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RunSavedEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Meta/progress save starting.
    ///     元数据/进度存档即将开始。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="ProfileId">
    ///     Profile being saved, when scoped.
    ///     有作用域时为正在保存的档案。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProgressSavingEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Meta/progress save finished.
    ///     元数据/进度存档已完成。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="ProfileId">
    ///     Profile that was saved, when scoped.
    ///     有作用域时为已保存的档案。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProgressSavedEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile deletion requested.
    ///     已请求删除档案。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="ProfileId">
    ///     Profile slated for deletion.
    ///     计划删除的档案。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProfileDeletingEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile deletion completed.
    ///     档案删除已完成。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="ProfileId">
    ///     Profile that was deleted.
    ///     已删除的档案。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ProfileDeletedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
