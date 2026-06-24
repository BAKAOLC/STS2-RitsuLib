namespace STS2RitsuLib.Settings
{
    internal readonly record struct ModSettingsMirrorSyncPolicy(
        ModSettingsMirrorSource Source,
        bool HasStableExternalSync);

    internal static class ModSettingsMirrorSyncPolicyRegistry
    {
        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, ModSettingsMirrorSyncPolicy> Policies =
            new(StringComparer.OrdinalIgnoreCase);

        public static void RegisterPage(string modId, string pageId, ModSettingsMirrorSource source,
            bool hasStableExternalSync = false)
        {
            if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(pageId))
                return;

            lock (Gate)
            {
                Policies[CreateCompositeId(modId, pageId)] = new(source, hasStableExternalSync);
            }
        }

        public static bool TryGetPolicy(string modId, string pageId, out ModSettingsMirrorSyncPolicy policy)
        {
            lock (Gate)
            {
                return Policies.TryGetValue(CreateCompositeId(modId, pageId), out policy);
            }
        }

        private static string CreateCompositeId(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
