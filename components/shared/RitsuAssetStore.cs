using STS2RitsuLib.Utils;

namespace STS2RitsuLib
{
    internal static class RitsuAssetStore
    {
        private static readonly Lazy<ResourcePack> Resources = new(() =>
        {
            var root = RootDirectory;
            var looseRoot = Path.Combine(root, "assets");
            return Directory.Exists(looseRoot)
                ? ResourcePack.FromDirectory(looseRoot)
                : ResourcePack.FromZip(Path.Combine(root, "assets.zip"));
        });

        internal static ResourcePack Pack => Resources.Value;

        private static string RootDirectory
        {
            get
            {
                var assemblyDirectory = Path.GetDirectoryName(typeof(RitsuAssetStore).Assembly.Location)
                                        ?? throw new InvalidOperationException(
                                            "The RitsuLib shared module has no installation directory.");
                return Path.GetFileName(assemblyDirectory) == "shared"
                    ? Path.GetDirectoryName(assemblyDirectory)!
                    : assemblyDirectory;
            }
        }

        internal static byte[] ReadBytes(string relativePath)
        {
            return Pack.ReadAllBytes(relativePath);
        }

        internal static IReadOnlyList<string> EnumerateFiles(string directory, string suffix)
        {
            return [.. Pack.EnumerateFiles(directory).Where(path => path.EndsWith(suffix, StringComparison.Ordinal))];
        }
    }
}
