using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Interop.Patches;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Extensible pipeline invoked from <see cref="ModTypeDiscoveryPatch" /> (early localization init),
    ///     mirroring BaseLib's post-mod-init scan without hard-wiring a single feature.
    ///     从 <see cref="ModTypeDiscoveryPatch" /> 调用的可扩展管线（早期本地化初始化），
    ///     对应 BaseLib 的 post-mod-init 扫描，但不硬编码到单一功能。
    /// </summary>
    public static class ModTypeDiscoveryHub
    {
        private static readonly Lock Gate = new();
        private static readonly List<IModTypeDiscoveryContributor> Contributors = [];

        private static readonly Dictionary<string, List<Assembly>> RegisteredAssembliesByModId =
            new(StringComparer.Ordinal);

        private static bool _builtInsRegistered;

        /// <summary>
        ///     Registers a contributor. Call from your mod initializer before framework patch application
        ///     if you rely on custom discovery; otherwise built-ins are registered from <see cref="RitsuLibFramework" />.
        ///     注册一个 contributor。如果依赖自定义 discovery，请在 framework patch application 前
        ///     从你的 mod initializer 调用；否则内置项会从 <see cref="RitsuLibFramework" /> 注册。
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
        ///     Registers a mod assembly for the one-shot discovery pipeline. Mods should call this from their initializer
        ///     before <see cref="ModTypeDiscoveryPatch" /> runs. On hosts that expose mod assembly association,
        ///     RitsuLib also forwards the registration to the game through that API.
        ///     为一次性 discovery 管线注册一个 mod assembly。mod 应在其 initializer 中、
        ///     <see cref="ModTypeDiscoveryPatch" /> 运行前调用。若宿主提供 mod assembly 关联 API，
        ///     RitsuLib 也会通过该 API 把注册同步给游戏本体。
        /// </summary>
        public static void RegisterModAssembly(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);

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

            Sts2ModManagerCompat.TryAssociateAssemblyWithMod(modId, assembly);
        }

        /// <summary>
        ///     Logs the current contributor list and registered mod assembly map to the RitsuLib logger.
        ///     将当前 contributor 列表及已注册 mod assembly 映射输出到 RitsuLib logger。
        /// </summary>
        public static void LogDiagnostics()
        {
            Dictionary<string, IReadOnlyList<Assembly>> assemblySnapshot;
            IModTypeDiscoveryContributor[] contributorSnapshot;
            lock (Gate)
            {
                assemblySnapshot = RegisteredAssembliesByModId.ToDictionary(
                    static pair => pair.Key,
                    static pair => (IReadOnlyList<Assembly>)pair.Value.ToArray(),
                    StringComparer.Ordinal);
                contributorSnapshot = Contributors.ToArray();
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
                registeredAssemblies = RegisteredAssembliesByModId.ToDictionary(
                    static pair => pair.Key,
                    static pair => (IReadOnlyList<Assembly>)pair.Value.ToArray(),
                    StringComparer.Ordinal);
                snapshot = Contributors.ToArray();
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

            var result = new List<Assembly>();
            AddRange(Sts2ModManagerCompat.GetLoadedModAssemblies(modId));

            lock (Gate)
            {
                if (RegisteredAssembliesByModId.TryGetValue(modId, out var registeredAssemblies))
                    AddRange(registeredAssemblies);
            }

            if (legacyFallback != null)
                Add(legacyFallback);

            return result.ToArray();

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
                Sts2ModManagerCompat.TryAssociateAssemblyWithMod(modId, assembly);
        }

        private readonly record struct ScanAssemblyEntry(string ModId, Assembly Assembly);
    }
}
