namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Formats developer-console autocomplete candidates with optional localized suffix labels.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为开发者控制台的自动补全候选项附加可选的本地化后缀标签。
    ///     </para>
    /// </summary>
    public static class DevConsoleAutocompleteDisplay
    {
        internal const string SuffixOpener = " (";

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <c> (localized-title)</c> to <paramref name="entryId" /> when its localized title is
        ///         available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若 <paramref name="entryId" /> 有本地化标题，则为其附加 <c> (localized-title)</c>。
        ///     </para>
        /// </summary>
        public static string FormatCandidate(string entryId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryId);

            var title = DevConsoleModelIdAutocompleteCatalog.TryGetLocalizedTitle(entryId);
            return FormatWithTitle(entryId, title);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <c> (localized-title)</c> to <paramref name="entryId" /> when
        ///         <paramref name="localizedTitle" /> is provided.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若提供了 <paramref name="localizedTitle" />，则为 <paramref name="entryId" /> 附加
        ///         <c> (localized-title)</c>。
        ///     </para>
        /// </summary>
        public static string FormatCandidate(string entryId, string? localizedTitle)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
            return FormatWithTitle(entryId, localizedTitle);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <c> (localized-title)</c> to an ancient-event option token when a display title is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若先古之民事件选项令牌有显示标题，则为其附加 <c> (localized-title)</c>。
        ///     </para>
        /// </summary>
        public static string FormatAncientChoiceCandidate(string ancientEntryId, string choiceToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ancientEntryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(choiceToken);

            var title = DevConsoleAncientChoiceAutocompleteCatalog.TryGetDisplayTitle(ancientEntryId, choiceToken);
            return FormatWithTitle(choiceToken, title);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <c> (localized-title)</c> to a pile token when its localized title is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若牌堆令牌有本地化标题，则为其附加 <c> (localized-title)</c>。
        ///     </para>
        /// </summary>
        public static string FormatPileCandidate(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            var title = DevConsolePileNameAutocompleteCatalog.TryGetLocalizedTitle(token);
            return FormatWithTitle(token, title);
        }

        private static string FormatWithTitle(string token, string? title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? token
                : $"{token}{SuffixOpener}{SanitizeSuffix(title)})";
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes a trailing localized suffix from a decorated autocomplete candidate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除已装饰自动补全候选项末尾的本地化后缀。
        ///     </para>
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
