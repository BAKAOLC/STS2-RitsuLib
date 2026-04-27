using System.Text.RegularExpressions;
using Godot;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class ModSettingsUiContext(RitsuModSettingsSubmenu submenu, string? pageScopeId = null)
        : IModSettingsUiActionHost
    {
        private readonly Dictionary<string, Dictionary<string, object?>> _rowUiState = [];

        private ModSettingsPage? _sectionBuildPage;
        private ModSettingsSection? _sectionBuildSection;

        public void MarkDirty(IModSettingsBinding binding)
        {
            submenu.MarkDirty(binding);
        }

        public void RequestRefresh()
        {
            submenu.RequestRefresh();
        }

        public void RequestRefreshAfterDataModelBatchChange()
        {
            submenu.RequestRefreshAfterDataModelBatchChange();
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
            return NormalizeDescriptionRichText(Resolve(description));
        }

        private static string NormalizeDescriptionRichText(string s)
        {
            return string.IsNullOrEmpty(s) ? s : LegacyCodeTagRegex().Replace(s, "[code]$1[/code]");
        }

        [GeneratedRegex("<c>(.*?)</c>", RegexOptions.Singleline)]
        private static partial Regex LegacyCodeTagRegex();

        public static string GetPersistenceScopeChipText(IModSettingsBinding binding)
        {
            return binding switch
            {
                ITransientModSettingsBinding => ModSettingsLocalization.Get("scope.transient",
                    "Preview only - not persisted"),
                IRunSidecarModSettingsBinding => ModSettingsLocalization.Get("scope.runSidecar", "Run sidecar"),
                IModSettingsBindingSemantics { Semantics: ModSettingsValueSemantics.RunSnapshot } =>
                    ModSettingsLocalization.Get("scope.runSnapshot", "Run snapshot"),
                IModSettingsBindingSemantics { Semantics: ModSettingsValueSemantics.SessionCombat } =>
                    ModSettingsLocalization.Get("scope.sessionCombat", "Combat/session only"),
                _ => binding.Scope == SaveScope.Profile
                    ? ModSettingsLocalization.Get("scope.profile", "Stored per profile")
                    : ModSettingsLocalization.Get("scope.global", "Stored globally"),
            };
        }

        /// <summary>
        ///     Registers a callback invoked on the next UI refresh. Same as calling
        ///     <see cref="RegisterRefresh(Action, ModSettingsUiRefreshSpec)" /> with a full-pass spec (legacy behavior for
        ///     extensions compiled against older RitsuLib).
        /// </summary>
        public void RegisterRefresh(Action action)
        {
            RegisterRefresh(action, default);
        }

        /// <summary>
        ///     Registers a callback invoked on the next UI refresh when its <paramref name="spec" /> matches the
        ///     bindings that were marked dirty since the last flush.
        /// </summary>
        public void RegisterRefresh(Action action, ModSettingsUiRefreshSpec spec)
        {
            submenu.RegisterRefreshAction(action, spec, pageScopeId);
        }

        internal void BeginSectionSurfaceScope(ModSettingsPage page, ModSettingsSection section)
        {
            _sectionBuildPage = page;
            _sectionBuildSection = section;
        }

        internal void EndSectionSurfaceScope()
        {
            _sectionBuildPage = null;
            _sectionBuildSection = null;
        }

        internal ModSettingsHostSurface GetSectionHostReadOnlyMask()
        {
            var mask = ModSettingsHostSurface.None;
            if (_sectionBuildPage != null)
                mask |= _sectionBuildPage.ReadOnlyOnHostSurfaces;
            if (_sectionBuildSection != null)
                mask |= _sectionBuildSection.ReadOnlyOnHostSurfaces;
            return mask;
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

        internal void MigrateRowState(string fromRowKey, string toRowKey)
        {
            if (string.Equals(fromRowKey, toRowKey, StringComparison.Ordinal))
                return;

            if (!_rowUiState.TryGetValue(fromRowKey, out var fromRow) || fromRow.Count == 0)
                return;

            if (!_rowUiState.TryGetValue(toRowKey, out var toRow))
            {
                toRow = [];
                _rowUiState[toRowKey] = toRow;
            }

            foreach (var kv in fromRow)
                toRow[kv.Key] = kv.Value;

            _rowUiState.Remove(fromRowKey);
        }
    }
}
