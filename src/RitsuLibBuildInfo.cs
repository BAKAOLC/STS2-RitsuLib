using System.Reflection;

namespace STS2RitsuLib
{
    internal static class RitsuLibBuildInfo
    {
        internal const string DevPackageVersionPrefix = "9999.0.0-dev.";

        private static readonly Assembly Assembly = typeof(Const).Assembly;

        internal static string InformationalVersion { get; } =
            Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
            Const.Version;

        internal static IReadOnlyDictionary<string, string> Metadata { get; } =
            Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(x => x.Key, x => x.Value ?? "", StringComparer.OrdinalIgnoreCase);

        internal static bool IsDevBuild =>
            InformationalVersion.StartsWith(DevPackageVersionPrefix, StringComparison.OrdinalIgnoreCase) ||
            (Metadata.TryGetValue("RitsuLibTelemetryBuildChannel", out var channel) &&
             string.Equals(channel.Trim(), "dev", StringComparison.OrdinalIgnoreCase));
    }
}
