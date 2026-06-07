namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Persistence scope for a secondary combat resource.
    ///     次级战斗资源的持久化范围。
    /// </summary>
    public enum SecondaryResourcePersistencePolicy
    {
        /// <summary>
        ///     The resource is runtime-only and is not written to run saves.
        ///     该资源仅存在于运行时，不写入跑局存档。
        /// </summary>
        None = 0,

        /// <summary>
        ///     The resource should be restored while the current combat is restored.
        ///     该资源应随当前战斗恢复。
        /// </summary>
        Combat = 1,

        /// <summary>
        ///     The resource persists across combats for the current run.
        ///     该资源在当前跑局中跨战斗持久化。
        /// </summary>
        Run = 2,
    }

    /// <summary>
    ///     Built-in turn-start behavior for a secondary resource.
    ///     次级资源的内建回合开始行为。
    /// </summary>
    public enum SecondaryResourceTurnStartPolicy
    {
        /// <summary>
        ///     Leave the current amount unchanged.
        ///     保持当前数量不变。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Set the current amount to the hook-modified max amount.
        ///     将当前数量设为经过 hook 修正后的最大数量。
        /// </summary>
        ResetToMax = 1,

        /// <summary>
        ///     Add the hook-modified max amount to the current amount.
        ///     将经过 hook 修正后的最大数量加到当前数量上。
        /// </summary>
        AddMaxToCurrent = 2,

        /// <summary>
        ///     Set the current amount to zero.
        ///     将当前数量设为零。
        /// </summary>
        Clear = 3,
    }

    /// <summary>
    ///     Reason attached to a secondary resource amount mutation.
    ///     附加在次级资源数量变更上的原因。
    /// </summary>
    public enum SecondaryResourceChangeReason
    {
        /// <summary>
        ///     Unspecified or custom reason.
        ///     未指定或自定义原因。
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Resource amount increased.
        ///     资源数量增加。
        /// </summary>
        Gain = 1,

        /// <summary>
        ///     Resource amount decreased without payment semantics.
        ///     资源数量减少，但不带支付语义。
        /// </summary>
        Lose = 2,

        /// <summary>
        ///     Resource amount was assigned directly.
        ///     资源数量被直接赋值。
        /// </summary>
        Set = 3,

        /// <summary>
        ///     Resource amount was spent as payment.
        ///     资源数量作为支付被消耗。
        /// </summary>
        Spend = 4,

        /// <summary>
        ///     Resource amount was reset.
        ///     资源数量被重置。
        /// </summary>
        Reset = 5,

        /// <summary>
        ///     Resource amount changed from a turn-start policy.
        ///     资源数量因回合开始策略而改变。
        /// </summary>
        TurnStart = 6,
    }
}
