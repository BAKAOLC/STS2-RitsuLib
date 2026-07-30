namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     <para xml:lang="en">Represents a strongly typed key used to store and retrieve a value in <see cref="StorageContext" />.</para>
    ///     <para xml:lang="zh-CN">表示在 <see cref="StorageContext" /> 中存取值时使用的强类型键。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Each key has a stable string identifier for cross-assembly lookup and diagnostics.</para>
    ///     <para xml:lang="zh-CN">每个键都有稳定的字符串标识符，可用于跨程序集查找和诊断。</para>
    /// </remarks>
    // ReSharper disable once UnusedTypeParameter
    public sealed class StorageContextKey<TValue>(string id)
    {
        /// <summary>
        ///     <para xml:lang="en">Stable identifier for this context key.</para>
        ///     <para xml:lang="zh-CN">此上下文键的稳定标识符。</para>
        /// </summary>
        public string Id { get; } = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Context key id must not be empty.", nameof(id))
            : id.Trim();
    }
}
