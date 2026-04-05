using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Extension methods for attaching runtime keyword ids to arbitrary objects and for hover-tip helpers.
    /// </summary>
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> RuntimeKeywords = new();

        extension(object target)
        {
            /// <summary>
            ///     Adds a runtime keyword id to the extended object (deduplicated, case-insensitive).
            /// </summary>
            public void AddModKeyword(string keywordId)
            {
                ArgumentNullException.ThrowIfNull(target);
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

                var normalized = keywordId.Trim().ToLowerInvariant();

                lock (SyncRoot)
                {
                    var set = RuntimeKeywords.GetOrCreateValue(target);
                    set.Add(normalized);
                }
            }

            /// <summary>
            ///     Removes a previously added runtime keyword id.
            /// </summary>
            /// <returns>True if the id was present.</returns>
            public bool RemoveModKeyword(string keywordId)
            {
                ArgumentNullException.ThrowIfNull(target);
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

                lock (SyncRoot)
                {
                    return RuntimeKeywords.TryGetValue(target, out var set) &&
                           set.Remove(keywordId.Trim().ToLowerInvariant());
                }
            }

            /// <summary>
            ///     Returns whether the extended object has the given runtime keyword id.
            /// </summary>
            public bool HasModKeyword(string keywordId)
            {
                ArgumentNullException.ThrowIfNull(target);
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

                lock (SyncRoot)
                {
                    return RuntimeKeywords.TryGetValue(target, out var set) &&
                           set.Contains(keywordId.Trim().ToLowerInvariant());
                }
            }

            /// <summary>
            ///     Sorted list of runtime keyword ids on the extended object.
            /// </summary>
            public IReadOnlyList<string> GetModKeywordIds()
            {
                ArgumentNullException.ThrowIfNull(target);

                lock (SyncRoot)
                {
                    return RuntimeKeywords.TryGetValue(target, out var set)
                        ? set.OrderBy(static x => x).ToArray()
                        : [];
                }
            }

            /// <summary>
            ///     Hover tips for all runtime keyword ids on the extended object.
            /// </summary>
            public IEnumerable<IHoverTip> GetModKeywordHoverTips()
            {
                ArgumentNullException.ThrowIfNull(target);
                return target.GetModKeywordIds().ToHoverTips();
            }
        }

        extension(IEnumerable<string> keywords)
        {
            /// <summary>
            ///     Case-insensitive containment check for a keyword id in the sequence.
            /// </summary>
            public bool ContainsModKeyword(string keywordId)
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
            public IEnumerable<IHoverTip> ToHoverTips()
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
        }

        extension(string keywordId)
        {
            /// <summary>
            ///     Card BBCode for the extended keyword id string via <see cref="ModKeywordRegistry.GetCardText" />.
            /// </summary>
            public string GetModKeywordCardText()
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
                return ModKeywordRegistry.GetCardText(keywordId);
            }
        }
    }
}
