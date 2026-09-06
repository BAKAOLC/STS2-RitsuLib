namespace STS2RitsuLib.Search.Pinyin
{
    internal static class PinyinSearchMatcher
    {
        internal static int Score(string text, string term, bool full, bool initials,
            CancellationToken cancellationToken = default)
        {
            var data = PinyinSearchDataManager.Data;
            if (data == null || (!full && !initials) || term.Length == 0 ||
                term.Any(static value => value is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z')))
                return -1;
            if (term.Length > (long)text.Length * 24)
                return -1;
            var normalized = term.ToLowerInvariant();
            var fullScore = full ? Match(text, normalized, data, false, cancellationToken) : -1;
            if (fullScore == 0 || !initials)
                return fullScore;
            var initialsScore = Match(text, normalized, data, true, cancellationToken);
            return fullScore < 0 ? initialsScore : initialsScore < 0 ? fullScore : Math.Min(fullScore, initialsScore);
        }

        private static int Match(string text, string term, PinyinSearchData data, bool initials,
            CancellationToken cancellationToken)
        {
            // States represent matched query prefixes, so all readings are considered without enumerating combinations.
            var current = new int[term.Length];
            var next = new int[term.Length];
            Array.Fill(current, -1);
            var position = 0;
            var processed = 0;
            var best = int.MaxValue;
            foreach (var rune in text.EnumerateRunes())
            {
                if (++processed % 128 == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                if (!data.TryGetReadings(rune, out var readings))
                    continue;
                Array.Fill(next, -1);
                for (var readingIndex = 0; readingIndex < readings.Length; readingIndex++)
                {
                    var reading = readings[readingIndex].AsSpan();
                    if (initials)
                        reading = reading[..1];
                    var penalty = !initials && readingIndex > 0 ? 30 : 0;
                    for (var matched = 1; matched < term.Length; matched++)
                    {
                        if (current[matched] < 0)
                            continue;
                        Advance(reading, matched, current[matched] + penalty);
                    }

                    for (var start = 0; start < reading.Length; start++)
                        Advance(reading[start..], 0, position + start + (initials ? 60 : penalty));
                }

                (current, next) = (next, current);
                position += initials ? 1 : readings[0].Length;
                if (best == 0)
                    return 0;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return best == int.MaxValue ? -1 : best;

            void Advance(ReadOnlySpan<char> reading, int matched, int score)
            {
                var count = Math.Min(reading.Length, term.Length - matched);
                for (var index = 0; index < count; index++)
                {
                    var value = reading[index];
                    var query = term[matched + index];
                    if (value != query && !(value == 'v' && query == 'u'))
                        return;
                }

                var length = matched + count;
                if (length == term.Length)
                    best = Math.Min(best, score);
                else if (next[length] < 0 || score < next[length])
                    next[length] = score;
            }
        }
    }
}
