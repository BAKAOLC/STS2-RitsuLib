namespace STS2RitsuLib.Content
{
    internal sealed class ActEnterPool<TContext, TCandidate> where TCandidate : notnull
    {
        private readonly Dictionary<string, Contribution> _contributions = new(StringComparer.OrdinalIgnoreCase);

        internal void Declare(string owner, ActEnterPoolModeKind mode)
        {
            if (_contributions.TryGetValue(owner, out var existing))
            {
                if (existing.Mode != mode)
                    throw new InvalidOperationException($"Mod '{owner}' already declared a {existing.Mode} pool.");
                return;
            }

            _contributions.Add(owner, new(mode));
        }

        internal void AddCandidate(string owner, ActEnterPoolModeKind mode, TCandidate candidate,
            Func<TContext, bool> eligibility, Func<TContext, double> weight)
        {
            if (!_contributions.TryGetValue(owner, out var contribution) || contribution.Mode != mode)
                throw new InvalidOperationException($"Mod '{owner}' must declare its {mode} pool first.");
            if (!contribution.Candidates.TryAdd(candidate, new(candidate, eligibility, weight)))
                throw new InvalidOperationException($"Mod '{owner}' already registered candidate '{candidate}'.");
        }

        internal Snapshot CreateSnapshot(Func<TCandidate, string> candidateKey)
        {
            return new([
                .. _contributions.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(pair => pair.Value.Candidates.Values.OrderBy(
                        candidate => candidateKey(candidate.Value), StringComparer.Ordinal)),
            ]);
        }

        internal static (TCandidate Candidate, double Weight)[] PrepareForcedCandidates(
            (TCandidate Candidate, double Weight)[] candidates, Action<TCandidate> validateCandidate,
            Func<TCandidate, string> candidateKey)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            if (candidates.Length is < 1 or > 256)
                throw new ArgumentOutOfRangeException(nameof(candidates), candidates.Length,
                    "Provide 1 to 256 candidates.");
            var snapshot = candidates.ToArray();
            var values = new HashSet<TCandidate>();
            foreach (var (candidate, weight) in snapshot)
            {
                ArgumentNullException.ThrowIfNull(candidate, nameof(candidates));
                validateCandidate(candidate);
                if (!values.Add(candidate))
                    throw new ArgumentException($"Duplicate candidate '{candidate}'.", nameof(candidates));
                if (!double.IsFinite(weight) || weight <= 0d)
                    throw new ArgumentException("Candidate weights must be finite and strictly positive.",
                        nameof(candidates));
            }

            return [.. snapshot.OrderBy(candidate => candidateKey(candidate.Candidate), StringComparer.Ordinal)];
        }

        internal static TCandidate SelectWeighted(IReadOnlyList<KeyValuePair<TCandidate, double>> candidates,
            Func<double> nextDouble)
        {
            switch (candidates.Count)
            {
                case 0:
                    throw new ArgumentException("At least one weighted candidate is required.", nameof(candidates));
                case 1:
                    return candidates[0].Key;
            }

            var scale = candidates.Max(pair => pair.Value);
            var total = candidates.Sum(pair => pair.Value / scale);
            var roll = nextDouble();
            if (!double.IsFinite(roll) || roll is < 0d or >= 1d)
                throw new InvalidOperationException("The random source must return a finite value in [0, 1).");

            roll *= total;
            foreach (var pair in candidates)
            {
                roll -= pair.Value / scale;
                if (roll < 0d)
                    return pair.Key;
            }

            return candidates[^1].Key;
        }

        private sealed class Contribution(ActEnterPoolModeKind mode)
        {
            internal ActEnterPoolModeKind Mode { get; } = mode;
            internal Dictionary<TCandidate, Candidate> Candidates { get; } = [];
        }

        internal readonly record struct Candidate(
            TCandidate Value,
            Func<TContext, bool> Eligibility,
            Func<TContext, double> Weight);

        internal sealed class Snapshot(Candidate[] candidates)
        {
            internal TCandidate Select(TContext context, TCandidate baseline, Func<TCandidate, string> candidateKey,
                Func<double> nextDouble)
            {
                var weights = new Dictionary<TCandidate, double>();
                foreach (var candidate in candidates)
                {
                    if (EqualityComparer<TCandidate>.Default.Equals(candidate.Value, baseline) ||
                        !candidate.Eligibility(context))
                        continue;
                    var weight = candidate.Weight(context);
                    if (!double.IsFinite(weight) || weight <= 0d)
                        continue;
                    weights.TryGetValue(candidate.Value, out var existing);
                    weights[candidate.Value] = Math.Max(existing, weight);
                }

                if (weights.Count == 0)
                    return baseline;

                return SelectWeighted([
                    new(baseline, 1d),
                    .. weights.OrderBy(pair => candidateKey(pair.Key), StringComparer.Ordinal),
                ], nextDouble);
            }
        }
    }
}
