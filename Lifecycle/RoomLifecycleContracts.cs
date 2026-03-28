using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Run is transitioning into a room (before full enter logic completes).
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="Room">Target room.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RoomEnteringEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Room enter logic has completed.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="Room">Entered room.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RoomEnteredEvent(
        IRunState RunState,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player left a room.
    /// </summary>
    /// <param name="RunManager">Run manager driving progression.</param>
    /// <param name="Room">Room that was exited.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RoomExitedEvent(
        RunManager RunManager,
        AbstractRoom Room,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Act transition is starting.
    /// </summary>
    /// <param name="RunManager">Run manager.</param>
    /// <param name="TargetActIndex">Destination act index.</param>
    /// <param name="DoTransition">Whether a visual transition will run.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ActEnteringEvent(
        RunManager RunManager,
        int TargetActIndex,
        bool DoTransition,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Act transition completed.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CurrentActIndex">Act index after the transition.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ActEnteredEvent(
        IRunState RunState,
        int CurrentActIndex,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Rewards flow is continuing (e.g. leaving rewards screen).
    /// </summary>
    /// <param name="RunManager">Run manager.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RewardsScreenContinuingEvent(
        RunManager RunManager,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
