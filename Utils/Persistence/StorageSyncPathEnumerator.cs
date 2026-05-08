using STS2RitsuLib.Settings.RunSidecar;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class StorageSyncPathEnumerator
    {
        internal static void CollectWhitelistedRelativePaths(int profileId, HashSet<string> sink,
            ModCloudSyncScope scope)
        {
            ModCloudSyncPathRegistry.CollectRegisteredRelativePaths(profileId, sink, scope);

            if (scope == ModCloudSyncScope.GlobalOnly)
                return;

            if (profileId >= 0)
                ModRunSidecarStore.AppendActiveRunSidecarSyncRelativePaths(profileId, sink);
        }
    }
}
