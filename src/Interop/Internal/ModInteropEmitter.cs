using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Interop.Internal
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Emits Harmony transpilers that forward annotated CLR stubs to another mod or assembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         生成 Harmony 转译补丁，使带有互操作标记的 CLR 存根转发到另一个模组或程序集。
    ///     </para>
    /// </summary>
    internal static class ModInteropEmitter
    {
        private const BindingFlags ValidMemberFlags = BindingFlags.DeclaredOnly | BindingFlags.Public |
                                                      BindingFlags.Static | BindingFlags.Instance;

        private static readonly FieldInfo WrappedValueField =
            AccessTools.DeclaredField(typeof(InteropClassWrapper), nameof(InteropClassWrapper.Value))!;

        private static readonly Dictionary<(string, string), Type> TypeResolutionCache = new();

        internal static void TryProcessType(
            Harmony harmony,
            IReadOnlyDictionary<string, Assembly> loadedAssembliesByModId,
            Type t)
        {
            var modInterop = t.GetCustomAttribute<ModInteropAttribute>();
            var assemblyInterop = t.GetCustomAttribute<AssemblyInteropAttribute>();
            if (modInterop != null && assemblyInterop != null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Interop] Type {t.FullName} declares both ModInterop and AssemblyInterop; skipping.");
                return;
            }

            if (modInterop != null)
            {
                loadedAssembliesByModId.TryGetValue(modInterop.ModId, out var legacyFallback);
                var assemblies = ModTypeDiscoveryHub.GetKnownAssembliesForMod(modInterop.ModId, legacyFallback);
                if (assemblies.Count == 0)
                    return;

                RitsuLibFramework.Logger.Info($"[ModInterop] Processing type {t.FullName} -> mod {modInterop.ModId}");

                var members = t.GetMembers(ValidMemberFlags);
                GenInteropMembers(members, harmony, TargetResolutionContext.ForModAssemblies(assemblies),
                    modInterop.Type, true);
                return;
            }

            if (assemblyInterop == null)
                return;

            RitsuLibFramework.Logger.Info($"[AssemblyInterop] Processing type {t.FullName}");
            GenInteropMembers(t.GetMembers(ValidMemberFlags), harmony,
                TargetResolutionContext.ForAssemblyQualifiedTypes(), assemblyInterop.Type, true);
        }

        private static bool GenInteropMembers(
            MemberInfo[] members,
            Harmony harmony,
            TargetResolutionContext targetContext,
            string? contextTargetType,
            bool requireStatic)
        {
            foreach (var member in members)
                switch (member)
                {
                    case PropertyInfo property:
                        if (requireStatic && !IsStaticProperty(property))
                            continue;
                        if (!GenInteropPropertyOrField(harmony, targetContext, contextTargetType, property))
                            return false;
                        break;
                    case MethodInfo method:
                        if (requireStatic && !method.IsStatic)
                            continue;
                        if (method.IsConstructor || method.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                            continue;
                        if (!GenInteropMethod(harmony, targetContext, contextTargetType, method))
                            return false;
                        break;
                    case TypeInfo nested:
                        if (!nested.IsAssignableTo(typeof(InteropClassWrapper)))
                            continue;
                        if (!GenInteropType(harmony, targetContext, contextTargetType, nested))
                            return false;
                        break;
                }

            return true;
        }

        private static bool GenInteropType(
            Harmony harmony,
            TargetResolutionContext targetContext,
            string? contextTargetType,
            TypeInfo type)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length < 1)
                throw new InvalidOperationException($"{type.FullName} must have at least one public constructor");

            var targetAttr = type.GetCustomAttribute<InteropTargetAttribute>();
            var targetName = targetAttr?.Type ?? targetAttr?.Name ?? contextTargetType
                ?? throw new InvalidOperationException($"No target type provided for interop type {type.FullName}");

            try
            {
                var targetType = ResolveTargetType(targetName, targetContext);
                if (targetType.IsValueType)
                    throw new InvalidOperationException(
                        $"InteropClassWrapper cannot wrap value type {targetType.FullName}.");

                // Validate all constructors before patching any to avoid partial application.
                var ctorPairs = constructors.Select(ctor =>
                {
                    var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
                    var match = targetType.GetConstructor(paramTypes);
                    if (match is null)
                        throw new InvalidOperationException(
                            $"No matching constructor in {targetType.FullName} for {FormatConstructor(ctor)}");
                    return (ctor, match, paramTypes);
                }).ToList();

                foreach (var (ctor, match, paramTypes) in ctorPairs)
                {
                    var ctorLoadArgs = new List<CodeInstruction> { CodeInstruction.LoadArgument(0) };
                    for (var i = 0; i < paramTypes.Length; i++)
                        ctorLoadArgs.Add(CodeInstruction.LoadArgument(i + 1));
                    ctorLoadArgs.Add(new(OpCodes.Newobj, match));
                    ctorLoadArgs.Add(new(OpCodes.Stfld, WrappedValueField));
                    PatchReturnInsertion(harmony, ctor, ctorLoadArgs);
                }

                RitsuLibFramework.Logger.Info($"[ModInterop] Generated interop type {type.FullName}");
                return GenInteropMembers(type.GetMembers(ValidMemberFlags), harmony, targetContext, targetName, false);
            }
            catch (Exception e)
            {
                RitsuLibFramework.Logger.Warn($"[ModInterop] {e}");
                return false;
            }
        }

        private static bool GenInteropMethod(
            Harmony harmony,
            TargetResolutionContext targetContext,
            string? contextTargetType,
            MethodInfo method)
        {
            var targetAttr = method.GetCustomAttribute<InteropTargetAttribute>();
            var typeName = targetAttr?.Type ?? contextTargetType
                ?? throw new InvalidOperationException(
                    $"Mod interop {FormatMethod(method)} does not define target type");
            var methodName = targetAttr?.Name ?? method.Name;

            try
            {
                var targetType = ResolveTargetType(typeName, targetContext);

                var methodParamInfos = method.GetParameters();
                var methodParams = methodParamInfos.Select(p => p.ParameterType).ToArray();
                var nonStaticParamInfos = method.IsStatic ? [.. methodParamInfos.Skip(1)] : methodParamInfos;

                var candidates = new List<MethodInfo>();
                foreach (var possibleTarget in AccessTools.GetDeclaredMethods(targetType))
                {
                    if (possibleTarget.Name != methodName || possibleTarget.ContainsGenericParameters)
                        continue;
                    if (possibleTarget.IsStatic && !method.IsStatic)
                        continue;
                    if (!possibleTarget.IsStatic && method.IsStatic &&
                        !CanUseStaticShimReceiver(methodParamInfos, targetType))
                        continue;

                    var candidateParams = possibleTarget.GetParameters();
                    var checkParamInfos = possibleTarget.IsStatic ? methodParamInfos : nonStaticParamInfos;
                    if (CheckParamMatch(candidateParams, checkParamInfos))
                        candidates.Add(possibleTarget);
                }

                var targetMethod = ResolveTargetMethodCandidate(method, targetType, methodName, candidates);
                var loadParams = new List<CodeInstruction>();

                if (targetMethod.ReturnType != typeof(void))
                    loadParams.Add(new(OpCodes.Pop));

                var sourceParameterOffset = 0;
                if (!targetMethod.IsStatic)
                {
                    if (method.IsStatic)
                    {
                        ValidateStaticShimReceiver(method, methodParamInfos, targetType, targetMethod);
                        loadParams.Add(CodeInstruction.LoadArgument(0));
                        AddTypeCoercion(loadParams, methodParams[0], targetType);
                        sourceParameterOffset = 1;
                    }
                    else
                    {
                        loadParams.AddRange(LoadWrappedTarget(targetType));
                    }
                }

                var targetParams = targetMethod.GetParameters();
                var instanceArgumentOffset = method.IsStatic ? 0 : 1;
                for (var i = 0; i < targetParams.Length; i++)
                {
                    var sourceParameterIndex = i + sourceParameterOffset;
                    loadParams.Add(CodeInstruction.LoadArgument(sourceParameterIndex + instanceArgumentOffset));
                    AddTypeCoercion(
                        loadParams,
                        methodParams[sourceParameterIndex],
                        targetParams[i].ParameterType);
                }

                if (targetMethod.ReturnType != method.ReturnType)
                    throw new InvalidOperationException(
                        $"{FormatMethod(method)} → {targetType.FullName}.{methodName}: " +
                        $"return type mismatch (stub: {method.ReturnType.Name}, target: {targetMethod.ReturnType.Name})");

                loadParams.Add(Call(targetMethod));
                PatchReturnInsertion(harmony, method, loadParams);
                RitsuLibFramework.Logger.Info($"[ModInterop] Generated interop method {method.Name}");
            }
            catch (Exception e)
            {
                RitsuLibFramework.Logger.Warn($"[ModInterop] {e}");
                return false;
            }

            return true;
        }

        private static bool GenInteropPropertyOrField(
            Harmony harmony,
            TargetResolutionContext targetContext,
            string? contextTargetType,
            PropertyInfo property)
        {
            var targetAttr = property.GetCustomAttribute<InteropTargetAttribute>();
            var typeName = targetAttr?.Type ?? contextTargetType
                ?? throw new InvalidOperationException($"Mod interop {property} does not define target type");
            var name = targetAttr?.Name ?? property.Name;

            try
            {
                var targetType = ResolveTargetType(typeName, targetContext);

                var targetProperty = AccessTools.DeclaredProperty(targetType, name);
                if (targetProperty is not null && targetProperty.PropertyType == property.PropertyType)
                {
                    if (targetProperty.SetMethod is null && targetProperty.GetMethod is null)
                        throw new InvalidOperationException($"Cannot get or set target property {targetProperty}");
                    var targetStatic = (targetProperty.SetMethod?.IsStatic ?? false)
                                       || (targetProperty.GetMethod?.IsStatic ?? false);
                    var sourceStatic = (property.SetMethod?.IsStatic ?? false)
                                       || (property.GetMethod?.IsStatic ?? false);
                    if (targetStatic && !sourceStatic)
                        throw new InvalidOperationException(
                            $"Target property {targetProperty} is static; interop property must also be static");
                    if (sourceStatic && !targetStatic)
                        throw new InvalidOperationException(
                            $"Target property {targetProperty} is not static; interop property should not be static");

                    if (targetProperty.SetMethod is not null)
                    {
                        if (property.SetMethod is null)
                            throw new InvalidOperationException(
                                $"Property {property} should have a setter to match target property");

                        if (targetStatic)
                            PatchReturnInsertion(harmony, property.SetMethod,
                            [
                                new(OpCodes.Ldarg_0),
                                Call(targetProperty.SetMethod),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.SetMethod,
                            [
                                .. LoadWrappedTarget(targetType), new(OpCodes.Ldarg_1),
                                Call(targetProperty.SetMethod),
                            ]);
                    }

                    if (targetProperty.GetMethod is not null)
                    {
                        if (property.GetMethod is null)
                            throw new InvalidOperationException(
                                $"Property {property} should have a getter to match target property");

                        if (targetStatic)
                            PatchReturnInsertion(harmony, property.GetMethod,
                            [
                                new(OpCodes.Pop),
                                Call(targetProperty.GetMethod),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.GetMethod,
                            [
                                new(OpCodes.Pop), .. LoadWrappedTarget(targetType),
                                Call(targetProperty.GetMethod),
                            ]);
                    }

                    RitsuLibFramework.Logger.Info($"[ModInterop] Generated interop property {property.Name}");
                    return true;
                }

                var targetField = AccessTools.DeclaredField(targetType, name);
                if (targetField is null || targetField.FieldType != property.PropertyType)
                    throw new InvalidOperationException(
                        $"Could not find property or field for name {name} in type {typeName}");
                {
                    if (property.SetMethod is null)
                        throw new InvalidOperationException(
                            $"Interop property {property} should have a setter for field {targetField}");
                    if (property.GetMethod is null)
                        throw new InvalidOperationException(
                            $"Interop property {property} should have a getter for field {targetField}");

                    var sourceStatic = (property.SetMethod?.IsStatic ?? false)
                                       || (property.GetMethod?.IsStatic ?? false);
                    if (targetField.IsStatic && !sourceStatic)
                        throw new InvalidOperationException(
                            $"Target field {targetField} is static; interop property must also be static");
                    if (sourceStatic && !targetField.IsStatic)
                        throw new InvalidOperationException(
                            $"Target field {targetField} is not static; interop property should not be static");

                    if (property.SetMethod is not null)
                    {
                        if (targetField.IsStatic)
                            PatchReturnInsertion(harmony, property.SetMethod,
                            [
                                new(OpCodes.Ldarg_0),
                                new(OpCodes.Stsfld, targetField),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.SetMethod,
                            [
                                .. LoadWrappedTarget(targetType), new(OpCodes.Ldarg_1), new(OpCodes.Stfld, targetField),
                            ]);
                    }

                    if (property.GetMethod is not null)
                    {
                        if (targetField.IsStatic)
                            PatchReturnInsertion(harmony, property.GetMethod,
                            [
                                new(OpCodes.Pop),
                                new(OpCodes.Ldsfld, targetField),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.GetMethod,
                                [new(OpCodes.Pop), .. LoadWrappedTarget(targetType), new(OpCodes.Ldfld, targetField)]);
                    }

                    RitsuLibFramework.Logger.Info($"[ModInterop] Generated interop field property {property.Name}");
                    return true;
                }
            }
            catch (Exception e)
            {
                RitsuLibFramework.Logger.Warn($"[ModInterop] {e}");
                return false;
            }
        }

        private static void PatchReturnInsertion(
            Harmony harmony,
            MethodBase target,
            IEnumerable<CodeInstruction> payload)
        {
            HarmonyIlPayloadTranspiler.PatchReturnInsertion(
                harmony,
                target,
                payload,
                "[ModInterop] Insert generated wrapper IL before single ret");
        }

        private static CodeInstruction[] LoadWrappedTarget(Type targetType)
        {
            if (targetType.IsValueType)
                throw new InvalidOperationException(
                    $"InteropClassWrapper cannot wrap value type {targetType.FullName}.");

            return
            [
                CodeInstruction.LoadArgument(0),
                new(OpCodes.Ldfld, WrappedValueField),
                new(OpCodes.Castclass, targetType),
            ];
        }

        private static void ValidateStaticShimReceiver(
            MethodInfo sourceMethod,
            ParameterInfo[] sourceParameters,
            Type targetType,
            MethodInfo targetMethod)
        {
            if (sourceParameters.Length > 0 &&
                (IsWildcardParam(sourceParameters[0]) || targetType.IsAssignableTo(sourceParameters[0].ParameterType)))
                return;

            throw new InvalidOperationException(
                $"Static shim {FormatMethod(sourceMethod)} must take target receiver {targetType.FullName} as its first parameter to match instance target {FormatMethod(targetMethod)}");
        }

        private static bool CanUseStaticShimReceiver(ParameterInfo[] sourceParameters, Type targetType)
        {
            return !targetType.IsValueType &&
                   sourceParameters.Length > 0 &&
                   (IsWildcardParam(sourceParameters[0]) ||
                    targetType.IsAssignableTo(sourceParameters[0].ParameterType));
        }

        private static MethodInfo ResolveTargetMethodCandidate(
            MethodInfo sourceMethod,
            Type targetType,
            string methodName,
            IReadOnlyList<MethodInfo> candidates)
        {
            if (candidates.Count == 0)
                throw new InvalidOperationException(
                    $"{FormatMethod(sourceMethod)} → {targetType.FullName}.{methodName}: " +
                    "no overload with matching parameters");
            if (candidates.Count == 1)
                return candidates[0];

            var signatures = string.Join(", ", candidates.Select(FormatMethodSignature));
            throw new InvalidOperationException(
                $"{FormatMethod(sourceMethod)} → {targetType.FullName}.{methodName}: " +
                $"multiple overloads match ({signatures}); use more specific stub parameter types");
        }

        private static void AddTypeCoercion(List<CodeInstruction> instructions, Type sourceType, Type targetType)
        {
            if (sourceType == targetType)
                return;

            if (sourceType.IsByRef || targetType.IsByRef || sourceType.IsPointer || targetType.IsPointer)
                throw new InvalidOperationException(
                    $"Cannot coerce interop argument from {sourceType} to {targetType}.");

            if (sourceType.IsValueType)
            {
                if (targetType.IsValueType)
                    throw new InvalidOperationException(
                        $"Cannot coerce interop value argument from {sourceType} to {targetType}.");

                instructions.Add(new(OpCodes.Box, sourceType));
                if (targetType != typeof(object))
                    instructions.Add(new(OpCodes.Castclass, targetType));
                return;
            }

            instructions.Add(targetType.IsValueType
                ? new(OpCodes.Unbox_Any, targetType)
                : new(OpCodes.Castclass, targetType));
        }

        private static CodeInstruction Call(MethodInfo method)
        {
            var opcode = method.IsStatic || method.DeclaringType?.IsValueType == true
                ? OpCodes.Call
                : OpCodes.Callvirt;
            return new(opcode, method);
        }

        private static bool IsStaticProperty(PropertyInfo property)
        {
            return property.GetAccessors(true).Any(static accessor => accessor.IsStatic);
        }

        private static Type ResolveTargetType(string targetName, TargetResolutionContext targetContext)
        {
            var key = (targetName, targetContext.CacheKey);
            if (TypeResolutionCache.TryGetValue(key, out var cached))
                return cached;
            var resolved = ResolveTargetTypeCore(targetName, targetContext);
            TypeResolutionCache[key] = resolved;
            return resolved;
        }

        private static Type ResolveTargetTypeCore(string targetName, TargetResolutionContext targetContext)
        {
            if (targetContext.AssemblyQualifiedOnly)
            {
                _ = TryResolveAssemblyQualifiedType(targetName, out var resolved);
                return resolved ?? throw new InvalidOperationException(
                    $"AssemblyInterop target type '{targetName}' must be an assembly-qualified CLR type name that can be resolved.");
            }

            if (IsAssemblyQualifiedTypeName(targetName))
                throw new InvalidOperationException(
                    $"ModInterop target type '{targetName}' must be a full type name inside the target mod assembly. Use AssemblyInterop for assembly-qualified CLR type names.");

            foreach (var assembly in targetContext.ModAssemblies)
            {
                var resolved = assembly.GetType(targetName, false)
                               ?? Type.GetType($"{targetName}, {assembly.FullName}", false);
                if (resolved != null)
                    return resolved;
            }

            var assemblyNames = string.Join(", ",
                targetContext.ModAssemblies.Select(static assembly => assembly.FullName));
            throw new InvalidOperationException(
                $"Type {targetName} not found in mod assemblies: {assemblyNames}");
        }

        private static bool IsAssemblyQualifiedTypeName(string? targetName)
        {
            return !string.IsNullOrWhiteSpace(targetName) && targetName.Contains(',', StringComparison.Ordinal);
        }

        private static bool TryResolveAssemblyQualifiedType(string? targetName, out Type? type)
        {
            type = null;
            if (!IsAssemblyQualifiedTypeName(targetName))
                return false;

            type = Type.GetType(targetName!, false);
            return type != null;
        }

        private static bool CheckParamMatch(ParameterInfo[] targetParams, ParameterInfo[] checkParams)
        {
            if (targetParams.Length != checkParams.Length)
                return false;
            return !checkParams.Where((p, i) =>
                    !IsWildcardParam(p) &&
                    !p.ParameterType.IsAssignableTo(targetParams[i].ParameterType))
                .Any();
        }

        private static bool IsWildcardParam(ParameterInfo p)
        {
            return p.ParameterType == typeof(object) || p.GetCustomAttribute<InteropAnyParamAttribute>() != null;
        }

        private static string FormatMethod(MethodInfo m)
        {
            return $"{m.DeclaringType?.FullName}.{m.Name}";
        }

        private static string FormatMethodSignature(MethodInfo method)
        {
            return $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";
        }

        private static string FormatConstructor(ConstructorInfo c)
        {
            return
                $"{c.DeclaringType?.FullName}.ctor({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})";
        }

        private readonly record struct TargetResolutionContext(
            IReadOnlyList<Assembly> ModAssemblies,
            bool AssemblyQualifiedOnly,
            string CacheKey)
        {
            public static TargetResolutionContext ForModAssemblies(IReadOnlyList<Assembly> assemblies)
            {
                var distinctAssemblies = assemblies.Distinct().ToArray();
                return new(
                    distinctAssemblies,
                    false,
                    string.Join("|", distinctAssemblies.Select(static assembly => assembly.FullName)));
            }

            public static TargetResolutionContext ForAssemblyQualifiedTypes()
            {
                return new([], true, "<assembly-qualified>");
            }
        }
    }
}
