using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Describes one completed vanilla model clone operation.
    ///     描述一次已完成的原版模型复制操作。
    /// </summary>
    /// <param name="Prototype">
    ///     The model instance that was cloned.
    ///     被复制的原型模型实例。
    /// </param>
    /// <param name="ClonedModel">
    ///     The cloned model instance.
    ///     复制出的模型实例。
    /// </param>
    public readonly record struct ModelCloneContext(AbstractModel Prototype, AbstractModel ClonedModel);
}
