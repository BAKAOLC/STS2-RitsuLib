using System.Reflection;
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
    ///         在本地化初始化早期调用的可扩展模组加载后类型发现管线。它与 BaseLib 的扫描时机保持一致，
    ///         但不会将发现流程绑定到单一功能。
    ///     </para>
    /// </summary>
    public static class ModTypeDiscoveryHub
    {
        private static readonly Lock Gate = new();
        private static readonly List<IModTypeDiscoveryContributor> Contributors = [];

        private static readonly Dictionary<string, List<Assembly>> RegisteredAssembliesByModId =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<Assembly, string> RegisteredModIdsByAssembly = [];

        private static bool _builtInsRegistered;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a discovery contributor. Custom contributors must be registered from the mod initializer
        ///         before the discovery pipeline runs; <see cref="RitsuLibFramework" /> registers built-ins.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册类型发现贡献器。自定义贡献器必须在类型发现管线运行前由模组初始化器注册；
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
        ///         initializer before <see cref="ModTypeDiscoveryPatch" /> runs. Auto-registration uses this ID before
        ///         the manifest ID unless the registration's declaring type specifies <see cref="RitsuLibOwnedByAttribute" />.
        ///         Repeating the same association has no effect. Existing game assembly ownership is preserved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为一次性类型发现管线建立程序集与模组的关联。请在模组初始化器中、且在
        ///         <see cref="ModTypeDiscoveryPatch" /> 运行前调用。自动注册优先使用此 ID，而非清单 ID；
        ///         注册特性声明类型上的 <see cref="RitsuLibOwnedByAttribute" /> 优先级更高。
        ///         重复建立相同关联没有效果，游戏已有的程序集归属保持不变。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">Case-sensitive owner ID; surrounding whitespace is removed.</para>
        ///     <para xml:lang="zh-CN">区分大小写的归属 ID，会移除首尾空白。</para>
        /// </param>
        /// <param name="assembly">
        ///     <para xml:lang="en">Assembly to scan; each assembly can have one registered owner.</para>
        ///     <para xml:lang="zh-CN">待扫描的程序集；每个程序集只能注册一个归属。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">An argument is null.</para>
        ///     <para xml:lang="zh-CN">参数为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en"><paramref name="modId" /> is empty or whitespace.</para>
        ///     <para xml:lang="zh-CN"><paramref name="modId" /> 为空或仅包含空白。</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The assembly is already registered with a different owner ID.</para>
        ///     <para xml:lang="zh-CN">程序集已注册到其他归属 ID。</para>
        /// </exception>
        public static void RegisterModAssembly(string modId, Assembly assembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(assembly);
            modId = modId.Trim();

            lock (Gate)
            {
                if (RegisteredModIdsByAssembly.TryGetValue(assembly, out var registeredModId))
                {
                    if (!string.Equals(registeredModId, modId, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"Assembly '{assembly.FullName}' is already registered to mod '{registeredModId}'.");
                    return;
                }

                if (!RegisteredAssembliesByModId.TryGetValue(modId, out var assemblies))
                {
                    assemblies = [];
                    RegisteredAssembliesByModId[modId] = assemblies;
                }

                assemblies.Add(assembly);
                RegisteredModIdsByAssembly.Add(assembly, modId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Logs the current contributor list and registered mod-to-assembly map.
        ///     </para>
        ///     <para xml:lang="zh-CN">将当前贡献器列表和已注册的模组到程序集映射写入 RitsuLib 日志。</para>
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
                if (RegisteredModIdsByAssembly.TryGetValue(assembly, out modId!))
                    return true;
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

            foreach (var (modId, assemblies) in registeredAssembliesByModId)
            foreach (var assembly in assemblies)
                Add(modId, assembly);

            foreach (var (modId, assemblies) in Sts2ModManagerCompat.BuildLoadedModAssemblyListsByManifestId())
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
            {
                var hostModId = modId;
                foreach (var assembly in assemblies)
                    if (Sts2ModManagerCompat.TryGetLoadedModIdForAssembly(assembly, out var loadedModId))
                    {
                        hostModId = loadedModId;
                        break;
                    }

                foreach (var assembly in assemblies)
                    if (!Sts2ModManagerCompat.TryGetLoadedModIdForAssembly(assembly, out _))
                        Sts2ModManagerCompat.TryAssociateAssemblyWithMod(hostModId, assembly);
            }
        }

        private readonly record struct ScanAssemblyEntry(string ModId, Assembly Assembly);
    }
}
