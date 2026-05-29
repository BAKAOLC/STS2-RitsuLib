using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Utils
{
    internal static class AssetPathDiagnostics
    {
        internal static bool Exists(string path, object owner, string memberName)
        {
            if (GodotResourcePath.ResourceExists(path))
                return true;

            WarnMissingPath(owner, memberName, path);
            return false;
        }

        /// <summary>
        ///     Logs every time a mod character asset profile supplies a non-empty path that does not resolve
        ///     (empty overrides are ignored by callers).
        ///     每当 mod 角色资源档案提供了无法解析的非空路径时都记录日志
        ///     （空覆盖值会被调用方忽略）。
        /// </summary>
        internal static void WarnModCharacterAssetOverrideMissing(object owner, string memberName, string path)
        {
            var ownerLabel = DescribeOwner(owner);

            RitsuLibFramework.Logger.Warn(
                $"[Assets] Mod character asset override path not found for {ownerLabel}.{memberName}: '{path}'. " +
                "Falling back to the base game asset.");
        }

        internal static string[] CollectExistingPaths(object owner,
            params (string? Path, string MemberName)[] candidates)
        {
            var results = new List<string>(candidates.Length);

            foreach (var (path, memberName) in candidates)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (Exists(path, owner, memberName))
                    results.Add(path);
            }

            return [.. results];
        }

        private static void WarnMissingPath(object owner, string memberName, string path)
        {
            var ownerLabel = DescribeOwner(owner);

            RitsuLibFramework.Logger.Warn(
                $"[Assets] Missing resource path for {ownerLabel}.{memberName}: '{path}'. Falling back to the base asset.");
        }

        private static string DescribeOwner(object owner)
        {
            try
            {
                if (owner is AbstractModel model && !string.IsNullOrWhiteSpace(model.Id.Entry))
                    return $"{owner.GetType().Name}<{model.Id.Entry}>";
            }
            catch
            {
                // Ignore model identity lookup failures and fall back to the CLR type name.
            }

            return owner.GetType().Name;
        }
    }
}
