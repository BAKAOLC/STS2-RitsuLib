using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Transforms
{
    /// <summary>
    ///     Describes one completed vanilla card transform operation.
    ///     描述一次已完成的原版卡牌转换操作。
    /// </summary>
    /// <param name="Original">
    ///     Card that was transformed away.
    ///     被转换掉的卡牌。
    /// </param>
    /// <param name="Replacement">
    ///     Card that replaced <paramref name="Original" /> after vanilla modifiers.
    ///     经过原版 modifier 后替换 <paramref name="Original" /> 的卡牌。
    /// </param>
    public readonly record struct ModCardTransformContext(CardModel Original, CardModel Replacement);
}
