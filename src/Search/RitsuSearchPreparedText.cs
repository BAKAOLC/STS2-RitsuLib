using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Search
{
    internal sealed class RitsuSearchPreparedText
    {
        private readonly Lock _lock = new();
        private readonly string _text;
        private IReadOnlyList<RitsuSearchExpansion> _expansions = [];
        private Task<IReadOnlyList<RitsuSearchExpansion>>? _expansionTask;
        private long _expansionTaskGeneration = -1;
        private string _expansionTaskLanguageCode = string.Empty;
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

        internal async ValueTask<int> ScoreExpansionAsync(
            string term,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(term);
            var expansions = await EnsureCurrentAsync(cancellationToken).ConfigureAwait(false);
            return ScoreExpansion(expansions, term);
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
                _expansionTask = null;
            }
        }

        private async ValueTask<IReadOnlyList<RitsuSearchExpansion>> EnsureCurrentAsync(
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var generation = RitsuSearchExpansionRegistry.Generation;
                var languageCode = I18N.ResolveCurrentLanguageCode();
                Task<IReadOnlyList<RitsuSearchExpansion>> expansionTask;
                lock (_lock)
                {
                    if (_generation == generation &&
                        string.Equals(_languageCode, languageCode, StringComparison.Ordinal))
                        return _expansions;

                    if (_expansionTaskGeneration != generation ||
                        !string.Equals(_expansionTaskLanguageCode, languageCode, StringComparison.Ordinal) ||
                        _expansionTask == null)
                    {
                        _expansionTaskGeneration = generation;
                        _expansionTaskLanguageCode = languageCode;
                        _expansionTask = Task.Run(
                            () => RitsuSearchExpansionRegistry.Expand(_text, languageCode),
                            CancellationToken.None);
                    }

                    expansionTask = _expansionTask;
                }

                var expansions = await expansionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (generation != RitsuSearchExpansionRegistry.Generation ||
                    !string.Equals(languageCode, I18N.ResolveCurrentLanguageCode(), StringComparison.Ordinal))
                    continue;

                lock (_lock)
                {
                    if (ReferenceEquals(_expansionTask, expansionTask))
                    {
                        _expansions = expansions;
                        _generation = generation;
                        _languageCode = languageCode;
                        _expansionTask = null;
                    }

                    return _generation == generation &&
                           string.Equals(_languageCode, languageCode, StringComparison.Ordinal)
                        ? _expansions
                        : expansions;
                }
            }
        }

        private static int ScoreExpansion(IReadOnlyList<RitsuSearchExpansion> expansions, string term)
        {
            var best = int.MaxValue;
            foreach (var expansion in expansions)
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
    }

    internal static class RitsuSearchMatcher
    {
        internal static bool Contains(string text, string term, RitsuSearchPreparedText? prepared = null)
        {
            return text.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                   (prepared ?? new(text)).ScoreExpansion(term) >= 0;
        }

        internal static async ValueTask<bool> ContainsAsync(
            string text,
            string term,
            RitsuSearchPreparedText? prepared = null,
            CancellationToken cancellationToken = default)
        {
            if (text.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                return true;
            return await (prepared ?? new(text)).ScoreExpansionAsync(term, cancellationToken).ConfigureAwait(false) >=
                   0;
        }
    }
}
