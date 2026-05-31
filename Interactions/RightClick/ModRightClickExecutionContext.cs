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
    public readonly record struct ModRightClickExecutionContext(
        Player Player,
        AbstractModel Model,
        ModRightClickTrigger Trigger,
        GameActionPlayerChoiceContext? PlayerChoiceContext,
        GenericHookGameAction? Action);
}
