namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Reflection-bound static accessors for generic keyed data exchange (persistence, settings DOM tiers,
    ///     networking payloads, …).
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
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        ///     Compiled getter for the convention’s object read method: <c>key → object?</c>.
        /// </summary>
        public Func<string, object?> GetObject { get; }

        /// <summary>
        ///     Compiled setter for the convention’s object write method: <c>(key, value)</c>.
        /// </summary>
        public Action<string, object?> SetObject { get; }

        /// <summary>
        ///     Optional JSON DOM tier delegates (merge, pointer, text, root object).
        /// </summary>
        public JsonDomChannelDelegates Json { get; }
    }
}
