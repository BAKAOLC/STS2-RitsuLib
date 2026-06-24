using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Published before run-saved data is exported for authoritative new-run initialization.
    ///     在为权威新开局初始化导出跑局保存数据前发布。
    /// </summary>
    public sealed record RunSavedDataPreparingEvent(
        RunState RunState,
        bool IsMultiplayer,
        DateTimeOffset OccurredAtUtc) : IFrameworkLifecycleEvent;
}
