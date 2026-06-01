namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Per-mod facade for registering dynamic enum values without exposing the internal id category segment.
    ///     注册动态枚举值的逐 mod facade，不向用户暴露内部 ID category 段。
    /// </summary>
    public sealed class ModDynamicEnumValueRegistry<TEnum> where TEnum : struct, Enum
    {
        internal ModDynamicEnumValueRegistry(string modId)
        {
            ModId = modId;
        }

        /// <summary>
        ///     Owning mod id.
        ///     所属 mod ID。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Registers a value owned by this registry's mod using the enum type's configured category segment.
        ///     使用此枚举类型配置的 category 段注册一个归属当前 mod 的值。
        /// </summary>
        public DynamicEnumValueDefinition<TEnum> RegisterOwned(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.RegisterOwned(ModId, localStem);
        }

        /// <summary>
        ///     Builds the canonical owned id for <paramref name="localStem" />.
        ///     为 <paramref name="localStem" /> 构建规范 owned ID。
        /// </summary>
        public string GetOwnedId(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.GetOwnedId(ModId, localStem);
        }

        /// <summary>
        ///     Returns the deterministic value for the canonical owned id without requiring registration.
        ///     返回规范 owned ID 对应的确定性值，不要求该值已注册。
        /// </summary>
        public TEnum GetOwnedValue(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.GetValue(GetOwnedId(localStem));
        }
    }
}
