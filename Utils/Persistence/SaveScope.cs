namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Defines the scope of save data storage
    ///     定义保存数据存储的作用域。
    /// </summary>
    public enum SaveScope
    {
        /// <summary>
        ///     Global scope - data is shared across all profiles
        ///     全局作用域 - 数据在所有档案之间共享。
        /// </summary>
        Global,

        /// <summary>
        ///     Profile scope - data is specific to the current profile
        ///     档案作用域 - 数据专属于当前档案。
        /// </summary>
        Profile,

        /// <summary>
        ///     In-memory scope (not persisted).
        ///     内存作用域（不持久化）。
        /// </summary>
        InMemory,
    }
}
