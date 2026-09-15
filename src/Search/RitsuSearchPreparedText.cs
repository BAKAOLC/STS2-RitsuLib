using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Search
{
    internal sealed class RitsuSearchPreparedText
    {
        private readonly Lock _lock = new();
        private readonly RitsuSearchOptions _options;
        private readonly Dictionary<string, int> _pinyinScores = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _text;
        private readonly bool _useProviderExpansions;
        private Task<IReadOnlyList<RitsuSearchExpansion>>? _expansionTask;
        private long _expansionTaskGeneration = -1;
        private string _expansionTaskLanguageCode = string.Empty;
        private IReadOnlyList<RitsuSearchExpansion> _expansions = [];
        private long _generation = -1;
        private string _languageCode = string.Empty;
        private long _pinyinGeneration = -1;

        internal RitsuSearchPreparedText(string text, RitsuSearchOptions? options = null)
        {
            _text = text;
            _options = options ?? RitsuSearchOptions.Default;
            _useProviderExpansions = _options.UseOtherProviders ||
                                     _options.ProviderOverrides.Values.Any(static enabled => enabled);
        }

        internal int ScoreExpansion(string term)
        {
            ArgumentNullException.ThrowIfNull(term);
            if (_options == RitsuSearchOptions.Literal)
                return -1;
            var pinyin = ScorePinyin(term, CancellationToken.None);
            if (pinyin == 0 || !_useProviderExpansions)
                return pinyin;
            EnsureCurrent();
            return Best(pinyin, ScoreExpansion(_expansions, term));
        }

        internal async ValueTask<int> ScoreExpansionAsync(
            string term,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(term);
            cancellationToken.ThrowIfCancellationRequested();
            if (_options == RitsuSearchOptions.Literal)
                return -1;
            var pinyin = await Task.Run(() => ScorePinyin(term, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
            if (pinyin == 0 || !_useProviderExpansions)
                return pinyin;
            var expansions = await EnsureCurrentAsync(cancellationToken).ConfigureAwait(false);
            return Best(pinyin, ScoreExpansion(expansions, term));
        }

        private int ScorePinyin(string term, CancellationToken cancellationToken)
        {
            if (_options is { Pinyin: false, PinyinInitials: false })
                return -1;
            lock (_lock)
            {
                var generation = RitsuSearchExpansionRegistry.Generation;
                if (_pinyinGeneration != generation)
                {
                    _pinyinGeneration = generation;
                    _pinyinScores.Clear();
                }

                if (_pinyinScores.TryGetValue(term, out var cached))
                    return cached;
                var score = RitsuSearchExpansionRegistry.ScorePinyin(_text, term, _options, cancellationToken);
                if (_pinyinScores.Count >= 32)
                    _pinyinScores.Clear();
                _pinyinScores.Add(term, score);
                return score;
            }
        }

        private static int Best(int first, int second)
        {
            return first < 0 ? second : second < 0 ? first : Math.Min(first, second);
        }

        private void EnsureCurrent()
        {
            var generation = RitsuSearchExpansionRegistry.Generation;
            var languageCode = I18N.ResolveCurrentLanguageCode();
            lock (_lock)
            {
                if (_generation == generation && string.Equals(_languageCode, languageCode, StringComparison.Ordinal))
                    return;
                _expansions = RitsuSearchExpansionRegistry.Expand(_text, languageCode, _options);
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
                            () => RitsuSearchExpansionRegistry.Expand(_text, languageCode, _options),
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
            cancellationToken.ThrowIfCancellationRequested();
            if (text.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                return true;
            return await (prepared ?? new(text)).ScoreExpansionAsync(term, cancellationToken).ConfigureAwait(false) >=
                   0;
        }
    }
}
