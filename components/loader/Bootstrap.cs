using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Compat;
using STS2RitsuLib.Platform;

namespace STS2RitsuLib.Loader
{
    /// <summary>
    ///     <para xml:lang="en">Loads shared modules and the selected game compatibility runtime before initializing RitsuLib.</para>
    ///     <para xml:lang="zh-CN">加载公共模块和匹配游戏版本的兼容运行时，然后初始化 RitsuLib。</para>
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static class Bootstrap
    {
        private const string ModId = "STS2-RitsuLib";
        private static int _initializationState;
        private static readonly Lock VariantAssembliesLock = new();
        private static readonly List<Assembly> VariantAssemblies = [];
        private static Type[]? _variantModTypes;
        private static readonly MethodInfo? AssociateAssemblyWithModMethod = CreateAssociateAssemblyWithModMethod();
        private static readonly FieldInfo? ManifestField = typeof(Mod).GetField("manifest");
        private static readonly FieldInfo? ManifestIdField = typeof(ModManifest).GetField("id");
        private static readonly FieldInfo? AssembliesField = typeof(Mod).GetField("assemblies");
        private static bool _reflectionBridgePatched;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Validates and loads the complete module set once, registers mod types and Godot scripts, and
        ///         initializes the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">校验并一次性加载完整模块集，注册模组类型和 Godot 脚本，然后初始化框架。</para>
        /// </summary>
        public static void Initialize()
        {
            var previous = Interlocked.CompareExchange(ref _initializationState, 1, 0);
            if (previous == 2)
                return;
            if (previous != 0)
                throw new InvalidOperationException("RitsuLib initialization is already running or previously failed.");
            try
            {
                LinuxHarmonyNativePreloader.EnsureLoaded(
                    message => Log.Info($"[RitsuLib.Loader] {message}"),
                    message => Log.Warn($"[RitsuLib.Loader] {message}"));
                var loaderAssembly = typeof(Bootstrap).Assembly;
                var loaderDir = Path.GetDirectoryName(loaderAssembly.Location)
                                ?? throw new InvalidOperationException(
                                    "Could not resolve the RitsuLib loader directory.");
                var layout = BundleLayout.Read(loaderDir, Sts2HostVersion.Numeric);
                Log.Info(
                    $"[RitsuLib.Loader] Host={Sts2HostVersion.ReleaseLabel ?? Sts2HostVersion.Numeric?.ToString() ?? "unknown"}; runtime={layout.CompatTarget}.");
                var context = AssemblyLoadContext.GetLoadContext(loaderAssembly) ?? AssemblyLoadContext.Default;
                var resolver = new ModuleAssemblyResolver(context, layout);
                resolver.Install();
                var assemblies = layout.Modules.Select(module => resolver.Load(module.Name)).ToArray();
                foreach (var assembly in assemblies.Where(assembly =>
                             assembly.GetName().Name!.StartsWith("STS2-RitsuLib", StringComparison.Ordinal)))
                {
                    if (!AssociateVariantAssemblyWithGame(assembly))
                        RegisterVariantAssembly(assembly);
                }

                var runtime = assemblies.Single(assembly => assembly.GetName().Name == "STS2-RitsuLib.Runtime");
                var framework = runtime.GetType("STS2RitsuLib.RitsuLibFramework", true)!;
                var registerScripts = framework.GetMethod("EnsureGodotScriptsRegistered",
                                          BindingFlags.Public | BindingFlags.Static)
                                      ?? throw new MissingMethodException(framework.FullName,
                                          "EnsureGodotScriptsRegistered");
                foreach (var assembly in assemblies.Where(assembly =>
                             assembly.GetName().Name!.StartsWith("STS2-RitsuLib.", StringComparison.Ordinal)))
                    registerScripts.Invoke(null, [assembly, null]);
                var initialize = framework.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)
                                 ?? throw new MissingMethodException(framework.FullName, "Initialize");
                initialize.Invoke(null, null);
                if (framework.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.Static)
                        ?.GetValue(null) is not true)
                    throw new InvalidOperationException("RitsuLib runtime initialization did not complete.");
                Volatile.Write(ref _initializationState, 2);
            }
            finally
            {
                if (Volatile.Read(ref _initializationState) != 2)
                    Volatile.Write(ref _initializationState, -1);
            }
        }

        internal static Type[] GetVariantModTypes()
        {
            lock (VariantAssembliesLock)
            {
                if (_variantModTypes != null)
                    return _variantModTypes;
                var types = new List<Type>();
                var complete = true;
                foreach (var assembly in VariantAssemblies)
                {
                    types.AddRange(GetLoadableTypes(assembly, out var loaded));
                    complete &= loaded;
                }

                var result = types.Distinct().ToArray();
                if (complete)
                    _variantModTypes = result;
                return result;
            }
        }

        private static void RegisterVariantAssembly(Assembly realAsm)
        {
            EnsureReflectionBridgePatch();

            lock (VariantAssembliesLock)
            {
                if (VariantAssemblies.Contains(realAsm))
                    return;

                VariantAssemblies.Add(realAsm);
                _variantModTypes = null;
            }
        }

        private static bool AssociateVariantAssemblyWithGame(Assembly assembly)
        {
            if (AssociateAssemblyWithModMethod != null)
                try
                {
                    AssociateAssemblyWithModMethod.Invoke(null, [ModId, assembly]);
                    if (IsAssemblyAssociatedWithMod(ModId, assembly))
                        return true;

                    Log.Warn(
                        $"[RitsuLib.Loader] Host AssociateAssemblyWithMod did not record variant assembly {assembly.FullName} for {ModId}; applying initializer fallback.");
                }
                catch (Exception ex)
                {
                    Log.Warn(
                        $"[RitsuLib.Loader] Failed to associate variant assembly {assembly.FullName} with {ModId}: {ex.Message}");
                }

            if (TryAssociateAssemblyWithModList(ModId, assembly))
                return true;

            Log.Warn(
                $"[RitsuLib.Loader] Could not associate variant assembly {assembly.FullName} with {ModId}; relying on reflection bridge for type discovery.");
            return false;
        }

        private static bool IsAssemblyAssociatedWithMod(string modId, Assembly assembly)
        {
            return TryFindMod(modId, out var mod) &&
                   TryGetMutableAssembliesList(mod, out var assemblies) &&
                   ContainsAssembly(assemblies, assembly);
        }

        private static bool TryAssociateAssemblyWithModList(string modId, Assembly assembly)
        {
            if (!TryFindMod(modId, out var mod))
                return false;

            if (!TryGetMutableAssembliesList(mod, out var assemblies))
                return false;

            // ReSharper disable once InvertIf
            if (!ContainsAssembly(assemblies, assembly))
            {
                assemblies.Add(assembly);
                Log.Info(
                    $"[RitsuLib.Loader] Associated variant assembly {assembly.FullName} with {modId} during initialization.");
            }

            return true;
        }

        private static bool TryFindMod(string modId, out Mod mod)
        {
            foreach (var candidate in ModManager.Mods)
            {
                if (!string.Equals(ReadManifestId(candidate), modId, StringComparison.Ordinal))
                    continue;

                mod = candidate;
                return true;
            }

            mod = null!;
            return false;
        }

        private static string? ReadManifestId(Mod mod)
        {
            var manifest = ManifestField?.GetValue(mod);
            return manifest == null ? null : ManifestIdField?.GetValue(manifest) as string;
        }

        private static bool TryGetMutableAssembliesList(Mod mod, out IList assemblies)
        {
            assemblies = null!;
            var value = AssembliesField?.GetValue(mod);
            if (value is not IList list)
                return false;

            assemblies = list;
            return true;
        }

        private static bool ContainsAssembly(IEnumerable assemblies, Assembly assembly)
        {
            return assemblies.Cast<object?>().Any(item => ReferenceEquals(item, assembly));
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

        private static void EnsureReflectionBridgePatch()
        {
            if (_reflectionBridgePatched)
                return;

            HarmonyPatchAllTypeLoadGuard.Install(message => Log.Warn("[RitsuLib.Loader] " + message));

            var harmony = new Harmony("OLC.STS2-RitsuLib.Loader.ReflectionBridge");
            harmony.PatchAll(typeof(Bootstrap).Assembly);
            _reflectionBridgePatched = true;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, out bool complete)
        {
            try
            {
                var types = assembly.GetTypes();
                complete = true;
                return types;
            }
            catch (ReflectionTypeLoadException ex)
            {
                complete = false;
                var loaderExceptions = ex.LoaderExceptions.OfType<Exception>().ToArray();
                var details = string.Join(
                    Environment.NewLine,
                    loaderExceptions.Take(8).Select(static exception => exception.ToString()));
                var detailBlock = details.Length > 0 ? Environment.NewLine + details : string.Empty;
                var omitted = loaderExceptions.Length > 8
                    ? $"{Environment.NewLine}... {loaderExceptions.Length - 8} more loader exception(s) omitted."
                    : string.Empty;
                Log.Warn(
                    $"[RitsuLib.Loader] Partial type load for {assembly.FullName}: {ex.Message}" +
                    $"{detailBlock}{omitted}");
                return ex.Types.OfType<Type>();
            }
        }
    }
}
