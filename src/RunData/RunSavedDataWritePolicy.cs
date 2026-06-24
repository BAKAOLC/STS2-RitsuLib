namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Write policy for a slot.
    ///     槽位写入策略。
    /// </summary>
    public enum RunSavedDataWritePolicy
    {
        /// <summary>
        ///     Writes only values explicitly set or modified through the API.
        ///     仅写入通过 API 显式设置或修改过的值。
        /// </summary>
        WhenSet,

        /// <summary>
        ///     Writes values that differ from a fresh default instance.
        ///     写入与全新默认实例不同的值。
        /// </summary>
        WhenNonDefault,

        /// <summary>
        ///     Writes the slot whenever it is registered and can be resolved for a run.
        ///     只要槽位已注册且可为跑局解析就写入。
        /// </summary>
        AlwaysWhenRegistered,
    }
}
