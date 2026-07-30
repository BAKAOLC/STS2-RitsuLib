using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves the running game's version on a best-effort basis from release metadata or the game assembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">尽可能从发布元数据或游戏程序集解析当前运行游戏的版本。</para>
    /// </summary>
    internal static class Sts2HostVersion
    {
        private static readonly Lazy<HostVersionSnapshot> Lazy = new(Resolve);

        /// <summary>
        ///     <para xml:lang="en">Gets the parsed numeric version, or <see langword="null" /> when unavailable.</para>
        ///     <para xml:lang="zh-CN">获取解析出的数字版本；无法取得时为 <see langword="null" />。</para>
        /// </summary>
        internal static Version? Numeric => Lazy.Value.Numeric;

        /// <summary>
        ///     <para xml:lang="en">Gets the original release label when one was found.</para>
        ///     <para xml:lang="zh-CN">获取找到的原始发布版本标签。</para>
        /// </summary>
        internal static string? ReleaseLabel => Lazy.Value.ReleaseLabel;

        private static HostVersionSnapshot Resolve()
        {
            string? fallbackLabel = null;

            try
            {
                var ri = ReleaseInfoManager.Instance.ReleaseInfo;
                if (TryCaptureVersionLabel(ri?.Version, ref fallbackLabel, out var snapshot))
                    return snapshot;
            }
            catch
            {
                // ReleaseInfoManager or file IO may fail in unusual environments
            }

            if (TryResolvePublishedReleaseInfo(ref fallbackLabel, out var publishedSnapshot))
                return publishedSnapshot;

            if (TryResolveLauncherDownloadedReleaseInfo(ref fallbackLabel, out var fileSnapshot))
                return fileSnapshot;

            if (TryResolveLauncherCacheStamp(ref fallbackLabel, out var stampSnapshot))
                return stampSnapshot;

            var av = typeof(SerializableRun).Assembly.GetName().Version;
            if (av != null && !IsAllZero(av))
                return new(av, fallbackLabel);

            return new(null, fallbackLabel);
        }

        private static bool IsAllZero(Version v)
        {
            return v.Major == 0 && v.Minor == 0 && v.Build <= 0 && v.Revision <= 0;
        }

        private static bool TryCaptureVersionLabel(
            string? label,
            ref string? fallbackLabel,
            out HostVersionSnapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrWhiteSpace(label))
                return false;

            fallbackLabel ??= label;
            if (!TryParseVersionCore(label, out var v))
                return false;

            snapshot = new(v, label);
            return true;
        }

        private static bool TryResolveLauncherDownloadedReleaseInfo(
            ref string? fallbackLabel,
            out HostVersionSnapshot snapshot)
        {
            snapshot = default;
            var dataDir = TryGetGodotDataDir();
            if (string.IsNullOrWhiteSpace(dataDir))
                return false;

            return TryReadJsonVersion(Path.Combine(dataDir, "game", "release_info.json"),
                "version",
                ref fallbackLabel,
                out snapshot);
        }

        private static bool TryResolvePublishedReleaseInfo(
            ref string? fallbackLabel,
            out HostVersionSnapshot snapshot)
        {
            snapshot = default;
            foreach (var path in GetPublishedReleaseInfoPaths())
                if (TryReadJsonVersion(path, "version", ref fallbackLabel, out snapshot))
                    return true;

            return false;
        }

        private static IEnumerable<string> GetPublishedReleaseInfoPaths()
        {
            var executablePath = TryCallGodotOsString("GetExecutablePath");
            if (string.IsNullOrWhiteSpace(executablePath))
                yield break;

            var executableDir = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(executableDir))
                yield break;

            if (string.Equals(TryCallGodotOsString("GetName"), "macOS", StringComparison.Ordinal))
                yield return Path.Combine(executableDir, "..", "Resources", "release_info.json");

            yield return Path.Combine(executableDir, "release_info.json");
        }

        private static bool TryResolveLauncherCacheStamp(
            ref string? fallbackLabel,
            out HostVersionSnapshot snapshot)
        {
            snapshot = default;
            var dataDir = TryGetGodotDataDir();
            if (string.IsNullOrWhiteSpace(dataDir))
                return false;

            var stampPath = Path.Combine(dataDir, ".cache_stamp");
            if (TryReadJsonVersion(stampPath, "version", ref fallbackLabel, out snapshot))
                return true;

            try
            {
                if (!File.Exists(stampPath))
                    return false;

                using var doc = JsonDocument.Parse(File.ReadAllText(stampPath));
                if (!doc.RootElement.TryGetProperty("buildId", out var buildIdElement))
                    return false;

                var buildId = buildIdElement.GetString();
                if (string.IsNullOrWhiteSpace(buildId))
                    return false;

                fallbackLabel ??= $"buildid:{buildId}";
            }
            catch
            {
                // Cache stamp is best-effort metadata only.
            }

            return false;
        }

        private static bool TryReadJsonVersion(
            string path,
            string propertyName,
            ref string? fallbackLabel,
            out HostVersionSnapshot snapshot)
        {
            snapshot = default;
            try
            {
                if (!File.Exists(path))
                    return false;

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return doc.RootElement.TryGetProperty(propertyName, out var versionElement) &&
                       TryCaptureVersionLabel(versionElement.GetString(), ref fallbackLabel, out snapshot);
            }
            catch
            {
                return false;
            }
        }

        private static string? TryGetGodotDataDir()
        {
            return TryCallGodotOsString("GetDataDir");
        }

        private static string? TryCallGodotOsString(string methodName)
        {
            try
            {
                var osType =
                    Type.GetType("Godot.OS, GodotSharp", false) ??
                    Type.GetType("Godot.OS, GodotSharpEditor", false) ??
                    AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(static asm => asm.GetType("Godot.OS", false))
                        .FirstOrDefault(static type => type != null);

                var method = osType?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                return method?.Invoke(null, null) as string;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Parses <c>major.minor[.build[.revision]]</c> after removing a leading <c>v</c> and common semantic
        ///         version suffixes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除开头的 <c>v</c> 及常见语义化版本后缀后，解析
        ///         <c>major.minor[.build[.revision]]</c>。
        ///     </para>
        /// </summary>
        internal static bool TryParseVersionCore(string text, out Version version)
        {
            var s = text.Trim();
            var dash = s.IndexOf('-', StringComparison.Ordinal);
            if (dash >= 0)
                s = s[..dash].Trim();
            var plus = s.IndexOf('+', StringComparison.Ordinal);
            if (plus >= 0)
                s = s[..plus].Trim();
            if (s.Length >= 2 && (s[0] == 'v' || s[0] == 'V') && char.IsDigit(s[1]))
                s = s[1..];
            if (Version.TryParse(s, out var parsed))
            {
                version = parsed;
                return true;
            }

            version = new(0, 0);
            return false;
        }

        // ReSharper disable MemberHidesStaticFromOuterClass
        private readonly record struct HostVersionSnapshot(Version? Numeric, string? ReleaseLabel);
        // ReSharper restore MemberHidesStaticFromOuterClass
    }
}
