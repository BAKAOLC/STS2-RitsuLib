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
    ///     Marks a class whose public members forward to a CLR type resolved by an assembly-qualified type name
    ///     such as <c>Namespace.Type, AssemblyName</c>.
    ///     标记一个类：其 public 成员会转发到用 assembly-qualified type name
    ///     （如 <c>Namespace.Type, AssemblyName</c>）解析的 CLR 类型。
    /// </summary>
    /// <param name="type">
    ///     Default assembly-qualified CLR type name for members without <see cref="InteropTargetAttribute" />.
    ///     没有 <see cref="InteropTargetAttribute" /> 的成员所使用的默认 assembly-qualified CLR 类型名。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AssemblyInteropAttribute(string? type = null) : Attribute
    {
        /// <summary>
        ///     Default assembly-qualified CLR type name for members without <see cref="InteropTargetAttribute" />.
        ///     没有 <see cref="InteropTargetAttribute" /> 的成员所使用的默认 assembly-qualified CLR 类型名。
        /// </summary>
        public string? Type { get; } = type;
    }

    /// <summary>
    ///     Optional per-member override for the target type or member name.
    ///     With <see cref="ModInteropAttribute" />, type is resolved inside the target mod assembly.
    ///     With <see cref="AssemblyInteropAttribute" />, type must be an assembly-qualified CLR type name.
    ///     针对目标类型或成员名的可选逐成员覆盖。
    ///     配合 <see cref="ModInteropAttribute" /> 时，type 在目标 mod assembly 内解析。
    ///     配合 <see cref="AssemblyInteropAttribute" /> 时，type 必须是 assembly-qualified CLR type name。
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
        ///     Target type name. Use a full type name for <see cref="ModInteropAttribute" />, or an assembly-qualified
        ///     type name for <see cref="AssemblyInteropAttribute" />.
        ///     目标类型名。配合 <see cref="ModInteropAttribute" /> 使用完整类型名；配合
        ///     <see cref="AssemblyInteropAttribute" /> 使用 assembly-qualified 类型名。
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
        ///     Overrides only the remote member name (type comes from the enclosing interop attribute or context).
        ///     仅覆盖远端成员名（类型来自外层 interop attribute 或上下文）。
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
        ///     Target type name when specified; otherwise inferred from the enclosing interop attribute.
        ///     显式指定时的目标类型名；否则从外层 interop attribute 推断。
        /// </summary>
        public string? Type { get; }

        /// <summary>
        ///     Remote member name when specified.
        ///     显式指定时的远端成员名。
        /// </summary>
        public string? Name { get; }
    }
}
