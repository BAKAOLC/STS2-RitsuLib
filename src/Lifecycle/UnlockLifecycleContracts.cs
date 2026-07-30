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
    ///     <para xml:lang="en">
    ///         Reports legacy unlock-counter advancement or, on game API 0.110.0 and later, the result of attempting to
    ///         grant the next score-based epoch.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         报告旧版解锁计数推进；从游戏 API 0.110.0 起，则报告尝试授予下一个基于分数的纪元的结果。
    ///     </para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">The save manager that performed the operation.</para>
    ///     <para xml:lang="zh-CN">执行该操作的存档管理器。</para>
    /// </param>
    /// <param name="TotalUnlocks">
    ///     <para xml:lang="en">The total unlock count after the operation.</para>
    ///     <para xml:lang="zh-CN">操作后的解锁总数。</para>
    /// </param>
    /// <param name="PendingEpochId">
    ///     <para xml:lang="en">
    ///         On game API 0.110.0 and later, the granted epoch ID, or <see langword="null" /> when none remains.
    ///         On earlier APIs, the next epoch ID returned by the legacy increment operation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从游戏 API 0.110.0 起，为本次授予的纪元 ID；没有剩余纪元时为 <see langword="null" />。
    ///         在更早的 API 中，为旧版计数推进操作返回的下一个纪元 ID。
    ///     </para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">The UTC time at which the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发时的 UTC 时间。</para>
    /// </param>
    public readonly record struct UnlockIncrementedEvent(
        SaveManager SaveManager,
        int TotalUnlocks,
        string? PendingEpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
