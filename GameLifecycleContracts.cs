using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Raised before essential (blocking) initialization work begins.
    ///     在必要（阻塞）初始化工作开始前触发。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct EssentialInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised after essential initialization completed; replayed to new subscribers.
    ///     在必要初始化完成后触发；会向新订阅者重放。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct EssentialInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised before deferred initialization starts.
    ///     在延迟初始化开始前触发。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct DeferredInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised after deferred initialization finished; replayed to new subscribers.
    ///     在延迟初始化完成后触发；会向新订阅者重放。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct DeferredInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Content registration phase has closed (no further registrations expected).
    ///     内容注册阶段已关闭（预期不会再有后续注册）。
    /// </summary>
    /// <param name="Reason">
    ///     Human-readable or diagnostic reason token.
    ///     人类可读或用于诊断的原因标记。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ContentRegistrationClosedEvent(
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Model registry is about to be populated.
    ///     模型注册表即将开始填充。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelRegistryInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model registry finished registering types; includes count for diagnostics.
    ///     模型注册表已完成类型注册；包含用于诊断的数量。
    /// </summary>
    /// <param name="RegisteredModelTypeCount">
    ///     Number of model types registered.
    ///     已注册的模型类型数量。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelRegistryInitializedEvent(
        int RegisteredModelTypeCount,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Model id assignment phase starting.
    ///     模型 ID 分配阶段开始。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelIdsInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model ids have been assigned; replayed to new subscribers.
    ///     模型 ID 已分配；会向新订阅者重放。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelIdsInitializedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Heavy model preloading is starting.
    ///     重型模型预加载即将开始。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelPreloadingStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model preloading finished; replayed to new subscribers.
    ///     模型预加载已完成；会向新订阅者重放。
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct ModelPreloadingCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Game node entered the scene tree.
    ///     游戏节点已进入场景树。
    /// </summary>
    /// <param name="Game">
    ///     Root game node instance.
    ///     根游戏节点实例。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct GameTreeEnteredEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Game is ready for play logic; replayed to new subscribers.
    ///     游戏已准备好运行玩法逻辑；会向新订阅者重放。
    /// </summary>
    /// <param name="Game">
    ///     Root game node instance.
    ///     根游戏节点实例。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct GameReadyEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     A new run has started.
    ///     新跑局已开始。
    /// </summary>
    /// <param name="RunState">
    ///     Active run state.
    ///     当前活动跑局状态。
    /// </param>
    /// <param name="IsMultiplayer">
    ///     Whether the run is multiplayer.
    ///     跑局是否为多人模式。
    /// </param>
    /// <param name="IsDaily">
    ///     Whether the run is a daily challenge.
    ///     跑局是否为每日挑战。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RunStartedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     An existing run was loaded from save.
    ///     已从存档加载既有跑局。
    /// </summary>
    /// <param name="RunState">
    ///     Active run state after load.
    ///     加载后的当前活动跑局状态。
    /// </param>
    /// <param name="IsMultiplayer">
    ///     Whether the run is multiplayer.
    ///     跑局是否为多人模式。
    /// </param>
    /// <param name="IsDaily">
    ///     Whether the run is a daily challenge.
    ///     跑局是否为每日挑战。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RunLoadedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Run finished (victory, defeat, or abandon).
    ///     跑局已结束（胜利、失败或放弃）。
    /// </summary>
    /// <param name="Run">
    ///     Serializable snapshot of the ended run.
    ///     已结束跑局的可序列化快照。
    /// </param>
    /// <param name="IsVictory">
    ///     True if the player won.
    ///     如果玩家获胜则为 true。
    /// </param>
    /// <param name="IsAbandoned">
    ///     True if the run was abandoned.
    ///     如果跑局被放弃则为 true。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RunEndedEvent(
        SerializableRun Run,
        bool IsVictory,
        bool IsAbandoned,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
