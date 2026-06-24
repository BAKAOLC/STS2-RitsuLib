namespace STS2RitsuLib.Updates
{
    internal readonly record struct SimpleSemanticVersion(
        IReadOnlyList<long> Numbers,
        IReadOnlyList<string> Prerelease
    ) : IComparable<SimpleSemanticVersion>
    {
        public int CompareTo(SimpleSemanticVersion other)
        {
            var max = Math.Max(Numbers.Count, other.Numbers.Count);
            for (var i = 0; i < max; i++)
            {
                var left = i < Numbers.Count ? Numbers[i] : 0L;
                var right = i < other.Numbers.Count ? other.Numbers[i] : 0L;
                var c = left.CompareTo(right);
                if (c != 0)
                    return c;
            }

            switch (Prerelease.Count)
            {
                case 0 when other.Prerelease.Count == 0:
                    return 0;
                case 0:
                    return 1;
            }

            if (other.Prerelease.Count == 0)
                return -1;

            max = Math.Max(Prerelease.Count, other.Prerelease.Count);
            for (var i = 0; i < max; i++)
            {
                if (i >= Prerelease.Count)
                    return -1;
                if (i >= other.Prerelease.Count)
                    return 1;

                var c = ComparePrereleaseIdentifier(Prerelease[i], other.Prerelease[i]);
                if (c != 0)
                    return c;
            }

            return 0;
        }

        public static bool TryParse(string text, out SimpleSemanticVersion version)
        {
            version = default;
            var normalized = text.Trim();
            if (normalized.Length == 0)
                return false;
            if (normalized[0] is 'v' or 'V')
                normalized = normalized[1..];

            var buildIndex = normalized.IndexOf('+', StringComparison.Ordinal);
            if (buildIndex >= 0)
                normalized = normalized[..buildIndex];

            var prerelease = Array.Empty<string>();
            var prereleaseIndex = normalized.IndexOf('-', StringComparison.Ordinal);
            if (prereleaseIndex >= 0)
            {
                var prereleaseText = normalized[(prereleaseIndex + 1)..];
                normalized = normalized[..prereleaseIndex];
                prerelease = prereleaseText
                    .Split(['.', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            var numberParts =
                normalized.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (numberParts.Length == 0)
                return false;

            var numbers = new long[numberParts.Length];
            for (var i = 0; i < numberParts.Length; i++)
            {
                if (!long.TryParse(numberParts[i], out var n) || n < 0)
                    return false;
                numbers[i] = n;
            }

            version = new(numbers, prerelease);
            return true;
        }

        private static int ComparePrereleaseIdentifier(string left, string right)
        {
            var leftNumeric = long.TryParse(left, out var leftNumber);
            var rightNumeric = long.TryParse(right, out var rightNumber);
            return leftNumeric switch
            {
                true when rightNumeric => leftNumber.CompareTo(rightNumber),
                true => -1,
                _ => rightNumeric ? 1 : string.Compare(left, right, StringComparison.OrdinalIgnoreCase),
            };
        }
    }
}
