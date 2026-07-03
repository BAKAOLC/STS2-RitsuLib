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

        internal static IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> GetItems(
            SteamWorkshopUpdateScope scope)
        {
            EnsureInitialized();
            Dictionary<ulong, SteamWorkshopStoredUpdateItem> items = [];
            var data = Store.Get<SteamWorkshopUpdateSnapshotData>(DataKey);
            var source = data.Scopes.TryGetValue(scope.Key, out var scopedData)
                ? scopedData.Items
                : data.Items;
            foreach (var (key, value) in source)
                if (ulong.TryParse(key, out var itemId))
                    items[itemId] = new(value.Updated, value.Title);
            return items;
        }

        internal static void Replace(
            SteamWorkshopUpdateScope scope,
            IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> items)
        {
            EnsureInitialized();
            Store.Modify<SteamWorkshopUpdateSnapshotData>(DataKey, data =>
            {
                var scopeData = GetOrCreateScopeData(data, scope);
                scopeData.Items.Clear();
                foreach (var (itemId, item) in items)
                    scopeData.Items[itemId.ToString()] = new()
                    {
                        Updated = item.Updated,
                        Title = item.Title,
                    };
            });
            Store.Save(DataKey);
        }

        internal static void Merge(
            SteamWorkshopUpdateScope scope,
            IReadOnlyDictionary<ulong, SteamWorkshopStoredUpdateItem> items)
        {
            EnsureInitialized();
            Store.Modify<SteamWorkshopUpdateSnapshotData>(DataKey, data =>
            {
                var scopeData = GetOrCreateScopeData(data, scope);
                foreach (var (itemId, item) in items)
                    scopeData.Items[itemId.ToString()] = new()
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

        private static SteamWorkshopUpdateScopeData GetOrCreateScopeData(
            SteamWorkshopUpdateSnapshotData data,
            SteamWorkshopUpdateScope scope)
        {
            if (data.Scopes.TryGetValue(scope.Key, out var scopeData))
            {
                scopeData.DisplayName = scope.DisplayName;
                scopeData.BranchName = scope.BranchName;
                return scopeData;
            }

            scopeData = new()
            {
                DisplayName = scope.DisplayName,
                BranchName = scope.BranchName,
            };
            data.Scopes[scope.Key] = scopeData;
            return scopeData;
        }

        private sealed class SteamWorkshopUpdateSnapshotData
        {
            [JsonPropertyName("items")] public Dictionary<string, SteamWorkshopUpdateSnapshotEntry> Items { get; } = [];

            [JsonPropertyName("scopes")] public Dictionary<string, SteamWorkshopUpdateScopeData> Scopes { get; } = [];
        }

        private sealed class SteamWorkshopUpdateScopeData
        {
            [JsonPropertyName("display_name")] public string? DisplayName { get; set; }

            [JsonPropertyName("branch_name")] public string? BranchName { get; set; }

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
