namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Overrides the owning mod ID for auto-registration attributes declared on this type. If one of those
    ///         attributes is inherited by a derived type through <see cref="AutoRegistrationAttribute.Inherit" />,
    ///         the inherited registration retains this owner. The override does not affect attributes declared
    ///         elsewhere.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         覆盖此类型上所声明自动注册特性的归属模组 ID。如果其中某个特性通过
    ///         <see cref="AutoRegistrationAttribute.Inherit" /> 继承到派生类型，对应注册仍使用此归属；
    ///         该覆盖不会影响在其他类型上声明的特性。
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RitsuLibOwnedByAttribute(string modId) : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Owning mod ID used by auto-registration operations declared on the annotated type.
        ///     </para>
        ///     <para xml:lang="zh-CN">在标注类型上声明的自动注册操作所使用的归属模组 ID。</para>
        /// </summary>
        public string ModId { get; } = string.IsNullOrWhiteSpace(modId)
            ? throw new ArgumentException("Mod id must not be null or whitespace.", nameof(modId))
            : modId.Trim();
    }
}
