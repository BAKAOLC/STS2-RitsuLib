using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.CardPiles
{
    internal static class ModCardPileFlightHistory
    {
        private static readonly Dictionary<CardModel, CardPile> LastRemovedPileByCard = [];

        internal static void RecordRemoved(CardPile pile, CardModel card)
        {
            if (pile == null || card == null)
                return;
            LastRemovedPileByCard[card] = pile;
        }

        internal static CardPile? TryGetLastRemovedPile(CardModel card)
        {
            return card == null ? null : LastRemovedPileByCard.GetValueOrDefault(card);
        }

        internal static void Clear()
        {
            LastRemovedPileByCard.Clear();
        }
    }
}
