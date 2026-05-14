using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Describes a static patch type targeting one vanilla method by reflection.
    ///     描述一个通过反射定位单个原版方法的静态 patch 类型。
    /// </summary>
    /// <param name="id">
    ///     Stable patch identifier.
    ///     稳定的 patch 标识符。
    /// </param>
    /// <param name="targetType">
    ///     Declaring type of the method to patch.
    ///     要 patch 的方法的声明类型。
    /// </param>
    /// <param name="methodName">
    ///     Name of the method to patch.
    ///     要 patch 的方法名称。
    /// </param>
    /// <param name="patchType">
    ///     Type containing optional Harmony <c>Prefix</c>/<c>Postfix</c>/<c>Transpiler</c>/
    ///     <c>Finalizer</c>.
    ///     包含可选 Harmony <c>Prefix</c>/<c>Postfix</c>/<c>Transpiler</c>/
    ///     <c>Finalizer</c> 的类型。
    /// </param>
    /// <param name="isCritical">
    ///     Whether failure should block the patcher.
    ///     失败是否应阻止 patcher。
    /// </param>
    /// <param name="description">
    ///     Optional description; defaults to <c>Patch Type.Method</c>.
    ///     可选描述；默认为 <c>Patch Type.Method</c>。
    /// </param>
    /// <param name="parameterTypes">
    ///     Method parameter types for overload resolution; null selects by name only.
    ///     用于重载解析的方法参数类型；null 表示只按名称选择。
    /// </param>
    /// <param name="ignoreIfTargetMissing">
    ///     When true, missing targets produce an ignored result instead of failure.
    ///     为 true 时，缺失目标会产生 ignored 结果而非失败。
    /// </param>
    /// <param name="harmonyMethodType">
    ///     Harmony <see cref="MethodType" /> for target resolution (e.g. <see cref="MethodType.Async" />), matching
    ///     <c>[HarmonyPatch(..., MethodType.X)]</c>.
    ///     用于目标解析的 Harmony <see cref="MethodType" />（例如 <see cref="MethodType.Async" />），匹配
    ///     <c>[HarmonyPatch(..., MethodType.X)]</c>。
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
        ///     Legacy constructor without <see cref="HarmonyMethodType" />; forwards to
        ///     <see cref="MethodType.Normal" />.
        ///     不含 <see cref="HarmonyMethodType" /> 的旧版构造函数；转发到
        ///     <see cref="MethodType.Normal" />。
        /// </summary>
        /// <param name="id">
        ///     Stable patch identifier.
        ///     稳定的 patch 标识符。
        /// </param>
        /// <param name="targetType">
        ///     Declaring type of the method to patch.
        ///     要 patch 的方法的声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Name of the method to patch.
        ///     要 patch 的方法名称。
        /// </param>
        /// <param name="patchType">
        ///     Type containing Harmony patch methods.
        ///     包含 Harmony patch 方法的类型。
        /// </param>
        /// <param name="isCritical">
        ///     Whether failure should block the patcher.
        ///     是否让失败阻止 patcher。
        /// </param>
        /// <param name="description">
        ///     Human-readable description.
        ///     便于阅读的描述。
        /// </param>
        /// <param name="parameterTypes">
        ///     Method parameter types for overload resolution.
        ///     用于重载解析的方法参数类型。
        /// </param>
        /// <param name="ignoreIfTargetMissing">
        ///     When true, missing targets are ignored.
        ///     为 true 时，忽略缺失的目标。
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
        ///     Unique patch id.
        ///     唯一 patch id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Type that declares the original method.
        ///     声明原始方法的类型。
        /// </summary>
        public Type TargetType { get; } = targetType;

        /// <summary>
        ///     Original method name.
        ///     原始方法名。
        /// </summary>
        public string MethodName { get; } = methodName;

        /// <summary>
        ///     Patch class applied via Harmony.
        ///     通过 Harmony 应用的 patch 类。
        /// </summary>
        public Type PatchType { get; } = patchType;

        /// <summary>
        ///     Whether this patch is critical.
        ///     此 patch 是否为关键 patch。
        /// </summary>
        public bool IsCritical { get; } = isCritical;

        /// <summary>
        ///     Parameter signature for overload resolution, when needed.
        ///     需要时用于重载解析的参数签名。
        /// </summary>
        public Type[]? ParameterTypes { get; } = parameterTypes;

        /// <summary>
        ///     When true, a missing vanilla method yields an ignored success result.
        ///     为 true 时，缺失的原版方法会产生被忽略的成功结果。
        /// </summary>
        public bool IgnoreIfTargetMissing { get; } = ignoreIfTargetMissing;

        /// <summary>
        ///     Harmony method-type discriminator used when resolving the original <see cref="System.Reflection.MethodBase" />.
        ///     解析原始 <see cref="System.Reflection.MethodBase" /> 时使用的 Harmony 方法类型判别器。
        /// </summary>
        public MethodType HarmonyMethodType { get; } = harmonyMethodType;

        /// <summary>
        ///     Human-readable description of the patch.
        ///     patch 的便于阅读的描述。
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
