using System.Reflection;
using System.Text;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Platform.Steam
{
    internal readonly record struct SteamWorkshopUpdateScope(string Key, string DisplayName, string? BranchName)
    {
        internal static SteamWorkshopUpdateScope Current()
        {
            var branchName = TryGetCurrentSteamBetaName();
            var branchKey = string.IsNullOrWhiteSpace(branchName)
                ? "production"
                : NormalizeKeyPart(branchName);
            var hostLabel = Sts2HostVersion.ReleaseLabel ?? Sts2HostVersion.Numeric?.ToString();
            var hostKey = string.IsNullOrWhiteSpace(hostLabel)
                ? "unknown-host"
                : NormalizeKeyPart(hostLabel);
            var displayName = string.IsNullOrWhiteSpace(branchName)
                ? "production"
                : branchName.Trim();

            return new($"steam:{branchKey}:host:{hostKey}", displayName, branchName);
        }

        private static string? TryGetCurrentSteamBetaName()
        {
            try
            {
                var steamApps = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(static asm => asm.GetType("Steamworks.SteamApps", false))
                    .FirstOrDefault(static type => type != null);
                var getCurrentBetaName = steamApps?.GetMethod(
                    "GetCurrentBetaName",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string).MakeByRefType(), typeof(int)],
                    null);
                if (getCurrentBetaName == null)
                    return null;

                object?[] args = [string.Empty, 128];
                return getCurrentBetaName.Invoke(null, args) is true &&
                       args[0] is string betaName &&
                       !string.IsNullOrWhiteSpace(betaName)
                    ? betaName.Trim()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeKeyPart(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value.Trim())
            {
                if (char.IsAsciiLetterOrDigit(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                    continue;
                }

                if (ch is '.' or '-' or '_')
                    builder.Append(ch);
                else if (builder.Length == 0 || builder[^1] != '-')
                    builder.Append('-');
            }

            return builder.Length == 0 ? "unknown" : builder.ToString().Trim('-');
        }
    }
}
