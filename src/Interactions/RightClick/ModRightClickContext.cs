using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Describes a locally dispatched right-click request.</para>
    ///     <para xml:lang="zh-CN">描述一个在本地分发的右键请求。</para>
    /// </summary>
    /// <param name="Player">
    ///     <para xml:lang="en">The local player who initiated the request.</para>
    ///     <para xml:lang="zh-CN">发起请求的本地玩家。</para>
    /// </param>
    /// <param name="Model">
    ///     <para xml:lang="en">The model that was clicked.</para>
    ///     <para xml:lang="zh-CN">被点击的模型。</para>
    /// </param>
    /// <param name="Trigger">
    ///     <para xml:lang="en">Metadata about the input that triggered the request.</para>
    ///     <para xml:lang="zh-CN">触发该请求的输入元数据。</para>
    /// </param>
    public readonly record struct ModRightClickContext(
        Player Player,
        AbstractModel Model,
        ModRightClickTrigger Trigger);
}
