using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Interop.Internal
{
    /// <summary>
    ///     Emits Harmony transpilers so annotated stub types forward to another mod's CLR surface.
    ///     发出 Harmony transpiler，使带注解的 stub 类型转发到另一个 mod 的 CLR surface。
    /// </summary>
    internal static class ModInteropEmitter
    {
        private const BindingFlags ValidMemberFlags = BindingFlags.DeclaredOnly | BindingFlags.Public |
                                                      BindingFlags.Static | BindingFlags.Instance;

        private static readonly FieldInfo WrappedValueField =
            AccessTools.DeclaredField(typeof(InteropClassWrapper), nameof(InteropClassWrapper.Value))!;

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
                if (!loadedAssembliesByModId.TryGetValue(modInterop.ModId, out var assembly))
                    return;

                RitsuLibFramework.Logger.Info($"[ModInterop] Processing type {t.FullName} -> mod {modInterop.ModId}");

                var members = t.GetMembers(ValidMemberFlags);
                GenInteropMembers(members, harmony, TargetResolutionContext.ForModAssembly(assembly),
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
                throw new InvalidOperationException($"{type} must have at least one public constructor");

            var targetAttr = type.GetCustomAttribute<InteropTargetAttribute>();
            var targetName = targetAttr?.Type ?? targetAttr?.Name ?? contextTargetType
                ?? throw new InvalidOperationException($"No target type provided for interop type {type}");

            try
            {
                var targetType = ResolveTargetType(targetName, targetContext);

                foreach (var constructor in constructors)
                {
                    var constructorParams = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    var constructorMatch = targetType.GetConstructor(constructorParams);
                    if (constructorMatch is null)
                        throw new InvalidOperationException(
                            $"Failed to find matching constructor for {FormatConstructor(constructor)}");

                    var ctorLoadArgs = new List<CodeInstruction>
                    {
                        CodeInstruction.LoadArgument(0),
                    };
                    for (var i = 0; i < constructorParams.Length; i++)
                        ctorLoadArgs.Add(CodeInstruction.LoadArgument(i + 1));
                    ctorLoadArgs.Add(new(OpCodes.Newobj, constructorMatch));
                    ctorLoadArgs.Add(new(OpCodes.Stfld, WrappedValueField));

                    PatchReturnInsertion(harmony, constructor, ctorLoadArgs);
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

                var methodParams = method.GetParameters().Select(p => p.ParameterType).ToArray();
                var nonStaticParams = method.IsStatic ? methodParams.Skip(1).ToArray() : methodParams;

                MethodInfo? targetMethod = null;
                var loadParams = new List<CodeInstruction>();
                foreach (var possibleTarget in AccessTools.GetDeclaredMethods(targetType))
                {
                    if (possibleTarget.Name != methodName)
                        continue;
                    var targetParams = possibleTarget.GetParameters();
                    var checkParams = possibleTarget.IsStatic ? methodParams : nonStaticParams;
                    if (!CheckParamMatch(targetParams, checkParams))
                        continue;
                    targetMethod = possibleTarget;

                    if (targetMethod.ReturnType != typeof(void))
                        loadParams.Add(new(OpCodes.Pop));

                    var off = 0;
                    if (!targetMethod.IsStatic)
                    {
                        if (method.IsStatic)
                        {
                            ValidateStaticShimReceiver(method, methodParams, targetType, targetMethod);
                            loadParams.Add(CodeInstruction.LoadArgument(0));
                            if (methodParams[0] != targetType)
                                loadParams.Add(new(OpCodes.Castclass, targetType));
                            ++off;
                        }
                        else
                        {
                            loadParams.AddRange(LoadWrappedTarget(targetType));
                        }
                    }

                    for (var i = 0; i < targetParams.Length; i++)
                    {
                        loadParams.Add(CodeInstruction.LoadArgument(i + off));
                        if (methodParams[i + off] != targetParams[i].ParameterType)
                            loadParams.Add(new(OpCodes.Castclass, targetParams[i].ParameterType));
                    }

                    break;
                }

                if (targetMethod is null)
                    throw new InvalidOperationException(
                        $"Method {methodName} with matching parameters not found in type {targetType}");

                if (targetMethod.ReturnType != method.ReturnType)
                    throw new InvalidOperationException(
                        $"Method {methodName} return type {method.ReturnType} does not match target return type {targetMethod.ReturnType}");

                loadParams.Add(new(OpCodes.Call, targetMethod));
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
                                new(OpCodes.Call, targetProperty.SetMethod),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.SetMethod,
                            [
                                ..LoadWrappedTarget(targetType), new(OpCodes.Ldarg_1),
                                new(OpCodes.Call, targetProperty.SetMethod),
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
                                new(OpCodes.Call, targetProperty.GetMethod),
                            ]);
                        else
                            PatchReturnInsertion(harmony, property.GetMethod,
                            [
                                new(OpCodes.Pop), ..LoadWrappedTarget(targetType),
                                new(OpCodes.Call, targetProperty.GetMethod),
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
                                ..LoadWrappedTarget(targetType), new(OpCodes.Ldarg_1), new(OpCodes.Stfld, targetField),
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
                                [new(OpCodes.Pop), ..LoadWrappedTarget(targetType), new(OpCodes.Ldfld, targetField)]);
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
            return
            [
                CodeInstruction.LoadArgument(0),
                new(OpCodes.Ldfld, WrappedValueField),
                new(OpCodes.Castclass, targetType),
            ];
        }

        private static void ValidateStaticShimReceiver(
            MethodInfo sourceMethod,
            IReadOnlyList<Type> sourceParameterTypes,
            Type targetType,
            MethodInfo targetMethod)
        {
            if (sourceParameterTypes.Count > 0 &&
                (sourceParameterTypes[0] == typeof(object) || targetType.IsAssignableTo(sourceParameterTypes[0])))
                return;

            throw new InvalidOperationException(
                $"Static shim {FormatMethod(sourceMethod)} must take target receiver {targetType.FullName} as its first parameter to match instance target {FormatMethod(targetMethod)}");
        }

        private static bool IsStaticProperty(PropertyInfo property)
        {
            return property.GetAccessors(true).Any(static accessor => accessor.IsStatic);
        }

        private static Type ResolveTargetType(string targetName, TargetResolutionContext targetContext)
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

            return targetContext.ModAssembly!.GetType(targetName, false)
                   ?? Type.GetType($"{targetName}, {targetContext.ModAssembly.FullName}", false)
                   ?? throw new InvalidOperationException(
                       $"Type {targetName} not found in assembly {targetContext.ModAssembly.FullName}");
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

        private static bool CheckParamMatch(ParameterInfo[] targetParams, Type[] checkParams)
        {
            if (targetParams.Length != checkParams.Length)
                return false;
            return !checkParams.Where((t, i) => t != typeof(object) && !t.IsAssignableTo(targetParams[i].ParameterType))
                .Any();
        }

        private static string FormatMethod(MethodInfo m)
        {
            return $"{m.DeclaringType?.FullName}.{m.Name}";
        }

        private static string FormatConstructor(ConstructorInfo c)
        {
            return
                $"{c.DeclaringType?.FullName}.ctor({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})";
        }

        private readonly record struct TargetResolutionContext(Assembly? ModAssembly, bool AssemblyQualifiedOnly)
        {
            public static TargetResolutionContext ForModAssembly(Assembly assembly)
            {
                return new(assembly, false);
            }

            public static TargetResolutionContext ForAssemblyQualifiedTypes()
            {
                return new(null, true);
            }
        }
    }
}
