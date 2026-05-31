using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Local right-click dispatch context.
    ///     本地右键分发上下文。
    /// </summary>
    /// <param name="Player">
    ///     Local player that initiated the request.
    ///     发起请求的本地玩家。
    /// </param>
    /// <param name="Model">
    ///     Clicked model.
    ///     被点击的模型。
    /// </param>
    /// <param name="Trigger">
    ///     Input metadata.
    ///     输入元数据。
    /// </param>
    public readonly record struct ModRightClickContext(
        Player Player,
        AbstractModel Model,
        ModRightClickTrigger Trigger);
}
