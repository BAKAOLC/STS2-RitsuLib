namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     How eligible pool candidates are combined for an act slot when no force wins.
    ///     没有强制项胜出时，章节槽位的合格池候选项如何组合。
    /// </summary>
    public enum ActEnterPoolModeKind
    {
        /// <summary>
        ///     Uniform over
        ///     Uniform over
        ///     <c>
        ///         { act already in <see cref="MegaCrit.Sts2.Core.Runs.RunState.Acts" />[slot] } ∪ eligible
        ///         candidates
        ///     </c>
        ///     .
        ///     对以下集合做均匀抽取：
        ///     对以下集合做均匀抽取：
        ///     <c>
        ///         { <see cref="MegaCrit.Sts2.Core.Runs.RunState.Acts" />[slot] 中已有的章节 } ∪ 合格
        ///         候选项
        ///     </c>
        ///     。
        /// </summary>
        Uniform = 0,

        /// <summary>
        ///     Weighted draw over eligible candidates and optional baseline weight (see
        ///     <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" />). Acts with non-positive weight are
        ///     skipped.
        ///     在合格候选项和可选基线权重上进行加权抽取（参见
        ///     <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" />）。权重非正的章节会
        ///     被跳过。
        /// </summary>
        Weighted = 1,
    }
}
