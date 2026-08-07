using STS2RitsuLib.Combat.SecondaryResources;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides autocomplete candidates and localized labels for registered secondary-resource IDs.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为已注册的次要资源 ID 提供自动补全候选项和本地化标签。
    ///     </para>
    /// </summary>
    public static class DevConsoleSecondaryResourceAutocompleteCatalog
    {
        /// <summary>
        ///     <para xml:lang="en">Returns registered secondary-resource IDs in deterministic order.</para>
        ///     <para xml:lang="zh-CN">按确定顺序返回已注册的次要资源 ID。</para>
        /// </summary>
        public static string[] GetResourceIds()
        {
            return
            [
                .. ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                    .Select(static definition => definition.Id)
                    .Order(StringComparer.OrdinalIgnoreCase),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends registered secondary-resource IDs to <paramref name="candidates" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将已注册的次要资源 ID 追加到 <paramref name="candidates" />。
        ///     </para>
        /// </summary>
        public static void AppendResourceIdCandidates(ICollection<string> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            foreach (var resourceId in GetResourceIds())
                candidates.Add(resourceId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a full resource ID or an unambiguous resource-local ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析完整资源 ID 或无歧义的资源本地 ID。
        ///     </para>
        /// </summary>
        public static bool TryResolveResource(
            string input,
            out SecondaryResourceDefinition definition)
        {
            definition = null!;
            var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(input).Trim();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (ModSecondaryResourceRegistry.TryGet(token, out definition))
                return true;

            var localMatches = ModSecondaryResourceRegistry.GetDefinitionsSnapshot()
                .Where(candidate => string.Equals(candidate.LocalId, token, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (localMatches.Length != 1)
                return false;

            definition = localMatches[0];
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the localized title for a registered resource ID, or <see langword="null" /> when
        ///         unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回已注册资源 ID 的本地化标题；无法取得时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        public static string? TryGetLocalizedTitle(string resourceId)
        {
            return TryResolveResource(resourceId, out var definition)
                ? TryGetLocalizedTitle(definition)
                : null;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the localized title for a registered resource, or <see langword="null" /> when unavailable.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回已注册资源的本地化标题；无法取得时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        public static string? TryGetLocalizedTitle(SecondaryResourceDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            try
            {
                return SecondaryResourceText.GetTitle(definition)?.GetFormattedText()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="partial" /> matches a resource ID, an unambiguous local ID, or a
        ///         localized title or one of its enabled search expansions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="partial" /> 是否匹配资源 ID、无歧义的本地 ID、本地化标题或已启用搜索扩展。
        ///     </para>
        /// </summary>
        public static bool MatchesResourceIdOrTitle(string resourceId, string partial)
        {
            if (!TryResolveResource(resourceId, out var definition))
                return false;

            var normalizedPartial = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(partial).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPartial))
                return true;

            var title = TryGetLocalizedTitle(definition);
            return definition.Id.StartsWith(normalizedPartial, StringComparison.OrdinalIgnoreCase) ||
                   definition.LocalId.StartsWith(normalizedPartial, StringComparison.OrdinalIgnoreCase) ||
                   DevConsoleAutocompleteMatchExtensions.MatchesLocalizedText(title, normalizedPartial);
        }
    }
}
