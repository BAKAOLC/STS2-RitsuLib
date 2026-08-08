using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace STS2RitsuLib.Search.Pinyin
{
    internal static class PinyinSearchDataCompiler
    {
        private const int MaximumArchiveEntries = 64;
        private const long MaximumArchiveUncompressedBytes = 128L * 1024 * 1024;
        private const int MaximumInputLineLength = 8192;
        private const int MaximumReadingsPerCodePoint = 32;

        private static readonly HashSet<string> SupportedProperties =
        [
            "kMandarin",
            "kHanyuPinyin",
            "kXHC1983",
        ];

        internal static IReadOnlyDictionary<int, string[]> Compile(string archivePath)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count is 0 or > MaximumArchiveEntries ||
                archive.Entries.Sum(static entry => entry.Length) > MaximumArchiveUncompressedBytes)
                throw new InvalidDataException("The Unihan archive has invalid resource bounds.");

            var entries = new Dictionary<int, ReadingAccumulator>();
            foreach (var entry in archive.Entries)
            {
                if (!entry.Name.StartsWith("Unihan_", StringComparison.Ordinal) ||
                    !entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    continue;
                ParseEntry(entry, entries);
            }

            var result = new Dictionary<int, string[]>(entries.Count);
            foreach (var (codePoint, accumulator) in entries)
            {
                var readings = accumulator.Build();
                if (readings.Length > 0)
                    result.Add(codePoint, readings);
            }

            if (result.Count == 0)
                throw new InvalidDataException("The Unihan archive did not contain supported Mandarin readings.");
            return result;
        }

        private static void ParseEntry(ZipArchiveEntry entry, IDictionary<int, ReadingAccumulator> entries)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 64 * 1024, false);
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;
                if (line.Length > MaximumInputLineLength)
                    throw new InvalidDataException("The Unihan archive contains an overlong input line.");
                var parts = line.Split('\t');
                if (parts.Length != 3 || !SupportedProperties.Contains(parts[1]))
                    continue;
                if (!parts[0].StartsWith("U+", StringComparison.Ordinal) ||
                    !int.TryParse(parts[0].AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                        out var codePoint) ||
                    !Rune.IsValid(codePoint))
                    throw new InvalidDataException("The Unihan archive contains an invalid code point.");

                if (!entries.TryGetValue(codePoint, out var accumulator))
                {
                    accumulator = new();
                    entries.Add(codePoint, accumulator);
                }

                AddPropertyReadings(accumulator, parts[1], parts[2]);
            }
        }

        private static void AddPropertyReadings(ReadingAccumulator accumulator, string property, string value)
        {
            if (property == "kMandarin")
            {
                var first = true;
                foreach (var reading in value.Split((char[]?)null,
                             StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    accumulator.Add(NormalizeReading(reading), first);
                    first = false;
                }

                return;
            }

            foreach (var field in value.Split((char[]?)null,
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = field.IndexOf(':');
                if (separator < 0 || separator == field.Length - 1)
                    continue;
                foreach (var reading in field[(separator + 1)..].Split(',',
                             StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    accumulator.Add(NormalizeReading(reading), false);
            }
        }

        private static string NormalizeReading(string value)
        {
            var prepared = value
                .Replace('ü', 'v')
                .Replace('ǖ', 'v')
                .Replace('ǘ', 'v')
                .Replace('ǚ', 'v')
                .Replace('ǜ', 'v')
                .Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(prepared.Length);
            foreach (var character in prepared)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;
                var lower = char.ToLowerInvariant(character);
                if (lower is >= 'a' and <= 'z')
                    builder.Append(lower);
            }

            return builder.ToString();
        }

        private sealed class ReadingAccumulator
        {
            private readonly List<string> _readings = [];
            private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
            private string? _primary;

            internal void Add(string value, bool preferred)
            {
                if (value.Length is 0 or > 24)
                    return;
                if (preferred && _primary == null)
                    _primary = value;
                if (!_seen.Add(value))
                    return;
                if (_readings.Count < MaximumReadingsPerCodePoint)
                    _readings.Add(value);
            }

            internal string[] Build()
            {
                if (_readings.Count == 0)
                    return [];
                var primary = _primary ?? _readings[0];
                return
                [
                    primary,
                    .. _readings
                        .Where(reading => !string.Equals(reading, primary, StringComparison.Ordinal))
                        .Take(MaximumReadingsPerCodePoint - 1),
                ];
            }
        }
    }
}
