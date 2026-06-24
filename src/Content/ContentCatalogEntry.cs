namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         One mod content catalog: global type set or act-scoped registrations, warm hooks, and merge
    ///         mode.
    ///     </para>
    ///     <para xml:lang="zh-CN">一条 mod 内容目录：全局类型集或 act 作用域注册、预热钩子与合并模式。</para>
    /// </summary>
    internal sealed class ContentCatalogEntry
    {
        internal required ContentCatalogId Id { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Registered types for global getters.</para>
        ///     <para xml:lang="zh-CN">全局 getter 的已注册类型。</para>
        /// </summary>
        internal Func<IEnumerable<Type>>? GlobalTypes { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Act type to registered model types.</para>
        ///     <para xml:lang="zh-CN">Act 类型到已注册模型类型的映射。</para>
        /// </summary>
        internal Func<Dictionary<Type, HashSet<Type>>>? ScopedRegistry { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Warm hook: global types to resolved model array.</para>
        ///     <para xml:lang="zh-CN">预热钩子：全局类型到已解析模型数组。</para>
        /// </summary>
        internal Func<IEnumerable<Type>, object>? WarmGlobal { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Warm hook: scoped registry to per-act arrays.</para>
        ///     <para xml:lang="zh-CN">预热钩子：作用域注册表到各 act 数组。</para>
        /// </summary>
        internal Func<Dictionary<Type, HashSet<Type>>, Dictionary<Type, object>>? WarmScoped { get; init; }

        internal ContentMergeMode MergeMode { get; init; } = ContentMergeMode.AppendDistinctById;

        internal bool IsScoped => ScopedRegistry != null;
    }
}
