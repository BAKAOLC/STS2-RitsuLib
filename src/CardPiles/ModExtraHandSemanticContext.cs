using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.CardPiles
{
    internal static class ModExtraHandSemanticContext
    {
        private static readonly AsyncLocal<State?> Current = new();

        internal static IDisposable? EnterPlayEvaluation(CardModel card)
        {
            if (IsActive(card))
                return null;
            if (card.Pile is not { } pile
                || !ModCardPileRegistry.TryGetByPileType(pile.Type, out var definition)
                || definition.Style != ModCardPileUiStyle.ExtraHand
                || !definition.ExtraHand.AllowCardPlay)
                return null;

            var previous = Current.Value;
            Current.Value = new(card, previous);
            return new Scope(previous);
        }

        internal static bool IsActive(CardModel card)
        {
            for (var state = Current.Value; state != null; state = state.Previous)
                if (ReferenceEquals(state.Card, card))
                    return true;
            return false;
        }

        private sealed class State(CardModel card, State? previous)
        {
            public CardModel Card { get; } = card;
            public State? Previous { get; } = previous;
        }

        private sealed class Scope(State? previous) : IDisposable
        {
            private State? _previous = previous;
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                Current.Value = _previous;
                _previous = null;
            }
        }
    }
}
