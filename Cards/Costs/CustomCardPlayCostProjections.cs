using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Query helpers for custom play costs (planned lines for UI).
    /// </summary>
    public static class CustomCardPlayCostProjections
    {
        /// <summary>
        ///     Collects planned spend lines from costs implementing <see cref="ICardCustomPlayCostPlannable" />.
        /// </summary>
        public static IReadOnlyList<CustomCardPlayCostSpendRecord> GetPlannedSpends(CardModel card, Player player)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(player);

            var costs = new List<ICardCustomPlayCost>();
            CustomCardPlayCostContributors.Collect(card, costs);
            if (costs.Count == 0)
                return [];

            var lines = new List<CustomCardPlayCostSpendRecord>();
            foreach (var cost in costs)
            {
                if (cost is not ICardCustomPlayCostPlannable plannable)
                    continue;

                if (!plannable.TryGetPlannedSpend(card, player, out var line))
                    continue;

                if (string.IsNullOrEmpty(line.KindId))
                    continue;

                lines.Add(line);
            }

            return lines;
        }
    }
}
