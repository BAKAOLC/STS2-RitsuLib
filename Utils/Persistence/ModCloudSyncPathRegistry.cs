namespace STS2RitsuLib.Utils.Persistence
{
    internal static class ModCloudSyncPathRegistry
    {
        private static readonly Lock Sync = new();
        private static readonly HashSet<Slot> Slots = [];

        internal static void RegisterModDataSlot(string modId, string fileName, SaveScope scope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            lock (Sync)
            {
                Slots.Add(new(modId, fileName, scope));
            }
        }

        internal static void CollectRegisteredRelativePaths(int profileId, HashSet<string> sink,
            ModCloudSyncScope scope = ModCloudSyncScope.All)
        {
            lock (Sync)
            {
                foreach (var slot in Slots)
                {
                    switch (scope)
                    {
                        case ModCloudSyncScope.GlobalOnly when slot.Scope != SaveScope.Global:
                        case ModCloudSyncScope.ProfileOnly when slot.Scope != SaveScope.Profile:
                            continue;
                    }

                    if (slot.Scope == SaveScope.Global)
                    {
                        var godot = ProfileManager.GetFilePath(slot.FileName, SaveScope.Global, 0, slot.ModId);
                        if (ModAccountRelativePath.TryGetRelativeAccountPath(godot, out var rel))
                            sink.Add(rel);
                    }
                    else
                    {
                        if (profileId < 0)
                            continue;
                        var godot = ProfileManager.GetFilePath(slot.FileName, SaveScope.Profile, profileId,
                            slot.ModId);
                        if (ModAccountRelativePath.TryGetRelativeAccountPath(godot, out var rel))
                            sink.Add(rel);
                    }
                }
            }
        }

        internal static bool IsRegisteredRelativePath(string relativeAccountPath, int activeProfileId)
        {
            lock (Sync)
            {
                foreach (var slot in Slots)
                    if (slot.Scope == SaveScope.Global)
                    {
                        var godot = ProfileManager.GetFilePath(slot.FileName, SaveScope.Global, 0, slot.ModId);
                        if (ModAccountRelativePath.TryGetRelativeAccountPath(godot, out var rel) &&
                            string.Equals(rel, relativeAccountPath, StringComparison.Ordinal))
                            return true;
                    }
                    else
                    {
                        if (activeProfileId < 0)
                            continue;
                        var godot = ProfileManager.GetFilePath(slot.FileName, SaveScope.Profile, activeProfileId,
                            slot.ModId);
                        if (ModAccountRelativePath.TryGetRelativeAccountPath(godot, out var rel) &&
                            string.Equals(rel, relativeAccountPath, StringComparison.Ordinal))
                            return true;
                    }
            }

            return false;
        }

        internal static void CollectProfileScopedRegisteredRelativePaths(int profileId, HashSet<string> sink)
        {
            if (profileId < 0)
                return;
            lock (Sync)
            {
                foreach (var godot in from slot in Slots
                         where slot.Scope == SaveScope.Profile
                         select ProfileManager.GetFilePath(slot.FileName, SaveScope.Profile, profileId, slot.ModId))
                    if (ModAccountRelativePath.TryGetRelativeAccountPath(godot, out var rel))
                        sink.Add(rel);
            }
        }

        private readonly record struct Slot(string ModId, string FileName, SaveScope Scope);
    }
}
