using System.Text.Json.Serialization;
using STS2RitsuLib.Data;
using STS2RitsuLib.Search;
using STS2RitsuLib.Ui.Catalog;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugToolsInterfaceStateStore
    {
        private const string DataKey = "debug_tools_interface_state";
        private const string FileName = "debug_tools_interface_state.json";
        private static ModDataStore? _store;
        private static IDisposable? _profileServicesSubscription;

        internal static event Action<bool>? StateRestored;

        internal static void Register(ModDataStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            if (_store != null)
                return;

            _store = store;
            store.Register<RitsuDebugToolsInterfaceState>(
                DataKey,
                FileName,
                SaveScope.Profile,
                static () => new(),
                true);
            store.EntryReloaded += OnEntryReloaded;
            _profileServicesSubscription =
                RitsuLibFramework.SubscribeLifecycle<ProfileServicesInitializedEvent>(_ => PublishRestoredState());
        }

        internal static bool IsVisible()
        {
            return TryGetState(out var state) && state.IsVisible;
        }

        internal static void RememberVisibility(bool isVisible)
        {
            if (_store is not { IsProfileInitialized: true } store)
                return;

            try
            {
                var state = store.Get<RitsuDebugToolsInterfaceState>(DataKey);
                if (state.IsVisible == isVisible)
                    return;

                store.Modify<RitsuDebugToolsInterfaceState>(
                    DataKey,
                    value => value.IsVisible = isVisible);
                store.Save(DataKey);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Failed to save the developer-tools panel state: {ex.Message}");
            }
        }

        internal static RitsuDebugSearchPreferences GetSearchPreferences(string catalogId)
        {
            if (TryGetState(out var state) &&
                state.Catalogs?.TryGetValue(catalogId, out var preferences) == true && preferences != null)
                return preferences.Copy();
            return new();
        }

        internal static void RememberSearchPreferences(string catalogId, RitsuDebugSearchPreferences preferences)
        {
            if (_store is not { IsProfileInitialized: true } store || !TryGetState(out var state))
                return;
            try
            {
                state.Catalogs ??= new(StringComparer.Ordinal);
                if (!state.Catalogs.ContainsKey(catalogId) && state.Catalogs.Count >= 128)
                    return;
                state.Catalogs[catalogId] = preferences.Copy();
                store.Save(DataKey);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[DebugTools] Failed to save search preferences: {ex.Message}");
            }
        }

        private static void OnEntryReloaded(string key)
        {
            if (key.Equals(DataKey, StringComparison.OrdinalIgnoreCase))
                PublishRestoredState();
        }

        private static void PublishRestoredState()
        {
            StateRestored?.Invoke(IsVisible());
        }

        private static bool TryGetState(out RitsuDebugToolsInterfaceState state)
        {
            state = null!;
            if (_store is not { IsProfileInitialized: true } store)
                return false;

            try
            {
                state = store.Get<RitsuDebugToolsInterfaceState>(DataKey);
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Failed to load the developer-tools panel state: {ex.Message}");
                return false;
            }
        }
    }

    internal sealed class RitsuDebugToolsInterfaceState
    {
        [JsonPropertyName("is_visible")] public bool IsVisible { get; set; }

        [JsonPropertyName("catalogs")]
        public Dictionary<string, RitsuDebugSearchPreferences> Catalogs { get; set; } = new(StringComparer.Ordinal);
    }

    internal sealed class RitsuDebugSearchPreferences
    {
        public bool AdvancedMode { get; set; }
        public RitsuCatalogSearchFields Fields { get; set; } = RitsuCatalogSearchFields.Default;
        public bool? Pinyin { get; set; }
        public bool? PinyinInitials { get; set; }
        public Dictionary<string, bool> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string?> Filters { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, bool> ExpandedSections { get; set; } = new(StringComparer.Ordinal);

        internal RitsuDebugSearchPreferences Copy()
        {
            return new()
            {
                AdvancedMode = AdvancedMode,
                Fields = Fields & RitsuCatalogSearchFields.All,
                Pinyin = Pinyin,
                PinyinInitials = PinyinInitials,
                Providers = CopyProviders(),
                ExpandedSections = ExpandedSections == null
                    ? new(StringComparer.Ordinal)
                    : ExpandedSections.Where(static pair => pair.Key.Length is > 0 and <= 160)
                        .Take(132).ToDictionary(static pair => pair.Key, static pair => pair.Value,
                            StringComparer.Ordinal),
                Filters = Filters == null
                    ? new(StringComparer.Ordinal)
                    : Filters.Where(static pair =>
                            pair.Key.Length <= 128 && pair.Value is not { Length: > 256 })
                        .Take(8).ToDictionary(static pair => pair.Key, static pair => pair.Value,
                            StringComparer.Ordinal),
            };
        }

        private Dictionary<string, bool> CopyProviders()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (Providers == null)
                return result;
            foreach (var (id, enabled) in Providers)
            {
                if (result.Count >= 128)
                    break;
                if (RitsuSearchOptions.IsValidProviderId(id))
                    result.TryAdd(id, enabled);
            }

            return result;
        }
    }
}
