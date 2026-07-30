using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Transforms
{
    /// <summary>
    ///     <para xml:lang="en">Describes one completed base-game card transformation.</para>
    ///     <para xml:lang="zh-CN">描述一次已完成的游戏本体卡牌转化。</para>
    /// </summary>
    /// <param name="Original">
    ///     <para xml:lang="en">Card that was transformed.</para>
    ///     <para xml:lang="zh-CN">被转化的卡牌。</para>
    /// </param>
    /// <param name="Replacement">
    ///     <para xml:lang="en">Card that replaced <paramref name="Original" /> after base-game modifiers.</para>
    ///     <para xml:lang="zh-CN">经过游戏本体修正后替换 <paramref name="Original" /> 的卡牌。</para>
    /// </param>
    /// <param name="OriginalPile">
    ///     <para xml:lang="en">Pile that contained <paramref name="Original" /> before the transformation.</para>
    ///     <para xml:lang="zh-CN">转化前包含 <paramref name="Original" /> 的牌堆。</para>
    /// </param>
    /// <param name="OriginalPileIndex">
    ///     <para xml:lang="en">Index of <paramref name="Original" /> in <paramref name="OriginalPile" /> before the transformation.</para>
    ///     <para xml:lang="zh-CN">转化前 <paramref name="Original" /> 在 <paramref name="OriginalPile" /> 中的索引。</para>
    /// </param>
    public readonly record struct ModCardTransformContext(
        CardModel Original,
        CardModel Replacement,
        CardPile OriginalPile,
        int OriginalPileIndex);
}
