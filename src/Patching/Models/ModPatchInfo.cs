using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">Describes a static patch type that targets one vanilla method through reflection.</para>
    ///     <para xml:lang="zh-CN">描述通过反射定位单个原版方法的静态补丁类型。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">Stable patch ID.</para>
    ///     <para xml:lang="zh-CN">稳定的补丁 ID。</para>
    /// </param>
    /// <param name="targetType">
    ///     <para xml:lang="en">Type that declares the target method.</para>
    ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
    /// </param>
    /// <param name="methodName">
    ///     <para xml:lang="en">Name of the target method.</para>
    ///     <para xml:lang="zh-CN">目标方法的名称。</para>
    /// </param>
    /// <param name="patchType">
    ///     <para xml:lang="en">
    ///         Type that contains optional Harmony <c>Prefix</c>, <c>Postfix</c>, <c>Transpiler</c>, and
    ///         <c>Finalizer</c> methods.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         包含可选 Harmony <c>Prefix</c>、<c>Postfix</c>、<c>Transpiler</c> 和
    ///         <c>Finalizer</c> 方法的类型。
    ///     </para>
    /// </param>
    /// <param name="isCritical">
    ///     <para xml:lang="en">Whether failure to apply the patch is critical.</para>
    ///     <para xml:lang="zh-CN">补丁应用失败是否属于严重错误。</para>
    /// </param>
    /// <param name="description">
    ///     <para xml:lang="en">Optional description; defaults to <c>Patch Type.Method</c>.</para>
    ///     <para xml:lang="zh-CN">可选的描述；默认值为 <c>Patch Type.Method</c>。</para>
    /// </param>
    /// <param name="parameterTypes">
    ///     <para xml:lang="en">
    ///         Method parameter types used to resolve an overload, or <see langword="null" /> to resolve by
    ///         name only.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于解析重载的方法参数类型；为 <see langword="null" /> 时仅按名称解析。</para>
    /// </param>
    /// <param name="ignoreIfTargetMissing">
    ///     <para xml:lang="en">Whether a missing target produces an ignored result instead of a failure.</para>
    ///     <para xml:lang="zh-CN">目标缺失时是否返回已忽略结果，而非失败结果。</para>
    /// </param>
    /// <param name="harmonyMethodType">
    ///     <para xml:lang="en">
    ///         Harmony <see cref="MethodType" /> used to resolve the target, such as <see cref="MethodType.Async" />;
    ///         equivalent to <c>[HarmonyPatch(..., MethodType.X)]</c>.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         用于解析目标的 Harmony <see cref="MethodType" />，例如 <see cref="MethodType.Async" />；
    ///         等同于 <c>[HarmonyPatch(..., MethodType.X)]</c>。
    ///     </para>
    /// </param>
    public class ModPatchInfo(
        string id,
        Type targetType,
        string methodName,
        Type patchType,
        bool isCritical = true,
        string description = "",
        Type[]? parameterTypes = null,
        bool ignoreIfTargetMissing = false,
        MethodType harmonyMethodType = MethodType.Normal)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes patch metadata with <see cref="HarmonyMethodType" /> set to
        ///         <see cref="MethodType.Normal" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始化补丁元数据，并将 <see cref="HarmonyMethodType" /> 设为
        ///         <see cref="MethodType.Normal" />。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">Stable patch ID.</para>
        ///     <para xml:lang="zh-CN">稳定的补丁 ID。</para>
        /// </param>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Name of the target method.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="patchType">
        ///     <para xml:lang="en">Type that contains the Harmony patch methods.</para>
        ///     <para xml:lang="zh-CN">包含 Harmony 补丁方法的类型。</para>
        /// </param>
        /// <param name="isCritical">
        ///     <para xml:lang="en">Whether failure to apply the patch is critical.</para>
        ///     <para xml:lang="zh-CN">补丁应用失败是否属于严重错误。</para>
        /// </param>
        /// <param name="description">
        ///     <para xml:lang="en">Human-readable description.</para>
        ///     <para xml:lang="zh-CN">便于阅读的描述。</para>
        /// </param>
        /// <param name="parameterTypes">
        ///     <para xml:lang="en">Method parameter types used to resolve an overload.</para>
        ///     <para xml:lang="zh-CN">用于解析重载的方法参数类型。</para>
        /// </param>
        /// <param name="ignoreIfTargetMissing">
        ///     <para xml:lang="en">Whether a missing target is ignored.</para>
        ///     <para xml:lang="zh-CN">是否忽略缺失的目标。</para>
        /// </param>
        public ModPatchInfo(
            string id,
            Type targetType,
            string methodName,
            Type patchType,
            bool isCritical,
            string description,
            Type[]? parameterTypes,
            bool ignoreIfTargetMissing)
            : this(
                id,
                targetType,
                methodName,
                patchType,
                isCritical,
                description,
                parameterTypes,
                ignoreIfTargetMissing,
                MethodType.Normal)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the patch ID.</para>
        ///     <para xml:lang="zh-CN">获取补丁 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">获取声明目标方法的类型。</para>
        /// </summary>
        public Type TargetType { get; } = targetType;

        /// <summary>
        ///     <para xml:lang="en">Gets the name of the target method.</para>
        ///     <para xml:lang="zh-CN">获取目标方法的名称。</para>
        /// </summary>
        public string MethodName { get; } = methodName;

        /// <summary>
        ///     <para xml:lang="en">Gets the type that contains the Harmony patch methods.</para>
        ///     <para xml:lang="zh-CN">获取包含 Harmony 补丁方法的类型。</para>
        /// </summary>
        public Type PatchType { get; } = patchType;

        /// <summary>
        ///     <para xml:lang="en">Gets whether failure to apply this patch is critical.</para>
        ///     <para xml:lang="zh-CN">获取补丁应用失败是否属于严重错误。</para>
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     <para xml:lang="en">Gets the parameter signature used to resolve an overload, if specified.</para>
        ///     <para xml:lang="zh-CN">获取用于解析重载的参数签名（如果已指定）。</para>
        /// </summary>
        public Type[]? ParameterTypes { get; } = parameterTypes;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a missing target produces an ignored result.</para>
        ///     <para xml:lang="zh-CN">获取目标缺失时是否返回已忽略结果。</para>
        /// </summary>
        public bool IgnoreIfTargetMissing { get; } = ignoreIfTargetMissing;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony method type used to resolve the target.</para>
        ///     <para xml:lang="zh-CN">获取用于解析目标的 Harmony 方法类型。</para>
        /// </summary>
        public MethodType HarmonyMethodType { get; } = harmonyMethodType;

        /// <summary>
        ///     <para xml:lang="en">Gets a human-readable description of the patch.</para>
        ///     <para xml:lang="zh-CN">获取便于阅读的补丁描述。</para>
        /// </summary>
        public string Description { get; } =
            string.IsNullOrEmpty(description) ? $"Patch {targetType.Name}.{methodName}" : description;

        /// <inheritdoc />
        public override string ToString()
        {
            var typeSuffix = HarmonyMethodType != MethodType.Normal ? $" [{HarmonyMethodType}]" : "";
            if (ParameterTypes == null)
                return $"{Id}: {TargetType.Name}.{MethodName}{typeSuffix} <- {PatchType.Name}";

            var paramNames = ParameterTypes.Length == 0
                ? "no parameters"
                : string.Join(", ", ParameterTypes.Select(p => p.Name));
            return $"{Id}: {TargetType.Name}.{MethodName}({paramNames}){typeSuffix} <- {PatchType.Name}";
        }
    }
}
