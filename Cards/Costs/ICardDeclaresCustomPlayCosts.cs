using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Implemented by <see cref="CardModel" /> subclasses to declare extra costs required to play this card.
    /// </summary>
    public interface ICardDeclaresCustomPlayCosts
    {
        /// <summary>Costs to validate and spend for this instance when played (may be empty).</summary>
        IEnumerable<ICardCustomPlayCost> EnumerateCustomPlayCosts();
    }
}
