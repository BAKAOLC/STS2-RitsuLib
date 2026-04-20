using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Unified per-instance mod-keyword store for every <see cref="CardModel" /> (vanilla or modded).
    ///     Mirrors the native <c>CardKeyword</c> pipeline: a lazily-materialized mutable set keyed by card reference,
    ///     automatically seeded from <see cref="ModCardTemplate.RegisteredKeywordIds" /> for
    ///     <see cref="ModCardTemplate" /> subclasses, preserved across <see cref="AbstractModel.MutableClone" />,
    ///     and serialized through <see cref="SavedAttachedState{TKey,TValue}" /> into <c>SavedProperties</c>.
    /// </summary>
    public static class CardModKeywordStore
    {
        private const string StateName = "mod_keyword_ids";
        private static readonly Lock SyncRoot = new();

        private static readonly SavedAttachedState<CardModel, HashSet<string>> State =
            new(StateName, CreateCanonicalSet);

        /// <summary>
        ///     Adds <paramref name="keywordId" /> to the card's runtime mod-keyword set (case-insensitive, deduplicated).
        /// </summary>
        /// <returns>True when the keyword was not already present.</returns>
        public static bool Add(CardModel card, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            var normalized = keywordId.Trim().ToLowerInvariant();
            lock (SyncRoot)
                return State.GetOrCreate(card).Add(normalized);
        }

        /// <summary>
        ///     Removes <paramref name="keywordId" /> from the card's runtime mod-keyword set.
        /// </summary>
        /// <returns>True when the keyword was present and removed.</returns>
        public static bool Remove(CardModel card, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            var normalized = keywordId.Trim().ToLowerInvariant();
            lock (SyncRoot)
                return State.GetOrCreate(card).Remove(normalized);
        }

        /// <summary>
        ///     Returns whether the card's current mod-keyword set contains <paramref name="keywordId" />.
        /// </summary>
        public static bool Contains(CardModel card, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            var normalized = keywordId.Trim().ToLowerInvariant();
            lock (SyncRoot)
            {
                if (State.TryGetValue(card, out var set) && set != null)
                    return set.Contains(normalized);
            }

            return EnumerateCanonicalIds(card)
                .Any(id => string.Equals(id, normalized, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Snapshot of the card's current effective mod-keyword ids (sorted, case-normalized).
        ///     Reads canonical <see cref="ModCardTemplate.RegisteredKeywordIds" /> without materializing when possible.
        /// </summary>
        public static IReadOnlyList<string> GetIds(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            lock (SyncRoot)
            {
                if (State.TryGetValue(card, out var set) && set != null)
                    return [.. set.OrderBy(static x => x, StringComparer.Ordinal)];
            }

            return [.. EnumerateCanonicalIds(card).OrderBy(static x => x, StringComparer.Ordinal)];
        }

        /// <summary>
        ///     Replaces the card's mod-keyword set (used by clone propagation and save restore).
        /// </summary>
        internal static void Replace(CardModel card, HashSet<string> newSet)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentNullException.ThrowIfNull(newSet);

            lock (SyncRoot)
                State.Set(card, newSet);
        }

        /// <summary>
        ///     Returns the live materialized set when present; otherwise <c>null</c>. Does not trigger factory.
        /// </summary>
        internal static HashSet<string>? TryGetLive(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            lock (SyncRoot)
                return State.TryGetValue(card, out var set) ? set : null;
        }

        private static HashSet<string> CreateCanonicalSet(CardModel card)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in EnumerateCanonicalIds(card))
                set.Add(id);

            return set;
        }

        private static IEnumerable<string> EnumerateCanonicalIds(CardModel card)
        {
            if (card is not ModCardTemplate mct)
                yield break;

            foreach (var id in mct.EnumerateRegisteredKeywordIds())
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                yield return id.Trim().ToLowerInvariant();
            }
        }
    }
}
