using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Game over UI was created for a finished run.
    ///     已为结束的跑局创建游戏结束 UI。
    /// </summary>
    /// <param name="RunState">
    ///     Run state presented on the screen.
    ///     屏幕上呈现的跑局状态。
    /// </param>
    /// <param name="SerializableRun">
    ///     Serialized run payload.
    ///     已序列化的跑局载荷。
    /// </param>
    /// <param name="Screen">
    ///     Game over screen node.
    ///     游戏结束界面节点。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct GameOverScreenCreatedEvent(
        RunState RunState,
        SerializableRun SerializableRun,
        NGameOverScreen Screen,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
