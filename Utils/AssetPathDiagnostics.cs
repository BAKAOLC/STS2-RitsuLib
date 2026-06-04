using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Utils
{
    internal static class AssetPathDiagnostics
    {
        private static readonly Lock StartupMissingPathCacheGate = new();
        private static readonly HashSet<string> StartupMissingPathCache = [];
        private static bool _startupMissingPathCacheEnabled = true;
        private static bool _startupMissingPathCacheShutdownRegistered;

        internal static bool Exists(string path, object owner, string memberName)
        {
            var ownerLabel = DescribeOwner(owner);
            var cacheKey = BuildMissingPathCacheKey(ownerLabel, memberName, path);

            if (IsCachedStartupMissingPath(cacheKey))
                return false;

            if (GodotResourcePath.ResourceExists(path))
                return true;

            if (ShouldWarnMissingPath(cacheKey))
                WarnMissingPath(ownerLabel, memberName, path);

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
            var cacheKey = BuildMissingPathCacheKey(ownerLabel, memberName, path);

            if (!ShouldWarnMissingPath(cacheKey))
                return;

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

        private static void WarnMissingPath(string ownerLabel, string memberName, string path)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Assets] Missing resource path for {ownerLabel}.{memberName}: '{path}'. Falling back to the base asset.");
        }

        private static bool IsCachedStartupMissingPath(string cacheKey)
        {
            EnsureStartupMissingPathCacheShutdownRegistered();

            lock (StartupMissingPathCacheGate)
            {
                return _startupMissingPathCacheEnabled && StartupMissingPathCache.Contains(cacheKey);
            }
        }

        private static bool ShouldWarnMissingPath(string cacheKey)
        {
            EnsureStartupMissingPathCacheShutdownRegistered();

            lock (StartupMissingPathCacheGate)
            {
                return !_startupMissingPathCacheEnabled || StartupMissingPathCache.Add(cacheKey);
            }
        }

        private static void EnsureStartupMissingPathCacheShutdownRegistered()
        {
            if (_startupMissingPathCacheShutdownRegistered)
                return;

            lock (StartupMissingPathCacheGate)
            {
                if (_startupMissingPathCacheShutdownRegistered)
                    return;

                _startupMissingPathCacheShutdownRegistered = true;
            }

            RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => DisableStartupMissingPathCache());
        }

        private static void DisableStartupMissingPathCache()
        {
            lock (StartupMissingPathCacheGate)
            {
                _startupMissingPathCacheEnabled = false;
                StartupMissingPathCache.Clear();
            }
        }

        private static string BuildMissingPathCacheKey(string ownerLabel, string memberName, string path)
        {
            return $"{ownerLabel}\n{memberName}\n{path}";
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
