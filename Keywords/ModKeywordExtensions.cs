using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Extension methods for attaching runtime keyword ids to arbitrary objects and for hover-tip helpers.
    ///     For every <see cref="CardModel" /> target (vanilla or modded) all operations are backed by
    ///     <see cref="CardModKeywordStore" />: the same single path handles instance storage, canonical seeding
    ///     from <see cref="Scaffolding.Content.ModCardTemplate.RegisteredKeywordIds" />, clone propagation, save/load
    ///     persistence, and hover-tip rendering. Non-card objects fall back to a weak-table lookup (no clone /
    ///     save persistence) for ad-hoc use cases.
    /// </summary>
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> FallbackKeywords = new();

        /// <summary>
        ///     Adds a runtime keyword id to the extended target (deduplicated, case-insensitive).
        ///     For every <see cref="CardModel" /> (vanilla or modded) the instance set in
        ///     <see cref="CardModKeywordStore" /> is used.
        /// </summary>
        public static void AddModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
            {
                CardModKeywordStore.Add(card, keywordId);
                return;
            }

            var normalized = keywordId.Trim().ToLowerInvariant();
            lock (SyncRoot)
                FallbackKeywords.GetOrCreateValue(target).Add(normalized);
        }

        /// <summary>
        ///     Removes a previously added runtime keyword id.
        ///     For every <see cref="CardModel" /> the instance set in <see cref="CardModKeywordStore" /> is used.
        /// </summary>
        /// <returns>True when the id was present and removed.</returns>
        public static bool RemoveModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return CardModKeywordStore.Remove(card, keywordId);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Remove(keywordId.Trim().ToLowerInvariant());
            }
        }

        /// <summary>
        ///     Returns whether the target has the given runtime keyword id currently in effect.
        /// </summary>
        public static bool HasModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return CardModKeywordStore.Contains(card, keywordId);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Contains(keywordId.Trim().ToLowerInvariant());
            }
        }

        /// <summary>
        ///     Sorted list of effective runtime keyword ids on the target.
        ///     For every <see cref="CardModel" /> this reflects the full runtime set
        ///     (canonical <see cref="Scaffolding.Content.ModCardTemplate.RegisteredKeywordIds" /> seeded on first access
        ///     plus any runtime additions / minus any removals).
        /// </summary>
        public static IReadOnlyList<string> GetModKeywordIds(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is CardModel card)
                return CardModKeywordStore.GetIds(card);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set)
                    ? set.OrderBy(static x => x).ToArray()
                    : [];
            }
        }

        /// <summary>
        ///     Hover tips for all runtime keyword ids on the target.
        /// </summary>
        public static IEnumerable<IHoverTip> GetModKeywordHoverTips(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);
            return target.GetModKeywordIds().ToHoverTips();
        }

        /// <summary>
        ///     Case-insensitive containment check for a keyword id in the sequence.
        /// </summary>
        public static bool ContainsModKeyword(this IEnumerable<string> keywords, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(keywords);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            var normalized = keywordId.Trim().ToLowerInvariant();
            return keywords.Any(id => string.Equals(id?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Maps each non-empty keyword id to a registered <see cref="IHoverTip" /> when
        ///     <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> is true.
        /// </summary>
        public static IEnumerable<IHoverTip> ToHoverTips(this IEnumerable<string> keywords)
        {
            ArgumentNullException.ThrowIfNull(keywords);

            return keywords
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Select(static id => id.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(static id =>
                    ModKeywordRegistry.TryGet(id, out var def) && def.IncludeInCardHoverTip)
                .Select(ModKeywordRegistry.CreateHoverTip)
                .ToArray();
        }

        /// <summary>
        ///     Card BBCode for the extended keyword id string via <see cref="ModKeywordRegistry.GetCardText" />.
        /// </summary>
        public static string GetModKeywordCardText(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardText(keywordId);
        }
    }
}
