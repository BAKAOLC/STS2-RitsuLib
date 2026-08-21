using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">Provides version-compatible access to the host <see cref="ModManager" />.</para>
    ///     <para xml:lang="zh-CN">提供兼容不同游戏版本的宿主 <see cref="ModManager" /> 访问方式。</para>
    /// </summary>
    internal static class Sts2ModManagerCompat
    {
        private const string RitsuLibManifestId = "STS2-RitsuLib";

        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly Func<Mod, ModManifest?> ReadManifest = CreateModManifestAccessor();
        private static readonly Func<Mod, IReadOnlyList<Assembly>> ReadAssemblies = CreateModAssembliesAccessor();
        private static readonly MethodInfo? AssociateAssemblyWithModMethod = CreateAssociateAssemblyWithModMethod();

        private static readonly Func<Mod, Assembly?> ReadAssembly =
            static mod => ReadAssemblies(mod).FirstOrDefault();

        private static readonly Func<Mod, IReadOnlyList<LocString>> ReadErrors = CreateModErrorsAccessor();
        private static readonly Func<Mod, string> ReadSource = CreateModSourceAccessor();
        private static readonly Func<Mod, string?> ReadPath = CreateModPathAccessor();
        private static readonly Func<Mod, ulong?> ReadWorkshopItemId = CreateModWorkshopItemIdAccessor();
        private static readonly Func<Mod, int, string> ReadLoadState = CreateLoadStateAccessor();

        private static readonly Func<ModManifest, string?> ReadManifestId =
            CreateManifestStringAccessor("id", static manifest => manifest.id);

        private static readonly Func<ModManifest, string?> ReadManifestName =
            CreateManifestStringAccessor("name", static manifest => manifest.name);

        private static readonly Func<ModManifest, string?> ReadManifestAuthor =
            CreateManifestStringAccessor("author", static manifest => manifest.author);

        private static readonly Func<ModManifest, string?> ReadManifestDescription =
            CreateManifestStringAccessor("description", static manifest => manifest.description);

        private static readonly Func<ModManifest, string?> ReadManifestVersion =
            CreateManifestStringAccessor("version", static manifest => manifest.version);

        private static readonly Func<ModManifest, bool> ReadManifestAffectsGameplay =
            CreateManifestBoolAccessor("affectsGameplay", static manifest => manifest.affectsGameplay, true);

        internal static IEnumerable<Mod> EnumerateLoadedModsWithAssembly()
        {
            return ModManager.GetLoadedMods();
        }

        internal static Assembly? GetAssembly(Mod mod)
        {
            return ReadAssembly(mod);
        }

        internal static IReadOnlyList<Assembly> GetAssemblies(Mod mod)
        {
            return ReadAssemblies(mod);
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

        internal static IReadOnlyDictionary<string, IReadOnlyList<Assembly>> BuildLoadedModAssemblyListsByManifestId()
        {
            var result = new Dictionary<string, IReadOnlyList<Assembly>>(StringComparer.Ordinal);

            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    var manifest = ReadManifest(mod);
                    var modId = manifest == null ? null : ReadManifestId(manifest);
                    var assemblies = ReadAssemblies(mod)
                        .Where(static assembly => assembly != null)
                        .Distinct()
                        .ToArray();
                    if (string.IsNullOrWhiteSpace(modId) || assemblies.Length == 0)
                        continue;

                    result[modId] = assemblies;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Failed to inspect loaded mod assemblies for discovery interop: {ex.Message}");
                }

            return result;
        }

        internal static IReadOnlyList<Sts2LoadedModAssemblyEntry> BuildLoadedModAssemblyEntries()
        {
            return
            [
                .. EnumerateLoadedModsWithAssembly()
                    .SelectMany(TryBuildLoadedModAssemblyEntries)
                    .Where(entry => entry != null)
                    .Select(entry => entry),
            ];
        }

        internal static bool IsGameplayRelevantLoadedModType(Type type)
        {
            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    if (!ReadAssemblies(mod).Contains(type.Assembly))
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

        internal static bool TryGetLoadedModIdForAssembly(Assembly assembly, out string modId)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    if (!ReadAssemblies(mod).Contains(assembly))
                        continue;

                    var manifest = ReadManifest(mod);
                    modId = manifest == null ? "" : ReadManifestId(manifest) ?? "";
                    return !string.IsNullOrWhiteSpace(modId);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Failed to resolve loaded mod ownership for assembly '{assembly.FullName}': {ex.Message}");
                    break;
                }

            modId = "";
            return false;
        }

        internal static IReadOnlyList<Assembly> GetLoadedModAssemblies(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            foreach (var mod in EnumerateLoadedModsWithAssembly())
                try
                {
                    var manifest = ReadManifest(mod);
                    if (manifest == null ||
                        !string.Equals(ReadManifestId(manifest), modId, StringComparison.Ordinal))
                        continue;

                    return [.. ReadAssemblies(mod).Distinct()];
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Compat] Failed to resolve loaded assemblies for mod '{modId}': {ex.Message}");
                    return [];
                }

            return [];
        }

        internal static bool TryAssociateAssemblyWithMod(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);

            if (TryGetLoadedModIdForAssembly(assembly, out var existingModId))
            {
                if (string.Equals(existingModId, modId, StringComparison.Ordinal))
                    return true;

                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Assembly '{assembly.FullName}' is already associated with mod '{existingModId}', not '{modId}'.");
                return false;
            }

            if (AssociateAssemblyWithModMethod == null)
                return false;

            try
            {
                AssociateAssemblyWithModMethod.Invoke(null, [modId, assembly]);
                return TryGetKnownModIdForAssembly(assembly, out var associatedModId) &&
                       string.Equals(associatedModId, modId, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to associate assembly '{assembly.FullName}' with mod '{modId}': {ex.Message}");
                return false;
            }
        }

        private static bool TryGetKnownModIdForAssembly(Assembly assembly, out string modId)
        {
            foreach (var mod in EnumerateModsForManifestLookup())
                try
                {
                    if (!ReadAssemblies(mod).Contains(assembly))
                        continue;

                    var manifest = ReadManifest(mod);
                    modId = manifest == null ? string.Empty : ReadManifestId(manifest) ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(modId);
                }
                catch
                {
                    // Skip entries whose version-specific accessors are unavailable.
                }

            modId = string.Empty;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enumerates all known mods, including disabled and unloaded entries, for manifest metadata lookup.
        ///     </para>
        ///     <para xml:lang="zh-CN">枚举所有已知模组，包括已禁用和未加载的条目，供查询清单元数据。</para>
        /// </summary>
        internal static IEnumerable<Mod> EnumerateModsForManifestLookup()
        {
            return ModManager.Mods;
        }

        internal static IReadOnlyList<Sts2ModInventoryEntry> BuildModInventoryEntries()
        {
            return
            [
                .. EnumerateModsForManifestLookup()
                    .Select(TryBuildModInventoryEntry)
                    .Where(entry => entry != null)
                    .Select(entry => entry!)
                    .OrderBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.AssemblyName ?? "", StringComparer.OrdinalIgnoreCase),
            ];
        }

        internal static IReadOnlyList<Sts2ModInventoryEntry> BuildLoadedModInventoryEntries()
        {
            return
            [
                .. EnumerateLoadedModsWithAssembly()
                    .Select(TryBuildModInventoryEntry)
                    .Where(entry => entry != null)
                    .Select(entry => entry!),
            ];
        }

        internal static IReadOnlyList<RitsuModInfo> BuildModInfos(string? modId = null, RitsuModSource? source = null)
        {
            return
            [
                .. EnumerateModsForManifestLookup()
                    .Select(TryBuildModInfo)
                    .Where(entry => entry != null)
                    .Select(entry => entry!)
                    .Where(entry => MatchesModQuery(entry, modId, source))
                    .OrderBy(GetBestModInfoRank)
                    .ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Source),
            ];
        }

        internal static ulong? TryGetWorkshopItemId(Mod mod)
        {
            ArgumentNullException.ThrowIfNull(mod);
            try
            {
                if (ReadWorkshopItemId(mod) is { } workshopItemId && workshopItemId > 0)
                    return workshopItemId;

                if (SteamWorkshopInstallSource.TryGetWorkshopItemIdFromPath(ReadPath(mod), out var pathItemId) &&
                    pathItemId > 0)
                    return pathItemId;

                foreach (var assembly in ReadAssemblies(mod))
                    if (SteamWorkshopInstallSource.TryGetWorkshopItemIdFromAssembly(assembly, out var assemblyItemId) &&
                        assemblyItemId > 0)
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

        internal static bool TryGetBestModPresentationInfo(string modId, out RitsuModPresentationInfo? info)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            var manifestId = string.Equals(modId, Const.ModId, StringComparison.OrdinalIgnoreCase)
                ? RitsuLibManifestId
                : modId;

            info = EnumerateModsForManifestLookup()
                .Select(TryBuildModPresentationInfo)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .Where(entry => string.Equals(entry.Id, manifestId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Rank)
                .FirstOrDefault();
            return info != null;
        }

        internal static bool TryGetBestModPresentationInfoForAssembly(Assembly assembly,
            out RitsuModPresentationInfo? info)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            info = EnumerateModsForManifestLookup()
                .Where(mod => TryModOwnsAssembly(mod, assembly))
                .Select(TryBuildModPresentationInfo)
                .Where(entry => entry != null)
                .Select(entry => entry!)
                .OrderBy(entry => entry.Rank)
                .FirstOrDefault();
            return info != null;
        }

        private static Sts2ModInventoryEntry? TryBuildModInventoryEntry(Mod mod)
        {
            try
            {
                var manifest = ReadManifest(mod);
                var assemblies = ReadAssemblies(mod);
                var assembly = assemblies.FirstOrDefault();
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
                    errors,
                    CommonIncompatibleModRegistry.IsMatch(assemblies),
                    TryGetWorkshopItemId(mod));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to describe a registered mod for inventory telemetry: {ex.Message}");
                return null;
            }
        }

        private static IReadOnlyList<Sts2LoadedModAssemblyEntry> TryBuildLoadedModAssemblyEntries(Mod mod)
        {
            try
            {
                var manifest = ReadManifest(mod);
                var assemblies = ReadAssemblies(mod).Distinct().ToArray();
                if (assemblies.Length == 0)
                    return [];

                var primaryAssemblyName = ResolveAssemblyName(assemblies.FirstOrDefault());
                var fallbackName = primaryAssemblyName?.Name ?? "<unknown>";
                var id = manifest == null ? fallbackName : ReadManifestId(manifest) ?? fallbackName;
                var name = manifest == null ? fallbackName : ReadManifestName(manifest) ?? fallbackName;

                return
                [
                    .. assemblies
                        .Select(assembly => new Sts2LoadedModAssemblyEntry(
                            id,
                            name,
                            manifest == null ? null : ReadManifestVersion(manifest),
                            assembly)),
                ];
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to inspect a loaded mod assembly for compatibility diagnostics: {ex.Message}");
                return [];
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

        private static RitsuModPresentationInfo? TryBuildModPresentationInfo(Mod mod)
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
                var source = ParseSource(ReadSource(mod));
                var state = ParseLoadState(ReadLoadState(mod, errors.Count));
                return new(
                    id,
                    name,
                    manifest == null ? null : ReadManifestAuthor(manifest),
                    manifest == null ? null : ReadManifestVersion(manifest),
                    manifest == null ? null : ReadManifestDescription(manifest),
                    $"res://{id}/mod_image.png",
                    GetBestModPresentationRank(state, source));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to describe a registered mod for settings UI presentation: {ex.Message}");
                return null;
            }
        }

        private static bool TryModOwnsAssembly(Mod mod, Assembly assembly)
        {
            try
            {
                return ReadAssemblies(mod).Contains(assembly);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Compat] Failed to inspect mod assemblies for settings UI presentation: {ex.Message}");
                return false;
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

        private static int GetBestModPresentationRank(RitsuModLoadState state, RitsuModSource source)
        {
            var stateRank = state switch
            {
                RitsuModLoadState.Loaded => 0,
                RitsuModLoadState.Pending => 1,
                RitsuModLoadState.AddedAtRuntime => 2,
                RitsuModLoadState.Failed => 3,
                RitsuModLoadState.Disabled => 4,
                RitsuModLoadState.DisabledDuplicate => 5,
                _ => 6,
            };

            var sourceRank = source switch
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

        private static Func<Mod, IReadOnlyList<Assembly>> CreateModAssembliesAccessor()
        {
#if STS2_AT_LEAST_0_108_0
            if (typeof(Mod).GetField("assemblies", InstanceMemberFlags) != null)
                return static mod => mod.assemblies;
#else
            if (typeof(Mod).GetField("assembly", InstanceMemberFlags) != null)
                return static mod => mod.assembly == null ? [] : [mod.assembly];
#endif

            var assembliesGetter = CreateUntypedMemberGetter(typeof(Mod), "assemblies");
            if (assembliesGetter != null)
                return mod => NormalizeAssemblies(assembliesGetter.Invoke(mod) as IEnumerable<Assembly>);

            var getter = CreateUntypedMemberGetter(typeof(Mod), "assembly");
            return mod => getter?.Invoke(mod) is Assembly assembly ? [assembly] : [];
        }

        private static MethodInfo? CreateAssociateAssemblyWithModMethod()
        {
            return typeof(ModManager).GetMethod(
                "AssociateAssemblyWithMod",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(Assembly)],
                null);
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

        private static Func<Mod, ulong?> CreateModWorkshopItemIdAccessor()
        {
            var getter = CreateUntypedMemberGetter(typeof(Mod), "workshopId");
            return mod => getter?.Invoke(mod) as ulong?;
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

        private static IReadOnlyList<Assembly> NormalizeAssemblies(IEnumerable<Assembly>? assemblies)
        {
            return assemblies?.Where(assembly => assembly != null).ToArray() ?? [];
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
        IReadOnlyList<LocString> Errors,
        bool IsCommonIncompatibleMod,
        ulong? WorkshopItemId);

    internal sealed record Sts2LoadedModAssemblyEntry(
        string Id,
        string Name,
        string? Version,
        Assembly Assembly);

    internal sealed record RitsuModPresentationInfo(
        string Id,
        string Name,
        string? Author,
        string? Version,
        string? Description,
        string? ModImagePath,
        int Rank);
}
