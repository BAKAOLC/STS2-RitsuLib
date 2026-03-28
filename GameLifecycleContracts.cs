using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Raised before essential (blocking) initialization work begins.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct EssentialInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised after essential initialization completed; replayed to new subscribers.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct EssentialInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised before deferred initialization starts.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct DeferredInitializationStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Raised after deferred initialization finished; replayed to new subscribers.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct DeferredInitializationCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Content registration phase has closed (no further registrations expected).
    /// </summary>
    /// <param name="Reason">Human-readable or diagnostic reason token.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ContentRegistrationClosedEvent(
        string Reason,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Model registry is about to be populated.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelRegistryInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model registry finished registering types; includes count for diagnostics.
    /// </summary>
    /// <param name="RegisteredModelTypeCount">Number of model types registered.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelRegistryInitializedEvent(
        int RegisteredModelTypeCount,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Model id assignment phase starting.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelIdsInitializingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model ids have been assigned; replayed to new subscribers.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelIdsInitializedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Heavy model preloading is starting.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelPreloadingStartingEvent(
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Model preloading finished; replayed to new subscribers.
    /// </summary>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ModelPreloadingCompletedEvent(
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Game node entered the scene tree.
    /// </summary>
    /// <param name="Game">Root game node instance.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct GameTreeEnteredEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Game is ready for play logic; replayed to new subscribers.
    /// </summary>
    /// <param name="Game">Root game node instance.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct GameReadyEvent(
        NGame Game,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     A new run has started.
    /// </summary>
    /// <param name="RunState">Active run state.</param>
    /// <param name="IsMultiplayer">Whether the run is multiplayer.</param>
    /// <param name="IsDaily">Whether the run is a daily challenge.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RunStartedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     An existing run was loaded from save.
    /// </summary>
    /// <param name="RunState">Active run state after load.</param>
    /// <param name="IsMultiplayer">Whether the run is multiplayer.</param>
    /// <param name="IsDaily">Whether the run is a daily challenge.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RunLoadedEvent(
        RunState RunState,
        bool IsMultiplayer,
        bool IsDaily,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Run finished (victory, defeat, or abandon).
    /// </summary>
    /// <param name="Run">Serializable snapshot of the ended run.</param>
    /// <param name="IsVictory">True if the player won.</param>
    /// <param name="IsAbandoned">True if the run was abandoned.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RunEndedEvent(
        SerializableRun Run,
        bool IsVictory,
        bool IsAbandoned,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
