using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Combat.CardTargeting
{
    internal static class CustomTargetTypeSelectionContext
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<NTargetManager, Stack<Source>> Sources = [];

        internal static IDisposable PushCard(NTargetManager manager, CardModel card)
        {
            return Push(manager, new(card.Owner, card, null));
        }

        internal static IDisposable PushPotion(NTargetManager manager, PotionModel potion)
        {
            return Push(manager, new(potion.Owner, null, potion));
        }

        internal static CustomTargetContext CreateContext(
            NTargetManager manager,
            Creature targetCreature,
            Player fallbackPlayer)
        {
            lock (SyncRoot)
            {
                if (Sources.TryGetValue(manager, out var stack) && stack.TryPeek(out var source))
                    return new(targetCreature, source.Player, source.Card, source.Potion);
            }

            return new(targetCreature, fallbackPlayer);
        }

        private static IDisposable Push(NTargetManager manager, Source source)
        {
            lock (SyncRoot)
            {
                if (!Sources.TryGetValue(manager, out var stack))
                {
                    stack = new();
                    Sources.Add(manager, stack);
                }

                stack.Push(source);
            }

            return new Popper(manager);
        }

        private static void Pop(NTargetManager manager)
        {
            lock (SyncRoot)
            {
                if (!Sources.TryGetValue(manager, out var stack))
                    return;

                stack.Pop();
                if (stack.Count == 0)
                    Sources.Remove(manager);
            }
        }

        private readonly record struct Source(Player Player, CardModel? Card, PotionModel? Potion);

        private sealed class Popper(NTargetManager manager) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                Pop(manager);
            }
        }
    }
}
