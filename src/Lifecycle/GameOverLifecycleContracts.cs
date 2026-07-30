using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">The Game Over screen was created for a finished run.</para>
    ///     <para xml:lang="zh-CN">已为结束的一局游戏创建游戏结束界面。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Run state presented on the screen.</para>
    ///     <para xml:lang="zh-CN">屏幕上呈现的局内状态。</para>
    /// </param>
    /// <param name="SerializableRun">
    ///     <para xml:lang="en">Serialized data for the finished run.</para>
    ///     <para xml:lang="zh-CN">已结束游戏的序列化数据。</para>
    /// </param>
    /// <param name="Screen">
    ///     <para xml:lang="en">Game over screen node.</para>
    ///     <para xml:lang="zh-CN">游戏结束界面节点。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct GameOverScreenCreatedEvent(
        RunState RunState,
        SerializableRun SerializableRun,
        NGameOverScreen Screen,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
