using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Optional card capability that contributes local secondary-resource play uses.
    ///     可选卡牌能力：贡献卡牌本地次级资源出牌条款。
    /// </summary>
    public interface ICardSecondaryResourceUseContributor
    {
        /// <summary>
        ///     Returns additional local secondary-resource uses for <paramref name="card" />.
        ///     返回 <paramref name="card" /> 的额外本地次级资源条款。
        /// </summary>
        IEnumerable<SecondaryResourcePlayUse> GetSecondaryResourceUses(CardModel card)
        {
            return [];
        }
    }

    /// <summary>
    ///     Optional card capability that contributes local secondary-resource cost modifications.
    ///     可选卡牌能力：贡献卡牌本地次级资源费用修正。
    /// </summary>
    public interface ICardSecondaryResourceCostContributor
    {
        /// <summary>
        ///     Modifies a local fixed secondary-resource cost before combat/global cost hooks run.
        ///     在战斗/global 费用 hook 运行前修正本地固定次级资源费用。
        /// </summary>
        decimal ModifySecondaryResourceCost(SecondaryResourceCardCostContext context, decimal cost)
        {
            return cost;
        }
    }
}
