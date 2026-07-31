namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">Specifies when a run saved-data slot is written.</para>
    ///     <para xml:lang="zh-CN">指定何时写入局内保存数据槽位。</para>
    /// </summary>
    public enum RunSavedDataWritePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">Writes only values explicitly set or modified through the API.</para>
        ///     <para xml:lang="zh-CN">仅写入通过 API 显式设置或修改的值。</para>
        /// </summary>
        WhenSet,

        /// <summary>
        ///     <para xml:lang="en">Writes values that differ from a newly created default value.</para>
        ///     <para xml:lang="zh-CN">写入与新建默认值不同的值。</para>
        /// </summary>
        WhenNonDefault,

        /// <summary>
        ///     <para xml:lang="en">Writes the slot whenever it is registered and can be resolved for a run.</para>
        ///     <para xml:lang="zh-CN">只要槽位已注册且可为一局游戏解析，就写入此槽位。</para>
        /// </summary>
        AlwaysWhenRegistered,
    }
}
