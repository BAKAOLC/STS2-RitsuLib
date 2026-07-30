namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one mod-content catalog and its model resolution and merge behavior.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述一个模组内容目录及其模型解析与合并行为。</para>
    /// </summary>
    internal sealed class ContentCatalogEntry
    {
        internal required ContentCatalogId Id { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered model types for a global catalog.</para>
        ///     <para xml:lang="zh-CN">获取全局目录中已注册的模型类型。</para>
        /// </summary>
        internal Func<IEnumerable<Type>>? GlobalTypes { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the act-type-to-model-types registry for an act-scoped catalog.</para>
        ///     <para xml:lang="zh-CN">获取章节作用域目录中章节类型到模型类型的注册表。</para>
        /// </summary>
        internal Func<Dictionary<Type, HashSet<Type>>>? ScopedRegistry { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the resolver that warms a global catalog from its registered types.</para>
        ///     <para xml:lang="zh-CN">获取根据已注册类型预热全局目录的解析器。</para>
        /// </summary>
        internal Func<IEnumerable<Type>, object>? WarmGlobal { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the resolver that warms an act-scoped catalog.</para>
        ///     <para xml:lang="zh-CN">获取用于预热章节作用域目录的解析器。</para>
        /// </summary>
        internal Func<Dictionary<Type, HashSet<Type>>, Dictionary<Type, object>>? WarmScoped { get; init; }

        internal ContentMergeMode MergeMode { get; init; } = ContentMergeMode.AppendDistinctById;

        internal bool IsScoped => ScopedRegistry != null;
    }
}
