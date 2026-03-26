using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Optional: expose planned custom spends before <see cref="ICardCustomPlayCost.SpendAsync" /> (hand UI, tooltips).
    /// </summary>
    public interface ICardCustomPlayCostPlannable
    {
        /// <summary>Returns false if this cost does not report a planned line for the given card/player.</summary>
        bool TryGetPlannedSpend(CardModel card, Player player, out CustomCardPlayCostSpendRecord record);
    }
}
