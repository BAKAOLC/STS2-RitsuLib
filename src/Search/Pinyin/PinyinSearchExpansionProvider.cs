using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Search.Pinyin
{
    internal sealed class PinyinSearchExpansionProvider : IRitsuSearchExpansionProvider
    {
        internal const string ProviderId = "ritsulib.pinyin";
        private const int MaximumVariantsPerRun = 24;
        private const int MaximumRunesPerRun = 64;

        public string Id => ProviderId;

        public string DisplayName =>
            ModSettingsLocalization.Get("ritsulib.searchProviders.pinyin.name", "Mandarin pinyin");

        public bool EnabledByDefault => false;

        public IReadOnlyList<RitsuSearchExpansion> Expand(
            string text,
            RitsuSearchExpansionContext context)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(context);
            var data = PinyinSearchDataManager.Data;
            if (data == null || text.Length == 0)
                return [];

            var expansions = new List<RitsuSearchExpansion>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var run = new List<string[]>();
            var combinedRuns = new List<string[]>();
            var runCount = 0;
            foreach (var rune in text.EnumerateRunes())
            {
                if (data.TryGetReadings(rune, out var readings))
                {
                    run.Add(readings);
                    combinedRuns.Add(readings);
                    if (run.Count == MaximumRunesPerRun)
                    {
                        FlushRun(run, expansions, seen);
                        runCount++;
                    }

                    if (combinedRuns.Count == MaximumRunesPerRun)
                        FlushRun(combinedRuns, expansions, seen);
                }
                else
                {
                    if (run.Count > 0)
                        runCount++;
                    FlushRun(run, expansions, seen);
                }
            }

            if (run.Count > 0)
                runCount++;
            FlushRun(run, expansions, seen);
            if (runCount > 1)
                FlushRun(combinedRuns, expansions, seen);
            return expansions;
        }

        private static void FlushRun(
            List<string[]> run,
            ICollection<RitsuSearchExpansion> expansions,
            ISet<string> seen)
        {
            if (run.Count == 0)
                return;
            List<Variant> variants = [new(string.Empty, string.Empty)];
            foreach (var readings in run)
            {
                var next = new List<Variant>(Math.Min(MaximumVariantsPerRun, variants.Count * readings.Length));
                foreach (var prefix in variants)
                foreach (var reading in readings)
                {
                    var value = new Variant(prefix.Full + reading, prefix.Initials + reading[0]);
                    if (!next.Contains(value))
                        next.Add(value);
                    if (next.Count == MaximumVariantsPerRun)
                        break;
                }

                variants = next;
            }

            for (var index = 0; index < variants.Count; index++)
            {
                var variant = variants[index];
                AddExpansion(
                    variant.Full,
                    index == 0
                        ? RitsuSearchExpansionKind.Transliteration
                        : RitsuSearchExpansionKind.AlternateReading,
                    expansions,
                    seen);
                AddExpansion(variant.Initials, RitsuSearchExpansionKind.Initialism, expansions, seen);
                if (variant.Full.Contains('v'))
                    AddExpansion(
                        variant.Full.Replace('v', 'u'),
                        index == 0
                            ? RitsuSearchExpansionKind.Transliteration
                            : RitsuSearchExpansionKind.AlternateReading,
                        expansions,
                        seen);
            }

            run.Clear();
        }

        private static void AddExpansion(
            string value,
            RitsuSearchExpansionKind kind,
            ICollection<RitsuSearchExpansion> expansions,
            ISet<string> seen)
        {
            if (value.Length == 0 || value.Length > RitsuSearchExpansion.MaximumTextLength || !seen.Add(value))
                return;
            expansions.Add(new(value, kind));
        }

        private sealed record Variant(string Full, string Initials);
    }
}
