using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Loader
{
    /// <summary>
    ///     Entry assembly for the multi-variant RitsuLib bundle: loads the matching <c>STS2-RitsuLib.dll</c> from
    ///     <c>lib/&lt;compat&gt;/</c> into the default ALC, then forwards to the real framework initializer.
    ///     多变体 RitsuLib bundle 的入口程序集：从
    ///     <c>lib/&lt;compat&gt;/</c> 将匹配的 <c>STS2-RitsuLib.dll</c> 加载到默认 ALC，然后转发到真正的框架初始化器。
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static class Bootstrap
    {
        private const string RealDllName = "STS2-RitsuLib.dll";
        private const string VariantManifestName = "ritsulib-variants.json";
        private const string CompatTargetMarkerName = "compat-target.txt";

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
            var picked = PickVariant(loaderDir, libRoot, hostNumeric);
            if (picked is null)
            {
                Log.Error(
                    $"[RitsuLib.Loader] No compatible variant under {libRoot} (host={(hostLabel ?? hostNumeric?.ToString()) ?? "unknown"}).");
                return;
            }

            Log.Info(
                $"[RitsuLib.Loader] Host version label={hostLabel ?? "<none>"} numeric={hostNumeric?.ToString() ?? "<none>"}; picked variant {picked.CompatTarget}.");

            var realDll = picked.DllPath;
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

        private static VariantCandidate? PickVariant(string loaderDir, string libRoot, Version? host)
        {
            var variants = LoadVariantManifest(loaderDir, libRoot);
            if (variants.Count == 0)
                return null;

            variants.Sort(static (a, b) => a.Version.CompareTo(b.Version));

            if (host is null)
            {
                Log.Info("[RitsuLib.Loader] Host numeric version unknown; using newest bundled variant.");
                return variants[^1];
            }

            var candidates = variants.Where(x => x.Version <= host).ToList();
            if (candidates.Count > 0)
                return candidates[^1];

            Log.Info(
                $"[RitsuLib.Loader] No bundled variant <= host {host}; using newest bundled variant as best-effort fallback.");
            return variants[^1];
        }

        private static List<VariantCandidate> LoadVariantManifest(string loaderDir, string libRoot)
        {
            var manifestPath = Path.Combine(loaderDir, VariantManifestName);
            if (!File.Exists(manifestPath))
            {
                Log.Error($"[RitsuLib.Loader] Missing variant manifest: {manifestPath}");
                return [];
            }

            BundleVariantManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<BundleVariantManifest>(
                    File.ReadAllText(manifestPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Log.Error($"[RitsuLib.Loader] Failed to read variant manifest {manifestPath}: {ex}");
                return [];
            }

            if (manifest?.Variants is not { Count: > 0 })
            {
                Log.Error($"[RitsuLib.Loader] Variant manifest contains no variants: {manifestPath}");
                return [];
            }

            var libRootFull = Path.GetFullPath(libRoot);
            var variants = new List<VariantCandidate>();
            foreach (var entry in manifest.Variants)
            {
                var candidate = TryCreateVariantCandidate(loaderDir, libRootFull, entry);
                if (candidate is not null)
                    variants.Add(candidate);
            }

            return variants;
        }

        private static VariantCandidate? TryCreateVariantCandidate(
            string loaderDir,
            string libRootFull,
            BundleVariantEntry entry)
        {
            var compatTarget = entry.CompatTarget?.Trim();
            if (string.IsNullOrWhiteSpace(compatTarget) ||
                !Sts2HostVersion.TryParseVersionCore(compatTarget, out var version))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring invalid variant target '{entry.CompatTarget}'.");
                return null;
            }

            var relativeDir = string.IsNullOrWhiteSpace(entry.Directory)
                ? Path.Combine("lib", compatTarget)
                : entry.Directory.Trim();
            var variantDir = Path.GetFullPath(Path.Combine(loaderDir, relativeDir));
            if (!IsUnderDirectory(variantDir, libRootFull))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant outside lib directory: {relativeDir}");
                return null;
            }

            if (!string.Equals(Path.GetFileName(variantDir), compatTarget, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant with mismatched directory: {relativeDir}");
                return null;
            }

            var marker = Path.Combine(variantDir, CompatTargetMarkerName);
            if (!File.Exists(marker) ||
                !string.Equals(File.ReadAllText(marker).Trim(), compatTarget, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant with missing or mismatched marker: {marker}");
                return null;
            }

            var assemblyName = string.IsNullOrWhiteSpace(entry.Assembly) ? RealDllName : entry.Assembly.Trim();
            if (!string.Equals(assemblyName, RealDllName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant with unexpected assembly name: {assemblyName}");
                return null;
            }

            var dllPath = Path.Combine(variantDir, assemblyName);
            if (!File.Exists(dllPath))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant missing {RealDllName}: {dllPath}");
                return null;
            }

            if (!MatchesExpectedHash(dllPath, entry.Sha256))
            {
                Log.Error($"[RitsuLib.Loader] Ignoring variant with mismatched hash: {dllPath}");
                return null;
            }

            return new(compatTarget, version, dllPath);
        }

        private static bool IsUnderDirectory(string path, string root)
        {
            var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesExpectedHash(string path, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256))
                return false;

            using var stream = File.OpenRead(path);
            var actual = Convert.ToHexString(SHA256.HashData(stream));
            return string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private sealed record VariantCandidate(string CompatTarget, Version Version, string DllPath);

        private sealed class BundleVariantManifest
        {
            public List<BundleVariantEntry>? Variants { get; set; }
        }

        private sealed class BundleVariantEntry
        {
            public string? CompatTarget { get; set; }

            public string? Directory { get; set; }

            public string? Assembly { get; set; }

            public string? Sha256 { get; set; }
        }
    }
}
