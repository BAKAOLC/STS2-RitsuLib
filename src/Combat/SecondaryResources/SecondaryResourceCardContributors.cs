using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     <para xml:lang="en">Provides card-specific secondary-resource payment uses.</para>
    ///     <para xml:lang="zh-CN">提供卡牌专属的次级资源支付条款。</para>
    /// </summary>
    public interface ICardSecondaryResourceUseContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Returns the additional payment uses contributed to <paramref name="card" />.</para>
        ///     <para xml:lang="zh-CN">返回为 <paramref name="card" /> 提供的额外支付条款。</para>
        /// </summary>
        IEnumerable<SecondaryResourcePlayUse> GetSecondaryResourceUses(CardModel card)
        {
            return [];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Modifies secondary-resource costs for a specific card.</para>
    ///     <para xml:lang="zh-CN">修正特定卡牌的次级资源费用。</para>
    /// </summary>
    public interface ICardSecondaryResourceCostContributor
    {
        /// <summary>
        ///     <para xml:lang="en">Modifies a fixed cost before the combat-wide cost hooks run.</para>
        ///     <para xml:lang="zh-CN">在战斗范围的费用钩子运行前修正固定费用。</para>
        /// </summary>
        decimal ModifySecondaryResourceCost(SecondaryResourceCardCostContext context, decimal cost)
        {
            return cost;
        }
    }
}
