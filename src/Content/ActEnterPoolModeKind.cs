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
        ///         Contributes weight 1 for eligible candidates. The act already occupying the slot always has weight 1.
        ///         Other mods may contribute weighted candidates to the same slot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为有效候选贡献权重 1。槽位中已有章节始终具有权重 1。其他模组可向同一槽位贡献加权候选。
        ///     </para>
        /// </summary>
        Uniform = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects by weight among eligible candidates and the existing act, whose weight is fixed at 1.
        ///         Candidates with non-finite or non-positive weights are excluded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在有效候选与已有章节之间按权重选择，已有章节的权重固定为 1。
        ///         非有限值或非正权重的候选被排除。
        ///     </para>
        /// </summary>
        Weighted = 1,
    }
}
