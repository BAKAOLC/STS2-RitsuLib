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

        private static readonly Dictionary<string, Assembly> RegisteredAssembliesByModId =
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
        ///     before <see cref="ModTypeDiscoveryPatch" /> runs.
        ///     为一次性 discovery 管线注册一个 mod assembly。mod 应在其 initializer 中、
        ///     <see cref="ModTypeDiscoveryPatch" /> 运行前调用。
        /// </summary>
        public static void RegisterModAssembly(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);

            lock (Gate)
            {
                RegisteredAssembliesByModId[modId] = assembly;
            }
        }

        /// <summary>
        ///     Logs the current contributor list and registered mod assembly map to the RitsuLib logger.
        ///     将当前 contributor 列表及已注册 mod assembly 映射输出到 RitsuLib logger。
        /// </summary>
        public static void LogDiagnostics()
        {
            Dictionary<string, Assembly> assemblySnapshot;
            IModTypeDiscoveryContributor[] contributorSnapshot;
            lock (Gate)
            {
                assemblySnapshot = new(RegisteredAssembliesByModId, StringComparer.Ordinal);
                contributorSnapshot = Contributors.ToArray();
            }

            RitsuLibFramework.Logger.Info("[ModTypeDiscoveryHub] Diagnostics:");
            RitsuLibFramework.Logger.Info($"  Contributors ({contributorSnapshot.Length}):");
            foreach (var c in contributorSnapshot)
                RitsuLibFramework.Logger.Info($"    - {c.GetType().FullName}");
            RitsuLibFramework.Logger.Info($"  Registered assemblies ({assemblySnapshot.Count}):");
            foreach (var (modId, assembly) in assemblySnapshot.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                RitsuLibFramework.Logger.Info($"    - {modId} → {assembly.GetName().Name}");
        }

        internal static void EnsureBuiltInContributorsRegistered()
        {
            lock (Gate)
            {
                if (_builtInsRegistered)
                    return;
                Contributors.Add(new ModInteropTypeDiscoveryContributor());
                Contributors.Add(new AttributeAutoRegistrationTypeDiscoveryContributor());
                _builtInsRegistered = true;
            }
        }

        internal static void RunOnce(Harmony harmony)
        {
            Dictionary<string, Assembly> scanMap;
            IModTypeDiscoveryContributor[] snapshot;
            lock (Gate)
            {
                scanMap = new(RegisteredAssembliesByModId, StringComparer.Ordinal);
                snapshot = Contributors.ToArray();
            }

            var targetMap = BuildTargetAssemblyMap(scanMap);
            var orderedAssemblies = scanMap
                .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
                .Select(static kv => kv.Value)
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

        private static IReadOnlyDictionary<string, Assembly> BuildTargetAssemblyMap(
            IReadOnlyDictionary<string, Assembly> registeredAssembliesByModId)
        {
            var result = new Dictionary<string, Assembly>(
                Sts2ModManagerCompat.BuildLoadedModAssembliesByManifestId(),
                StringComparer.Ordinal);

            foreach (var (modId, assembly) in registeredAssembliesByModId)
                result[modId] = assembly;

            return result;
        }
    }
}
