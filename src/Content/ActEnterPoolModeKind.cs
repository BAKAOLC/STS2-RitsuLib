namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how eligible act candidates are selected when no forced candidate wins.
    ///     </para>
    ///     <para xml:lang="zh-CN">指定没有强制候选章节胜出时如何选择符合条件的章节。</para>
    /// </summary>
    public enum ActEnterPoolModeKind
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects uniformly from the act already occupying the slot and all eligible candidates.
        ///     </para>
        ///     <para xml:lang="zh-CN">在槽位中已有的章节与全部符合条件的候选章节之间进行均匀选择。</para>
        /// </summary>
        Uniform = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects by weight from eligible candidates and the optional baseline. Candidates with
        ///         non-positive weights are excluded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按权重从符合条件的候选章节与可选基线中选择。权重不大于零的候选章节会被排除。
        ///     </para>
        /// </summary>
        Weighted = 1,
    }
}
