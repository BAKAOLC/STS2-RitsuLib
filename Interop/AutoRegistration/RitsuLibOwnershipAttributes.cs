namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Overrides the owning manifest id for auto-registration attributes declared on a specific type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RitsuLibOwnedByAttribute(string modId) : Attribute
    {
        /// <summary>
        ///     Manifest id that owns auto-registered entries on the annotated type.
        /// </summary>
        public string ModId { get; } = string.IsNullOrWhiteSpace(modId)
            ? throw new ArgumentException("Mod id must not be null or whitespace.", nameof(modId))
            : modId.Trim();
    }
}
