using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.Keywords
{
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> RuntimeKeywords = new();

        extension(object target)
        {
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

            public IEnumerable<IHoverTip> GetModKeywordHoverTips()
            {
                ArgumentNullException.ThrowIfNull(target);
                return target.GetModKeywordIds().ToHoverTips();
            }
        }

        extension(IEnumerable<string> keywords)
        {
            public bool ContainsModKeyword(string keywordId)
            {
                ArgumentNullException.ThrowIfNull(keywords);
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

                var normalized = keywordId.Trim().ToLowerInvariant();
                return keywords.Any(id => string.Equals(id?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
            }

            public IEnumerable<IHoverTip> ToHoverTips()
            {
                ArgumentNullException.ThrowIfNull(keywords);

                return keywords
                    .Where(static id => !string.IsNullOrWhiteSpace(id))
                    .Select(static id => id.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(ModKeywordRegistry.CreateHoverTip)
                    .ToArray();
            }
        }

        extension(string keywordId)
        {
            public string GetModKeywordCardText()
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
                return ModKeywordRegistry.GetCardText(keywordId);
            }
        }
    }
}
