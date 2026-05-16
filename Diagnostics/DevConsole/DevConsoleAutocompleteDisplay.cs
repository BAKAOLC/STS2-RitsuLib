namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     Formats dev-console autocomplete candidates with optional localized suffix labels.
    /// </summary>
    public static class DevConsoleAutocompleteDisplay
    {
        internal const string SuffixOpener = " (";

        /// <summary>
        ///     Appends <c> (localized-title)</c> to <paramref name="entryId" /> when a localized title exists.
        /// </summary>
        public static string FormatCandidate(string entryId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryId);

            var title = DevConsoleModelIdAutocompleteCatalog.TryGetLocalizedTitle(entryId);
            return string.IsNullOrWhiteSpace(title)
                ? entryId
                : $"{entryId}{SuffixOpener}{SanitizeSuffix(title)})";
        }

        /// <summary>
        ///     Strips a trailing localized suffix from a decorated autocomplete candidate.
        /// </summary>
        public static string StripLocalizedSuffix(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return candidate;

            var suffixStart = candidate.LastIndexOf(SuffixOpener, StringComparison.Ordinal);
            if (suffixStart < 0 || !candidate.EndsWith(')'))
                return candidate;

            return candidate[..suffixStart];
        }

        internal static string SanitizeSuffix(string title)
        {
            return title.Replace(')', '\uFF09').Trim();
        }

        internal static string ComputeCommonPrefix(IReadOnlyList<string> entryIds, string commandPrefix)
        {
            switch (entryIds.Count)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return commandPrefix + entryIds[0] + " ";
            }

            var minLength = entryIds.Min(static id => id.Length);
            var first = entryIds[0];
            var sharedLength = 0;

            for (var i = 0; i < minLength; i++)
            {
                var ch = first[i];
                if (entryIds.Any(id => char.ToLowerInvariant(id[i]) != char.ToLowerInvariant(ch)))
                    break;

                sharedLength = i + 1;
            }

            return sharedLength > 0
                ? commandPrefix + first[..sharedLength]
                : string.Empty;
        }
    }
}
