using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Run is transitioning into a room (before full enter logic completes).
    ///     跑局正在进入房间（完整进入逻辑完成前）。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="Room">
    ///     Target room.
    ///     目标房间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RoomEnteringEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Room enter logic has completed.
    ///     房间进入逻辑已完成。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="Room">
    ///     Entered room.
    ///     已进入的房间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RoomEnteredEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player left a room.
    ///     玩家离开了房间。
    /// </summary>
    /// <param name="RunManager">
    ///     Run manager driving progression.
    ///     驱动流程推进的跑局管理器。
    /// </param>
    /// <param name="Room">
    ///     Room that was exited.
    ///     已离开的房间。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RoomExitedEvent(
        RunManager RunManager,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Act transition is starting.
    ///     章节过渡即将开始。
    /// </summary>
    /// <param name="RunManager">
    ///     Run manager.
    ///     跑局管理器。
    /// </param>
    /// <param name="TargetActIndex">
    ///     Destination act index.
    ///     目标章节索引。
    /// </param>
    /// <param name="DoTransition">
    ///     Whether a visual transition will run.
    ///     是否会播放视觉过渡。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ActEnteringEvent(
        RunManager RunManager,
        int TargetActIndex,
        bool DoTransition,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Act transition completed.
    ///     章节过渡已完成。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CurrentActIndex">
    ///     Act index after the transition.
    ///     过渡后的章节索引。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ActEnteredEvent(
        IRunState RunState,
        int CurrentActIndex,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Rewards flow is continuing (e.g. leaving rewards screen).
    ///     奖励流程正在继续（例如离开奖励界面）。
    /// </summary>
    /// <param name="RunManager">
    ///     Run manager.
    ///     跑局管理器。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RewardsScreenContinuingEvent(
        RunManager RunManager,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
