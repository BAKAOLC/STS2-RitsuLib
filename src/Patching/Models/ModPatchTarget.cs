using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">Identifies a vanilla method used by <see cref="IPatchMethod.GetTargets" /> to build patch metadata.</para>
    ///     <para xml:lang="zh-CN">标识供 <see cref="IPatchMethod.GetTargets" /> 构建补丁元数据的原版方法。</para>
    /// </summary>
    /// <param name="TargetType">
    ///     <para xml:lang="en">Type that declares the target method.</para>
    ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
    /// </param>
    /// <param name="MethodName">
    ///     <para xml:lang="en">Target method name.</para>
    ///     <para xml:lang="zh-CN">目标方法的名称。</para>
    /// </param>
    /// <param name="ParameterTypes">
    ///     <para xml:lang="en">Overload parameter types, or <see langword="null" /> for name-only lookup.</para>
    ///     <para xml:lang="zh-CN">重载的参数类型；为 <see langword="null" /> 时仅按名称查找。</para>
    /// </param>
    /// <param name="IgnoreIfMissing">
    ///     <para xml:lang="en">Whether a missing target should be ignored.</para>
    ///     <para xml:lang="zh-CN">是否忽略缺失的目标。</para>
    /// </param>
    /// <param name="HarmonyMethodType">
    ///     <para xml:lang="en">Harmony <see cref="MethodType" /> used to resolve the target.</para>
    ///     <para xml:lang="zh-CN">用于解析目标的 Harmony <see cref="MethodType" />。</para>
    /// </param>
    public record ModPatchTarget(
        Type TargetType,
        string MethodName,
        Type[]? ParameterTypes,
        bool IgnoreIfMissing,
        MethodType HarmonyMethodType)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a target with <see cref="HarmonyMethodType" /> set to <see cref="MethodType.Normal" />.</para>
        ///     <para xml:lang="zh-CN">创建目标，并将 <see cref="HarmonyMethodType" /> 设为 <see cref="MethodType.Normal" />。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="parameterTypes">
        ///     <para xml:lang="en">Overload parameter types.</para>
        ///     <para xml:lang="zh-CN">重载的参数类型。</para>
        /// </param>
        /// <param name="ignoreIfMissing">
        ///     <para xml:lang="en">Whether a missing target should be ignored.</para>
        ///     <para xml:lang="zh-CN">是否忽略缺失的目标。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes, bool ignoreIfMissing)
            : this(targetType, methodName, parameterTypes, ignoreIfMissing, MethodType.Normal)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a required target with an optional overload signature.</para>
        ///     <para xml:lang="zh-CN">创建可指定重载签名的必需目标。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="parameterTypes">
        ///     <para xml:lang="en">Overload parameter types.</para>
        ///     <para xml:lang="zh-CN">重载的参数类型。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes)
            : this(targetType, methodName, parameterTypes, false)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a required target with an overload signature and Harmony method type.</para>
        ///     <para xml:lang="zh-CN">创建带重载签名和 Harmony 方法类型的必需目标。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="parameterTypes">
        ///     <para xml:lang="en">Overload parameter types.</para>
        ///     <para xml:lang="zh-CN">重载的参数类型。</para>
        /// </param>
        /// <param name="harmonyMethodType">
        ///     <para xml:lang="en">Harmony method type.</para>
        ///     <para xml:lang="zh-CN">Harmony 方法类型。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, Type[]? parameterTypes, MethodType harmonyMethodType)
            : this(targetType, methodName, parameterTypes, false, harmonyMethodType)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a name-only target that may be ignored when missing.</para>
        ///     <para xml:lang="zh-CN">创建仅按名称解析、可在缺失时忽略的目标。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="ignoreIfMissing">
        ///     <para xml:lang="en">Whether a missing target should be ignored.</para>
        ///     <para xml:lang="zh-CN">是否忽略缺失的目标。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, bool ignoreIfMissing)
            : this(targetType, methodName, null, ignoreIfMissing)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a required target resolved by name and Harmony method type.</para>
        ///     <para xml:lang="zh-CN">创建按名称和 Harmony 方法类型解析的必需目标。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        /// <param name="harmonyMethodType">
        ///     <para xml:lang="en">Harmony method type.</para>
        ///     <para xml:lang="zh-CN">Harmony 方法类型。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName, MethodType harmonyMethodType)
            : this(targetType, methodName, null, false, harmonyMethodType)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a required target resolved by name only.</para>
        ///     <para xml:lang="zh-CN">创建仅按名称解析的必需目标。</para>
        /// </summary>
        /// <param name="targetType">
        ///     <para xml:lang="en">Type that declares the target method.</para>
        ///     <para xml:lang="zh-CN">声明目标方法的类型。</para>
        /// </param>
        /// <param name="methodName">
        ///     <para xml:lang="en">Target method name.</para>
        ///     <para xml:lang="zh-CN">目标方法的名称。</para>
        /// </param>
        public ModPatchTarget(Type targetType, string methodName)
            : this(targetType, methodName, null, false)
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
