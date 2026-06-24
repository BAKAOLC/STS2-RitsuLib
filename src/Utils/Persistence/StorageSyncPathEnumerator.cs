namespace STS2RitsuLib.Utils.Persistence
{
    internal static class StorageSyncPathEnumerator
    {
        internal static void CollectWhitelistedRelativePaths(int profileId, HashSet<string> sink,
            ModCloudSyncScope scope)
        {
            ModCloudSyncPathRegistry.CollectRegisteredRelativePaths(profileId, sink, scope);
        }
    }
}
