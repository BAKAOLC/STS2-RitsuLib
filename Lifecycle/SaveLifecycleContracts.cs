using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Active profile id is known; replayed to new subscribers.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="ProfileId">Current profile id.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileIdInitializedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile switch requested.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="PreviousProfileId">Prior profile id, if any.</param>
    /// <param name="NextProfileId">Target profile id.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileSwitchingEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int NextProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile switch completed; replayed to new subscribers.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="PreviousProfileId">Prior profile id, if any.</param>
    /// <param name="CurrentProfileId">New active profile id.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileSwitchedEvent(
        SaveManager SaveManager,
        int? PreviousProfileId,
        int CurrentProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IReplayableFrameworkLifecycleEvent;

    /// <summary>
    ///     Run save is about to be written.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="PreFinishedRoom">Room snapshot before completion, when applicable.</param>
    /// <param name="SaveProgress">Whether progress should be persisted.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RunSavingEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Run save completed.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="PreFinishedRoom">Room snapshot before completion, when applicable.</param>
    /// <param name="SaveProgress">Whether progress was persisted.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RunSavedEvent(
        SaveManager SaveManager,
        AbstractRoom? PreFinishedRoom,
        bool SaveProgress,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Meta/progress save starting.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="ProfileId">Profile being saved, when scoped.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProgressSavingEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Meta/progress save finished.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="ProfileId">Profile that was saved, when scoped.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProgressSavedEvent(
        SaveManager SaveManager,
        int? ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile deletion requested.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="ProfileId">Profile slated for deletion.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileDeletingEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Profile deletion completed.
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="ProfileId">Profile that was deleted.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct ProfileDeletedEvent(
        SaveManager SaveManager,
        int ProfileId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
