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

        public static bool TryParse(string? text, out SimpleSemanticVersion version)
        {
            version = default;
            if (text == null)
                return false;

            var normalized = text.Trim();
            if (normalized.Length == 0)
                return false;
            if (normalized[0] is 'v' or 'V')
                normalized = normalized[1..];

            var buildIndex = normalized.IndexOf('+', StringComparison.Ordinal);
            if (buildIndex >= 0)
            {
                var build = normalized[(buildIndex + 1)..];
                if (!IsValidIdentifierList(build))
                    return false;
                normalized = normalized[..buildIndex];
            }

            var prerelease = Array.Empty<string>();
            var prereleaseIndex = normalized.IndexOf('-', StringComparison.Ordinal);
            if (prereleaseIndex >= 0)
            {
                var prereleaseText = normalized[(prereleaseIndex + 1)..];
                normalized = normalized[..prereleaseIndex];
                if (!IsValidIdentifierList(prereleaseText))
                    return false;
                prerelease = prereleaseText.Split('.');
            }

            var numberParts = normalized.Split('.');
            if (numberParts.Length == 0)
                return false;

            var numbers = new long[numberParts.Length];
            for (var i = 0; i < numberParts.Length; i++)
            {
                if (numberParts[i].Length == 0 ||
                    !numberParts[i].All(char.IsAsciiDigit) ||
                    !long.TryParse(numberParts[i], out var n))
                    return false;
                numbers[i] = n;
            }

            version = new(numbers, prerelease);
            return true;
        }

        private static bool IsValidIdentifierList(string text)
        {
            if (text.Length == 0)
                return false;

            return text.Split('.').All(static identifier =>
                identifier.Length > 0 &&
                identifier.All(static character =>
                    char.IsAsciiLetterOrDigit(character) || character == '-'));
        }

        private static int ComparePrereleaseIdentifier(string left, string right)
        {
            var leftNumeric = left.All(char.IsAsciiDigit);
            var rightNumeric = right.All(char.IsAsciiDigit);
            return leftNumeric switch
            {
                true when rightNumeric => CompareNumericIdentifiers(left, right),
                true => -1,
                _ => rightNumeric ? 1 : string.Compare(left, right, StringComparison.Ordinal),
            };
        }

        private static int CompareNumericIdentifiers(string left, string right)
        {
            var normalizedLeft = left.TrimStart('0');
            var normalizedRight = right.TrimStart('0');
            if (normalizedLeft.Length == 0)
                normalizedLeft = "0";
            if (normalizedRight.Length == 0)
                normalizedRight = "0";

            var lengthComparison = normalizedLeft.Length.CompareTo(normalizedRight.Length);
            return lengthComparison != 0
                ? lengthComparison
                : string.Compare(normalizedLeft, normalizedRight, StringComparison.Ordinal);
        }
    }
}
