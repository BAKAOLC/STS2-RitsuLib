using System.Text;

namespace STS2RitsuLib.Search.Pinyin
{
    internal sealed class PinyinSearchData
    {
        private const uint FileMagic = 0x59505352;
        private const int FileFormatVersion = 1;
        private const int MaximumCodePointCount = 200_000;
        private const int MaximumFileBytes = 64 * 1024 * 1024;
        private const int MaximumReadingsPerCodePoint = 32;
        private const int MaximumReadingBytes = 24;
        private readonly IReadOnlyDictionary<int, string[]> _readings;

        internal PinyinSearchData(IReadOnlyDictionary<int, string[]> readings)
        {
            _readings = readings;
        }

        internal int Count => _readings.Count;

        internal bool TryGetReadings(Rune rune, out string[] readings)
        {
            return _readings.TryGetValue(rune.Value, out readings!);
        }

        internal static PinyinSearchData Load(string path, PinyinSearchDataSource source)
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length is <= 0 or > MaximumFileBytes)
                throw new InvalidDataException("The cached pinyin data file has an invalid size.");

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.SequentialScan);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadUInt32() != FileMagic)
                throw new InvalidDataException("The cached pinyin data file has an invalid signature.");
            if (reader.ReadInt32() != FileFormatVersion)
                throw new InvalidDataException("The cached pinyin data format is not supported.");
            var unicodeVersion = ReadAscii(reader, 32);
            if (!string.Equals(unicodeVersion, source.UnicodeVersion, StringComparison.Ordinal))
                throw new InvalidDataException("The cached pinyin data targets a different Unicode version.");
            var expectedSourceHash = Convert.FromHexString(source.ExpectedSha256);
            var sourceHash = reader.ReadBytes(expectedSourceHash.Length);
            if (sourceHash.Length != expectedSourceHash.Length ||
                !sourceHash.AsSpan().SequenceEqual(expectedSourceHash))
                throw new InvalidDataException("The cached pinyin data was generated from an unexpected source.");

            var count = reader.ReadInt32();
            if (count is <= 0 or > MaximumCodePointCount)
                throw new InvalidDataException("The cached pinyin data contains an invalid entry count.");
            var readingsByCodePoint = new Dictionary<int, string[]>(count);
            for (var index = 0; index < count; index++)
            {
                var codePoint = reader.ReadInt32();
                if (!Rune.IsValid(codePoint) || readingsByCodePoint.ContainsKey(codePoint))
                    throw new InvalidDataException("The cached pinyin data contains an invalid code point.");
                var readingCount = reader.ReadByte();
                if (readingCount is 0 or > MaximumReadingsPerCodePoint)
                    throw new InvalidDataException("The cached pinyin data contains an invalid reading count.");
                var readings = new string[readingCount];
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var readingIndex = 0; readingIndex < readingCount; readingIndex++)
                {
                    var reading = ReadAscii(reader, MaximumReadingBytes);
                    if (!IsValidReading(reading) || !seen.Add(reading))
                        throw new InvalidDataException("The cached pinyin data contains an invalid reading.");
                    readings[readingIndex] = reading;
                }

                readingsByCodePoint.Add(codePoint, readings);
            }

            if (stream.Position != stream.Length)
                throw new InvalidDataException("The cached pinyin data contains trailing bytes.");
            return new(readingsByCodePoint);
        }

        internal static void Write(
            string path,
            PinyinSearchDataSource source,
            IReadOnlyDictionary<int, string[]> readings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(readings);
            if (readings.Count is <= 0 or > MaximumCodePointCount)
                throw new InvalidDataException("The generated pinyin data has an invalid entry count.");

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024,
                FileOptions.SequentialScan | FileOptions.WriteThrough);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(FileMagic);
            writer.Write(FileFormatVersion);
            WriteAscii(writer, source.UnicodeVersion, 32);
            writer.Write(Convert.FromHexString(source.ExpectedSha256));
            writer.Write(readings.Count);
            foreach (var (codePoint, values) in readings.OrderBy(static pair => pair.Key))
            {
                if (!Rune.IsValid(codePoint) || values.Length is 0 or > MaximumReadingsPerCodePoint)
                    throw new InvalidDataException("The generated pinyin data contains an invalid entry.");
                writer.Write(codePoint);
                writer.Write((byte)values.Length);
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var value in values)
                {
                    if (!IsValidReading(value) || !seen.Add(value))
                        throw new InvalidDataException("The generated pinyin data contains an invalid reading.");
                    WriteAscii(writer, value, MaximumReadingBytes);
                }
            }

            writer.Flush();
            stream.Flush(true);
        }

        private static string ReadAscii(BinaryReader reader, int maximumBytes)
        {
            var length = reader.ReadByte();
            if (length == 0 || length > maximumBytes)
                throw new InvalidDataException("The cached pinyin data contains an invalid string length.");
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length || bytes.Any(static value => value > 0x7f))
                throw new InvalidDataException("The cached pinyin data contains invalid ASCII text.");
            return Encoding.ASCII.GetString(bytes);
        }

        private static void WriteAscii(BinaryWriter writer, string value, int maximumBytes)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            if (bytes.Length is 0 or > byte.MaxValue || bytes.Length > maximumBytes)
                throw new InvalidDataException("The generated pinyin data contains an invalid ASCII string.");
            writer.Write((byte)bytes.Length);
            writer.Write(bytes);
        }

        private static bool IsValidReading(string value)
        {
            return value.Length is > 0 and <= MaximumReadingBytes &&
                   value.All(static character => character is >= 'a' and <= 'z');
        }
    }
}
