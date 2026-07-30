using MegaCrit.Sts2.Core.DevConsole;

namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies registered developer-console autocomplete enhancements to match predicates and results.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将已注册的开发者控制台自动补全增强应用于匹配谓词和结果。
    ///     </para>
    /// </summary>
    public static class DevConsoleAutocompleteEnhancer
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a match-predicate chain for <paramref name="enhancements" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="enhancements" /> 构建匹配谓词链。
        ///     </para>
        /// </summary>
        public static Func<string, string, bool>? BuildMatchPredicate(
            DevConsoleAutocompleteEnhancements enhancements,
            Func<string, string, bool>? inner = null,
            IReadOnlyList<string>? completedArgs = null)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return inner;

            var predicate = inner;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.RitsuLibOwnedIdShorthandMatch) &&
                predicate == null)
                predicate = DevConsoleAutocompleteOwnedIdMatch.Match;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithLocalizedModelTitleMatch(predicate);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.PileNameLocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithLocalizedPileTitleMatch(predicate);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.SecondaryResourceLocalizedTitleMatch))
                predicate = DevConsoleAutocompleteMatchExtensions.WithSecondaryResourceLocalizedTitleMatch(predicate);

            if (!enhancements.HasFlag(DevConsoleAutocompleteEnhancements.AncientChoiceLocalizedTitleMatch))
                return predicate;
            var ancientEntryId = completedArgs is { Count: > 0 } ? completedArgs[0] : null;
            predicate = DevConsoleAutocompleteMatchExtensions.WithAncientChoiceLocalizedMatch(
                predicate,
                ancientEntryId);

            return predicate;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies result-side enhancements such as localized labels and duplicate removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用本地化标签、移除重复项等结果侧增强。
        ///     </para>
        /// </summary>
        public static void ApplyToResult(
            ref CompletionResult result,
            DevConsoleAutocompleteEnhancements enhancements,
            IReadOnlyList<string>? completedArgs = null)
        {
            if (enhancements == DevConsoleAutocompleteEnhancements.None)
                return;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.IncludeModPileCandidates))
                DevConsolePileNameAutocompleteCatalog.AppendModPileCandidates(result.Candidates);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.IncludeSecondaryResourceCandidates))
                DevConsoleSecondaryResourceAutocompleteCatalog.AppendResourceIdCandidates(result.Candidates);

            if (result.Candidates.Count == 0)
                return;

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.LocalizedDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplyLocalizedDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.AncientChoiceDisplayLabels) &&
                completedArgs is { Count: > 0 })
                DevConsoleAutocompleteMatchExtensions.ApplyAncientChoiceDisplayLabels(ref result, completedArgs[0]);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.PileNameDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplyPileDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.SecondaryResourceDisplayLabels))
                DevConsoleAutocompleteMatchExtensions.ApplySecondaryResourceDisplayLabels(ref result);

            if (enhancements.HasFlag(DevConsoleAutocompleteEnhancements.DeduplicateCandidates))
                result.Candidates =
                [
                    .. result.Candidates
                        .Distinct(StringComparer.OrdinalIgnoreCase),
                ];
        }
    }
}
