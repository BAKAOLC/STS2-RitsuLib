using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Central entry for enumerating the host <see cref="ModManager" />'s mod lists.
    ///     枚举宿主 <see cref="ModManager" /> 的 mod 列表的中央入口。
    /// </summary>
    internal static class Sts2ModManagerCompat
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly Func<Mod, ModManifest?> ReadManifest = CreateModManifestAccessor();
        private static readonly Func<Mod, Assembly?> ReadAssembly = CreateModAssemblyAccessor();
        private static readonly Func<Mod, IReadOnlyList<LocString>> ReadErrors = CreateModErrorsAccessor();
        private static readonly Func<Mod, string> ReadSource = CreateModSourceAccessor();
        private static readonly Func<Mod, int, string> ReadLoadState = CreateLoadStateAccessor();

        private static readonly Func<ModManifest, string?> ReadManifestId =
            CreateManifestStringAccessor("id", static manifest => manifest.id);

        private static readonly Func<ModManifest, string?> ReadManifestName =
            CreateManifestStringAccessor("name", static manifest => manifest.name);

        private static readonly Func<ModManifest, string?> ReadManifestVersion =
            CreateManifestStringAccessor("version", static manifest => manifest.version);

        private static readonly Func<ModManifest, bool> ReadManifestAffectsGameplay =
            CreateManifestBoolAccessor("affectsGameplay", static manifest => manifest.affectsGameplay, true);

        internal static IEnumerable<Mod> EnumerateLoadedModsWithAssembly()
        {
            return ModManager.GetLoadedMods();
        }

        internal static IReadOnlyDictionary<string, Assembly> BuildLoadedModAssembliesByManifestId()
        {
            var result = new Dictionary<string, Assembly>(StringComparer.Ordinal);

            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    var manifest = ReadManifest(mod);
                    var modId = manifest == null ? null : ReadManifestId(manifest);
                    var assembly = ReadAssembly(mod);
                    if (string.IsNullOrWhiteSpace(modId) || assembly == null)
                        continue;

                    result[modId] = assembly;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Failed to inspect a loaded mod assembly for discovery interop: {ex.Message}");
                }

            return result;
        }

        /// <summary>
        ///     All registered mods (including disabled / not loaded), for manifest name/description lookup.
        ///     所有已注册 mod（包括禁用/未加载的 mod），用于清单名称/描述查找。
        /// </summary>
        internal static IEnumerable<Mod> EnumerateModsForManifestLookup()
        {
            return ModManager.Mods;
        }

        internal static IReadOnlyList<Sts2ModInventoryEntry> BuildModInventoryEntries()
        {
            return EnumerateModsForManifestLookup()
                .Select(TryBuildModInventoryEntry)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .OrderBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.AssemblyName ?? "", StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static Sts2ModInventoryEntry? TryBuildModInventoryEntry(Mod mod)
        {
            try
            {
                var manifest = ReadManifest(mod);
                var assembly = ReadAssembly(mod);
                var assemblyName = ResolveAssemblyName(assembly);
                var errors = ReadErrors(mod);
                var fallbackName = assemblyName?.Name ?? "<unknown>";
                return new(
                    manifest == null ? fallbackName : ReadManifestId(manifest) ?? fallbackName,
                    manifest == null ? fallbackName : ReadManifestName(manifest) ?? fallbackName,
                    manifest == null ? null : ReadManifestVersion(manifest),
                    ReadLoadState(mod, errors.Count),
                    ReadSource(mod),
                    manifest == null || ReadManifestAffectsGameplay(manifest),
                    assemblyName?.Name,
                    assemblyName?.Version?.ToString(),
                    errors);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to describe a registered mod for inventory telemetry: {ex.Message}");
                return null;
            }
        }

        private static Func<Mod, ModManifest?> CreateModManifestAccessor()
        {
            if (typeof(Mod).GetField("manifest", InstanceMemberFlags) != null)
                return static mod => mod.manifest;

            var getter = CreateUntypedMemberGetter(typeof(Mod), "manifest");
            return mod => getter?.Invoke(mod) as ModManifest;
        }

        private static Func<Mod, Assembly?> CreateModAssemblyAccessor()
        {
            if (typeof(Mod).GetField("assembly", InstanceMemberFlags) != null)
                return static mod => mod.assembly;

            var getter = CreateUntypedMemberGetter(typeof(Mod), "assembly");
            return mod => getter?.Invoke(mod) as Assembly;
        }

        private static Func<Mod, IReadOnlyList<LocString>> CreateModErrorsAccessor()
        {
            if (typeof(Mod).GetField("errors", InstanceMemberFlags) != null)
                return static mod => NormalizeErrors(mod.errors);

            var getter = CreateUntypedMemberGetter(typeof(Mod), "errors");
            return mod => NormalizeErrors(getter?.Invoke(mod) as IEnumerable<LocString>);
        }

        private static Func<Mod, string> CreateModSourceAccessor()
        {
            if (typeof(Mod).GetField("modSource", InstanceMemberFlags) != null)
                return static mod => mod.modSource.ToString();

            var getter = CreateUntypedMemberGetter(typeof(Mod), "modSource");
            return mod => getter?.Invoke(mod)?.ToString() ?? "None";
        }

        private static Func<Mod, int, string> CreateLoadStateAccessor()
        {
            if (typeof(Mod).GetField("state", InstanceMemberFlags) != null)
                return static (mod, _) => mod.state.ToString();

            var stateGetter = CreateUntypedMemberGetter(typeof(Mod), "state");
            var wasLoadedGetter = CreateUntypedMemberGetter(typeof(Mod), "wasLoaded");
            var assemblyLoadedSuccessfullyGetter =
                CreateUntypedMemberGetter(typeof(Mod), "assemblyLoadedSuccessfully");
            return (mod, errorCount) =>
            {
                if (stateGetter?.Invoke(mod) is { } stateValue)
                    return stateValue.ToString() ?? "None";

                if (ReadBool(wasLoadedGetter, mod) == true)
                    return "Loaded";

                if (ReadBool(assemblyLoadedSuccessfullyGetter, mod) == false || errorCount > 0)
                    return "Failed";

                return "None";
            };
        }

        private static Func<ModManifest, string?> CreateManifestStringAccessor(
            string memberName,
            Func<ModManifest, string?> directAccessor)
        {
            if (typeof(ModManifest).GetField(memberName, InstanceMemberFlags) != null)
                return directAccessor;

            var getter = CreateUntypedMemberGetter(typeof(ModManifest), memberName);
            return manifest => getter?.Invoke(manifest) as string;
        }

        private static Func<ModManifest, bool> CreateManifestBoolAccessor(
            string memberName,
            Func<ModManifest, bool> directAccessor,
            bool defaultValue)
        {
            if (typeof(ModManifest).GetField(memberName, InstanceMemberFlags) != null)
                return directAccessor;

            var getter = CreateUntypedMemberGetter(typeof(ModManifest), memberName);
            return manifest => ReadBool(getter, manifest) ?? defaultValue;
        }

        private static AssemblyName? ResolveAssemblyName(Assembly? assembly)
        {
            if (assembly == null)
                return null;

            try
            {
                return assembly.GetName();
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyList<LocString> NormalizeErrors(IEnumerable<LocString>? errors)
        {
            return errors?.Where(error => error != null).ToArray() ?? [];
        }

        private static bool? ReadBool(Func<object, object?>? getter, object target)
        {
            return getter?.Invoke(target) is bool value ? value : null;
        }

        private static Func<object, object?>? CreateUntypedMemberGetter(Type type, string memberName)
        {
            var field = type.GetField(memberName, InstanceMemberFlags);
            if (field != null)
                return field.GetValue;

            var property = type.GetProperty(memberName, InstanceMemberFlags);
            if (property == null)
                return null;

            var getter = property.GetGetMethod(true);
            if (getter == null)
                return property.GetValue;

            try
            {
                return CreateUntypedPropertyGetter(type, property.PropertyType, getter);
            }
            catch
            {
                return target => getter.Invoke(target, null);
            }
        }

        private static Func<object, object?> CreateUntypedPropertyGetter(
            Type declaringType,
            Type valueType,
            MethodInfo getter)
        {
            var method = typeof(Sts2ModManagerCompat)
                .GetMethod(nameof(CreateUntypedPropertyGetterCore), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(declaringType, valueType);
            return (Func<object, object?>)method.Invoke(null, [getter])!;
        }

        private static Func<object, object?> CreateUntypedPropertyGetterCore<TDeclaring, TValue>(
            MethodInfo getter)
        {
            var typedGetter = getter.CreateDelegate<Func<TDeclaring, TValue>>();

            return target => typedGetter((TDeclaring)target);
        }
    }

    internal sealed record Sts2ModInventoryEntry(
        string Id,
        string Name,
        string? Version,
        string State,
        string Source,
        bool AffectsGameplay,
        string? AssemblyName,
        string? AssemblyVersion,
        IReadOnlyList<LocString> Errors);
}
