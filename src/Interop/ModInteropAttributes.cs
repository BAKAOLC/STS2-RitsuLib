namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Marks a class whose public methods, properties, and nested <see cref="InteropClassWrapper" /> types
    ///         are rewritten at runtime to invoke members in another mod's assemblies without a compile-time reference.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标记一个类，使其公共方法、属性及嵌套 <see cref="InteropClassWrapper" /> 类型在运行时被重写，
    ///         从而无需编译期引用即可调用另一个模组程序集中的成员。
    ///     </para>
    /// </summary>
    /// <param name="modId">
    ///     <para xml:lang="en">Manifest ID of the mod required by this interop surface.</para>
    ///     <para xml:lang="zh-CN">此互操作接口所依赖模组的清单 ID。</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">
    ///         Default target CLR type name for members without <see cref="InteropTargetAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         未指定 <see cref="InteropTargetAttribute" /> 的成员所使用的默认目标 CLR 类型名。
    ///     </para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ModInteropAttribute(string modId, string? type = null) : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">Manifest ID of the target mod required by this interop surface.</para>
        ///     <para xml:lang="zh-CN">此互操作接口所依赖目标模组的清单 ID。</para>
        /// </summary>
        public string ModId { get; } = string.IsNullOrWhiteSpace(modId)
            ? throw new ArgumentException("Mod ID must not be null or whitespace.", nameof(modId))
            : modId.Trim();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Default remote CLR type name for members without <see cref="InteropTargetAttribute" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         未指定 <see cref="InteropTargetAttribute" /> 的成员所使用的默认远端 CLR 类型名。
        ///     </para>
        /// </summary>
        public string? Type { get; } = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Marks a class whose public methods, properties, and nested <see cref="InteropClassWrapper" /> types
    ///         forward to a CLR type resolved from an assembly-qualified name, such as
    ///         <c>Namespace.Type, AssemblyName</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标记一个类，使其公共方法、属性及嵌套 <see cref="InteropClassWrapper" /> 类型转发到
    ///         由程序集限定名称解析的 CLR 类型，例如 <c>Namespace.Type, AssemblyName</c>。
    ///     </para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">
    ///         Default assembly-qualified CLR type name for members without <see cref="InteropTargetAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         未指定 <see cref="InteropTargetAttribute" /> 的成员所使用的默认程序集限定 CLR 类型名。
    ///     </para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AssemblyInteropAttribute(string? type = null) : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Default assembly-qualified CLR type name for members without <see cref="InteropTargetAttribute" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         未指定 <see cref="InteropTargetAttribute" /> 的成员所使用的默认程序集限定 CLR 类型名。
        ///     </para>
        /// </summary>
        public string? Type { get; } = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Marks a method parameter as a wildcard when resolving a target overload. It matches any target
    ///         parameter type regardless of assignability.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在解析目标重载时将方法参数标记为通配符；无论可赋值性如何，它都可匹配任意目标参数类型。
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InteropAnyParamAttribute : Attribute;

    /// <summary>
    ///     <para xml:lang="en">
    ///         Overrides the target type or member name for a nested wrapper class, method, or property. With
    ///         <see cref="ModInteropAttribute" />, the type is resolved inside the target mod's assemblies.
    ///         With <see cref="AssemblyInteropAttribute" />, it must be an assembly-qualified CLR type name.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为嵌套包装器类、方法或属性覆盖目标类型或成员名。与 <see cref="ModInteropAttribute" />
    ///         配合时，类型在目标模组的程序集中解析；与 <see cref="AssemblyInteropAttribute" />
    ///         配合时，类型必须使用程序集限定的 CLR 类型名。
    ///     </para>
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public sealed class InteropTargetAttribute : Attribute
    {
        private Type[]? _genericTypes;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets concrete generic arguments for the target method, or for the target type on a
        ///         wrapper class or property. Use the CLR type name including its arity, such as Namespace.Box`1.
        ///         Arrays are copied on assignment and access. Null or empty means no specialization; at most 32
        ///         closed, non-pointer, non-byref, non-byref-like and non-void types are accepted.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置目标方法的具体泛型实参；标注包装类或属性时则用于目标类型。类型名须包含 CLR 元数，
        ///         例如 Namespace.Box`1。赋值及读取时复制数组；null 或空数组表示不指定泛型实参。最多接受 32 个
        ///         已封闭、非指针、非引用、非类引用且非 void 的类型。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Declare a non-generic wrapper and non-generic stub with concrete parameter and return types for
        ///         each specialization. Open generic stubs are not patched. Target arity and generic constraints
        ///         are checked when binding; mismatches are logged and leave that member unpatched.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每组特化使用独立的非泛型包装类和非泛型存根，参数及返回值采用具体类型；不为开放泛型存根打补丁。
        ///         绑定时校验目标元数和泛型约束；不匹配会记录日志并保留该成员原实现。
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">More than 32 arguments are supplied, or an argument is null or unsupported.</para>
        ///     <para xml:lang="zh-CN">实参超过 32 个，或某项为 null 或不支持的类型。</para>
        /// </exception>
        public Type[]? GenericTypes
        {
            get => _genericTypes == null ? null : [.. _genericTypes];
            set
            {
                if (value == null)
                {
                    _genericTypes = null;
                    return;
                }

                if (value.Length > 32)
                    throw new ArgumentException("At most 32 generic arguments are supported.", nameof(value));
                var copy = value.ToArray();
                if (copy.Any(type => type == null || type.ContainsGenericParameters || type.IsPointer ||
                                     type.IsByRef || type.IsByRefLike || type.IsFunctionPointer ||
                                     type == typeof(void)))
                    throw new ArgumentException("Generic arguments must be concrete supported CLR types.",
                        nameof(value));
                _genericTypes = copy;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Overrides the remote type and, optionally, the member name.</para>
        ///     <para xml:lang="zh-CN">覆盖远端类型，并可选择覆盖成员名。</para>
        /// </summary>
        /// <param name="type">
        ///     <para xml:lang="en">
        ///         Target type name. Use a full type name with <see cref="ModInteropAttribute" />, or an
        ///         assembly-qualified type name with <see cref="AssemblyInteropAttribute" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         目标类型名。与 <see cref="ModInteropAttribute" /> 配合时使用完整类型名；与
        ///         <see cref="AssemblyInteropAttribute" /> 配合时使用程序集限定类型名。
        ///     </para>
        /// </param>
        /// <param name="name">
        ///     <para xml:lang="en">Remote member name when it differs from the stub member.</para>
        ///     <para xml:lang="zh-CN">与存根成员名称不同时使用的远端成员名。</para>
        /// </param>
        public InteropTargetAttribute(string type, string? name)
        {
            Type = string.IsNullOrWhiteSpace(type)
                ? throw new ArgumentException("Target type must not be null or whitespace.", nameof(type))
                : type.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Overrides only the remote member name; the type comes from the enclosing interop attribute
        ///         or wrapper context.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅覆盖远端成员名；类型来自外层互操作特性或包装器上下文。
        ///     </para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">Remote member name when it differs from the stub member.</para>
        ///     <para xml:lang="zh-CN">与存根成员名称不同时使用的远端成员名。</para>
        /// </param>
        public InteropTargetAttribute(string? name = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Explicit target type name, or <see langword="null" /> when inherited from the enclosing context.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         显式指定的目标类型名；从外层上下文继承时为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public string? Type { get; }

        /// <summary>
        ///     <para xml:lang="en">Explicit remote member name, or <see langword="null" /> to use the stub name.</para>
        ///     <para xml:lang="zh-CN">显式指定的远端成员名；使用存根名称时为 <see langword="null" />。</para>
        /// </summary>
        public string? Name { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Excludes a declared member from interop binding. Ignored properties retain both accessors;
    ///         ignored methods keep their original bodies. Annotating either accessor excludes the entire
    ///         property. Other members are processed normally.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将声明的成员排除在互操作绑定之外。被忽略的属性保留两个访问器，方法保留原实现；
    ///         标注任一访问器会排除整个属性。其他成员照常处理。
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, Inherited = false)]
    public sealed class InteropIgnoreAttribute : Attribute;
}
