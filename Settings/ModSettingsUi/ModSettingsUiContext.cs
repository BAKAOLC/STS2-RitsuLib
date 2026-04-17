using Godot;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsUiContext(RitsuModSettingsSubmenu submenu, string? pageScopeId = null)
        : IModSettingsUiActionHost
    {
        private readonly Dictionary<string, Dictionary<string, object?>> _rowUiState = [];

        public void MarkDirty(IModSettingsBinding binding)
        {
            submenu.MarkDirty(binding);
        }

        public void RequestRefresh()
        {
            submenu.RequestRefresh();
        }

        public static string Resolve(ModSettingsText? text, string fallback = "")
        {
            return text?.Resolve() ?? fallback;
        }

        public static string ResolvePageTitle(ModSettingsPage page)
        {
            return ModSettingsLocalization.ResolvePageDisplayName(page);
        }

        public static string? ResolvePageDescription(ModSettingsPage page)
        {
            var resolved = page.Description?.Resolve();
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            return Sts2ModManagerCompat.EnumerateModsForManifestLookup()
                .FirstOrDefault(mod => string.Equals(mod.manifest?.id, page.ModId, StringComparison.OrdinalIgnoreCase))
                ?.manifest?.description;
        }

        public static string ResolveBindingDescriptionBody(ModSettingsText? description)
        {
            return Resolve(description);
        }

        public static string GetPersistenceScopeChipText(IModSettingsBinding binding)
        {
            if (binding is ITransientModSettingsBinding)
                return ModSettingsLocalization.Get("scope.transient", "Preview only - not persisted");

            return binding.Scope == SaveScope.Profile
                ? ModSettingsLocalization.Get("scope.profile", "Stored per profile")
                : ModSettingsLocalization.Get("scope.global", "Stored globally");
        }

        public void RegisterRefresh(Action action)
        {
            submenu.RegisterRefreshAction(action, pageScopeId);
        }

        /// <summary>
        ///     Re-evaluates Godot <c>Control.Visible</c> on each debounced refresh (sidebar targets that are not part of
        ///     the main content refresh graph).
        /// </summary>
        public void RegisterDynamicVisibility(Control control, Func<bool> predicate)
        {
            submenu.RegisterDynamicVisibility(control, predicate, pageScopeId);
        }

        public void NavigateToPage(string pageId)
        {
            submenu.NavigateToPage(pageId);
        }

        public void NotifyPasteFailure(ModSettingsPasteFailureReason reason)
        {
            submenu.ShowPasteFailure(reason);
        }

        public bool TryGetRowState<TValue>(string rowKey, string stateKey, out TValue? value)
        {
            value = default;
            if (!_rowUiState.TryGetValue(rowKey, out var row) || !row.TryGetValue(stateKey, out var stored))
                return false;
            if (stored is not TValue typed) return false;
            value = typed;
            return true;
        }

        public void SetRowState<TValue>(string rowKey, string stateKey, TValue value)
        {
            if (!_rowUiState.TryGetValue(rowKey, out var row))
            {
                row = [];
                _rowUiState[rowKey] = row;
            }

            row[stateKey] = value;
        }
    }
}
