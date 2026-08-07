namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies bounded alternate representations for RitsuLib's local text searches. Implementations must be
    ///         deterministic, thread-safe, and non-blocking. Metadata getters and <see cref="Expand" /> must be free
    ///         of network or file-system work; acquire heavy data before invalidating the registration.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 RitsuLib 的本地文本搜索提供长度受限的可选表示。实现必须确定、线程安全且不会阻塞。元数据
    ///         getter 与 <see cref="Expand" /> 均不得执行网络或文件系统操作；应先取得重型数据，再通知注册失效。
    ///     </para>
    /// </summary>
    public interface IRitsuSearchExpansionProvider
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the globally unique stable provider ID. IDs may contain ASCII letters, digits, periods,
        ///         hyphens, and underscores, and should be prefixed by the owning mod ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取全局唯一且稳定的提供器 ID。ID 可包含 ASCII 字母、数字、句点、连字符和下划线，并应以所属
        ///         mod ID 为前缀。
        ///     </para>
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the user-facing provider name shown in search settings. RitsuLib may query it repeatedly.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取在搜索设置中显示的提供器名称；RitsuLib 可能会重复读取它。</para>
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the provider is active until the user stores an explicit per-provider preference. The
        ///         value is captured once during registration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用户尚未保存此提供器的明确偏好时，提供器是否默认启用；该值会在注册时读取一次。
        ///     </para>
        /// </summary>
        bool EnabledByDefault { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Produces alternate searchable forms for <paramref name="text" />. Return an empty list when the text
        ///         or locale is unsupported. RitsuLib accepts at most 64 unique expansions and 8,192 total expansion
        ///         characters per provider call; excess and case-insensitive duplicates are ignored, and result ordering
        ///         is not a public contract. Callbacks may run concurrently and must not retain or mutate
        ///         <paramref name="context" />. Recoverable failures are logged and treated as no expansions;
        ///         non-recoverable exceptions are preserved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="text" /> 生成可选搜索形式。文本或语言环境不受支持时返回空列表。RitsuLib 每次
        ///         调用最多接受 64 个唯一扩展和总计 8,192 个扩展字符；超出部分及不区分大小写的重复项会被忽略，
        ///         结果顺序不属于公开契约。回调可能并发执行，且不得保留或修改 <paramref name="context" />。可恢复
        ///         故障会写入日志并按无扩展处理；不可恢复异常会继续传播。
        ///     </para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The immutable source text being indexed.</para>
        ///     <para xml:lang="zh-CN">正在建立索引的不可变源文本。</para>
        /// </param>
        /// <param name="context">
        ///     <para xml:lang="en">The current search expansion context.</para>
        ///     <para xml:lang="zh-CN">当前搜索扩展上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A non-null, finite list of searchable expansions.</para>
        ///     <para xml:lang="zh-CN">非 null 且数量有限的可搜索扩展列表。</para>
        /// </returns>
        IReadOnlyList<RitsuSearchExpansion> Expand(string text, RitsuSearchExpansionContext context);
    }
}
