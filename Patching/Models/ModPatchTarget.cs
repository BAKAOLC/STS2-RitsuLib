using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Vanilla method identity used with <see cref="IPatchMethod.GetTargets" /> to build <see cref="ModPatchInfo" />.
    ///     与 <see cref="IPatchMethod.GetTargets" /> 配合使用的原版方法标识，用于构建 <see cref="ModPatchInfo" />。
    /// </summary>
    /// <param name="TargetType">
    ///     Declaring type.
    ///     声明类型。
    /// </param>
    /// <param name="MethodName">
    ///     Method name.
    ///     方法名。
    /// </param>
    /// <param name="ParameterTypes">
    ///     Overload parameter types, or null for name-only lookup.
    ///     重载参数类型；为 null 时仅按名称查找。
    /// </param>
    /// <param name="IgnoreIfMissing">
    ///     Maps to <see cref="ModPatchInfo.IgnoreIfTargetMissing" />.
    ///     映射到 <see cref="ModPatchInfo.IgnoreIfTargetMissing" />。
    /// </param>
    /// <param name="HarmonyMethodType">
    ///     Harmony <see cref="MethodType" /> for resolution (e.g. <see cref="MethodType.Async" /> for async state
    ///     machines).
    ///     用于解析的 Harmony <see cref="MethodType" />（例如用于 async 状态机的 <see cref="MethodType.Async" />）。
    /// </param>
    public record ModPatchTarget(
        Type TargetType,
        string MethodName,
        Type[]? ParameterTypes,
        bool IgnoreIfMissing,
        MethodType HarmonyMethodType)
    {
        /// <summary>
        ///     Legacy four-argument constructor; sets <see cref="HarmonyMethodType" /> to <see cref="MethodType.Normal" />.
        ///     旧版四参数构造函数；将 <see cref="HarmonyMethodType" /> 设置为 <see cref="MethodType.Normal" />。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        /// <param name="parameterTypes">
        ///     Overload parameter types.
        ///     重载参数类型。
        /// </param>
        /// <param name="ignoreIfMissing">
        ///     When true, missing method is non-fatal for optional patches.
        ///     为 true 时，缺失方法对可选 patch 不是致命错误。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes, bool ignoreIfMissing)
            : this(targetType, methodName, parameterTypes, ignoreIfMissing, MethodType.Normal)
        {
        }

        /// <summary>
        ///     Target with optional overload signature; not ignored if missing.
        ///     带可选重载签名的目标；缺失时不忽略。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        /// <param name="parameterTypes">
        ///     Overload parameter types.
        ///     重载参数类型。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes)
            // ReSharper disable once IntroduceOptionalParameters.Global
            : this(targetType, methodName, parameterTypes, false)
        {
        }

        /// <summary>
        ///     Target with overload signature and Harmony <see cref="MethodType" />.
        ///     带重载签名和 Harmony <see cref="MethodType" /> 的目标。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        /// <param name="parameterTypes">
        ///     Overload parameter types.
        ///     重载参数类型。
        /// </param>
        /// <param name="harmonyMethodType">
        ///     Harmony method type (e.g. <see cref="MethodType.Async" />).
        ///     Harmony 方法类型（例如 <see cref="MethodType.Async" />）。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes, MethodType harmonyMethodType)
            : this(targetType, methodName, parameterTypes, false, harmonyMethodType)
        {
        }

        /// <summary>
        ///     Target without overload disambiguation; optional ignore-if-missing flag.
        ///     没有重载消歧的目标；可选的缺失时忽略标志。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        /// <param name="ignoreIfMissing">
        ///     When true, missing method is non-fatal for optional patches.
        ///     为 true 时，缺失方法对可选 patch 不是致命错误。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, bool ignoreIfMissing)
            : this(targetType, methodName, null, ignoreIfMissing)
        {
        }

        /// <summary>
        ///     Target by name and Harmony <see cref="MethodType" /> only (no overload disambiguation).
        ///     仅按名称和 Harmony <see cref="MethodType" /> 定位的目标（无重载消歧）。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        /// <param name="harmonyMethodType">
        ///     Harmony method type (e.g. <see cref="MethodType.Async" />).
        ///     Harmony 方法类型（例如 <see cref="MethodType.Async" />）。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, MethodType harmonyMethodType)
            : this(targetType, methodName, null, false, harmonyMethodType)
        {
        }

        /// <summary>
        ///     Simple target: any overload with that name, fail if missing.
        ///     简单目标：任意同名重载，缺失则失败。
        /// </summary>
        /// <param name="targetType">
        ///     Declaring type.
        ///     声明类型。
        /// </param>
        /// <param name="methodName">
        ///     Method name.
        ///     方法名。
        /// </param>
        public ModPatchTarget(Type targetType, string methodName)
            // ReSharper disable IntroduceOptionalParameters.Global
            : this(targetType, methodName, null, false)
        // ReSharper restore IntroduceOptionalParameters.Global
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var typeSuffix = HarmonyMethodType != MethodType.Normal ? $" [{HarmonyMethodType}]" : "";
            if (ParameterTypes == null) return $"{TargetType.Name}.{MethodName}{typeSuffix}";

            var paramNames = ParameterTypes.Length == 0
                ? "no parameters"
                : string.Join(", ", ParameterTypes.Select(p => p.Name));
            return $"{TargetType.Name}.{MethodName}({paramNames}){typeSuffix}";
        }
    }
}
