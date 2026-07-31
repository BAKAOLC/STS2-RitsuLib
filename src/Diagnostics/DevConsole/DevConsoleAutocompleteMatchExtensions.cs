using MegaCrit.Sts2.Core.DevConsole;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides low-level helpers for developer-console autocomplete predicates and display formatting.
    ///         Prefer <see cref="DevConsoleAutocomplete" /> for registration and argument-slot resolution.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供开发者控制台自动补全谓词和显示格式的底层辅助方法。注册和解析参数位置时应优先使用
    ///         <see cref="DevConsoleAutocomplete" />。
    ///     </para>
    /// </summary>
    public static class DevConsoleAutocompleteMatchExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Extends <paramref name="inner" /> with localized-title matching for model entry IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="inner" /> 基础上添加模型条目 ID 的本地化标题匹配。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool> WithLocalizedModelTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            var titles = DevConsoleModelIdAutocompleteCatalog.GetTitlesSnapshot();
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var entryId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return titles.TryGetValue(entryId, out var title) &&
                       title.Contains(partial.Trim(), StringComparison.OrdinalIgnoreCase);
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Decorates completion candidates with localized suffix labels and recomputes
        ///         <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" /> from undecorated IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为补全候选项添加本地化后缀标签，并根据未装饰的 ID 重新计算
        ///         <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" />。
        ///     </para>
        /// </summary>
        public static void ApplyLocalizedDisplayLabels(
            ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var titles = DevConsoleModelIdAutocompleteCatalog.GetTitlesSnapshot();
            var entryIds = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates =
            [
                .. entryIds
                    .Select(entryId => DevConsoleAutocompleteDisplay.FormatCandidate(
                        entryId,
                        titles.GetValueOrDefault(entryId))),
            ];

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(entryIds, result.CommandPrefix);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Extends <paramref name="inner" /> with localized pile-title matching.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="inner" /> 基础上添加本地化牌堆标题匹配。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool> WithLocalizedPileTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            var titles = DevConsolePileNameAutocompleteCatalog.GetTitlesSnapshot();
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return titles.TryGetValue(token, out var title) &&
                       title.Contains(partial.Trim(), StringComparison.OrdinalIgnoreCase);
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Extends <paramref name="inner" /> with localized secondary-resource title matching and
        ///         unambiguous local-ID matching.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="inner" /> 基础上添加次要资源的本地化标题匹配和无歧义本地 ID 匹配。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool> WithSecondaryResourceLocalizedTitleMatch(
            Func<string, string, bool>? inner = null)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                var resourceId = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsoleSecondaryResourceAutocompleteCatalog.MatchesResourceIdOrTitle(resourceId, partial);
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Extends <paramref name="inner" /> with localized ancient-event option matching for a resolved ancient-event ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <paramref name="inner" /> 基础上，为已解析的先古之民事件 ID 添加本地化选项匹配。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool> WithAncientChoiceLocalizedMatch(
            Func<string, string, bool>? inner,
            string? ancientEntryId)
        {
            var baseMatch = inner ?? DefaultPrefixMatch;
            return (candidate, partial) =>
            {
                if (baseMatch(candidate, partial))
                    return true;

                if (string.IsNullOrWhiteSpace(ancientEntryId))
                    return false;

                var token = DevConsoleAutocompleteDisplay.StripLocalizedSuffix(candidate);
                return DevConsoleAncientChoiceAutocompleteCatalog.MatchesLocalizedTitle(
                    ancientEntryId,
                    token,
                    partial);
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Decorates <c>ancient</c> choice candidates with localized option or relic titles.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <c>ancient</c> 的选项候选项添加本地化选项或遗物标题。
        ///     </para>
        /// </summary>
        public static void ApplyAncientChoiceDisplayLabels(
            ref CompletionResult result,
            string ancientEntryId)
        {
            if (result.Candidates.Count == 0)
                return;

            var tokens = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates =
            [
                .. tokens
                    .Select(token => DevConsoleAutocompleteDisplay.FormatAncientChoiceCandidate(ancientEntryId, token)),
            ];

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(tokens, result.CommandPrefix);
        }

        /// <summary>
        ///     <para xml:lang="en">Decorates pile-argument candidates with localized suffix labels.</para>
        ///     <para xml:lang="zh-CN">为牌堆参数候选项添加本地化后缀标签。</para>
        /// </summary>
        public static void ApplyPileDisplayLabels(ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var titles = DevConsolePileNameAutocompleteCatalog.GetTitlesSnapshot();
            var tokens = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates =
            [
                .. tokens
                    .Select(token => DevConsoleAutocompleteDisplay.FormatCandidate(
                        token,
                        titles.GetValueOrDefault(token))),
            ];

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(tokens, result.CommandPrefix);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Decorates secondary-resource ID candidates with localized suffix labels.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为次要资源 ID 候选项添加本地化后缀标签。
        ///     </para>
        /// </summary>
        public static void ApplySecondaryResourceDisplayLabels(ref CompletionResult result)
        {
            if (result.Candidates.Count == 0)
                return;

            var resourceIds = result.Candidates
                .Select(DevConsoleAutocompleteDisplay.StripLocalizedSuffix)
                .ToList();

            result.Candidates =
            [
                .. resourceIds
                    .Select(resourceId => DevConsoleAutocompleteDisplay.FormatCandidate(
                        resourceId,
                        DevConsoleSecondaryResourceAutocompleteCatalog.TryGetLocalizedTitle(resourceId))),
            ];

            result.CommonPrefix = DevConsoleAutocompleteDisplay.ComputeCommonPrefix(resourceIds, result.CommandPrefix);
        }

        private static bool DefaultPrefixMatch(string candidate, string partial)
        {
            return candidate.StartsWith(partial, StringComparison.OrdinalIgnoreCase);
        }
    }
}
