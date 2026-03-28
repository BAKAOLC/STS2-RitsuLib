using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Player obtained a new epoch (unlock tier).
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="EpochId">Epoch identifier.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct EpochObtainedEvent(
        SaveManager SaveManager,
        string EpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Epoch became visible in UI (may include debug epochs).
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="EpochId">Epoch identifier.</param>
    /// <param name="IsDebug">True for debug-only reveal paths.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct EpochRevealedEvent(
        SaveManager SaveManager,
        string EpochId,
        bool IsDebug,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Unlock counter advanced (e.g. after a run).
    /// </summary>
    /// <param name="SaveManager">Save manager instance.</param>
    /// <param name="TotalUnlocks">New total unlock count.</param>
    /// <param name="PendingEpochId">Next epoch id when queued.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct UnlockIncrementedEvent(
        SaveManager SaveManager,
        int TotalUnlocks,
        string? PendingEpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
