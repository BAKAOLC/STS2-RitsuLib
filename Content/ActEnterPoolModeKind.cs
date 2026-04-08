namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     How eligible pool candidates are combined for an act slot when no force wins.
    /// </summary>
    public enum ActEnterPoolModeKind
    {
        /// <summary>
        ///     Uniform over
        ///     <c>
        ///         { act already in <see cref="MegaCrit.Sts2.Core.Runs.RunState.Acts" />[slot] } ∪ eligible
        ///         candidates
        ///     </c>
        ///     .
        /// </summary>
        Uniform = 0,

        /// <summary>
        ///     Weighted draw over eligible candidates and optional baseline weight (see
        ///     <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" />). Acts with non-positive weight are
        ///     skipped.
        /// </summary>
        Weighted = 1,
    }
}
