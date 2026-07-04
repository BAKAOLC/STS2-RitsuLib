namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Overrides the owner id used by auto-registration attributes declared on this type. When another
    ///     auto-registration attribute declared on this type is inherited by a derived type through
    ///     <see cref="AutoRegistrationAttribute.Inherit" />, the inherited operation also uses this owner id.
    ///     This attribute itself is not inherited by unrelated auto-registration operations.
    ///     覆盖此类型上声明的自动注册 attribute 使用的 owner id。当此类型上声明的其他自动注册 attribute
    ///     通过 <see cref="AutoRegistrationAttribute.Inherit" /> 被派生类型继承时，继承得到的注册操作也会使用此 owner id。
    ///     此 attribute 本身不会被无关的自动注册操作继承。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RitsuLibOwnedByAttribute(string modId) : Attribute
    {
        /// <summary>
        ///     Owner id used by auto-registration operations sourced from the annotated type.
        ///     带注解类型作为来源的自动注册操作使用的 owner id。
        /// </summary>
        public string ModId { get; } = string.IsNullOrWhiteSpace(modId)
            ? throw new ArgumentException("Mod id must not be null or whitespace.", nameof(modId))
            : modId.Trim();
    }
}
