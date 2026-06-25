using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Patching.Compat
{
    /// <summary>
    ///     Preserves init-only setter signatures when Harmony/MonoMod imports reflected methods into Cecil wrappers.
    /// </summary>
    internal static class HarmonyInitSetterCompat
    {
        private const string IsExternalInitFullName = "System.Runtime.CompilerServices.IsExternalInit";
        private const string HarmonyId = Const.ModId + ".harmony-init-setter-compat";
        private static bool _installed;
        private static bool _reportedPostfixFailure;
        private static ImporterAccess? _access;

        public static void Install()
        {
            if (_installed)
                return;

            _installed = true;

            try
            {
                var harmony = new Harmony(HarmonyId);
                var harmonyAssembly = typeof(Harmony).Assembly;
                _access = ImporterAccess.TryCreate(harmonyAssembly, out var accessError);
                if (_access == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[HarmonyInitSetterCompat] Reflection importer accessors unavailable: {accessError}");
                    return;
                }

                var patched = 0;

                patched += TryPatchImporter(
                    harmony,
                    _access,
                    ResolveType(harmonyAssembly, "MonoMod.Utils", "MonoMod.Utils.MMReflectionImporter"),
                    "MonoMod.Utils.MMReflectionImporter");
                patched += TryPatchImporter(
                    harmony,
                    _access,
                    ResolveType(harmonyAssembly, "Mono.Cecil", "Mono.Cecil.DefaultReflectionImporter"),
                    "Mono.Cecil.DefaultReflectionImporter");

                if (patched == 0)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[HarmonyInitSetterCompat] No reflection importer entry points were patched.");
                    return;
                }

                RitsuLibFramework.Logger.Info(
                    $"[HarmonyInitSetterCompat] Installed on {patched} reflection importer entry point(s).");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[HarmonyInitSetterCompat] Install failed; continuing without init-setter import compat: {ex.Message}");
            }
        }

        private static int TryPatchImporter(
            Harmony harmony,
            ImporterAccess access,
            Type? importerType,
            string displayName)
        {
            if (importerType == null)
            {
                RitsuLibFramework.Logger.Warn($"[HarmonyInitSetterCompat] {displayName} not found.");
                return 0;
            }

            var method = importerType.GetMethod(
                "ImportReference",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(MethodBase), access.GenericParameterProviderType],
                null);
            if (method == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[HarmonyInitSetterCompat] {displayName}.ImportReference(MethodBase, IGenericParameterProvider) not found.");
                return 0;
            }

            harmony.Patch(
                method,
                postfix: new(typeof(HarmonyInitSetterCompat), nameof(ImportReferencePostfix)));
            return 1;
        }

        private static void ImportReferencePostfix(MethodBase method, object __result)
        {
            if (__result == null || method is not MethodInfo methodInfo)
                return;

            var requiredModifiers = methodInfo.ReturnParameter.GetRequiredCustomModifiers();
            if (requiredModifiers.Length == 0)
                return;

            var optionalModifiers = methodInfo.ReturnParameter.GetOptionalCustomModifiers();
            try
            {
                PreserveReturnModifiers(__result, requiredModifiers, optionalModifiers);
            }
            catch (Exception ex)
            {
                if (_reportedPostfixFailure)
                    return;

                _reportedPostfixFailure = true;
                RitsuLibFramework.Logger.Warn(
                    $"[HarmonyInitSetterCompat] ImportReference postfix failed; continuing without preserving return modifiers for this import: {ex.GetBaseException().Message}");
            }
        }

        private static void PreserveReturnModifiers(
            object methodReference,
            IReadOnlyList<Type> requiredModifiers,
            IReadOnlyList<Type> optionalModifiers)
        {
            var access = _access;
            // ReSharper disable once UseNullPropagation
            if (access == null)
                return;

            var methodReturnType = access.GetMethodReturnType(methodReference);
            if (methodReturnType == null)
                return;

            var returnType = access.GetReturnType(methodReturnType);
            if (returnType == null)
                return;

            foreach (var modifier in optionalModifiers.Reverse())
                if (!HasModifier(access, returnType, modifier.FullName!, false) &&
                    TryImportModifier(methodReference, modifier, out var modifierType))
                    returnType = access.CreateOptionalModifier(modifierType, returnType);

            foreach (var modifier in requiredModifiers.Reverse())
                if (!HasModifier(access, returnType, modifier.FullName!, true) &&
                    TryImportModifier(methodReference, modifier, out var modifierType))
                    returnType = access.CreateRequiredModifier(modifierType, returnType);

            access.SetReturnType(methodReturnType, returnType);
        }

        private static bool TryImportModifier(
            object methodReference,
            Type modifier,
            out object modifierType)
        {
            modifierType = null!;

            try
            {
                var access = _access;
                var module = access?.GetModule(methodReference);
                modifierType = module == null ? null! : access!.ImportType(module, modifier)!;
                return modifierType != null;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[HarmonyInitSetterCompat] Could not import return modifier {modifier.FullName}: {ex.Message}");
                return false;
            }
        }

        private static bool HasModifier(ImporterAccess access, object type, string modifierFullName, bool required)
        {
            for (var current = type; current != null; current = access.GetElementType(current))
            {
                var currentTypeName = current.GetType().FullName;
                switch (currentTypeName)
                {
                    case "Mono.Cecil.RequiredModifierType"
                        when required && GetModifierFullName(current) == modifierFullName:
                        return true;
                    case "Mono.Cecil.RequiredModifierType":
                        continue;
                    case "Mono.Cecil.OptionalModifierType"
                        when !required && GetModifierFullName(current) == modifierFullName:
                        return true;
                    case "Mono.Cecil.OptionalModifierType":
                        continue;
                    default:
                        return false;
                }
            }

            return false;

            string? GetModifierFullName(object modifierType)
            {
                return access.GetModifierType(modifierType) is { } modifier
                    ? access.GetFullName(modifier)
                    : null;
            }
        }

        private static Type? ResolveType(Assembly fallbackAssembly, string assemblyName, string fullName)
        {
            var type = fallbackAssembly.GetType(fullName, false);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            type = Type.GetType(fullName + ", " + assemblyName, false);
            if (type != null)
                return type;

            try
            {
                return Assembly.Load(new AssemblyName(assemblyName)).GetType(fullName, false);
            }
            catch
            {
                return null;
            }
        }

        private sealed class ImporterAccess
        {
            private ImporterAccess(
                Type genericParameterProviderType,
                Func<object, object?> getMethodReturnType,
                Func<object, object?> getReturnType,
                Action<object, object> setReturnType,
                Func<object, object?> getModule,
                Func<object, Type, object?> importType,
                Func<object, object?> getElementType,
                Func<object, object?> getModifierType,
                Func<object, string?> getFullName,
                Func<object, object, object> createRequiredModifier,
                Func<object, object, object> createOptionalModifier)
            {
                GenericParameterProviderType = genericParameterProviderType;
                GetMethodReturnType = getMethodReturnType;
                GetReturnType = getReturnType;
                SetReturnType = setReturnType;
                GetModule = getModule;
                ImportType = importType;
                GetElementType = getElementType;
                GetModifierType = getModifierType;
                GetFullName = getFullName;
                CreateRequiredModifier = createRequiredModifier;
                CreateOptionalModifier = createOptionalModifier;
            }

            public Type GenericParameterProviderType { get; }
            public Func<object, object?> GetMethodReturnType { get; }
            public Func<object, object?> GetReturnType { get; }
            public Action<object, object> SetReturnType { get; }
            public Func<object, object?> GetModule { get; }
            public Func<object, Type, object?> ImportType { get; }
            public Func<object, object?> GetElementType { get; }
            public Func<object, object?> GetModifierType { get; }
            public Func<object, string?> GetFullName { get; }
            public Func<object, object, object> CreateRequiredModifier { get; }
            public Func<object, object, object> CreateOptionalModifier { get; }

            public static ImporterAccess? TryCreate(Assembly harmonyAssembly, out string error)
            {
                error = "";

                try
                {
                    var moduleDefinitionType = RequiredType(harmonyAssembly, "Mono.Cecil.ModuleDefinition");
                    var genericParameterProviderType =
                        RequiredType(harmonyAssembly, "Mono.Cecil.IGenericParameterProvider");
                    var methodReferenceType = RequiredType(harmonyAssembly, "Mono.Cecil.MethodReference");
                    var methodReturnTypeType = RequiredType(harmonyAssembly, "Mono.Cecil.MethodReturnType");
                    var typeReferenceType = RequiredType(harmonyAssembly, "Mono.Cecil.TypeReference");
                    var typeSpecificationType = RequiredType(harmonyAssembly, "Mono.Cecil.TypeSpecification");
                    var requiredModifierType = RequiredType(harmonyAssembly, "Mono.Cecil.RequiredModifierType");
                    var optionalModifierType = RequiredType(harmonyAssembly, "Mono.Cecil.OptionalModifierType");
                    var modifierInterfaceType = RequiredType(harmonyAssembly, "Mono.Cecil.IModifierType");

                    var getMethodReturnType = CompileGetter<object?>(
                        RequiredProperty(methodReferenceType, "MethodReturnType"));
                    var getReturnType = CompileGetter<object?>(RequiredProperty(methodReturnTypeType, "ReturnType"));
                    var setReturnType = CompileSetter(RequiredProperty(methodReturnTypeType, "ReturnType"));
                    var getModule = CompileGetter<object?>(RequiredProperty(methodReferenceType, "Module"));
                    var importType = CompileTypeInstanceMethod<object?>(
                        RequiredMethod(
                            moduleDefinitionType,
                            "ImportReference",
                            BindingFlags.Instance | BindingFlags.Public,
                            [typeof(Type)]),
                        moduleDefinitionType,
                        [typeof(Type)],
                        [typeof(Type)]);
                    var getElementType = CompileGetter<object?>(RequiredProperty(typeSpecificationType, "ElementType"));
                    var getModifierType =
                        CompileGetter<object?>(RequiredProperty(modifierInterfaceType, "ModifierType"));
                    var getFullName = CompileGetter<string?>(RequiredProperty(typeReferenceType, "FullName"));
                    var createRequiredModifier =
                        CompileTwoArgConstructor(
                            RequiredConstructor(requiredModifierType, [typeReferenceType, typeReferenceType]),
                            typeReferenceType,
                            typeReferenceType);
                    var createOptionalModifier =
                        CompileTwoArgConstructor(
                            RequiredConstructor(optionalModifierType, [typeReferenceType, typeReferenceType]),
                            typeReferenceType,
                            typeReferenceType);

                    return new(
                        genericParameterProviderType,
                        getMethodReturnType,
                        getReturnType,
                        setReturnType,
                        getModule,
                        importType,
                        getElementType,
                        getModifierType,
                        getFullName,
                        createRequiredModifier,
                        createOptionalModifier);
                }
                catch (Exception ex)
                {
                    error = ex.GetBaseException().Message;
                    return null;
                }
            }

            private static Type RequiredType(Assembly assembly, string fullName)
            {
                var assemblyName = fullName.StartsWith("Mono.Cecil.", StringComparison.Ordinal)
                    ? "Mono.Cecil"
                    : assembly.GetName().Name!;
                return ResolveType(assembly, assemblyName, fullName) ??
                       throw new TypeLoadException(
                           $"{fullName} not found in {assembly.GetName().Name} or {assemblyName}.");
            }

            private static PropertyInfo RequiredProperty(Type type, string name)
            {
                return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                       throw new MissingMemberException(type.FullName, name);
            }

            private static MethodInfo RequiredMethod(
                Type type,
                string name,
                BindingFlags flags,
                Type[] parameters)
            {
                return type.GetMethod(name, flags, null, parameters, null) ??
                       throw new MissingMethodException(type.FullName, name);
            }

            private static ConstructorInfo RequiredConstructor(Type type, Type[] parameters)
            {
                return type.GetConstructor(
                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                           null,
                           parameters,
                           null) ??
                       throw new MissingMethodException(type.FullName, ".ctor");
            }

            private static Func<object, TResult> CompileGetter<TResult>(PropertyInfo property)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var body = Expression.Convert(
                    Expression.Property(Expression.Convert(instance, property.DeclaringType!), property),
                    typeof(TResult));
                return Expression.Lambda<Func<object, TResult>>(body, instance).Compile();
            }

            private static Action<object, object> CompileSetter(PropertyInfo property)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                var body = Expression.Assign(
                    Expression.Property(Expression.Convert(instance, property.DeclaringType!), property),
                    Expression.Convert(value, property.PropertyType));
                return Expression.Lambda<Action<object, object>>(body, instance, value).Compile();
            }

            private static Func<object, object> CompileOneArgConstructor(ConstructorInfo constructor,
                Type parameterType)
            {
                var arg = Expression.Parameter(typeof(object), "arg");
                var body = Expression.Convert(
                    Expression.New(constructor, Expression.Convert(arg, parameterType)),
                    typeof(object));
                return Expression.Lambda<Func<object, object>>(body, arg).Compile();
            }

            private static Func<object, object, object> CompileTwoArgConstructor(
                ConstructorInfo constructor,
                Type firstParameterType,
                Type secondParameterType)
            {
                var first = Expression.Parameter(typeof(object), "first");
                var second = Expression.Parameter(typeof(object), "second");
                var body = Expression.Convert(
                    Expression.New(
                        constructor,
                        Expression.Convert(first, firstParameterType),
                        Expression.Convert(second, secondParameterType)),
                    typeof(object));
                return Expression.Lambda<Func<object, object, object>>(body, first, second).Compile();
            }

            private static Func<object, Type, object?> CompileTypeInstanceMethod<TResult>(
                MethodInfo method,
                Type instanceType,
                Type[] parameterTypes,
                object?[] fixedArguments)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var type = Expression.Parameter(typeof(Type), "type");
                var arguments = parameterTypes.Select<Type, Expression>(parameterType =>
                    parameterType == typeof(Type)
                        ? Expression.Convert(type, parameterType)
                        : Expression.Constant(null, parameterType));
                var body = Expression.Convert(
                    Expression.Call(Expression.Convert(instance, instanceType), method, arguments),
                    typeof(TResult));
                return Expression.Lambda<Func<object, Type, object?>>(body, instance, type).Compile();
            }
        }
    }
}
