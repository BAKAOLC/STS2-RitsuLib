using System.Text.Json.Serialization;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Search
{
    internal sealed class RitsuSearchSettings
    {
        internal const int CurrentSchemaVersion = 1;

        [JsonPropertyName(ModDataVersion.SchemaVersionProperty)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("provider_enabled")]
        public Dictionary<string, bool> ProviderEnabled { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("automatic_pinyin_data_downloads")]
        public bool AutomaticPinyinDataDownloads { get; set; }

        [JsonPropertyName("keep_pinyin_source_archive")]
        public bool KeepPinyinSourceArchive { get; set; }
    }

    internal static class RitsuSearchSettingsStore
    {
        internal const string DataKey = "search-settings";
        internal const string FileName = "ritsulib-search-settings.json";
        private static readonly Lock SyncRoot = new();
        private static ModDataStore? _store;

        internal static void Register(ModDataStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            lock (SyncRoot)
            {
                if (_store != null)
                    return;
                store.Register<RitsuSearchSettings>(
                    DataKey,
                    FileName,
                    SaveScope.Global,
                    static () => new(),
                    true,
                    new()
                    {
                        CurrentDataVersion = RitsuSearchSettings.CurrentSchemaVersion,
                        MinimumSupportedDataVersion = RitsuSearchSettings.CurrentSchemaVersion,
                    });
                _store = store;
            }
        }

        internal static bool IsProviderEnabled(string providerId, bool defaultValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
            lock (SyncRoot)
            {
                var settings = GetSettings();
                if (settings.ProviderEnabled.TryGetValue(providerId, out var enabled))
                    return enabled;
                foreach (var (id, value) in settings.ProviderEnabled)
                    if (string.Equals(id, providerId, StringComparison.OrdinalIgnoreCase))
                        return value;
                return defaultValue;
            }
        }

        internal static void SetProviderEnabled(string providerId, bool enabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
            lock (SyncRoot)
            {
                var settings = GetSettings();
                var existingKey = settings.ProviderEnabled.Keys.FirstOrDefault(id =>
                    string.Equals(id, providerId, StringComparison.OrdinalIgnoreCase));
                if (existingKey != null && !string.Equals(existingKey, providerId, StringComparison.Ordinal))
                    settings.ProviderEnabled.Remove(existingKey);
                settings.ProviderEnabled[providerId] = enabled;
                Save();
            }

            RitsuSearchExpansionRegistry.NotifyConfigurationChanged();
        }

        internal static bool GetAutomaticPinyinDataDownloads()
        {
            lock (SyncRoot)
            {
                return GetSettings().AutomaticPinyinDataDownloads;
            }
        }

        internal static void SetAutomaticPinyinDataDownloads(bool value)
        {
            lock (SyncRoot)
            {
                var settings = GetSettings();
                if (settings.AutomaticPinyinDataDownloads == value)
                    return;
                settings.AutomaticPinyinDataDownloads = value;
                Save();
            }
        }

        internal static bool GetKeepPinyinSourceArchive()
        {
            lock (SyncRoot)
            {
                return GetSettings().KeepPinyinSourceArchive;
            }
        }

        internal static void SetKeepPinyinSourceArchive(bool value)
        {
            lock (SyncRoot)
            {
                var settings = GetSettings();
                if (settings.KeepPinyinSourceArchive == value)
                    return;
                settings.KeepPinyinSourceArchive = value;
                Save();
            }
        }

        private static RitsuSearchSettings GetSettings()
        {
            if (_store == null)
                throw new InvalidOperationException("Ritsu search settings have not been registered.");
            return _store.Get<RitsuSearchSettings>(DataKey);
        }

        private static void Save()
        {
            _store!.Save(DataKey);
        }
    }
}
