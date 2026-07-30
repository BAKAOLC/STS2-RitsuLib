namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Collects registered account-relative mod-data paths that are allowed to synchronize.</para>
    ///     <para xml:lang="zh-CN">收集允许同步的已注册账户相对模组数据路径。</para>
    /// </summary>
    internal static class StorageSyncPathEnumerator
    {
        internal static void CollectWhitelistedRelativePaths(int profileId, HashSet<string> sink,
            ModCloudSyncScope scope)
        {
            ModCloudSyncPathRegistry.CollectRegisteredRelativePaths(profileId, sink, scope);
        }
    }
}
