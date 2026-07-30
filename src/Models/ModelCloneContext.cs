using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en">Describes one completed base-game model clone operation.</para>
    ///     <para xml:lang="zh-CN">描述一次已完成的游戏原版模型复制操作。</para>
    /// </summary>
    /// <param name="Prototype">
    ///     <para xml:lang="en">The model instance that was cloned.</para>
    ///     <para xml:lang="zh-CN">被复制的原型模型实例。</para>
    /// </param>
    /// <param name="ClonedModel">
    ///     <para xml:lang="en">The cloned model instance.</para>
    ///     <para xml:lang="zh-CN">复制出的模型实例。</para>
    /// </param>
    public readonly record struct ModelCloneContext(AbstractModel Prototype, AbstractModel ClonedModel);
}
