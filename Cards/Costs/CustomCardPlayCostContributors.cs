using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Registers global cost contributors; combine with <see cref="ICardDeclaresCustomPlayCosts" /> on cards.
    /// </summary>
    public static class CustomCardPlayCostContributors
    {
        private static readonly Lock Gate = new();
        private static readonly List<ICustomCardPlayCostContributor> Contributors = [];

        public static void Register(ICustomCardPlayCostContributor contributor)
        {
            ArgumentNullException.ThrowIfNull(contributor);
            lock (Gate)
            {
                if (!Contributors.Contains(contributor))
                    Contributors.Add(contributor);
            }
        }

        public static void Unregister(ICustomCardPlayCostContributor contributor)
        {
            ArgumentNullException.ThrowIfNull(contributor);
            lock (Gate)
            {
                Contributors.Remove(contributor);
            }
        }

        internal static void Collect(CardModel card, List<ICardCustomPlayCost> buffer)
        {
            buffer.Clear();

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (card is ICardDeclaresCustomPlayCosts declares)
                buffer.AddRange(declares.EnumerateCustomPlayCosts());

            ICustomCardPlayCostContributor[] snapshot;
            lock (Gate)
            {
                snapshot = Contributors.ToArray();
            }

            foreach (var c in snapshot)
                c.AppendCosts(card, buffer);
        }
    }
}
