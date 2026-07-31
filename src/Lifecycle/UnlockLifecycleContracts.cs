using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">The player obtained a new Epoch unlock tier.</para>
    ///     <para xml:lang="zh-CN">玩家已获得新的纪元解锁层级。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="EpochId">
    ///     <para xml:lang="en">Epoch identifier.</para>
    ///     <para xml:lang="zh-CN">纪元标识符。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct EpochObtainedEvent(
        SaveManager SaveManager,
        string EpochId,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">An Epoch became visible in the UI, including through a debug reveal.</para>
    ///     <para xml:lang="zh-CN">一个纪元已在界面中显示，包括通过调试方式显示的情况。</para>
    /// </summary>
    /// <param name="SaveManager">
    ///     <para xml:lang="en">Save manager instance.</para>
    ///     <para xml:lang="zh-CN">存档管理器实例。</para>
    /// </param>
    /// <param name="EpochId">
    ///     <para xml:lang="en">Epoch identifier.</para>
    ///     <para xml:lang="zh-CN">纪元标识符。</para>
    /// </param>
    /// <param name="IsDebug">
    ///     <para xml:lang="en">True for debug-only reveal paths.</para>
    ///     <para xml:lang="zh-CN">调试专用揭示路径为 true。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
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
