namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Global registry of mod settings pages, optional per-mod display names, optional mod-group sidebar ordering,
    ///     and optional per-page sort overrides.
    /// </summary>
    public static class ModSettingsRegistry
    {
        private static readonly Dictionary<string, ModSettingsText> ModDisplayNames =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Optional sidebar group order per mod (lower appears earlier). Mods without an entry sort by display name
        ///     among those sharing the same order value (default <c>0</c>).
        /// </summary>
        private static readonly Dictionary<string, int> ModSidebarOrders = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Overrides <see cref="ModSettingsPage.SortOrder" /> for a page after registration (key: composite id).
        /// </summary>
        private static readonly Dictionary<string, int> PageSortOverrides = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSettingsPage> PagesById =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     True after at least one page has been registered.
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
        /// </summary>
        public static void Register(ModSettingsPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            lock (SyncRoot)
            {
                PagesById[CreateCompositeId(page.ModId, page.Id)] = page;
            }
        }

        /// <summary>
        ///     Registers localized (or literal) text shown for <paramref name="modId" /> in the settings chrome.
        /// </summary>
        public static void RegisterModDisplayName(string modId, ModSettingsText displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(displayName);

            lock (SyncRoot)
            {
                ModDisplayNames[modId] = displayName;
            }
        }

        /// <summary>
        ///     Returns the display name for <paramref name="modId" />, if any.
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
        /// </summary>
        public static void RegisterModSidebarOrder(string modId, int order)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                ModSidebarOrders[modId] = order;
            }
        }

        /// <summary>
        ///     Overrides the sort key for a page among siblings (same <see cref="ModSettingsPage.ModId" /> and
        ///     <see cref="ModSettingsPage.ParentPageId" />). Lower appears earlier; ties break by page id.
        /// </summary>
        public static void RegisterPageSortOrder(string modId, string pageId, int sortOrder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (SyncRoot)
            {
                PageSortOverrides[CreateCompositeId(modId, pageId)] = sortOrder;
            }
        }

        /// <summary>
        ///     Sets <paramref name="pageId" />&apos;s effective order to just after <paramref name="afterPageId" /> (same mod).
        /// </summary>
        /// <returns><see langword="true" /> when <paramref name="afterPageId" /> exists.</returns>
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
                return true;
            }
        }

        /// <summary>
        ///     Sets <paramref name="pageId" />&apos;s effective order to just before <paramref name="beforePageId" /> (same mod).
        /// </summary>
        /// <returns><see langword="true" /> when <paramref name="beforePageId" /> exists.</returns>
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
                return true;
            }
        }

        /// <summary>
        ///     Sidebar group order for <paramref name="modId" />; <c>0</c> when unset.
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
        /// </summary>
        public static IReadOnlyList<ModSettingsPage> GetPages()
        {
            lock (SyncRoot)
            {
                return PagesById.Values
                    .OrderBy(page => ModSidebarOrders.GetValueOrDefault(page.ModId, 0))
                    .ThenBy(page => ModSettingsLocalization.ResolveModName(page.ModId, page.ModId),
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
