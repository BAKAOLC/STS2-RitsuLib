namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Overrides the owning manifest id for auto-registration attributes declared on a specific type.
    ///     覆盖特定类型上声明的 auto-registration attribute 所属的 manifest id。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RitsuLibOwnedByAttribute(string modId) : Attribute
    {
        /// <summary>
        ///     Manifest id that owns auto-registered entries on the annotated type.
        ///     拥有带注解类型上自动注册条目的 manifest id。
        /// </summary>
        public string ModId { get; } = string.IsNullOrWhiteSpace(modId)
            ? throw new ArgumentException("Mod id must not be null or whitespace.", nameof(modId))
            : modId.Trim();
    }
}
