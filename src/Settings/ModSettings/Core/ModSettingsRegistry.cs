using System.Reflection;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores mod settings pages together with optional mod display names, mod-group sidebar orders, and page
    ///         sort-order overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">存储模组设置页面，以及可选的模组显示名称、模组分组侧边栏排序和页面排序覆盖值。</para>
    /// </summary>
    public static class ModSettingsRegistry
    {
        private static readonly Dictionary<string, ModSettingsText> ModDisplayNames =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Stores the optional sidebar group order for each mod. Lower values appear first; unregistered mods
        ///         use zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">存储各模组可选的侧边栏分组排序；数值较小的分组排在前面，未注册的模组使用零。</para>
        /// </summary>
        private static readonly Dictionary<string, int> ModSidebarOrders = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">Stores page sort-order overrides by composite mod and page ID.</para>
        ///     <para xml:lang="zh-CN">按模组 ID 与页面 ID 的复合键存储页面排序覆盖值。</para>
        /// </summary>
        private static readonly Dictionary<string, int> PageSortOverrides = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSettingsPage> PagesById =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Caches the fully ordered <see cref="GetPages" /> snapshot until page registration, display-name
        ///         fallback, or an ordering input changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         缓存完全排序后的 <see cref="GetPages" /> 快照，直至页面注册、显示名称回退或排序输入发生变化。
        ///     </para>
        /// </summary>
        private static IReadOnlyList<ModSettingsPage>? _sortedPagesCache;

        /// <summary>
        ///     <para xml:lang="en">Gets whether at least one page is currently registered.</para>
        ///     <para xml:lang="zh-CN">获取当前是否至少注册了一个页面。</para>
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
        ///     <para xml:lang="en">
        ///         Adds or replaces a built page using its composite mod and page ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">按页面的模组 ID 与页面 ID 复合键添加或替换已构建页面。</para>
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
        ///     <para xml:lang="en">
        ///         Invalidates the ordered page snapshot so the next <see cref="GetPages" /> call rebuilds it. Use this
        ///         when an external display-name fallback or other ordering input changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使有序页面快照失效，以便下一次 <see cref="GetPages" /> 调用重新构建；外部显示名称回退或其他排序输入变化时使用。
        ///     </para>
        /// </summary>
        public static void InvalidateOrderingCache()
        {
            lock (SyncRoot)
            {
                _sortedPagesCache = null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds or replaces the localized or literal display text for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加或替换 <paramref name="modId" /> 的本地化或字面显示文本。</para>
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
        ///     <para xml:lang="en">Gets the registered display text for <paramref name="modId" />, if any.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 已注册的显示文本（如有）。</para>
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
        ///     <para xml:lang="en">
        ///         Adds or replaces the sidebar group order for <paramref name="modId" />. Lower values appear first;
        ///         unregistered mods use zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加或替换 <paramref name="modId" /> 的侧边栏分组排序值；数值较小的分组排在前面，未注册的模组使用零。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Adds or replaces a page sort-order override. The override may be registered before or after the page;
        ///         lower values place sibling pages earlier.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加或替换页面排序覆盖值；可在页面注册前或注册后设置，数值较小的同级页面排在前面。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Sets <paramref name="pageId" />'s effective order to the positive <paramref name="gap" /> immediately
        ///         after <paramref name="afterPageId" /> in the same mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将同一模组中 <paramref name="pageId" /> 的有效排序设为紧随 <paramref name="afterPageId" />
        ///         之后的正数 <paramref name="gap" />。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when <paramref name="afterPageId" /> is registered; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="afterPageId" /> 已注册时为 <see langword="true" />，否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryRegisterPageSortOrderAfter(string modId, string pageId, string afterPageId, int gap = 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(afterPageId);
            if (gap <= 0)
                throw new ArgumentOutOfRangeException(nameof(gap), "Page sort-order gap must be greater than zero.");

            lock (SyncRoot)
            {
                if (!PagesById.TryGetValue(CreateCompositeId(modId, afterPageId), out var after))
                    return false;

                var baseOrder =
                    PageSortOverrides.GetValueOrDefault(CreateCompositeId(modId, afterPageId), after.SortOrder);
                PageSortOverrides[CreateCompositeId(modId, pageId)] = checked(baseOrder + gap);
                _sortedPagesCache = null;
                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets <paramref name="pageId" />'s effective order to the positive <paramref name="gap" /> immediately
        ///         before <paramref name="beforePageId" /> in the same mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将同一模组中 <paramref name="pageId" /> 的有效排序设为紧邻 <paramref name="beforePageId" />
        ///         之前的正数 <paramref name="gap" />。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when <paramref name="beforePageId" /> is registered; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <paramref name="beforePageId" /> 已注册时为 <see langword="true" />，否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryRegisterPageSortOrderBefore(string modId, string pageId, string beforePageId, int gap = 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(beforePageId);
            if (gap <= 0)
                throw new ArgumentOutOfRangeException(nameof(gap), "Page sort-order gap must be greater than zero.");

            lock (SyncRoot)
            {
                if (!PagesById.TryGetValue(CreateCompositeId(modId, beforePageId), out var before))
                    return false;

                var baseOrder = PageSortOverrides.GetValueOrDefault(CreateCompositeId(modId, beforePageId),
                    before.SortOrder);
                PageSortOverrides[CreateCompositeId(modId, pageId)] = checked(baseOrder - gap);
                _sortedPagesCache = null;
                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the sidebar group order for <paramref name="modId" />, or zero when unset.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的侧边栏分组排序值；未设置时为零。</para>
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
        ///     <para xml:lang="en">
        ///         Gets the registered override for <paramref name="page" />, or its
        ///         <see cref="ModSettingsPage.SortOrder" /> when no override exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="page" /> 已注册的排序覆盖值；没有覆盖值时返回
        ///         <see cref="ModSettingsPage.SortOrder" />。
        ///     </para>
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
        ///     <para xml:lang="en">Builds a page through <paramref name="configure" /> and registers the result.</para>
        ///     <para xml:lang="zh-CN">通过 <paramref name="configure" /> 构建页面并注册结果。</para>
        /// </summary>
        public static void Register(string modId, Action<ModSettingsPageBuilder> configure, string? pageId = null)
        {
            RegisterCore(modId, configure, pageId, null);
        }

        internal static void RegisterWithSourceAssembly(string modId, Action<ModSettingsPageBuilder> configure,
            string? pageId, Assembly? sourceAssembly)
        {
            RegisterCore(modId, configure, pageId, sourceAssembly);
        }

        private static void RegisterCore(string modId, Action<ModSettingsPageBuilder> configure, string? pageId,
            Assembly? sourceAssembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(configure);

            var builder = new ModSettingsPageBuilder(modId, pageId, sourceAssembly);
            configure(builder);
            Register(builder.Build());
        }

        /// <summary>
        ///     <para xml:lang="en">Looks up a page by mod ID and page ID, case-insensitively.</para>
        ///     <para xml:lang="zh-CN">按模组 ID 与页面 ID 查找页面，不区分大小写。</para>
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
        ///     <para xml:lang="en">
        ///         Gets a cached immutable snapshot of all registered pages, ordered by mod-group order, mod display-name
        ///         fallback, mod ID, effective page order, and page ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取所有已注册页面的缓存不可变快照，依次按模组分组排序、模组显示名称回退、模组 ID、页面有效排序及页面 ID 排列。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<ModSettingsPage> GetPages()
        {
            lock (SyncRoot)
            {
                return _sortedPagesCache ??= Array.AsReadOnly(
                    [.. PagesById.Values
                        .OrderBy(page => ModSidebarOrders.GetValueOrDefault(page.ModId, 0))
                        .ThenBy(page => ModSettingsLocalization.ResolveModNameFallback(page.ModId, page.ModId),
                            StringComparer.OrdinalIgnoreCase)
                        .ThenBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(page => PageSortOverrides.GetValueOrDefault(CreateCompositeId(page.ModId, page.Id),
                            page.SortOrder))
                        .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)]);
            }
        }

        private static string CreateCompositeId(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
