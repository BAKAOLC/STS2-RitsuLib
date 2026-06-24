using MegaCrit.Sts2.Core.Entities.Cards;
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
    /// <param name="OriginalPile">
    ///     Pile that contained <paramref name="Original" /> before the transform.
    ///     转换前包含 <paramref name="Original" /> 的牌堆。
    /// </param>
    /// <param name="OriginalPileIndex">
    ///     Index of <paramref name="Original" /> in <paramref name="OriginalPile" /> before the transform.
    ///     转换前 <paramref name="Original" /> 在 <paramref name="OriginalPile" /> 中的位置。
    /// </param>
    public readonly record struct ModCardTransformContext(
        CardModel Original,
        CardModel Replacement,
        CardPile OriginalPile,
        int OriginalPileIndex);
}
