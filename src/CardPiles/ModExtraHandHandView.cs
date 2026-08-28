using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    internal static class ModExtraHandHandView
    {
        private static readonly AccessTools.FieldRef<CardPile, List<CardModel>> RawCards =
            AccessTools.FieldRefAccess<CardPile, List<CardModel>>("_cards");

        private static readonly AccessTools.FieldRef<PlayerCombatState, Player> CombatStatePlayer =
            AccessTools.FieldRefAccess<PlayerCombatState, Player>("_player");

        internal static CardPile GetForQueries(PileType pileType, Player player)
        {
            return Get(pileType, player, IsCardPlayEnabled);
        }

        internal static CardPile GetQueryHand(PlayerCombatState state)
        {
            return Get(PileType.Hand, CombatStatePlayer(state), IsCardPlayEnabled);
        }

        internal static IReadOnlyList<CardModel> GetCardsForQueries(CardPile pile)
        {
            var player = FindHandOwner(pile);
            return player == null
                ? pile.Cards
                : Get(PileType.Hand, player, IsCardPlayEnabled).Cards;
        }

        internal static CardPile GetForTurnEnd(PileType pileType, Player player)
        {
            return Get(pileType, player, ModExtraHandBehavior.ApplyHandTurnEndRules);
        }

        internal static CardPile GetForFlush(PileType pileType, Player player)
        {
            return Get(pileType, player, ModExtraHandBehavior.FlushWithHand);
        }

        internal static CardModel[] GetExtraCards(Player player, ModExtraHandBehavior behavior)
        {
            return
            [
                .. GetExistingPiles(player)
                    .Where(pile => pile.Definition.Style == ModCardPileUiStyle.ExtraHand
                                   && pile.Definition.ExtraHand.Behaviors.HasFlag(behavior))
                    .OrderBy(pile => pile.Definition.Id, StringComparer.Ordinal)
                    .SelectMany(pile => pile.Cards)
                    .Where(card => ReferenceEquals(card.Owner, player)),
            ];
        }

        private static CardPile Get(PileType pileType, Player player, ModExtraHandBehavior behavior)
        {
            return Get(pileType, player, pile => pile.Definition.ExtraHand.Behaviors.HasFlag(behavior));
        }

        private static CardPile Get(PileType pileType, Player player, Func<ModCardPile, bool> includePile)
        {
            var pile = pileType.GetPile(player);
            if (pileType != PileType.Hand)
                return pile;

            var extraCards = GetExistingPiles(player)
                .Where(candidate => candidate.Definition.Style == ModCardPileUiStyle.ExtraHand
                                    && includePile(candidate))
                .OrderBy(candidate => candidate.Definition.Id, StringComparer.Ordinal)
                .SelectMany(candidate => candidate.Cards)
                .Where(card => ReferenceEquals(card.Owner, player))
                .ToArray();
            if (extraCards.Length == 0)
                return pile;

            var snapshot = new CardPile(PileType.Hand);
            var cards = RawCards(snapshot);
            cards.AddRange(pile.Cards);
            foreach (var card in extraCards)
                if (!cards.Contains(card))
                    cards.Add(card);
            return snapshot;
        }

        private static bool IsCardPlayEnabled(ModCardPile pile)
        {
            if (!pile.Definition.ExtraHand.AllowCardPlay)
                return false;

            return ModCardPileButtonRegistry.TryGetExtraHand(pile.Definition)?.CardPlayEnabled != false;
        }

        private static Player? FindHandOwner(CardPile pile)
        {
            return CombatManager.Instance.DebugOnlyGetState()?.Players
                .FirstOrDefault(candidate => ReferenceEquals(candidate.PlayerCombatState?.Hand, pile));
        }

        private static IEnumerable<ModCardPile> GetExistingPiles(Player player)
        {
            if (player.PlayerCombatState is { } state)
                foreach (var pile in ModCardPileStorage.GetCombatPiles(state))
                    yield return pile;
            foreach (var pile in ModCardPileStorage.GetRunPiles(player))
                yield return pile;
        }
    }
}
