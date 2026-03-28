namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Marks a class whose public methods, properties, and nested <see cref="InteropClassWrapper" /> types
    ///     are rewritten at runtime to call into another mod's assembly, avoiding a compile-time reference.
    /// </summary>
    /// <param name="modId">Manifest id of the mod that must be loaded for this interop block.</param>
    /// <param name="type">Default target CLR type name for members that do not specify <see cref="InteropTargetAttribute" />.</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ModInteropAttribute(string modId, string? type = null) : Attribute
    {
        /// <summary>
        ///     Target mod manifest id required for this interop surface.
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Default remote CLR type name for members without <see cref="InteropTargetAttribute" />.
        /// </summary>
        public string? Type { get; } = type;
    }

    /// <summary>
    ///     Optional per-member override for the target type or member name in the remote mod.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public sealed class InteropTargetAttribute : Attribute
    {
        /// <summary>
        ///     Overrides the remote type and optionally the member name.
        /// </summary>
        /// <param name="type">Fully qualified or assembly-qualified type name in the remote mod.</param>
        /// <param name="name">Remote member name when different from the stub.</param>
        public InteropTargetAttribute(string type, string? name = null)
        {
            Type = type;
            Name = name;
        }

        /// <summary>
        ///     Overrides only the remote member name (type comes from <see cref="ModInteropAttribute.Type" /> or enclosing
        ///     context).
        /// </summary>
        /// <param name="name">Remote member name when different from the stub.</param>
        public InteropTargetAttribute(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        ///     Remote type name when specified; otherwise inferred from <see cref="ModInteropAttribute" />.
        /// </summary>
        public string? Type { get; }

        /// <summary>
        ///     Remote member name when specified.
        /// </summary>
        public string? Name { get; }
    }
}
