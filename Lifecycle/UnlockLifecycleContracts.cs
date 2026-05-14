using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Player obtained a new epoch (unlock tier).
    ///     玩家取得了新的 epoch（解锁层级）。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="EpochId">
    ///     Epoch identifier.
    ///     epoch 标识符。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct EpochObtainedEvent(
        SaveManager SaveManager,
        string EpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Epoch became visible in UI (may include debug epochs).
    ///     epoch 在 UI 中变为可见（可能包含调试 epoch）。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="EpochId">
    ///     Epoch identifier.
    ///     epoch 标识符。
    /// </param>
    /// <param name="IsDebug">
    ///     True for debug-only reveal paths.
    ///     调试专用揭示路径为 true。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct EpochRevealedEvent(
        SaveManager SaveManager,
        string EpochId,
        bool IsDebug,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Unlock counter advanced (e.g. after a run).
    ///     解锁计数已推进（例如跑局结束后）。
    /// </summary>
    /// <param name="SaveManager">
    ///     Save manager instance.
    ///     存档管理器实例。
    /// </param>
    /// <param name="TotalUnlocks">
    ///     New total unlock count.
    ///     新的总解锁数量。
    /// </param>
    /// <param name="PendingEpochId">
    ///     Next epoch id when queued.
    ///     已排队时的下一个 epoch ID。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct UnlockIncrementedEvent(
        SaveManager SaveManager,
        int TotalUnlocks,
        string? PendingEpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
