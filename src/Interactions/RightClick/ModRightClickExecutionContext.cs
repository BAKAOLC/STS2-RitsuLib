using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a synchronized right-click action when it reaches the action queue.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述同步的右键操作进入行动队列时的执行状态。</para>
    /// </summary>
    /// <param name="Player">
    ///     <para xml:lang="en">The player who owns the queued right-click action.</para>
    ///     <para xml:lang="zh-CN">拥有队列中右键操作的玩家。</para>
    /// </param>
    /// <param name="Model">
    ///     <para xml:lang="en">The model resolved at execution time.</para>
    ///     <para xml:lang="zh-CN">执行时解析出的模型。</para>
    /// </param>
    /// <param name="Trigger">
    ///     <para xml:lang="en">Metadata about the input that triggered the action.</para>
    ///     <para xml:lang="zh-CN">触发该操作的输入元数据。</para>
    /// </param>
    /// <param name="PlayerChoiceContext">
    ///     <para xml:lang="en">
    ///         The queue-backed choice context available to command APIs that require <c>PlayerChoiceContext</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         由队列提供的选择上下文，可供需要 <c>PlayerChoiceContext</c> 的命令 API 使用。
    ///     </para>
    /// </param>
    /// <param name="Action">
    ///     <para xml:lang="en">The underlying base-game action used for queue ordering.</para>
    ///     <para xml:lang="zh-CN">用于队列排序的底层原版行动。</para>
    /// </param>
    public readonly record struct ModRightClickExecutionContext(
        Player Player,
        AbstractModel Model,
        ModRightClickTrigger Trigger,
        GameActionPlayerChoiceContext? PlayerChoiceContext,
        GameAction? Action);
}
