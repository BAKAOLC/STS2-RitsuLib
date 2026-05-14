namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Strongly-typed key used to store and retrieve values from <see cref="StorageContext" />.
    ///     用于在 <see cref="StorageContext" /> 中存储和检索值的强类型键。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Keys have a stable string identifier so they can be logged and compared safely across assemblies.
    ///     </para>
    ///     <para>
    ///         键具有稳定的字符串标识符，因此可以跨程序集安全记录和比较。
    ///     </para>
    /// </remarks>
    public sealed class StorageContextKey<TValue>(string id)
    {
        /// <summary>
        ///     Stable identifier for this context key.
        ///     此上下文键的稳定标识符。
        /// </summary>
        public string Id { get; } = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Context key id must not be empty.", nameof(id))
            : id.Trim();
    }
}
