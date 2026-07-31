using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">Raised before essential blocking initialization begins.</para>
    ///     <para xml:lang="zh-CN">在必要的阻塞初始化开始前引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct EssentialInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after essential initialization completes and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在必要初始化完成后引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct EssentialInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised before deferred initialization starts.</para>
    ///     <para xml:lang="zh-CN">在延迟初始化开始前引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct DeferredInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after deferred initialization finishes and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在延迟初始化完成后引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct DeferredInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised when content registration closes and no further registrations are expected.</para>
    ///     <para xml:lang="zh-CN">在内容注册关闭且预期不再接受后续注册时引发。</para>
    /// </summary>
    /// <param name="Reason">
    ///     <para xml:lang="en">Human-readable or diagnostic reason token.</para>
    ///     <para xml:lang="zh-CN">供人阅读或用于诊断的原因标记。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ContentRegistrationClosedEvent(
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised before the model registry is populated.</para>
    ///     <para xml:lang="zh-CN">在填充模型注册表前引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelRegistryInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after model-type registration completes, including its diagnostic count.</para>
    ///     <para xml:lang="zh-CN">在模型类型注册完成后引发，并包含用于诊断的数量。</para>
    /// </summary>
    /// <param name="RegisteredModelTypeCount">
    ///     <para xml:lang="en">Number of registered model types.</para>
    ///     <para xml:lang="zh-CN">已注册的模型类型数量。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelRegistryInitializedEvent(
        int RegisteredModelTypeCount,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised when the model-ID assignment phase starts.</para>
    ///     <para xml:lang="zh-CN">在模型 ID 分配阶段开始时引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelIdsInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after model IDs are assigned and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在模型 ID 分配后引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelIdsInitializedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised before heavy model preloading starts.</para>
    ///     <para xml:lang="zh-CN">在耗时的模型预加载开始前引发。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelPreloadingStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after model preloading finishes and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在模型预加载完成后引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct ModelPreloadingCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after the root game node enters the scene tree.</para>
    ///     <para xml:lang="zh-CN">在根游戏节点进入场景树后引发。</para>
    /// </summary>
    /// <param name="Game">
    ///     <para xml:lang="en">Root game node instance.</para>
    ///     <para xml:lang="zh-CN">根游戏节点实例。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct GameTreeEnteredEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised when the game is ready for gameplay logic and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在游戏准备好运行玩法逻辑时引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="Game">
    ///     <para xml:lang="en">Root game node instance.</para>
    ///     <para xml:lang="zh-CN">根游戏节点实例。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct GameReadyEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after the main-menu node finishes its ready callback and replayed to new subscribers.</para>
    ///     <para xml:lang="zh-CN">在主菜单节点完成其就绪回调后引发，并向新订阅者重放。</para>
    /// </summary>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct MainMenuReadyEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after startup telemetry facts are sampled once and ready to replay following consent.</para>
    ///     <para xml:lang="zh-CN">在启动遥测信息完成一次采样且可在获得授权后重放时引发。</para>
    /// </summary>
    /// <param name="SnapshotAtUtc">
    ///     <para xml:lang="en">Time when the persistent startup snapshot was captured.</para>
    ///     <para xml:lang="zh-CN">持久启动快照的采样时间。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct TelemetryStartupSnapshotReadyEvent(
        DateTimeOffset SnapshotAtUtc,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised when a new run starts.</para>
    ///     <para xml:lang="zh-CN">在新的一局游戏开始时引发。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Active run state.</para>
    ///     <para xml:lang="zh-CN">当前活动的一局游戏状态。</para>
    /// </param>
    /// <param name="IsMultiplayer">
    ///     <para xml:lang="en">Whether the run is multiplayer.</para>
    ///     <para xml:lang="zh-CN">跑局是否为多人模式。</para>
    /// </param>
    /// <param name="IsDaily">
    ///     <para xml:lang="en">Whether the run is a daily challenge.</para>
    ///     <para xml:lang="zh-CN">跑局是否为每日挑战。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct RunStartedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised after an existing run is loaded from a save.</para>
    ///     <para xml:lang="zh-CN">在从存档加载既有的一局游戏后引发。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Active run state after loading.</para>
    ///     <para xml:lang="zh-CN">加载后的当前活动一局游戏状态。</para>
    /// </param>
    /// <param name="IsMultiplayer">
    ///     <para xml:lang="en">Whether the run is multiplayer.</para>
    ///     <para xml:lang="zh-CN">跑局是否为多人模式。</para>
    /// </param>
    /// <param name="IsDaily">
    ///     <para xml:lang="en">Whether the run is a daily challenge.</para>
    ///     <para xml:lang="zh-CN">跑局是否为每日挑战。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct RunLoadedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">Raised when a run ends through victory, defeat, or abandonment.</para>
    ///     <para xml:lang="zh-CN">在一局游戏因胜利、失败或放弃而结束时引发。</para>
    /// </summary>
    /// <param name="Run">
    ///     <para xml:lang="en">Serializable snapshot of the ended run.</para>
    ///     <para xml:lang="zh-CN">已结束一局游戏的可序列化快照。</para>
    /// </param>
    /// <param name="IsVictory">
    ///     <para xml:lang="en"><see langword="true" /> if the player won.</para>
    ///     <para xml:lang="zh-CN">玩家获胜时为 <see langword="true" />。</para>
    /// </param>
    /// <param name="IsAbandoned">
    ///     <para xml:lang="en"><see langword="true" /> if the run was abandoned.</para>
    ///     <para xml:lang="zh-CN">跑局被放弃时为 <see langword="true" />。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">Time when the event was raised.</para>
    ///     <para xml:lang="zh-CN">引发事件的时间。</para>
    /// </param>
    public readonly record struct RunEndedEvent(
        SerializableRun Run,
        bool IsVictory,
        bool IsAbandoned,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
