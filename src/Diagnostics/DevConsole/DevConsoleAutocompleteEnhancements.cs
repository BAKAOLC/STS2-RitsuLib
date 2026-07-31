namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Developer-console autocomplete behaviors that can be bound to individual command arguments.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可分别绑定到各命令参数的开发者控制台自动补全行为。
    ///     </para>
    /// </summary>
    [Flags]
    public enum DevConsoleAutocompleteEnhancements
    {
        /// <summary>
        ///     <para xml:lang="en">No enhancements.</para>
        ///     <para xml:lang="zh-CN">不启用增强。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allows candidates to be matched by localized title text as well as by entry-ID prefix.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         除条目 ID 前缀外，还允许按本地化标题文本匹配候选项。
        ///     </para>
        /// </summary>
        LocalizedTitleMatch = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends <c> (localized-title)</c> to displayed candidates while keeping
        ///         <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" /> free of display labels.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为显示的候选项附加 <c> (localized-title)</c>，同时使
        ///         <see cref="MegaCrit.Sts2.Core.DevConsole.CompletionResult.CommonPrefix" /> 不含显示标签。
        ///     </para>
        /// </summary>
        LocalizedDisplayLabels = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enables suffix shorthand matching for RitsuLib-registered mod entry IDs when no custom matcher is
        ///         supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         未提供自定义匹配器时，允许使用 RitsuLib 所注册模组条目 ID 的尾部简写进行匹配。
        ///     </para>
        /// </summary>
        RitsuLibOwnedIdShorthandMatch = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">Removes duplicate candidates while preserving their order.</para>
        ///     <para xml:lang="zh-CN">在保留顺序的同时移除重复候选项。</para>
        /// </summary>
        DeduplicateCandidates = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">Adds registered mod pile IDs to pile-argument candidates.</para>
        ///     <para xml:lang="zh-CN">将已注册的模组牌堆 ID 添加到牌堆参数候选项中。</para>
        /// </summary>
        IncludeModPileCandidates = 1 << 4,

        /// <summary>
        ///     <para xml:lang="en">Allows pile tokens to be matched by localized title text.</para>
        ///     <para xml:lang="zh-CN">允许按本地化标题文本匹配牌堆令牌。</para>
        /// </summary>
        PileNameLocalizedTitleMatch = 1 << 5,

        /// <summary>
        ///     <para xml:lang="en">Appends localized pile titles in parentheses to pile-argument candidates.</para>
        ///     <para xml:lang="zh-CN">在牌堆参数候选项后以括号附加本地化牌堆标题。</para>
        /// </summary>
        PileNameDisplayLabels = 1 << 6,

        /// <summary>
        ///     <para xml:lang="en">Enables localized-title matching and display labels for model entry IDs.</para>
        ///     <para xml:lang="zh-CN">为模型条目 ID 启用本地化标题匹配和显示标签。</para>
        /// </summary>
        ModelEntryId = LocalizedTitleMatch | LocalizedDisplayLabels | DeduplicateCandidates,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Combines <see cref="ModelEntryId" /> with shorthand matching for RitsuLib-owned IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="ModelEntryId" /> 的基础上启用 RitsuLib 所注册 ID 的简写匹配。
        ///     </para>
        /// </summary>
        RitsuLibModEntryId = ModelEntryId | RitsuLibOwnedIdShorthandMatch,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enables complete pile-name autocomplete: mod pile IDs, localized matching and labels,
        ///         RitsuLib-owned ID shorthand, and duplicate removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         启用完整的牌堆名称自动补全：模组牌堆 ID、本地化匹配和标签、RitsuLib 所注册 ID 的简写及去重。
        ///     </para>
        /// </summary>
        PileName = IncludeModPileCandidates |
                   PileNameLocalizedTitleMatch |
                   PileNameDisplayLabels |
                   DeduplicateCandidates |
                   RitsuLibOwnedIdShorthandMatch,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allows ancient-event option tokens to be matched by an option or relic's localized title, or by the
        ///         relic's entry ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         允许按选项或遗物的本地化标题以及遗物条目 ID 匹配先古之民事件选项令牌。
        ///     </para>
        /// </summary>
        AncientChoiceLocalizedTitleMatch = 1 << 7,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends localized option or relic titles to second-argument candidates of <c>ancient</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <c>ancient</c> 的第二个参数候选项附加本地化选项或遗物标题。
        ///     </para>
        /// </summary>
        AncientChoiceDisplayLabels = 1 << 8,

        /// <summary>
        ///     <para xml:lang="en">Adds registered secondary-resource IDs to candidate lists.</para>
        ///     <para xml:lang="zh-CN">将已注册的次要资源 ID 添加到候选列表中。</para>
        /// </summary>
        IncludeSecondaryResourceCandidates = 1 << 9,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allows secondary-resource IDs to be matched by localized title text or an unambiguous local ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         允许按本地化标题文本或无歧义的本地 ID 匹配次要资源 ID。
        ///     </para>
        /// </summary>
        SecondaryResourceLocalizedTitleMatch = 1 << 10,

        /// <summary>
        ///     <para xml:lang="en">Appends localized secondary-resource titles in parentheses.</para>
        ///     <para xml:lang="zh-CN">以括号附加本地化次要资源标题。</para>
        /// </summary>
        SecondaryResourceDisplayLabels = 1 << 11,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enables localized matching and display labels for <c>ancient</c> choice arguments, which often
        ///         represent relic rewards.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <c>ancient</c> 的选项参数启用本地化匹配和显示标签；这些选项通常代表遗物奖励。
        ///     </para>
        /// </summary>
        AncientChoice = AncientChoiceLocalizedTitleMatch |
                        AncientChoiceDisplayLabels |
                        DeduplicateCandidates,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enables complete secondary-resource ID autocomplete: registered IDs, localized matching and labels,
        ///         unambiguous local-ID matching, and duplicate removal.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         启用完整的次要资源 ID 自动补全：已注册 ID、本地化匹配和标签、无歧义的本地 ID 匹配及去重。
        ///     </para>
        /// </summary>
        SecondaryResourceId = IncludeSecondaryResourceCandidates |
                              SecondaryResourceLocalizedTitleMatch |
                              SecondaryResourceDisplayLabels |
                              DeduplicateCandidates,
    }
}
