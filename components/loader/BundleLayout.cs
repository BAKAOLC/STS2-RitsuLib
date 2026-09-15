using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace STS2RitsuLib.Loader
{
    internal sealed record BundleModule(string Name, string Path, AssemblyName Identity);

    internal sealed record BundleLayout(string CompatTarget, IReadOnlyList<BundleModule> Modules)
    {
        internal const string ManifestName = "ritsulib-variants.manifest";

        internal static readonly string[] SharedNames =
            ["STS2-RitsuLib.Shared", "STS2-RitsuLib.Ui", "STS2-RitsuLib.Settings", "System.IO.Hashing"];

        internal static readonly string[] VariantNames = ["STS2-RitsuLib.Runtime", "STS2-RitsuLib"];

        internal static BundleLayout Read(string root, Version? host)
        {
            var manifestPath = Path.Combine(root, ManifestName);
            using var stream = File.OpenRead(manifestPath);
            if (stream.Length > 1024 * 1024)
                throw new InvalidDataException("RitsuLib module manifest exceeds 1 MiB.");
            using var manifest = JsonDocument.Parse(stream, new() { MaxDepth = 8 });
            var document = manifest.RootElement;
            if (document.GetProperty("schema").GetInt32() != 2)
                throw new InvalidDataException(
                    "Unsupported RitsuLib module manifest schema; reinstall the complete bundle.");

            var variants = document.GetProperty("variants").EnumerateArray().ToArray();
            if (variants.Length is < 1 or > 64)
                throw new InvalidDataException("RitsuLib bundle must contain between 1 and 64 variants.");
            var targets = new HashSet<Version>();
            var candidates = new List<(Version Version, string Target, JsonElement Entry)>();
            foreach (var variant in variants)
            {
                var target = variant.GetProperty("compatTarget").GetString();
                if (!Version.TryParse(target, out var version) || version.Build < 0 || version.Revision >= 0 ||
                    !string.Equals(version.ToString(), target, StringComparison.Ordinal) || !targets.Add(version))
                    throw new InvalidDataException($"Invalid or duplicate RitsuLib compatibility target: {target}");
                candidates.Add((version, target, variant));
            }

            var compatible = candidates.Where(candidate => host == null || candidate.Version <= host)
                .OrderByDescending(candidate => candidate.Version).ToArray();
            if (compatible.Length == 0)
                throw new NotSupportedException($"This RitsuLib bundle does not support game version {host}.");
            var selected = compatible[0];
            var modules = new List<BundleModule>();
            ReadModules(root, "shared", document.GetProperty("shared"), SharedNames, modules);
            ReadModules(root, $"compat/{selected.Target}", selected.Entry.GetProperty("files"), VariantNames, modules);
            var frameworkVersions = modules
                .Where(module => module.Name.StartsWith("STS2-RitsuLib", StringComparison.Ordinal))
                .Select(module => module.Identity.Version).Distinct().ToArray();
            if (frameworkVersions.Length != 1)
                throw new InvalidDataException(
                    "RitsuLib module assembly versions do not match; reinstall the complete bundle.");
            return new(selected.Target, modules);
        }

        private static void ReadModules(string root, string directory, JsonElement entries, string[] requiredNames,
            List<BundleModule> modules)
        {
            var files = entries.EnumerateArray().ToArray();
            if (files.Length != requiredNames.Length)
                throw new InvalidDataException($"Unexpected module count in {directory}.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                var name = file.GetProperty("assembly").GetString();
                if (name == null || !requiredNames.Contains(name, StringComparer.Ordinal) || !seen.Add(name))
                    throw new InvalidDataException($"Unexpected or duplicate module in {directory}: {name}");
                var expectedHash = file.GetProperty("sha256").GetString();
                if (expectedHash is not { Length: 64 } ||
                    expectedHash.Any(character => !Uri.IsHexDigit(character)))
                    throw new InvalidDataException($"Invalid module hash: {name}");
                var path = Path.GetFullPath(Path.Combine(root, directory, name + ".dll"));
                using var moduleStream = File.OpenRead(path);
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(moduleStream)), expectedHash,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"RitsuLib module hash mismatch: {path}");
                var identity = AssemblyName.GetAssemblyName(path);
                if (!string.Equals(identity.Name, name, StringComparison.Ordinal))
                    throw new InvalidDataException($"RitsuLib module assembly identity mismatch: {path}");
                modules.Add(new(name, path, identity));
            }
        }
    }
}
