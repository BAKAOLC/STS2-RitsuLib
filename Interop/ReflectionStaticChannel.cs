namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Reflection-bound static accessors for generic keyed data exchange (persistence, settings DOM tiers,
    ///     networking payloads, …).
    ///     用于通用 keyed 数据交换的反射绑定静态访问器（持久化、settings DOM 层、网络载荷等）。
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
        ///     Provider type these delegates target.
        ///     这些委托指向的提供方类型。
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        ///     Compiled getter for the convention’s object read method: <c>key → object?</c>.
        ///     convention 对象读取方法的已编译 getter：<c>key → object?</c>。
        /// </summary>
        public Func<string, object?> GetObject { get; }

        /// <summary>
        ///     Compiled setter for the convention’s object write method: <c>(key, value)</c>.
        ///     convention 对象写入方法的已编译 setter：<c>(key, value)</c>。
        /// </summary>
        public Action<string, object?> SetObject { get; }

        /// <summary>
        ///     Optional JSON DOM tier delegates (merge, pointer, text, root object).
        ///     可选 JSON DOM tier delegate（merge、pointer、text、root object）。
        /// </summary>
        public JsonDomChannelDelegates Json { get; }
    }
}
