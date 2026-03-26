using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     When you cannot change the card base type, register a contributor to append costs at runtime (e.g. by id,
    ///     tags, or attachments).
    /// </summary>
    public interface ICustomCardPlayCostContributor
    {
        /// <summary>Append any costs that apply to <paramref name="card" /> onto <paramref name="costs" />.</summary>
        void AppendCosts(CardModel card, List<ICardCustomPlayCost> costs);
    }
}
