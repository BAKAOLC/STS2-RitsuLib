namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Immutable row for a mod-owned dynamic enum value registered through
    ///     <see cref="DynamicEnumValueRegistry{TEnum}" />.
    ///     表示通过 <see cref="DynamicEnumValueRegistry{TEnum}" /> 注册的 mod 所有动态枚举值的不可变记录。
    /// </summary>
    public sealed record DynamicEnumValueDefinition<TEnum>(string ModId, string Id, TEnum Value)
        where TEnum : struct, Enum;
}
