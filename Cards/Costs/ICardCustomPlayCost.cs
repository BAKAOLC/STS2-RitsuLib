using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Extra resources paid when playing a card, alongside vanilla energy and stars. RitsuLib patches invoke this on
    ///     <see cref="MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState.HasEnoughResourcesFor" /> and
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardModel.SpendResources" />.
    /// </summary>
    public interface ICardCustomPlayCost
    {
        /// <summary>Whether the player can afford this cost (used for CanPlay / resource checks).</summary>
        bool IsAffordable(CardModel card, Player player);

        /// <summary>Called after vanilla energy and stars are spent; perform the actual deduction or side effects.</summary>
        Task SpendAsync(CardModel card, Player player);
    }

    /// <summary>Delegate-based <see cref="ICardCustomPlayCost" /> for inline composition in contributors or card code.</summary>
    public sealed class DelegatingCardCustomPlayCost(
        Func<CardModel, Player, bool> isAffordable,
        Func<CardModel, Player, Task> spend,
        Func<CardModel, Player, CustomCardPlayCostSpendRecord?>? plannedSpend = null,
        Func<CardModel, Player, CustomCardPlayCostSpendRecord?>? spendRecord = null)
        : ICardCustomPlayCost, ICardCustomPlayCostPlannable, ICardCustomPlayCostSpendRecordable
    {
        private readonly Func<CardModel, Player, bool> _isAffordable =
            isAffordable ?? throw new ArgumentNullException(nameof(isAffordable));

        private readonly Func<CardModel, Player, CustomCardPlayCostSpendRecord?>? _plannedSpend = plannedSpend;

        private readonly Func<CardModel, Player, Task> _spend =
            spend ?? throw new ArgumentNullException(nameof(spend));

        private readonly Func<CardModel, Player, CustomCardPlayCostSpendRecord?>? _spendRecord = spendRecord;

        public bool IsAffordable(CardModel card, Player player)
        {
            return _isAffordable(card, player);
        }

        public Task SpendAsync(CardModel card, Player player)
        {
            return _spend(card, player);
        }

        bool ICardCustomPlayCostPlannable.TryGetPlannedSpend(CardModel card, Player player,
            out CustomCardPlayCostSpendRecord record)
        {
            record = default;
            if (_plannedSpend == null)
                return false;

            var line = _plannedSpend(card, player);
            if (line is not { } v || string.IsNullOrEmpty(v.KindId))
                return false;

            record = v;
            return true;
        }

        CustomCardPlayCostSpendRecord? ICardCustomPlayCostSpendRecordable.TryBuildSpendRecord(CardModel card,
            Player player)
        {
            var line = _spendRecord?.Invoke(card, player);
            return line is { KindId: not null and not "" }
                ? line
                : null;
        }
    }
}
