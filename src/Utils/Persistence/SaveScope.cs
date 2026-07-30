namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Defines the storage scope of saved data.</para>
    ///     <para xml:lang="zh-CN">定义存档数据的存储作用域。</para>
    /// </summary>
    public enum SaveScope
    {
        /// <summary>
        ///     <para xml:lang="en">Data is shared across all profiles.</para>
        ///     <para xml:lang="zh-CN">数据在所有档案之间共享。</para>
        /// </summary>
        Global,

        /// <summary>
        ///     <para xml:lang="en">Data is specific to one profile.</para>
        ///     <para xml:lang="zh-CN">数据专属于一个档案。</para>
        /// </summary>
        Profile,

        /// <summary>
        ///     <para xml:lang="en">Data is held only in memory and is not persisted.</para>
        ///     <para xml:lang="zh-CN">数据仅保留在内存中且不会持久化。</para>
        /// </summary>
        InMemory,
    }
}
