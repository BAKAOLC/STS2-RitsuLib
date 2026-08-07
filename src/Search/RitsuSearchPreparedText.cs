using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Search
{
    internal sealed class RitsuSearchPreparedText
    {
        private readonly Lock _lock = new();
        private readonly string _text;
        private IReadOnlyList<RitsuSearchExpansion> _expansions = [];
        private long _generation = -1;
        private string _languageCode = string.Empty;

        internal RitsuSearchPreparedText(string text)
        {
            _text = text;
        }

        internal int ScoreExpansion(string term)
        {
            ArgumentNullException.ThrowIfNull(term);
            EnsureCurrent();
            var best = int.MaxValue;
            foreach (var expansion in _expansions)
            {
                var index = expansion.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    continue;
                var score = expansion.Kind switch
                {
                    RitsuSearchExpansionKind.Transliteration => 0,
                    RitsuSearchExpansionKind.AlternateReading => 30,
                    RitsuSearchExpansionKind.Initialism => 60,
                    _ => 90,
                };
                best = Math.Min(best, score + index);
            }

            return best == int.MaxValue ? -1 : best;
        }

        private void EnsureCurrent()
        {
            var generation = RitsuSearchExpansionRegistry.Generation;
            var languageCode = I18N.ResolveCurrentLanguageCode();
            lock (_lock)
            {
                if (_generation == generation && string.Equals(_languageCode, languageCode, StringComparison.Ordinal))
                    return;
                _expansions = RitsuSearchExpansionRegistry.Expand(_text, languageCode);
                _generation = generation;
                _languageCode = languageCode;
            }
        }
    }

    internal static class RitsuSearchMatcher
    {
        internal static bool Contains(string text, string term, RitsuSearchPreparedText? prepared = null)
        {
            return text.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                   (prepared ?? new(text)).ScoreExpansion(term) >= 0;
        }
    }
}
