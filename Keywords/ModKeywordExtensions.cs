using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Extension methods for attaching runtime keyword ids to arbitrary objects and for hover-tip helpers.
    ///     Every <see cref="CardModel" /> operation is routed straight through vanilla <c>CardModel.Keywords</c>
    ///     using the pre-minted <see cref="ModKeywordDefinition.CardKeywordValue" /> (so the mod keyword rides
    ///     vanilla <c>AddKeyword</c> / <c>RemoveKeyword</c> / <c>DeepCloneFields</c> / canonical seeding without
    ///     any side-loaded state). Non-card objects fall back to a <see cref="ConditionalWeakTable{TKey,TValue}" />
    ///     for ad-hoc usage (no clone / save persistence).
    /// </summary>
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> FallbackKeywords = new();

        /// <summary>
        ///     Adds a runtime keyword id to the extended target (deduplicated, case-insensitive).
        ///     For every <see cref="CardModel" /> (vanilla or modded) the minted
        ///     <see cref="ModKeywordDefinition.CardKeywordValue" /> is pushed into vanilla
        ///     <c>CardModel.Keywords</c>; the id must already be registered via <see cref="ModKeywordRegistry" />.
        /// </summary>
        public static void AddModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
            {
                card.AddModKeyword(ModKeywordRegistry.GetCardKeyword(keywordId));
                return;
            }

            var normalized = keywordId.Trim().ToLowerInvariant();
            lock (SyncRoot)
            {
                FallbackKeywords.GetOrCreateValue(target).Add(normalized);
            }
        }

        /// <summary>
        ///     Adds a pre-minted mod <see cref="CardKeyword" /> value directly to vanilla
        ///     <c>CardModel.Keywords</c>, enabling native-style call sites like
        ///     <c>card.AddModKeyword(ModKeywordRegistry.GetCardKeyword("mymod.blazed"))</c>. The card's keyword
        ///     set is materialized first (mirroring the vanilla getter) so the underlying
        ///     <c>_keywords</c> field is never null when <see cref="CardModel.AddKeyword" /> runs.
        /// </summary>
        public static void AddModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            _ = card.Keywords;
            card.AddKeyword(value);
        }

        /// <summary>
        ///     Removes a previously added runtime keyword id.
        ///     For every <see cref="CardModel" /> the corresponding minted value is removed from vanilla
        ///     <c>CardModel.Keywords</c>; unregistered ids return <c>false</c> without touching the card.
        /// </summary>
        /// <returns>True when the id was present and removed.</returns>
        public static bool RemoveModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return ModKeywordRegistry.TryGetCardKeyword(keywordId, out var value) &&
                       card.RemoveModKeyword(value);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Remove(keywordId.Trim().ToLowerInvariant());
            }
        }

        /// <summary>
        ///     Removes <paramref name="value" /> from vanilla <c>CardModel.Keywords</c>. Returns <c>true</c> when
        ///     the keyword was present.
        /// </summary>
        public static bool RemoveModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            if (!card.Keywords.Contains(value))
                return false;

            card.RemoveKeyword(value);
            return true;
        }

        /// <summary>
        ///     Returns whether the target has the given runtime keyword id currently in effect.
        /// </summary>
        public static bool HasModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return ModKeywordRegistry.TryGetCardKeyword(keywordId, out var value) &&
                       card.Keywords.Contains(value);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Contains(keywordId.Trim().ToLowerInvariant());
            }
        }

        /// <summary>
        ///     Whether <paramref name="card" /> currently carries the minted mod keyword <paramref name="value" />.
        /// </summary>
        public static bool HasModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            return card.Keywords.Contains(value);
        }

        /// <summary>
        ///     Sorted list of effective runtime mod-keyword ids on the target. For every
        ///     <see cref="CardModel" /> this enumerates vanilla <c>CardModel.Keywords</c> and reverse-maps minted
        ///     values back to their registered ids (skipping vanilla and unregistered entries).
        /// </summary>
        public static IReadOnlyList<string> GetModKeywordIds(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is CardModel card)
            {
                var ids = new List<string>();
                foreach (var keyword in card.Keywords)
                    if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out var def))
                        ids.Add(def.Id);

                ids.Sort(StringComparer.Ordinal);
                return ids;
            }

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set)
                    ? [.. set.OrderBy(static x => x, StringComparer.Ordinal)]
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

        /// <summary>
        ///     Convenience: minted <see cref="CardKeyword" /> value for <paramref name="keywordId" />, intended
        ///     for call sites that want to use the native vanilla keyword API directly
        ///     (<c>card.AddKeyword(id.GetModKeywordCardKeyword())</c>).
        /// </summary>
        public static CardKeyword GetModKeywordCardKeyword(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardKeyword(keywordId);
        }
    }
}
