using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">A run is entering a room, before the room's entry logic completes.</para>
    ///     <para xml:lang="zh-CN">一局游戏正在进入一个房间，此时房间的进入逻辑尚未完成。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="Room">
    ///     <para xml:lang="en">Target room.</para>
    ///     <para xml:lang="zh-CN">目标房间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RoomEnteringEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A room's entry logic has completed.</para>
    ///     <para xml:lang="zh-CN">一个房间的进入逻辑已完成。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="Room">
    ///     <para xml:lang="en">Entered room.</para>
    ///     <para xml:lang="zh-CN">已进入的房间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RoomEnteredEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">The run has left a room.</para>
    ///     <para xml:lang="zh-CN">一局游戏已离开一个房间。</para>
    /// </summary>
    /// <param name="RunManager">
    ///     <para xml:lang="en">Run manager driving progression.</para>
    ///     <para xml:lang="zh-CN">驱动流程推进的游戏流程管理器。</para>
    /// </param>
    /// <param name="Room">
    ///     <para xml:lang="en">Room that was exited.</para>
    ///     <para xml:lang="zh-CN">已离开的房间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RoomExitedEvent(
        RunManager RunManager,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A transition to another Act is starting.</para>
    ///     <para xml:lang="zh-CN">向另一章节的过渡即将开始。</para>
    /// </summary>
    /// <param name="RunManager">
    ///     <para xml:lang="en">Run manager.</para>
    ///     <para xml:lang="zh-CN">游戏流程管理器。</para>
    /// </param>
    /// <param name="TargetActIndex">
    ///     <para xml:lang="en">Destination act index.</para>
    ///     <para xml:lang="zh-CN">目标章节索引。</para>
    /// </param>
    /// <param name="DoTransition">
    ///     <para xml:lang="en">Whether a visual transition will run.</para>
    ///     <para xml:lang="zh-CN">是否会播放视觉过渡。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ActEnteringEvent(
        RunManager RunManager,
        int TargetActIndex,
        bool DoTransition,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A transition to another Act has completed.</para>
    ///     <para xml:lang="zh-CN">向另一章节的过渡已完成。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CurrentActIndex">
    ///     <para xml:lang="en">Act index after the transition.</para>
    ///     <para xml:lang="zh-CN">过渡后的章节索引。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct ActEnteredEvent(
        IRunState RunState,
        int CurrentActIndex,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">The run is continuing from the rewards flow, such as when leaving the Rewards screen.</para>
    ///     <para xml:lang="zh-CN">一局游戏正在从奖励流程继续，例如离开奖励界面时。</para>
    /// </summary>
    /// <param name="RunManager">
    ///     <para xml:lang="en">Run manager.</para>
    ///     <para xml:lang="zh-CN">游戏流程管理器。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RewardsScreenContinuingEvent(
        RunManager RunManager,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
