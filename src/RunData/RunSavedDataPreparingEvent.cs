using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Notifies mods before run saved data is exported for authoritative new-run initialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在为权威端的新一局游戏初始化导出局内保存数据前通知模组。
    ///     </para>
    /// </summary>
    public sealed record RunSavedDataPreparingEvent(
        RunState RunState,
        bool IsMultiplayer,
        DateTimeOffset OccurredAtUtc) : IFrameworkLifecycleEvent;
}
