using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Costs
{
    /// <summary>
    ///     Per-combat append-only ledger of reported custom spends, plus pending snapshot for the last custom spend batch per
    ///     card instance.
    /// </summary>
    public static class CustomCardPlayCostLedger
    {
        private static readonly Lock Gate = new();
        private static bool _subscribed;

        private static readonly Dictionary<CombatState, List<CustomCardPlayCostLedgerEntry>> ByCombat =
            new(ReferenceEqualityComparer.Instance);

        private static readonly ConditionalWeakTable<CardModel, List<CustomCardPlayCostSpendRecord>> PendingByCard =
            new();

        /// <summary>
        ///     Raised after custom spends for one play; <see cref="CustomCardPlayCostsSpentEventArgs.Records" /> may be
        ///     empty.
        /// </summary>
        public static event EventHandler<CustomCardPlayCostsSpentEventArgs>? CostsSpent;

        internal static void EnsureSubscribed()
        {
            lock (Gate)
            {
                if (_subscribed)
                    return;

                RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(OnCombatEnded, false);
                _subscribed = true;
            }
        }

        private static void OnCombatEnded(CombatEndedEvent evt)
        {
            if (evt.CombatState is not { } cs)
                return;

            lock (Gate)
            {
                ByCombat.Remove(cs);
            }
        }

        /// <summary>All ledger lines for this combat (empty if none).</summary>
        public static IReadOnlyList<CustomCardPlayCostLedgerEntry> GetEntries(CombatState combatState)
        {
            lock (Gate)
            {
                return ByCombat.TryGetValue(combatState, out var list)
                    ? list.ToArray()
                    : [];
            }
        }

        /// <summary>Sum amounts for a kind id in the given combat.</summary>
        public static decimal SumAmountForKind(CombatState combatState, string kindId)
        {
            ArgumentException.ThrowIfNullOrEmpty(kindId);
            lock (Gate)
            {
                return !ByCombat.TryGetValue(combatState, out var list)
                    ? 0m
                    : list.Where(e => e.KindId == kindId).Sum(e => e.Amount);
            }
        }

        internal static void AppendAndRaise(CombatState combatState, CardModel card, Player player,
            List<CustomCardPlayCostSpendRecord> records)
        {
            if (records.Count == 0)
                return;

            var round = combatState.RoundNumber;
            var cardId = card.Id;
            var netId = player.NetId;
            var utc = DateTimeOffset.UtcNow;

            lock (Gate)
            {
                if (!ByCombat.TryGetValue(combatState, out var ledger))
                {
                    ledger = [];
                    ByCombat[combatState] = ledger;
                }

                var entries = from r in records
                    where !string.IsNullOrEmpty(r.KindId)
                    select new CustomCardPlayCostLedgerEntry(
                        utc,
                        round,
                        cardId,
                        r.KindId,
                        r.Amount,
                        r.DisplayHint,
                        netId);
                ledger.AddRange(entries);

                PendingByCard.Remove(card);
                PendingByCard.Add(card, [.. records]);
            }

            var snapshot = (IReadOnlyList<CustomCardPlayCostSpendRecord>)records.ToArray();
            CostsSpent?.Invoke(
                null,
                new()
                {
                    CombatState = combatState,
                    Card = card,
                    Player = player,
                    Records = snapshot,
                });
        }

        /// <summary>
        ///     Consume custom spend lines attached to this card by the last <see cref="CardModel.SpendResources" /> custom-cost
        ///     phase.
        ///     Returns false if there is no pending snapshot (or it was already consumed).
        /// </summary>
        public static bool TryConsumePendingSpendRecords(CardModel card,
            out IReadOnlyList<CustomCardPlayCostSpendRecord> records)
        {
            ArgumentNullException.ThrowIfNull(card);
            lock (Gate)
            {
                if (!PendingByCard.TryGetValue(card, out var list))
                {
                    records = [];
                    return false;
                }

                PendingByCard.Remove(card);
                records = list.ToArray();
                return records.Count > 0;
            }
        }
    }

    /// <summary>Reference equality for combat state instances.</summary>
    file sealed class ReferenceEqualityComparer : IEqualityComparer<CombatState>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(CombatState? x, CombatState? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(CombatState obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
