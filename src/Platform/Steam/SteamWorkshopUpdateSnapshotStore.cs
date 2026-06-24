using System.Text.Json.Serialization;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class SteamWorkshopUpdateSnapshotStore
    {
        private const string DataKey = "steam_workshop_update_snapshot";
        private const string FileName = "steam_workshop_update_snapshot.json";
        private static readonly Lock SyncRoot = new();
        private static readonly ModDataStore Store = ModDataStore.For(Const.ModId);
        private static bool _initialized;

        internal static IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> GetItems()
        {
            EnsureInitialized();
            Dictionary<ulong, SteamWorkshopStoredUpdateItem> items = [];
            foreach (var (key, value) in Store.Get<SteamWorkshopUpdateSnapshotData>(DataKey).Items)
                if (ulong.TryParse(key, out var itemId))
                    items[itemId] = new(value.Updated, value.Title);
            return items;
        }

        internal static void Replace(IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> items)
        {
            EnsureInitialized();
            Store.Modify<SteamWorkshopUpdateSnapshotData>(DataKey, data =>
            {
                data.Items.Clear();
                foreach (var (itemId, item) in items)
                    data.Items[itemId.ToString()] = new()
                    {
                        Updated = item.Updated,
                        Title = item.Title,
                    };
            });
            Store.Save(DataKey);
        }

        internal static void Merge(IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> items)
        {
            EnsureInitialized();
            Store.Modify<SteamWorkshopUpdateSnapshotData>(DataKey, data =>
            {
                foreach (var (itemId, item) in items)
                    data.Items[itemId.ToString()] = new()
                    {
                        Updated = item.Updated,
                        Title = item.Title,
                    };
            });
            Store.Save(DataKey);
        }

        private static void EnsureInitialized()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                using (RitsuLibFramework.BeginModDataRegistration(Const.ModId, false))
                {
                    Store.Register<SteamWorkshopUpdateSnapshotData>(
                        DataKey,
                        FileName,
                        SaveScope.Global,
                        () => new(),
                        true);
                }

                _initialized = true;
            }
        }

        private sealed class SteamWorkshopUpdateSnapshotData
        {
            [JsonPropertyName("items")] public Dictionary<string, SteamWorkshopUpdateSnapshotEntry> Items { get; } = [];
        }

        private sealed class SteamWorkshopUpdateSnapshotEntry
        {
            [JsonPropertyName("updated")] public uint Updated { get; set; }

            [JsonPropertyName("title")] public string? Title { get; set; }
        }
    }

    internal readonly record struct SteamWorkshopStoredUpdateItem(uint Updated, string? Title);
}
