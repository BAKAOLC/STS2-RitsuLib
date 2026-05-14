namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Marks a class whose public methods, properties, and nested <see cref="InteropClassWrapper" /> types
    ///     are rewritten at runtime to call into another mod's assembly, avoiding a compile-time reference.
    ///     标记一个类：其 public 方法、属性以及嵌套 <see cref="InteropClassWrapper" /> 类型
    ///     会在运行时被重写为调用另一个 mod 的 assembly，从而避免编译期引用。
    /// </summary>
    /// <param name="modId">
    ///     Manifest id of the mod that must be loaded for this interop block.
    ///     此 interop block 要求已加载的 mod manifest id。
    /// </param>
    /// <param name="type">
    ///     Default target CLR type name for members that do not specify <see cref="InteropTargetAttribute" />.
    ///     未指定 <see cref="InteropTargetAttribute" /> 的成员所使用的默认目标 CLR 类型名。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ModInteropAttribute(string modId, string? type = null) : Attribute
    {
        /// <summary>
        ///     Target mod manifest id required for this interop surface.
        ///     此 interop surface 所需的目标 mod manifest id。
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Default remote CLR type name for members without <see cref="InteropTargetAttribute" />.
        ///     没有 <see cref="InteropTargetAttribute" /> 的成员所使用的默认远端 CLR 类型名。
        /// </summary>
        public string? Type { get; } = type;
    }

    /// <summary>
    ///     Optional per-member override for the target type or member name in the remote mod.
    ///     针对远端 mod 中目标类型或成员名的可选逐成员覆盖。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public sealed class InteropTargetAttribute : Attribute
    {
        /// <summary>
        ///     Overrides the remote type and optionally the member name.
        ///     覆盖远端类型，并可选覆盖成员名。
        /// </summary>
        /// <param name="type">
        ///     Fully qualified or assembly-qualified type name in the remote mod.
        ///     远端 mod 中的完全限定或 assembly-qualified 类型名。
        /// </param>
        /// <param name="name">
        ///     Remote member name when different from the stub.
        ///     与 stub 不同时使用的远端成员名。
        /// </param>
        public InteropTargetAttribute(string type, string? name = null)
        {
            Type = type;
            Name = name;
        }

        /// <summary>
        ///     Overrides only the remote member name (type comes from <see cref="ModInteropAttribute.Type" /> or enclosing
        ///     context).
        ///     仅覆盖远端成员名（类型来自 <see cref="ModInteropAttribute.Type" /> 或外层
        ///     上下文）。
        /// </summary>
        /// <param name="name">
        ///     Remote member name when different from the stub.
        ///     与 stub 不同时使用的远端成员名。
        /// </param>
        public InteropTargetAttribute(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        ///     Remote type name when specified; otherwise inferred from <see cref="ModInteropAttribute" />.
        ///     显式指定时的远端类型名；否则从 <see cref="ModInteropAttribute" /> 推断。
        /// </summary>
        public string? Type { get; }

        /// <summary>
        ///     Remote member name when specified.
        ///     显式指定时的远端成员名。
        /// </summary>
        public string? Name { get; }
    }
}
