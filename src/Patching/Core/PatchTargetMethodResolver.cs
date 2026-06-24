using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     Resolves a vanilla <see cref="MethodBase" /> from patch-target metadata, matching
    ///     <see cref="ModPatcher" /> / <see cref="ModPatchTarget" /> semantics.
    ///     根据 patch-target 元数据解析原版 <see cref="MethodBase" />，语义与
    ///     <see cref="ModPatcher" />
    ///     <see cref="ModPatchTarget" /> 一致。
    /// </summary>
    public static class PatchTargetMethodResolver
    {
        private const BindingFlags AnyDeclaredMethod =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        /// <summary>
        ///     Resolves using fields from <see cref="ModPatchInfo" />.
        ///     使用 <see cref="ModPatchInfo" /> 中的字段解析。
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
        ///     Resolves using <see cref="ModPatchTarget" />.
        ///     使用 <see cref="ModPatchTarget" /> 解析。
        /// </summary>
        public static MethodBase? Resolve(ModPatchTarget target)
        {
            return Resolve(target.TargetType, target.MethodName, target.ParameterTypes, target.HarmonyMethodType);
        }

        /// <summary>
        ///     Like <see cref="Resolve(ModPatchTarget)" /> but throws <see cref="MissingMethodException" /> when unresolved.
        ///     类似 <see cref="Resolve(ModPatchTarget)" />，但在无法解析时抛出 <see cref="MissingMethodException" />。
        /// </summary>
        public static MethodBase ResolveRequired(ModPatchTarget target)
        {
            return Resolve(target) ?? throw new MissingMethodException(
                target.TargetType.FullName,
                $"{target.MethodName} ({target.HarmonyMethodType})");
        }

        /// <summary>
        ///     Like <see cref="Resolve(System.Type,string,System.Type[],HarmonyLib.MethodType)" /> but throws when unresolved.
        ///     类似 <see cref="Resolve(System.Type,string,System.Type[],HarmonyLib.MethodType)" />，但在无法解析时抛出异常。
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
        ///     Core resolution: <see cref="MethodType.Normal" /> uses reflection <c>Type.GetMethod</c> (inheritance-aware);
        ///     other <see cref="MethodType" /> values use Harmony <see cref="AccessTools" /> helpers.
        ///     核心解析：<see cref="MethodType.Normal" /> 使用反射 <c>Type.GetMethod</c>（支持继承）；
        ///     其他 <see cref="MethodType" /> 值使用 Harmony <see cref="AccessTools" /> 辅助方法。
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
