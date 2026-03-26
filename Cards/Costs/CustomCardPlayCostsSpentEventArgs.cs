using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Raised once per card play after all custom <see cref="ICardCustomPlayCost.SpendAsync" /> calls finished.
    /// </summary>
    public sealed class CustomCardPlayCostsSpentEventArgs : EventArgs
    {
        public required CombatState CombatState { get; init; }
        public required CardModel Card { get; init; }
        public required Player Player { get; init; }

        /// <summary>Spend lines from costs implementing <see cref="ICardCustomPlayCostSpendRecordable" />.</summary>
        public required IReadOnlyList<CustomCardPlayCostSpendRecord> Records { get; init; }
    }
}
