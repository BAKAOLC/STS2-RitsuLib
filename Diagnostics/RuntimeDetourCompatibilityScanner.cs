using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Diagnostics
{
    internal static class RuntimeDetourCompatibilityScanner
    {
        private const string Prefix = "[RuntimeDetourCompat]";

        private static readonly string[] InfrastructureAssemblyNamePrefixes =
        [
            "0Harmony",
            "HarmonyLib",
            "Mono.Cecil",
            "MonoMod.",
            "System.",
            "Microsoft.",
            "netstandard",
        ];

        private static int _scanIssuedForSession;

        internal static void Initialize()
        {
            RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ =>
                RitsuLibStartupAudit.Measure("runtimeDetourCompatibilityScan", ScanAndWarn));
        }

        private static void ScanAndWarn()
        {
            if (Interlocked.CompareExchange(ref _scanIssuedForSession, 1, 0) != 0)
                return;

            var riskMods = FindRuntimeDetourRiskMods();
            if (riskMods.Count == 0)
                return;

            RitsuLibFramework.Logger.Warn(BuildRiskDependencyWarning(riskMods));

            var bridge = RuntimeDetourReflectionBridge.TryCreate();
            if (bridge == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{Prefix} Risky hook dependency detected, but MonoMod.RuntimeDetour query API is not available. RuntimeDetour/Harmony overlap cannot be checked.");
                return;
            }

            var conflicts = FindHarmonyRuntimeDetourConflicts(bridge);
            if (conflicts.Count == 0)
            {
                RitsuLibFramework.Logger.Info(
                    $"{Prefix} RuntimeDetour dependency detected, but no RuntimeDetour hook currently overlaps a Harmony-patched method.");
                return;
            }

            RitsuLibFramework.Logger.Warn(BuildConflictWarning(conflicts));
        }

        private static IReadOnlyList<RuntimeDetourRiskMod> FindRuntimeDetourRiskMods()
        {
            IReadOnlyList<Sts2LoadedModAssemblyEntry> mods;
            try
            {
                mods = Sts2ModManagerCompat.BuildLoadedModAssemblyEntries();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"{Prefix} Failed to enumerate loaded mod assemblies: {ex.Message}");
                return [];
            }

            return mods
                .Select(TryBuildRiskMod)
                .Where(risk => risk != null)
                .Select(risk => risk!)
                .ToArray();
        }

        private static RuntimeDetourRiskMod? TryBuildRiskMod(Sts2LoadedModAssemblyEntry mod)
        {
            var referencingAssemblies = EnumerateModOwnedAssemblies(mod)
                .Where(ReferencesRuntimeDetour)
                .Select(assembly => assembly.GetName().Name ?? assembly.FullName ?? "<unknown>")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return referencingAssemblies.Length == 0
                ? null
                : new RuntimeDetourRiskMod(mod, referencingAssemblies);
        }

        private static IEnumerable<Assembly> EnumerateModOwnedAssemblies(Sts2LoadedModAssemblyEntry mod)
        {
            var primaryAssembly = mod.Assembly;
            var modDirectory = TryGetAssemblyDirectory(primaryAssembly);
            var yielded = new HashSet<Assembly>();

            if (ShouldInspectAssembly(primaryAssembly) && yielded.Add(primaryAssembly))
                yield return primaryAssembly;

            if (string.IsNullOrWhiteSpace(modDirectory))
                yield break;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!ShouldInspectAssembly(assembly) || !yielded.Add(assembly))
                    continue;

                var assemblyDirectory = TryGetAssemblyDirectory(assembly);
                if (string.Equals(assemblyDirectory, modDirectory, StringComparison.OrdinalIgnoreCase))
                    yield return assembly;
            }
        }

        private static bool ShouldInspectAssembly(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            return !string.IsNullOrWhiteSpace(assemblyName) && !IsInfrastructureAssemblyName(assemblyName);
        }

        private static bool ReferencesRuntimeDetour(Assembly assembly)
        {
            try
            {
                return assembly
                    .GetReferencedAssemblies()
                    .Any(static reference => IsRuntimeDetourAssemblyName(reference.Name));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"{Prefix} Failed to inspect assembly references for {FormatAssemblyName(assembly)}: {ex.Message}");
                return false;
            }
        }

        private static IReadOnlyList<RuntimeDetourHarmonyConflict> FindHarmonyRuntimeDetourConflicts(
            RuntimeDetourReflectionBridge bridge)
        {
            var conflicts = new List<RuntimeDetourHarmonyConflict>();

            foreach (var group in BuildHarmonyPatchIndex())
            {
                IReadOnlyList<RuntimeDetourHook> hooks;
                try
                {
                    hooks = bridge.GetRuntimeDetourHooks(group.OriginalMethod);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"{Prefix} Failed to query RuntimeDetour hooks for {FormatMethod(group.OriginalMethod)}: {ex.Message}");
                    continue;
                }

                if (hooks.Count > 0)
                    conflicts.Add(new(group, hooks));
            }

            return conflicts
                .OrderBy(conflict => FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod), StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<HarmonyPatchedMethodGroup> BuildHarmonyPatchIndex()
        {
            var result = new List<HarmonyPatchedMethodGroup>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var method in Harmony.GetAllPatchedMethods()
                         .OrderBy(static method => method.DeclaringType?.FullName ?? "", StringComparer.Ordinal)
                         .ThenBy(static method => method.Name, StringComparer.Ordinal))
            {
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo == null)
                    continue;

                var patches = FormatPatches("Prefix", patchInfo.Prefixes)
                    .Concat(FormatPatches("Postfix", patchInfo.Postfixes))
                    .Concat(FormatPatches("Transpiler", patchInfo.Transpilers))
                    .Concat(FormatPatches("Finalizer", patchInfo.Finalizers))
                    .Order(StringComparer.Ordinal)
                    .ToArray();

                if (patches.Length > 0)
                    result.Add(new(method, patches));
            }

            return result;
        }

        private static IEnumerable<string> FormatPatches(string kind, IEnumerable<Patch> patches)
        {
            return patches
                .OrderBy(static patch => patch.priority)
                .ThenBy(static patch => patch.owner, StringComparer.Ordinal)
                .ThenBy(static patch => FormatMethod(patch.PatchMethod), StringComparer.Ordinal)
                .Select(patch =>
                    $"{kind} owner={patch.owner} priority={patch.priority} method={FormatMethod(patch.PatchMethod)}");
        }

        private static string BuildRiskDependencyWarning(IReadOnlyList<RuntimeDetourRiskMod> riskMods)
        {
            var text = new StringBuilder()
                .AppendLine(
                    $"{Prefix} Risky hook dependency detected: loaded mod assemblies reference MonoMod.RuntimeDetour.")
                .AppendLine(
                    "If a MonoMod.RuntimeDetour hook targets a Harmony-patched method, it redirects the call path and the Harmony patches on that method are expected to be skipped.")
                .AppendLine("Referencing mods:");

            foreach (var riskMod in riskMods.OrderBy(static mod => mod.Mod.Id, StringComparer.OrdinalIgnoreCase))
                text.AppendLine(
                    $"  - {FormatMod(riskMod.Mod)} references: {string.Join(", ", riskMod.ReferencingAssemblies)}");

            return text.ToString().TrimEnd();
        }

        private static string BuildConflictWarning(IReadOnlyList<RuntimeDetourHarmonyConflict> conflicts)
        {
            var text = new StringBuilder()
                .AppendLine($"{Prefix} WARNING: RuntimeDetour hook overlaps Harmony patches.")
                .AppendLine(
                    "These RuntimeDetour hooks redirect methods that already have Harmony patches, so the following Harmony patches are expected to be skipped instead of executing normally:")
                .AppendLine($"Conflicts: {conflicts.Count}");

            foreach (var conflict in conflicts)
            {
                text.AppendLine($"  * {FormatMethod(conflict.HarmonyPatchGroup.OriginalMethod)}")
                    .AppendLine("    Harmony patches:");

                foreach (var patch in conflict.HarmonyPatchGroup.Patches)
                    text.AppendLine($"      - {patch}");

                text.AppendLine("    RuntimeDetour hooks:");
                foreach (var hook in conflict.Hooks)
                    text.AppendLine($"      - {hook.Kind} {hook.Target} config={hook.Config}");
            }

            return text.ToString().TrimEnd();
        }

        private static string FormatMod(Sts2LoadedModAssemblyEntry mod)
        {
            var version = string.IsNullOrWhiteSpace(mod.Version) ? "" : $", version={mod.Version}";
            return $"{mod.Name} [{mod.Id}] (assembly={FormatAssemblyName(mod.Assembly)}{version})";
        }

        private static string FormatAssemblyName(Assembly assembly)
        {
            try
            {
                return assembly.GetName().Name ?? assembly.FullName ?? "<unknown>";
            }
            catch
            {
                return assembly.FullName ?? "<unknown>";
            }
        }

        private static string FormatMethod(MethodBase method)
        {
            var declaringType = method.DeclaringType?.FullName ?? "<unknown>";
            var parameterList = string.Join(", ", method.GetParameters().Select(static parameter =>
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name));
            return $"{declaringType}.{method.Name}({parameterList})";
        }

        private static string? TryGetAssemblyDirectory(Assembly assembly)
        {
            try
            {
                var location = assembly.Location;
                return string.IsNullOrWhiteSpace(location) ? null : Path.GetDirectoryName(location);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsInfrastructureAssemblyName(string? assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   InfrastructureAssemblyNamePrefixes.Any(prefix =>
                       assemblyName.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                       assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsRuntimeDetourAssemblyName(string? assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName) &&
                   (assemblyName.Equals("MonoMod.RuntimeDetour", StringComparison.OrdinalIgnoreCase) ||
                    assemblyName.StartsWith("MonoMod.RuntimeDetour.", StringComparison.OrdinalIgnoreCase));
        }

        private sealed class RuntimeDetourReflectionBridge
        {
            private const BindingFlags PublicInstanceFlags = BindingFlags.Instance | BindingFlags.Public;
            private readonly PropertyInfo _detoursProperty;

            private readonly MethodInfo _getDetourInfoMethod;
            private readonly PropertyInfo _ilHooksProperty;

            private RuntimeDetourReflectionBridge(
                MethodInfo getDetourInfoMethod,
                PropertyInfo detoursProperty,
                PropertyInfo ilHooksProperty)
            {
                _getDetourInfoMethod = getDetourInfoMethod;
                _detoursProperty = detoursProperty;
                _ilHooksProperty = ilHooksProperty;
            }

            internal static RuntimeDetourReflectionBridge? TryCreate()
            {
                var managerType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(static assembly => assembly.GetType("MonoMod.RuntimeDetour.DetourManager", false))
                    .FirstOrDefault(static type => type != null);
                if (managerType == null)
                    return null;

                var getDetourInfoMethod = managerType.GetMethod(
                    "GetDetourInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(MethodBase)],
                    null);
                if (getDetourInfoMethod == null)
                    return null;

                var returnType = getDetourInfoMethod.ReturnType;
                var detoursProperty = returnType.GetProperty("Detours", PublicInstanceFlags);
                var ilHooksProperty = returnType.GetProperty("ILHooks", PublicInstanceFlags);
                if (detoursProperty == null || ilHooksProperty == null)
                    return null;

                return new(getDetourInfoMethod, detoursProperty, ilHooksProperty);
            }

            internal IReadOnlyList<RuntimeDetourHook> GetRuntimeDetourHooks(MethodBase originalMethod)
            {
                var detourInfo = _getDetourInfoMethod.Invoke(null, [originalMethod]);
                if (detourInfo == null)
                    return [];

                return
                [
                    .. EnumerateDetourHooks(_detoursProperty.GetValue(detourInfo)),
                    .. EnumerateIlHooks(_ilHooksProperty.GetValue(detourInfo)),
                ];
            }

            private static IEnumerable<RuntimeDetourHook> EnumerateDetourHooks(object? detours)
            {
                return from detour in EnumerateObjects(detours)
                    let target = FormatReflectedMember(detour.GetType().GetProperty("Entry", PublicInstanceFlags)
                        ?.GetValue(detour))
                    let config = FormatDetourConfig(detour.GetType().GetProperty("Config", PublicInstanceFlags)
                        ?.GetValue(detour))
                    select new RuntimeDetourHook("Detour", $"entry={target}", config);
            }

            private static IEnumerable<RuntimeDetourHook> EnumerateIlHooks(object? ilHooks)
            {
                return from ilHook in EnumerateObjects(ilHooks)
                    let target = FormatReflectedMember(ilHook.GetType()
                        .GetProperty("ManipulatorMethod", PublicInstanceFlags)?.GetValue(ilHook))
                    let config = FormatDetourConfig(ilHook.GetType().GetProperty("Config", PublicInstanceFlags)
                        ?.GetValue(ilHook))
                    select new RuntimeDetourHook("ILHook", $"manipulator={target}", config);
            }

            private static IEnumerable<object> EnumerateObjects(object? value)
            {
                if (value is not IEnumerable enumerable)
                    yield break;

                foreach (var item in enumerable)
                    if (item != null)
                        yield return item;
            }

            private static string FormatReflectedMember(object? value)
            {
                return value is MethodBase method ? FormatMethod(method) : value?.ToString() ?? "<unknown>";
            }

            private static string FormatDetourConfig(object? config)
            {
                if (config == null)
                    return "<none>";

                var type = config.GetType();
                return string.Join(
                    ", ",
                    ReadConfigValue(type, config, "Id"),
                    ReadConfigValue(type, config, "Priority"),
                    ReadConfigValue(type, config, "Before"),
                    ReadConfigValue(type, config, "After"));
            }

            private static string ReadConfigValue(Type type, object config, string propertyName)
            {
                var property = type.GetProperty(propertyName, PublicInstanceFlags);
                if (property == null)
                    return $"{propertyName}=<unknown>";

                try
                {
                    var value = property.GetValue(config);
                    return $"{propertyName}={FormatConfigValue(value)}";
                }
                catch
                {
                    return $"{propertyName}=<unreadable>";
                }
            }

            private static string FormatConfigValue(object? value)
            {
                if (value == null)
                    return "<null>";

                return value is IEnumerable enumerable and not string
                    ? "[" + string.Join(", ", enumerable.Cast<object>().Select(static item => item?.ToString())) + "]"
                    : value.ToString() ?? "<unknown>";
            }
        }

        private sealed record RuntimeDetourRiskMod(
            Sts2LoadedModAssemblyEntry Mod,
            IReadOnlyList<string> ReferencingAssemblies);

        private sealed record HarmonyPatchedMethodGroup(
            MethodBase OriginalMethod,
            IReadOnlyList<string> Patches);

        private sealed record RuntimeDetourHook(
            string Kind,
            string Target,
            string Config);

        private sealed record RuntimeDetourHarmonyConflict(
            HarmonyPatchedMethodGroup HarmonyPatchGroup,
            IReadOnlyList<RuntimeDetourHook> Hooks);
    }
}
