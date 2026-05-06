using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class ModAccountRelativePath
    {
        internal static bool TryGetRelativeAccountPath(string godotUserPath, out string relative)
        {
            relative = string.Empty;
            var account = UserDataPathProvider.GetAccountScopedBasePath(null).Replace('\\', '/').TrimEnd('/');
            var normalized = godotUserPath.Replace('\\', '/');
            if (normalized.Length <= account.Length + 1)
                return false;

            if (!normalized.StartsWith(account, StringComparison.Ordinal))
                return false;

            if (normalized[account.Length] != '/')
                return false;

            relative = normalized[(account.Length + 1)..];
            return true;
        }
    }
}
