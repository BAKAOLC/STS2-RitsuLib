using System.Text.RegularExpressions;
using Godot;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class ModSettingsUiContext(
        RitsuModSettingsSubmenu submenu,
        string? pageScopeId = null,
        object? pageEnableGate = null)
        : IModSettingsUiActionHost
    {
        private readonly Dictionary<string, Dictionary<string, object?>> _rowUiState = [];

        private ModSettingsEntryDefinition? _sectionBuildEntry;
        private ModSettingsPage? _sectionBuildPage;
        private ModSettingsSection? _sectionBuildSection;

        internal object? PageEnableGate => pageEnableGate;

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
            ArgumentNullException.ThrowIfNull(fallback);
            return text?.Resolve() ?? fallback;
        }

        public static string ResolvePageTitle(ModSettingsPage page)
        {
            return ModSettingsLocalization.ResolvePageDisplayName(page);
        }

        public static string? ResolvePageDescription(ModSettingsPage page)
        {
            if (page.HideDescription)
                return null;

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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a callback for full-pass UI refreshes. This is equivalent to calling
        ///         <see cref="RegisterRefresh(Action, ModSettingsUiRefreshSpec)" /> with the default full-pass
        ///         specification and preserves compatibility with extensions built against older RitsuLib versions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册用于完整遍历界面刷新的回调。该方法等同于使用默认的完整遍历规范调用
        ///         <see cref="RegisterRefresh(Action, ModSettingsUiRefreshSpec)" />，并保留对旧版 RitsuLib 扩展的兼容性。
        ///     </para>
        /// </summary>
        public void RegisterRefresh(Action action)
        {
            RegisterRefresh(action, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a UI-refresh callback selected by <paramref name="spec" /> against bindings marked dirty
        ///         since the previous refresh flush.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册界面刷新回调；<paramref name="spec" /> 会与上一次刷新队列执行后标记为脏的绑定进行匹配。
        ///     </para>
        /// </summary>
        public void RegisterRefresh(Action action, ModSettingsUiRefreshSpec spec)
        {
            ArgumentNullException.ThrowIfNull(action);
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
            _sectionBuildEntry = null;
        }

        internal void BeginEntrySurfaceScope(ModSettingsEntryDefinition entry)
        {
            _sectionBuildEntry = entry;
        }

        internal void EndEntrySurfaceScope()
        {
            _sectionBuildEntry = null;
        }

        internal void RegisterEntryAnchor(ModSettingsPage page, ModSettingsSection section,
            ModSettingsEntryDefinition entry, Control control)
        {
            submenu.RegisterEntryAnchor(page, section, entry, control);
        }

        internal ModSettingsHostSurface GetSectionHostReadOnlyMask()
        {
            return ModSettingsUiHostSurfacePolicy.MergeReadOnlyMask(_sectionBuildPage, _sectionBuildSection,
                _sectionBuildEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a Godot control whose visibility predicate is reevaluated on every debounced refresh. This
        ///         supports sidebar controls outside the main content refresh graph.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个 Godot 控件，在每次防抖刷新时重新计算其可见性谓词。用于主内容刷新图之外的侧边栏控件。
        ///     </para>
        /// </summary>
        public void RegisterDynamicVisibility(Control control, Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(predicate);
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
            ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(stateKey);
            value = default;
            if (!_rowUiState.TryGetValue(rowKey, out var row) || !row.TryGetValue(stateKey, out var stored))
                return false;
            if (stored is not TValue typed) return false;
            value = typed;
            return true;
        }

        public void SetRowState<TValue>(string rowKey, string stateKey, TValue value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(stateKey);
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
