using System.Reflection;
using System.Text;
using HarmonyLib;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Interop.Patches;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Extensible post-mod-load type-discovery pipeline invoked during early localization initialization.
    ///         It mirrors BaseLib's scan timing without coupling discovery to one feature.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在本地化初始化早期调用的可扩展 mod 加载后类型发现管线。它与 BaseLib 的扫描时机保持一致，
    ///         但不会将发现流程绑定到单一功能。
    ///     </para>
    /// </summary>
    public static class ModTypeDiscoveryHub
    {
        private static readonly Lock Gate = new();
        private static readonly List<IModTypeDiscoveryContributor> Contributors = [];

        private static readonly Dictionary<string, List<Assembly>> RegisteredAssembliesByModId =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<string, AssemblyModIdMismatch> AssemblyModIdMismatches =
            new(StringComparer.Ordinal);

        private static bool _builtInsRegistered;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a discovery contributor. Custom contributors must be registered from the mod initializer
        ///         before framework patches are applied; <see cref="RitsuLibFramework" /> registers built-ins.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册类型发现贡献器。自定义贡献器必须在框架补丁应用前由 mod 初始化器注册；
        ///         内置贡献器由 <see cref="RitsuLibFramework" /> 注册。
        ///     </para>
        /// </summary>
        public static void RegisterContributor(IModTypeDiscoveryContributor contributor)
        {
            ArgumentNullException.ThrowIfNull(contributor);
            lock (Gate)
            {
                Contributors.Add(contributor);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Associates an assembly with a mod for the one-shot discovery pipeline. Call it from the mod
        ///         initializer before <see cref="ModTypeDiscoveryPatch" /> runs. On hosts that expose mod-assembly
        ///         associations, RitsuLib also forwards the association to the game after mod initialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为一次性类型发现管线建立程序集与 mod 的关联。请在 mod 初始化器中、且在
        ///         <see cref="ModTypeDiscoveryPatch" /> 运行前调用。若宿主公开 mod 与程序集关联 API，
        ///         RitsuLib 还会在 mod 初始化完成后将该关联同步给游戏。
        ///     </para>
        /// </summary>
        public static void RegisterModAssembly(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);
            modId = modId.Trim();

            lock (Gate)
            {
                if (!RegisteredAssembliesByModId.TryGetValue(modId, out var assemblies))
                {
                    assemblies = [];
                    RegisteredAssembliesByModId[modId] = assemblies;
                }

                if (!assemblies.Contains(assembly))
                    assemblies.Add(assembly);
            }

            RecordAssemblyModIdMismatch(modId, assembly);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Logs the current contributor list and registered mod-to-assembly map.
        ///     </para>
        ///     <para xml:lang="zh-CN">将当前贡献器列表和已注册的 mod 至程序集映射写入 RitsuLib 日志。</para>
        /// </summary>
        public static void LogDiagnostics()
        {
            Dictionary<string, IReadOnlyList<Assembly>> assemblySnapshot;
            IModTypeDiscoveryContributor[] contributorSnapshot;
            lock (Gate)
            {
                assemblySnapshot = RegisteredAssembliesByModId.ToDictionary(
                    static pair => pair.Key,
                    static IReadOnlyList<Assembly> (pair) => [.. pair.Value],
                    StringComparer.Ordinal);
                contributorSnapshot = [.. Contributors];
            }

            RitsuLibFramework.Logger.Info("[ModTypeDiscoveryHub] Diagnostics:");
            RitsuLibFramework.Logger.Info($"  Contributors ({contributorSnapshot.Length}):");
            foreach (var c in contributorSnapshot)
                RitsuLibFramework.Logger.Info($"    - {c.GetType().FullName}");
            RitsuLibFramework.Logger.Info(
                $"  Registered assemblies ({assemblySnapshot.Sum(static pair => pair.Value.Count)}):");
            foreach (var (modId, assemblies) in assemblySnapshot.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
            foreach (var assembly in assemblies.OrderBy(static assembly => assembly.GetName().Name,
                         StringComparer.Ordinal))
                RitsuLibFramework.Logger.Info($"    - {modId} -> {assembly.GetName().Name}");
        }

        internal static void EnsureBuiltInContributorsRegistered()
        {
            lock (Gate)
            {
                if (_builtInsRegistered)
                    return;
                Contributors.Add(new ModInteropTypeDiscoveryContributor());
                Contributors.Add(new SavedAttachedStateTypeDiscoveryContributor());
                Contributors.Add(new AttributeAutoRegistrationTypeDiscoveryContributor());
                _builtInsRegistered = true;
            }
        }

        internal static void RunOnce(Harmony harmony)
        {
            Dictionary<string, IReadOnlyList<Assembly>> registeredAssemblies;
            IModTypeDiscoveryContributor[] snapshot;
            lock (Gate)
            {
                registeredAssemblies = new(StringComparer.Ordinal);
                foreach (var pair in RegisteredAssembliesByModId)
                    registeredAssemblies.Add(pair.Key, [.. pair.Value]);
                snapshot = [.. Contributors];
            }

            AlignRegisteredAssembliesWithGame(registeredAssemblies);

            var targetMap = BuildTargetAssemblyMap(registeredAssemblies);
            var orderedAssemblies = BuildScanAssemblyMap(registeredAssemblies)
                .OrderBy(static kv => kv.ModId, StringComparer.Ordinal)
                .ThenBy(static kv => kv.Assembly.GetName().Name, StringComparer.Ordinal)
                .Select(static kv => kv.Assembly)
                .Distinct()
                .ToArray();

            foreach (var assembly in orderedAssemblies)
            {
                var modTypes = AssemblyTypeScanHelper.GetLoadableTypes(assembly, RitsuLibFramework.Logger)
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                    .ToArray();

                foreach (var modType in modTypes)
                foreach (var contributor in snapshot)
                    contributor.Contribute(harmony, targetMap, modType);
            }
        }

        internal static bool TryResolveRegisteredModId(Assembly assembly, out string modId)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            lock (Gate)
            {
                foreach (var (candidateModId, assemblies) in RegisteredAssembliesByModId)
                    if (assemblies.Contains(assembly))
                    {
                        modId = candidateModId;
                        return true;
                    }
            }

            modId = "";
            return false;
        }

        internal static IReadOnlyList<Assembly> GetKnownAssembliesForMod(string modId, Assembly? legacyFallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            modId = modId.Trim();

            var result = new List<Assembly>();
            AddRange(Sts2ModManagerCompat.GetLoadedModAssemblies(modId));

            lock (Gate)
            {
                if (RegisteredAssembliesByModId.TryGetValue(modId, out var registeredAssemblies))
                    AddRange(registeredAssemblies);
            }

            if (legacyFallback != null)
                Add(legacyFallback);

            return [.. result];

            void AddRange(IEnumerable<Assembly> assemblies)
            {
                foreach (var assembly in assemblies)
                    Add(assembly);
            }

            void Add(Assembly assembly)
            {
                if (!result.Contains(assembly))
                    result.Add(assembly);
            }
        }

        internal static void LogAutoRegistrationModIdMismatchSummary()
        {
            AssemblyModIdMismatch[] mismatches;
            lock (Gate)
            {
                mismatches =
                [
                    .. AssemblyModIdMismatches.Values
                        .OrderBy(static mismatch => mismatch.CurrentEntryOwnerModId, StringComparer.Ordinal)
                        .ThenBy(static mismatch => mismatch.RegisteredModId, StringComparer.Ordinal)
                        .ThenBy(static mismatch => mismatch.AssemblyName, StringComparer.Ordinal),
                ];
            }

            if (mismatches.Length == 0)
                return;

            var text = new StringBuilder()
                .AppendLine()
                .AppendLine("=== RitsuLib Auto-Registration Mod Id Mismatch Summary ===")
                .AppendLine(
                    "RitsuLib detected assemblies whose ModManager/mod_manifest.json assembly ownership id differs " +
                    "from the ModTypeDiscoveryHub.RegisterModAssembly argument.")
                .AppendLine(
                    "This issue only affects RitsuLib auto-discovered types and attributes from those assemblies. " +
                    "Explicit content-pack registrations created with RitsuLibFramework.CreateContentPack or " +
                    "ModContentPackBuilder.For are not affected; attribute-driven pack helpers on auto-discovered " +
                    "types are affected.")
                .AppendLine(
                    "Current auto-registration owner/registry ids are resolved from ModManager/mod_manifest.json " +
                    "assembly ownership. This can also determine default public entries for auto-discovered models. " +
                    "A future major RitsuLib release is expected to use the RegisterModAssembly argument as the " +
                    "primary owner id instead.")
                .AppendLine(
                    "Mod authors should align their manifest id and runtime mod id, or prepare localization/save " +
                    "compatibility before that update.")
                .AppendLine(
                    "If a mod intentionally keeps these ids different, annotate the auto-registration source types " +
                    "with [RitsuLibOwnedBy(\"...\")] to pin those auto-discovered entries to a fixed owner id. " +
                    "For inherited auto-registration attributes, place [RitsuLibOwnedBy(\"...\")] on the type that " +
                    "declares the inherited registration attribute.")
                .AppendLine("Mismatches:");

            foreach (var mismatch in mismatches)
                text.AppendLine(
                    $"  - assembly='{mismatch.AssemblyName}', currentAutoRegistrationOwnerId='" +
                    $"{mismatch.CurrentEntryOwnerModId}' " +
                    $"(source: ModManager/mod_manifest.json assembly ownership), registerModAssemblyArgument='" +
                    $"{mismatch.RegisteredModId}' (source: ModTypeDiscoveryHub.RegisterModAssembly argument)");

            RitsuLibFramework.Logger.Warn(text.ToString().TrimEnd());
        }

        private static IReadOnlyDictionary<string, Assembly> BuildTargetAssemblyMap(
            IReadOnlyDictionary<string, IReadOnlyList<Assembly>> registeredAssembliesByModId)
        {
            var result = new Dictionary<string, Assembly>(
                Sts2ModManagerCompat.BuildLoadedModAssembliesByManifestId(),
                StringComparer.Ordinal);

            foreach (var (modId, assemblies) in registeredAssembliesByModId)
                if (assemblies.Count > 0)
                    result[modId] = assemblies[0];

            return result;
        }

        private static IReadOnlyList<ScanAssemblyEntry> BuildScanAssemblyMap(
            IReadOnlyDictionary<string, IReadOnlyList<Assembly>> registeredAssembliesByModId)
        {
            var result = new List<ScanAssemblyEntry>();

            foreach (var (modId, assemblies) in Sts2ModManagerCompat.BuildLoadedModAssemblyListsByManifestId())
            foreach (var assembly in assemblies)
                Add(modId, assembly);

            foreach (var (modId, assemblies) in registeredAssembliesByModId)
            foreach (var assembly in assemblies)
                Add(modId, assembly);

            return result;

            void Add(string modId, Assembly assembly)
            {
                if (result.Any(entry => entry.Assembly == assembly))
                    return;

                result.Add(new(modId, assembly));
            }
        }

        private static void AlignRegisteredAssembliesWithGame(
            IReadOnlyDictionary<string, IReadOnlyList<Assembly>> registeredAssembliesByModId)
        {
            foreach (var (modId, assemblies) in registeredAssembliesByModId)
            foreach (var assembly in assemblies)
            {
                RecordAssemblyModIdMismatch(modId, assembly);
                Sts2ModManagerCompat.TryAssociateAssemblyWithMod(modId, assembly);
            }
        }

        private static void RecordAssemblyModIdMismatch(string registeredModId, Assembly assembly)
        {
            if (!Sts2ModManagerCompat.TryGetLoadedModIdForAssembly(assembly, out var manifestModId))
                return;

            if (string.Equals(manifestModId, registeredModId, StringComparison.Ordinal))
                return;

            var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "<unknown>";
            var warningKey = $"{assembly.FullName}\0{manifestModId}\0{registeredModId}";
            lock (Gate)
            {
                AssemblyModIdMismatches.TryAdd(warningKey,
                    new(assemblyName, manifestModId, registeredModId));
            }
        }

        private readonly record struct AssemblyModIdMismatch(
            string AssemblyName,
            string CurrentEntryOwnerModId,
            string RegisteredModId);

        private readonly record struct ScanAssemblyEntry(string ModId, Assembly Assembly);
    }
}
