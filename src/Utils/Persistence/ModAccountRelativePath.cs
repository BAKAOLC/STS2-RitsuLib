using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Converts Godot user-data paths under the current account root to account-relative paths.</para>
    ///     <para xml:lang="zh-CN">将当前账户根目录下的 Godot 用户数据路径转换为账户相对路径。</para>
    /// </summary>
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
