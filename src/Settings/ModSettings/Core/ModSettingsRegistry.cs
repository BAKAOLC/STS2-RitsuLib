namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Global registry of mod settings pages, optional per-mod display names, optional mod-group sidebar ordering,
    ///     and optional per-page sort overrides.
    ///     Mod 设置页的全局注册表，包含可选的每 Mod 显示名称、可选的 Mod 分组侧边栏排序，以及可选的逐页面排序覆盖。
    /// </summary>
    public static class ModSettingsRegistry
    {
        private static readonly Dictionary<string, ModSettingsText> ModDisplayNames =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Optional sidebar group order per mod (lower appears earlier). Mods without an entry sort by display name
        ///     among those sharing the same order value (default <c>0</c>).
        ///     每个 Mod 可选的侧边栏分组排序（数值越小越靠前）。没有条目的 Mod 使用默认值 <c>0</c>，
        ///     并在同排序值中按显示名称排序。
        /// </summary>
        private static readonly Dictionary<string, int> ModSidebarOrders = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Overrides <see cref="ModSettingsPage.SortOrder" /> for a page after registration (key: composite id).
        ///     在注册后覆盖页面的 <see cref="ModSettingsPage.SortOrder" />（key：复合 id）。
        /// </summary>
        private static readonly Dictionary<string, int> PageSortOverrides = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSettingsPage> PagesById =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Cached, fully ordered <see cref="GetPages" /> result. Invalidated whenever the page set or any
        ///     ordering input changes (see <see cref="InvalidateOrderingCache" />). Rebuilding this is expensive
        ///     (multi-key sort + per-page localization), and the settings UI calls <see cref="GetPages" /> many
        ///     times per refresh, so the snapshot is reused until something actually changes.
        ///     已缓存的、完全排序的 <see cref="GetPages" /> 结果。当页面集合或任意排序输入变化时失效（见
        ///     <see cref="InvalidateOrderingCache" />）。重建开销很大（多键排序 + 逐页本地化），而设置 UI 每次刷新
        ///     都会多次调用 <see cref="GetPages" />，因此在真正发生变化前复用该快照。
        /// </summary>
        private static IReadOnlyList<ModSettingsPage>? _sortedPagesCache;

        /// <summary>
        ///     True after at least one page has been registered.
        ///     至少注册一个页面后为 true。
        /// </summary>
        public static bool HasPages
        {
            get
            {
                lock (SyncRoot)
                {
                    return PagesById.Count > 0;
                }
            }
        }

        /// <summary>
        ///     Registers a built <see cref="ModSettingsPage" /> (typically from <see cref="ModSettingsPageBuilder" />).
        ///     注册已构建的 <see cref="ModSettingsPage" />（通常来自 <see cref="ModSettingsPageBuilder" />）。
        /// </summary>
        public static void Register(ModSettingsPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            lock (SyncRoot)
            {
                PagesById[CreateCompositeId(page.ModId, page.Id)] = page;
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     Drops the cached <see cref="GetPages" /> ordering so the next call rebuilds it. Call this when an
        ///     external ordering input changes that the registry cannot observe itself (e.g. the active locale,
        ///     which participates in the mod display-name tie-break).
        ///     丢弃缓存的 <see cref="GetPages" /> 排序，使下次调用重建。当注册表自身无法观察到的外部排序输入变化时
        ///     调用此方法（例如当前语言，它参与 mod 显示名称的同序打破）。
        /// </summary>
        public static void InvalidateOrderingCache()
        {
            lock (SyncRoot)
            {
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     Registers localized (or literal) text shown for <paramref name="modId" /> in the settings chrome.
        ///     注册在设置 chrome 中为 <paramref name="modId" /> 显示的本地化（或字面）文本。
        /// </summary>
        public static void RegisterModDisplayName(string modId, ModSettingsText displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(displayName);

            lock (SyncRoot)
            {
                ModDisplayNames[modId] = displayName;
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     Returns the display name for <paramref name="modId" />, if any.
        ///     返回 <paramref name="modId" /> 的显示名称（如果有）。
        /// </summary>
        public static ModSettingsText? GetModDisplayName(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                return ModDisplayNames.GetValueOrDefault(modId);
            }
        }

        /// <summary>
        ///     Registers ordering for this mod&apos;s group in the settings sidebar. Lower <paramref name="order" /> appears
        ///     earlier. Mods without a registered order use <c>0</c> and sort by resolved display name among peers.
        ///     注册此 mod 在设置侧边栏中分组的顺序。<paramref name="order" /> 越小越靠前。Mods
        ///     未注册顺序时使用 <c>0</c>，并在同级之间按解析后的显示名称排序。
        /// </summary>
        public static void RegisterModSidebarOrder(string modId, int order)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                ModSidebarOrders[modId] = order;
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     Overrides the sort key for a page among siblings (same <see cref="ModSettingsPage.ModId" /> and
        ///     <see cref="ModSettingsPage.ParentPageId" />). Lower appears earlier; ties break by page id.
        ///     覆盖页面在兄弟页面（相同 <see cref="ModSettingsPage.ModId" /> 和
        ///     <see cref="ModSettingsPage.ParentPageId" />）中的排序 key。值越小越靠前；相同则按 page id 打破平局。
        /// </summary>
        public static void RegisterPageSortOrder(string modId, string pageId, int sortOrder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (SyncRoot)
            {
                PageSortOverrides[CreateCompositeId(modId, pageId)] = sortOrder;
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     Sets <paramref name="pageId" />&apos;s effective order to just after <paramref name="afterPageId" /> (same mod).
        /// </summary>
        /// 将
        /// <paramref name="pageId" />
        /// 的有效排序设置为刚好位于
        /// <paramref name="afterPageId" />
        /// 之后（同一 Mod）。
        /// <returns>
        ///     <see langword="true" /> when <paramref name="afterPageId" /> exists.
        ///     当 <paramref name="afterPageId" /> 存在时为 <see langword="true" />。
        /// </returns>
        public static bool TryRegisterPageSortOrderAfter(string modId, string pageId, string afterPageId, int gap = 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(afterPageId);

            lock (SyncRoot)
            {
                if (!PagesById.TryGetValue(CreateCompositeId(modId, afterPageId), out var after))
                    return false;

                var baseOrder =
                    PageSortOverrides.GetValueOrDefault(CreateCompositeId(modId, afterPageId), after.SortOrder);
                PageSortOverrides[CreateCompositeId(modId, pageId)] = baseOrder + gap;
                _sortedPagesCache = null;
                return true;
            }
        }

        /// <summary>
        ///     Sets <paramref name="pageId" />&apos;s effective order to just before <paramref name="beforePageId" /> (same mod).
        /// </summary>
        /// 将
        /// <paramref name="pageId" />
        /// 的有效排序设置为刚好位于
        /// <paramref name="beforePageId" />
        /// 之前（同一 Mod）。
        /// <returns>
        ///     <see langword="true" /> when <paramref name="beforePageId" /> exists.
        ///     当 <paramref name="beforePageId" /> 存在时为 <see langword="true" />。
        /// </returns>
        public static bool TryRegisterPageSortOrderBefore(string modId, string pageId, string beforePageId, int gap = 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(beforePageId);

            lock (SyncRoot)
            {
                if (!PagesById.TryGetValue(CreateCompositeId(modId, beforePageId), out var before))
                    return false;

                var baseOrder = PageSortOverrides.GetValueOrDefault(CreateCompositeId(modId, beforePageId),
                    before.SortOrder);
                PageSortOverrides[CreateCompositeId(modId, pageId)] = baseOrder - gap;
                _sortedPagesCache = null;
                return true;
            }
        }

        /// <summary>
        ///     Sidebar group order for <paramref name="modId" />; <c>0</c> when unset.
        ///     <paramref name="modId" /> 的侧边栏分组顺序；未设置时为 <c>0</c>。
        /// </summary>
        public static int GetModSidebarOrder(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                return ModSidebarOrders.GetValueOrDefault(modId, 0);
            }
        }

        /// <summary>
        ///     Effective sort key for <paramref name="page" /> (override or <see cref="ModSettingsPage.SortOrder" />).
        ///     <paramref name="page" /> 的有效排序 key（覆盖值或 <see cref="ModSettingsPage.SortOrder" />）。
        /// </summary>
        public static int GetEffectivePageSortOrder(ModSettingsPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            lock (SyncRoot)
            {
                return PageSortOverrides.GetValueOrDefault(CreateCompositeId(page.ModId, page.Id), page.SortOrder);
            }
        }

        /// <summary>
        ///     Fluent helper: builds a page via <paramref name="configure" /> and registers it.
        ///     流式 helper：通过 <paramref name="configure" /> 构建页面并注册。
        /// </summary>
        public static void Register(string modId, Action<ModSettingsPageBuilder> configure, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(configure);

            var builder = new ModSettingsPageBuilder(modId, pageId);
            configure(builder);
            Register(builder.Build());
        }

        /// <summary>
        ///     Looks up a page by mod id and page id.
        ///     通过 Mod id 和页面 id 查找页面。
        /// </summary>
        public static bool TryGetPage(string modId, string pageId, out ModSettingsPage? page)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (SyncRoot)
            {
                return PagesById.TryGetValue(CreateCompositeId(modId, pageId), out page);
            }
        }

        /// <summary>
        ///     All registered pages, ordered for stable sidebar display: mod group order, then mod display name, then
        ///     effective page order within the mod, then page id.
        ///     所有已注册页面，按稳定侧边栏显示顺序排序：Mod 分组排序、Mod 显示名称、Mod 内有效页面排序、页面 id。
        /// </summary>
        public static IReadOnlyList<ModSettingsPage> GetPages()
        {
            lock (SyncRoot)
            {
                return _sortedPagesCache ??= PagesById.Values
                    .OrderBy(page => ModSidebarOrders.GetValueOrDefault(page.ModId, 0))
                    .ThenBy(page => ModSettingsLocalization.ResolveModNameFallback(page.ModId, page.ModId),
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(page => PageSortOverrides.GetValueOrDefault(CreateCompositeId(page.ModId, page.Id),
                        page.SortOrder))
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        private static string CreateCompositeId(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
