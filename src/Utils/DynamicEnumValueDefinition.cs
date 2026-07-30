namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Describes an immutable mod-owned dynamic enum value registered through <see cref="DynamicEnumValueRegistry{TEnum}" />.</para>
    ///     <para xml:lang="zh-CN">表示通过 <see cref="DynamicEnumValueRegistry{TEnum}" /> 注册的模组归属动态枚举值定义。</para>
    /// </summary>
    public sealed record DynamicEnumValueDefinition<TEnum>(string ModId, string Id, TEnum Value)
        where TEnum : struct, Enum;
}
