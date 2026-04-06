using Godot;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed class ModSettingsUiContext(RitsuModSettingsSubmenu submenu) : IModSettingsUiActionHost
    {
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
            return binding switch
            {
                ITransientModSettingsBinding => ModSettingsLocalization.Get("scope.transient",
                    "Preview only - not persisted"),
                IModSettingsRunOverlayBinding => ModSettingsLocalization.Get("scope.runOverlay",
                    "Per-run overlay (values isolated during an active run; commit/discards depend on binding)"),
                _ => binding.Scope == SaveScope.Profile
                    ? ModSettingsLocalization.Get("scope.profile", "Stored per profile")
                    : ModSettingsLocalization.Get("scope.global", "Stored globally"),
            };
        }

        public void RegisterRefresh(Action action)
        {
            submenu.RegisterRefreshAction(action);
        }

        /// <summary>
        ///     Re-evaluates Godot <c>Control.Visible</c> on each debounced refresh (sidebar targets that are not part of
        ///     the main content refresh graph).
        /// </summary>
        public void RegisterDynamicVisibility(Control control, Func<bool> predicate)
        {
            submenu.RegisterDynamicVisibility(control, predicate);
        }

        public void NavigateToPage(string pageId)
        {
            submenu.NavigateToPage(pageId);
        }

        public void NotifyPasteFailure(ModSettingsPasteFailureReason reason)
        {
            submenu.ShowPasteFailure(reason);
        }
    }
}
