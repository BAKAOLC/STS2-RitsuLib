using System.Text.Json.Serialization;
using STS2RitsuLib.Data;
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
    }
}
