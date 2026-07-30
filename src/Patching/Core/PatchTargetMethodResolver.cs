using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves a vanilla <see cref="MethodBase" /> from patch-target metadata using the same semantics as
    ///         <see cref="ModPatcher" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用与 <see cref="ModPatcher" /> 相同的语义，从补丁目标元数据解析原版
    ///         <see cref="MethodBase" />。
    ///     </para>
    /// </summary>
    public static class PatchTargetMethodResolver
    {
        private const BindingFlags AnyDeclaredMethod =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        /// <summary>
        ///     <para xml:lang="en">Resolves a target from <paramref name="modPatchInfo" />.</para>
        ///     <para xml:lang="zh-CN">从 <paramref name="modPatchInfo" /> 解析目标。</para>
        /// </summary>
        public static MethodBase? Resolve(ModPatchInfo modPatchInfo)
        {
            return Resolve(
                modPatchInfo.TargetType,
                modPatchInfo.MethodName,
                modPatchInfo.ParameterTypes,
                modPatchInfo.HarmonyMethodType);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves <paramref name="target" />.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="target" />。</para>
        /// </summary>
        public static MethodBase? Resolve(ModPatchTarget target)
        {
            return Resolve(target.TargetType, target.MethodName, target.ParameterTypes, target.HarmonyMethodType);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves <paramref name="target" /> or throws <see cref="MissingMethodException" />.</para>
        ///     <para xml:lang="zh-CN">解析 <paramref name="target" />；无法解析时抛出 <see cref="MissingMethodException" />。</para>
        /// </summary>
        public static MethodBase ResolveRequired(ModPatchTarget target)
        {
            return Resolve(target) ?? throw new MissingMethodException(
                target.TargetType.FullName,
                $"{target.MethodName} ({target.HarmonyMethodType})");
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the specified target or throws <see cref="MissingMethodException" />.</para>
        ///     <para xml:lang="zh-CN">解析指定目标；无法解析时抛出 <see cref="MissingMethodException" />。</para>
        /// </summary>
        public static MethodBase ResolveRequired(
            Type targetType,
            string methodName,
            Type[]? parameterTypes,
            MethodType harmonyMethodType)
        {
            return Resolve(targetType, methodName, parameterTypes, harmonyMethodType) ??
                   throw new MissingMethodException(targetType.FullName, $"{methodName} ({harmonyMethodType})");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a target method. <see cref="MethodType.Normal" /> uses reflection with inherited-member
        ///         lookup; other method types use Harmony <see cref="AccessTools" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析目标方法。<see cref="MethodType.Normal" /> 使用包含继承成员的反射查找；
        ///         其他方法类型使用 Harmony <see cref="AccessTools" />。
        ///     </para>
        /// </summary>
        public static MethodBase? Resolve(
            Type targetType,
            string methodName,
            Type[]? parameterTypes,
            MethodType harmonyMethodType)
        {
            return harmonyMethodType switch
            {
                MethodType.Normal => ResolveNormal(targetType, methodName, parameterTypes),
                MethodType.Async => GetAsyncStateMachineMoveNext(targetType, methodName, parameterTypes),
                MethodType.Getter => GetDeclaredImplementation(
                    AccessTools.DeclaredProperty(targetType, methodName)?.GetGetMethod(true)),
                MethodType.Setter => GetDeclaredImplementation(
                    AccessTools.DeclaredProperty(targetType, methodName)?.GetSetMethod(true)),
                MethodType.Constructor => AccessTools.DeclaredConstructor(targetType, parameterTypes),
                MethodType.Enumerator => GetEnumeratorMoveNext(targetType, methodName, parameterTypes),
                _ => ResolveNormal(targetType, methodName, parameterTypes),
            };
        }

        private static MethodInfo? ResolveNormal(Type targetType, string methodName, Type[]? parameterTypes)
        {
            MethodInfo? method;
            if (parameterTypes != null)
                method = targetType.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    parameterTypes,
                    null);
            else
                method = targetType.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return GetDeclaredImplementation(method);
        }

        private static MethodInfo? GetAsyncStateMachineMoveNext(Type targetType, string methodName,
            Type[]? parameterTypes)
        {
            var outer = ResolveNormal(targetType, methodName, parameterTypes);
            return outer is null ? null : AccessTools.AsyncMoveNext(outer);
        }

        private static MethodInfo? GetEnumeratorMoveNext(Type targetType, string methodName, Type[]? parameterTypes)
        {
            var outer = ResolveNormal(targetType, methodName, parameterTypes);
            return outer is null ? null : AccessTools.EnumeratorMoveNext(outer);
        }

        private static MethodInfo? GetDeclaredImplementation(MethodInfo? method)
        {
            if (method is not { IsAbstract: false })
                return null;

            var declaringType = method.DeclaringType;
            if (declaringType == null || method.ReflectedType == declaringType)
                return method;

            var parameterTypes = method.GetParameters()
                .Select(static parameter => parameter.ParameterType)
                .ToArray();
            return declaringType.GetMethod(method.Name, AnyDeclaredMethod, null, parameterTypes, null) ?? method;
        }
    }
}
