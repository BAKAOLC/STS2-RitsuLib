namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Per-mod facade for registering dynamic enum values without making the internal ID category
    ///         segment public.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于注册动态枚举值的逐模组门面，不向调用方公开内部 ID 类别段。</para>
    /// </summary>
    public sealed class ModDynamicEnumValueRegistry<TEnum> where TEnum : struct, Enum
    {
        internal ModDynamicEnumValueRegistry(string modId)
        {
            ModId = modId;
        }

        /// <summary>
        ///     <para xml:lang="en">Owning mod ID.</para>
        ///     <para xml:lang="zh-CN">所属模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a value owned by this registry's mod using the enum type's configured category
        ///         segment.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用该枚举类型配置的类别段注册归属此注册表模组的值。</para>
        /// </summary>
        public DynamicEnumValueDefinition<TEnum> RegisterOwned(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.RegisterOwned(ModId, localStem);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds the canonical owned ID for <paramref name="localStem" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="localStem" /> 构建规范的归属 ID。</para>
        /// </summary>
        public string GetOwnedId(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.GetOwnedId(ModId, localStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns and records the deterministic value for the canonical owned ID without registering a
        ///         definition.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回并登记规范归属 ID 对应的确定性值，但不注册定义。</para>
        /// </summary>
        public TEnum GetOwnedValue(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.GetValue(GetOwnedId(localStem));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the registered value for the canonical owned ID, or computes it without failing on hash
        ///         collisions or adding a minter reverse lookup.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回规范归属 ID 的已注册值；未注册时则直接计算，且不会因哈希碰撞而失败，也不会加入生成器的反向查找。</para>
        /// </summary>
        public TEnum GetOwnedValueIgnoringCollisions(string localStem)
        {
            return DynamicEnumValueRegistry<TEnum>.GetValueIgnoringCollisions(GetOwnedId(localStem));
        }
    }
}
