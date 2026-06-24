using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Context passed when a synced right-click action reaches the action queue.
    ///     当同步右键动作到达动作队列时传递的上下文。
    /// </summary>
    /// <param name="Player">
    ///     Player that owns the queued right-click action.
    ///     拥有此队列化右键动作的玩家。
    /// </param>
    /// <param name="Model">
    ///     Model resolved at execution time.
    ///     执行时解析到的模型。
    /// </param>
    /// <param name="Trigger">
    ///     Input metadata.
    ///     输入元数据。
    /// </param>
    /// <param name="PlayerChoiceContext">
    ///     Queue-backed choice context for command APIs that require <c>PlayerChoiceContext</c>.
    ///     供需要 <c>PlayerChoiceContext</c> 的命令 API 使用的队列上下文。
    /// </param>
    /// <param name="Action">
    ///     Underlying vanilla queue action used for ordering.
    ///     用于队列排序的底层原版队列 action。
    /// </param>
    public readonly record struct ModRightClickExecutionContext(
        Player Player,
        AbstractModel Model,
        ModRightClickTrigger Trigger,
        GameActionPlayerChoiceContext? PlayerChoiceContext,
        GameAction? Action);
}
