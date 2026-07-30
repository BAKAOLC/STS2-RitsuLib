using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Centralizes resource-path existence checks and startup-scoped deduplication of missing-path
    ///         diagnostics for mod assets.
    ///     </para>
    ///     <para xml:lang="zh-CN">集中处理模组资源路径的存在性检查，并在启动阶段对缺失路径诊断去重。</para>
    /// </summary>
    internal static class AssetPathDiagnostics
    {
        private static readonly Lock StartupMissingPathCacheGate = new();
        private static readonly HashSet<string> StartupMissingPathCache = [];
        private static bool _startupMissingPathCacheEnabled = true;
        private static bool _startupMissingPathCacheShutdownRegistered;
        private static bool _startupMissingPathCacheShutdownRegistering;

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
        ///     <para xml:lang="en">
        ///         Logs an unresolved, non-empty path supplied by a mod character asset profile, with duplicate
        ///         diagnostics suppressed during startup.
        ///     </para>
        ///     <para xml:lang="zh-CN">记录模组角色资源档案中无法解析的非空路径，并在启动阶段抑制重复诊断。</para>
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
                if (_startupMissingPathCacheShutdownRegistered ||
                    _startupMissingPathCacheShutdownRegistering)
                    return;

                _startupMissingPathCacheShutdownRegistering = true;
            }

            try
            {
                RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => DisableStartupMissingPathCache());
                lock (StartupMissingPathCacheGate)
                {
                    _startupMissingPathCacheShutdownRegistered = true;
                }
            }
            finally
            {
                lock (StartupMissingPathCacheGate)
                {
                    _startupMissingPathCacheShutdownRegistering = false;
                }
            }
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                // Ignore model identity lookup failures and fall back to the CLR type name.
            }

            return owner.GetType().Name;
        }
    }
}
