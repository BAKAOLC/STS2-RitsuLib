using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     Best-effort version of the running STS2 host from <c>release_info.json</c> or the <c>sts2</c> assembly.
    ///     Not currently consumed by RitsuLib; kept for future version-gated behavior or diagnostics.
    /// </summary>
    internal static class Sts2HostVersion
    {
        private static readonly Lazy<HostVersionSnapshot> Lazy = new(Resolve);

        /// <summary>
        ///     Parsed numeric version when reliable; otherwise <c>null</c>.
        /// </summary>
        internal static Version? Numeric => Lazy.Value.Numeric;

        /// <summary>
        ///     Original label from <see cref="ReleaseInfo.Version" /> when present.
        /// </summary>
        internal static string? ReleaseLabel => Lazy.Value.ReleaseLabel;

        private static HostVersionSnapshot Resolve()
        {
            try
            {
                var ri = ReleaseInfoManager.Instance.ReleaseInfo;
                if (ri?.Version is { Length: > 0 } label && TryParseVersionCore(label, out var v))
                    return new(v, label);
            }
            catch
            {
                // ReleaseInfoManager or file IO may fail in unusual environments
            }

            var av = typeof(SerializableRun).Assembly.GetName().Version;
            if (av != null && !IsAllZero(av))
                return new(av, null);

            return new(null, null);
        }

        private static bool IsAllZero(Version v)
        {
            return v.Major == 0 && v is { Minor: 0, Build: 0, Revision: 0 };
        }

        /// <summary>
        ///     Accepts <c>major.minor[.build[.revision]]</c>; strips common semver suffixes (<c>-beta</c>, <c>+build</c>).
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
