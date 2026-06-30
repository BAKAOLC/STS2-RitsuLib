using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Utils;

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
        private static readonly Func<Mod, string?> ReadPath = CreateModPathAccessor();
        private static readonly Func<Mod, int, string> ReadLoadState = CreateLoadStateAccessor();

        private static readonly Func<ModManifest, string?> ReadManifestId =
            CreateManifestStringAccessor("id", static manifest => manifest.id);

        private static readonly Func<ModManifest, string?> ReadManifestName =
            CreateManifestStringAccessor("name", static manifest => manifest.name);

        private static readonly Func<ModManifest, string?> ReadManifestAuthor =
            CreateManifestStringAccessor("author", static manifest => manifest.author);

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

        internal static IReadOnlyList<Sts2LoadedModAssemblyEntry> BuildLoadedModAssemblyEntries()
        {
            return EnumerateLoadedModsWithAssembly()
                .Select(TryBuildLoadedModAssemblyEntry)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .ToArray();
        }

        internal static bool IsGameplayRelevantLoadedModType(Type type)
        {
            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    if (ReadAssembly(mod) != type.Assembly)
                        continue;

                    var manifest = ReadManifest(mod);
                    return manifest == null || ReadManifestAffectsGameplay(manifest);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Failed to inspect loaded mod gameplay relevance for {type.FullName}: {ex.Message}");
                    return true;
                }

            return true;
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

        internal static IReadOnlyList<Sts2ModInventoryEntry> BuildLoadedModInventoryEntries()
        {
            return EnumerateLoadedModsWithAssembly()
                .Select(TryBuildModInventoryEntry)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .ToArray();
        }

        internal static IReadOnlyList<RitsuModInfo> BuildModInfos(string? modId = null, RitsuModSource? source = null)
        {
            return EnumerateModsForManifestLookup()
                .Select(TryBuildModInfo)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .Where(entry => MatchesModQuery(entry, modId, source))
                .OrderBy(GetBestModInfoRank)
                .ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Source)
                .ToArray();
        }

        internal static ulong? TryGetWorkshopItemId(Mod mod)
        {
            ArgumentNullException.ThrowIfNull(mod);
            try
            {
                if (SteamWorkshopInstallSource.TryGetWorkshopItemIdFromPath(ReadPath(mod), out var pathItemId))
                    return pathItemId;

                var assembly = ReadAssembly(mod);
                if (assembly != null &&
                    SteamWorkshopInstallSource.TryGetWorkshopItemIdFromAssembly(assembly, out var assemblyItemId))
                    return assemblyItemId;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to inspect Workshop item id for a registered mod: {ex.Message}");
            }

            return null;
        }

        internal static bool TryGetBestModInfo(string modId, RitsuModSource? source, out RitsuModInfo? info)
        {
            info = BuildModInfos(modId, source).FirstOrDefault();
            return info != null;
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

        private static Sts2LoadedModAssemblyEntry? TryBuildLoadedModAssemblyEntry(Mod mod)
        {
            try
            {
                var manifest = ReadManifest(mod);
                var assembly = ReadAssembly(mod);
                if (assembly == null)
                    return null;

                var assemblyName = ResolveAssemblyName(assembly);
                var fallbackName = assemblyName?.Name ?? "<unknown>";
                var id = manifest == null ? fallbackName : ReadManifestId(manifest) ?? fallbackName;
                var name = manifest == null ? fallbackName : ReadManifestName(manifest) ?? fallbackName;

                return new(
                    id,
                    name,
                    manifest == null ? null : ReadManifestVersion(manifest),
                    assembly);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to inspect a loaded mod assembly for compatibility diagnostics: {ex.Message}");
                return null;
            }
        }

        private static RitsuModInfo? TryBuildModInfo(Mod mod)
        {
            try
            {
                var manifest = ReadManifest(mod);
                var assembly = ReadAssembly(mod);
                var assemblyName = ResolveAssemblyName(assembly);
                var errors = ReadErrors(mod);
                var fallbackName = assemblyName?.Name ?? "<unknown>";
                var id = manifest == null ? fallbackName : ReadManifestId(manifest) ?? fallbackName;
                var name = manifest == null ? fallbackName : ReadManifestName(manifest) ?? fallbackName;

                return new(
                    id,
                    name,
                    manifest == null ? null : ReadManifestAuthor(manifest),
                    manifest == null ? null : ReadManifestVersion(manifest),
                    ParseLoadState(ReadLoadState(mod, errors.Count)),
                    ParseSource(ReadSource(mod)),
                    manifest == null || ReadManifestAffectsGameplay(manifest),
                    assemblyName?.Name,
                    assemblyName?.Version?.ToString(),
                    errors,
                    TryGetWorkshopItemId(mod));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to describe a registered mod for ModManager interop: {ex.Message}");
                return null;
            }
        }

        private static bool MatchesModQuery(RitsuModInfo entry, string? modId, RitsuModSource? source)
        {
            if (modId != null && !string.Equals(entry.Id, modId, StringComparison.Ordinal))
                return false;

            return source == null || entry.Source == source.Value;
        }

        private static int GetBestModInfoRank(RitsuModInfo entry)
        {
            var stateRank = entry.State switch
            {
                RitsuModLoadState.Loaded => 0,
                RitsuModLoadState.Pending => 1,
                RitsuModLoadState.AddedAtRuntime => 2,
                RitsuModLoadState.Failed => 3,
                RitsuModLoadState.Disabled => 4,
                RitsuModLoadState.DisabledDuplicate => 5,
                _ => 6,
            };

            var sourceRank = entry.Source switch
            {
                RitsuModSource.ModsDirectory => 0,
                RitsuModSource.SteamWorkshop => 1,
                _ => 2,
            };

            return stateRank * 10 + sourceRank;
        }

        private static RitsuModSource ParseSource(string source)
        {
            return source switch
            {
                nameof(ModSource.ModsDirectory) => RitsuModSource.ModsDirectory,
                nameof(ModSource.SteamWorkshop) => RitsuModSource.SteamWorkshop,
                _ => RitsuModSource.Unknown,
            };
        }

        private static RitsuModLoadState ParseLoadState(string state)
        {
            return state switch
            {
                nameof(ModLoadState.None) => RitsuModLoadState.Pending,
                nameof(ModLoadState.Loaded) => RitsuModLoadState.Loaded,
                nameof(ModLoadState.Failed) => RitsuModLoadState.Failed,
                nameof(ModLoadState.Disabled) => RitsuModLoadState.Disabled,
                "DisabledDuplicate" => RitsuModLoadState.DisabledDuplicate,
                nameof(ModLoadState.AddedAtRuntime) => RitsuModLoadState.AddedAtRuntime,
                _ => RitsuModLoadState.Unknown,
            };
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

        private static Func<Mod, string?> CreateModPathAccessor()
        {
            if (typeof(Mod).GetField("path", InstanceMemberFlags) != null)
                return static mod => mod.path;

            var getter = CreateUntypedMemberGetter(typeof(Mod), "path");
            return mod => getter?.Invoke(mod) as string;
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
                try
                {
                    return FastMethodInvoker.CreateInstanceGetter(field);
                }
                catch
                {
                    return field.GetValue;
                }

            var property = type.GetProperty(memberName, InstanceMemberFlags);
            if (property == null)
                return null;

            try
            {
                return FastMethodInvoker.CreateInstanceGetter(property);
            }
            catch
            {
                return property.GetValue;
            }
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

    internal sealed record Sts2LoadedModAssemblyEntry(
        string Id,
        string Name,
        string? Version,
        Assembly Assembly);
}
