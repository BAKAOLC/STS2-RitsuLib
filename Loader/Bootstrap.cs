using System.Reflection;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Loader
{
    /// <summary>
    ///     Entry assembly for the multi-variant RitsuLib bundle: loads the matching <c>STS2-RitsuLib.dll</c> from
    ///     <c>lib/&lt;compat&gt;/</c> into the default ALC, then forwards to the real framework initializer.
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static class Bootstrap
    {
        public static void Initialize()
        {
            var loaderDir = Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location);
            if (string.IsNullOrEmpty(loaderDir))
            {
                Log.Error("[RitsuLib.Loader] Could not resolve loader directory.");
                return;
            }

            var libRoot = Path.Combine(loaderDir, "lib");
            if (!Directory.Exists(libRoot))
            {
                Log.Error($"[RitsuLib.Loader] Missing lib directory: {libRoot}");
                return;
            }

            var hostNumeric = Sts2HostVersion.Numeric;
            var hostLabel = Sts2HostVersion.ReleaseLabel;
            var pickedDir = PickVariantDir(libRoot, hostNumeric);
            if (pickedDir is null)
            {
                Log.Error(
                    $"[RitsuLib.Loader] No compatible variant under {libRoot} (host={(hostLabel ?? hostNumeric?.ToString()) ?? "unknown"}).");
                return;
            }

            var pickedName =
                Path.GetFileName(pickedDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Log.Info(
                $"[RitsuLib.Loader] Host version label={hostLabel ?? "<none>"} numeric={hostNumeric?.ToString() ?? "<none>"}; picked variant {pickedName}.");

            var realDll = Path.Combine(pickedDir, "STS2-RitsuLib.dll");
            if (!File.Exists(realDll))
            {
                Log.Error($"[RitsuLib.Loader] Variant folder missing STS2-RitsuLib.dll: {realDll}");
                return;
            }

            var alc = AssemblyLoadContext.GetLoadContext(typeof(Bootstrap).Assembly) ?? AssemblyLoadContext.Default;
            Assembly realAsm;
            try
            {
                realAsm = alc.LoadFromAssemblyPath(realDll);
            }
            catch (Exception ex)
            {
                Log.Error($"[RitsuLib.Loader] Failed to load {realDll}: {ex}");
                return;
            }

            try
            {
                InvokeRealInitializer(realAsm);
            }
            catch (Exception ex)
            {
                Log.Error($"[RitsuLib.Loader] Failed to initialize real RitsuLib: {ex}");
            }
        }

        private static void InvokeRealInitializer(Assembly realAsm)
        {
            Type[] types;
            try
            {
                types = realAsm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Error($"[RitsuLib.Loader] ReflectionTypeLoadException while scanning {realAsm.FullName}: {ex}");
                if (ex.Types is null) return;
                foreach (var t in ex.Types.Where(static x => x is not null))
                    TryInvokeInitializerOnType(t!);

                return;
            }

            if (types.Any(TryInvokeInitializerOnType)) return;

            Log.Error($"[RitsuLib.Loader] No type with {nameof(ModInitializerAttribute)} found in {realAsm.FullName}.");
        }

        private static bool TryInvokeInitializerOnType(Type t)
        {
            var attr = t.GetCustomAttribute<ModInitializerAttribute>();
            if (attr is null)
                return false;

            var method = t.GetMethod(attr.initializerMethod,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method is null)
            {
                Log.Error(
                    $"[RitsuLib.Loader] Type {t.FullName} has {nameof(ModInitializerAttribute)} but no static method {attr.initializerMethod}.");
                return false;
            }

            method.Invoke(null, null);
            return true;
        }

        private static string? PickVariantDir(string libRoot, Version? host)
        {
            var dirs = new List<(string Path, Version Ver)>();
            foreach (var d in Directory.EnumerateDirectories(libRoot))
            {
                var name = Path.GetFileName(d.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!Sts2HostVersion.TryParseVersionCore(name, out var v))
                    continue;
                dirs.Add((d, v));
            }

            if (dirs.Count == 0)
                return null;

            dirs.Sort(static (a, b) => a.Ver.CompareTo(b.Ver));

            if (host is null)
            {
                Log.Info("[RitsuLib.Loader] Host numeric version unknown; using newest bundled variant.");
                return dirs[^1].Path;
            }

            var candidates = dirs.Where(x => x.Ver <= host).ToList();
            if (candidates.Count > 0)
                return candidates[^1].Path;

            Log.Info(
                $"[RitsuLib.Loader] No bundled variant <= host {host}; using newest bundled variant as best-effort fallback.");
            return dirs[^1].Path;
        }
    }
}
