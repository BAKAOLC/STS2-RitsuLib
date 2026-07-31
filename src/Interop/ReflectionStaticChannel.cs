namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Reflection-bound static accessors for generic keyed data exchange, such as persistence,
    ///         settings documents, and network payloads.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         用于通用键控数据交换的反射绑定静态访问器，可用于持久化、设置文档和网络载荷等场景。
    ///     </para>
    /// </summary>
    public sealed class ReflectionStaticChannel
    {
        internal ReflectionStaticChannel(
            Type providerType,
            Func<string, object?> getObject,
            Action<string, object?> setObject,
            JsonDomChannelDelegates json)
        {
            ProviderType = providerType;
            GetObject = getObject;
            SetObject = setObject;
            Json = json;
        }

        /// <summary>
        ///     <para xml:lang="en">Provider type targeted by the bound delegates.</para>
        ///     <para xml:lang="zh-CN">已绑定委托所指向的提供方类型。</para>
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        ///     <para xml:lang="en">Compiled object getter with the signature <c>key → object?</c>.</para>
        ///     <para xml:lang="zh-CN">签名为 <c>key → object?</c> 的已编译对象读取器。</para>
        /// </summary>
        public Func<string, object?> GetObject { get; }

        /// <summary>
        ///     <para xml:lang="en">Compiled object setter with the signature <c>(key, value) → void</c>.</para>
        ///     <para xml:lang="zh-CN">签名为 <c>(key, value) → void</c> 的已编译对象写入器。</para>
        /// </summary>
        public Action<string, object?> SetObject { get; }

        /// <summary>
        ///     <para xml:lang="en">Optional JSON document operations bound for this provider.</para>
        ///     <para xml:lang="zh-CN">为此提供方绑定的可选 JSON 文档操作。</para>
        /// </summary>
        public JsonDomChannelDelegates Json { get; }
    }
}
