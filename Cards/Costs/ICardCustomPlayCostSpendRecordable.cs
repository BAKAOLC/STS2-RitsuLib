using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Optional: after <see cref="ICardCustomPlayCost.SpendAsync" />, report what was spent for ledgers and events.
    /// </summary>
    public interface ICardCustomPlayCostSpendRecordable
    {
        /// <summary>Return null to omit this cost from the ledger and batch event for this play.</summary>
        CustomCardPlayCostSpendRecord? TryBuildSpendRecord(CardModel card, Player player);
    }
}
