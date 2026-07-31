using System.Reflection;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the standard context-menu actions exposed for pages, sections, and entries.</para>
    ///     <para xml:lang="zh-CN">指定页面、节及条目所公开的标准上下文菜单操作。</para>
    /// </summary>
    [Flags]
    public enum ModSettingsMenuCapabilities
    {
        /// <summary>
        ///     <para xml:lang="en">Exposes no standard context-menu actions.</para>
        ///     <para xml:lang="zh-CN">不公开任何标准上下文菜单操作。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">Allows copying the current value or supported subtree.</para>
        ///     <para xml:lang="zh-CN">允许复制当前值或其支持的子树。</para>
        /// </summary>
        Copy = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">Allows pasting compatible clipboard content.</para>
        ///     <para xml:lang="zh-CN">允许粘贴兼容的剪贴板内容。</para>
        /// </summary>
        Paste = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Allows explicitly resetting supported values to their defaults.</para>
        ///     <para xml:lang="zh-CN">允许将支持的值显式重置为默认值。</para>
        /// </summary>
        ResetToDefault = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">Exposes all standard context-menu actions.</para>
        ///     <para xml:lang="zh-CN">公开所有标准上下文菜单操作。</para>
        /// </summary>
        All = Copy | Paste | ResetToDefault,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents one immutable settings page definition, including its sidebar metadata, hierarchy,
    ///         availability, host-surface behavior, and ordered sections.
    ///     </para>
    ///     <para xml:lang="zh-CN">表示不可变的设置页面定义，包括侧边栏元数据、页面层级、可用状态、宿主界面行为及有序节。</para>
    /// </summary>
    public sealed class ModSettingsPage
    {
        internal ModSettingsPage(
            string modId,
            string id,
            string? parentPageId,
            ModSettingsText? title,
            ModSettingsText? description,
            int sortOrder,
            IReadOnlyList<ModSettingsSection> sections,
            Func<bool>? visibleWhen = null,
            Func<bool>? enabledWhen = null,
            ModSettingsMenuCapabilities menuCapabilities = ModSettingsMenuCapabilities.All,
            ModSettingsHostSurface visibleOnHostSurfaces = ModSettingsHostSurface.All,
            ModSettingsHostSurface readOnlyOnHostSurfaces = ModSettingsHostSurface.None,
            bool sidebarVisibleOnlyWhenActive = false,
            bool hideDescription = false,
            Assembly? sourceAssembly = null,
            bool useSourceAssemblyManifestLookup = true)
        {
            ModId = modId;
            Id = id;
            ParentPageId = parentPageId;
            Title = title;
            Description = description;
            SortOrder = sortOrder;
            Sections = Array.AsReadOnly(
                [.. sections ?? throw new ArgumentNullException(nameof(sections))]);
            VisibleWhen = visibleWhen;
            EnabledWhen = enabledWhen;
            MenuCapabilities = menuCapabilities;
            VisibleOnHostSurfaces = visibleOnHostSurfaces;
            ReadOnlyOnHostSurfaces = readOnlyOnHostSurfaces;
            SidebarVisibleOnlyWhenActive = sidebarVisibleOnlyWhenActive;
            HideDescription = hideDescription;
            SourceAssembly = sourceAssembly;
            UseSourceAssemblyManifestLookup = useSourceAssemblyManifestLookup;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the owning mod ID.</para>
        ///     <para xml:lang="zh-CN">获取所属模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable page ID within the mod.</para>
        ///     <para xml:lang="zh-CN">获取页面在模组内的稳定 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parent page ID; root pages have no parent.</para>
        ///     <para xml:lang="zh-CN">获取可选的父页面 ID；根页面没有父页面。</para>
        /// </summary>
        public string? ParentPageId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized page title displayed by the settings UI.</para>
        ///     <para xml:lang="zh-CN">获取设置界面显示的可选本地化页面标题。</para>
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized page description shown above its sections.</para>
        ///     <para xml:lang="zh-CN">获取在页面各节上方显示的可选本地化页面说明。</para>
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the page header omits both the explicit description and the manifest-description
        ///         fallback.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取页面标题区域是否同时隐藏显式说明及清单说明回退。</para>
        /// </summary>
        public bool HideDescription { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the default sibling sort order; lower values appear first. An override can be registered through
        ///         <see cref="ModSettingsRegistry.RegisterPageSortOrder" /> without rebuilding the page.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取默认的同级页面排序值，数值较小的页面排在前面；可通过
        ///         <see cref="ModSettingsRegistry.RegisterPageSortOrder" /> 注册覆盖值而无需重建页面。
        ///     </para>
        /// </summary>
        public int SortOrder { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of the sections in display order.</para>
        ///     <para xml:lang="zh-CN">获取按显示顺序排列的节不可变快照。</para>
        /// </summary>
        public IReadOnlyList<ModSettingsSection> Sections { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional visibility predicate. It is re-evaluated on settings UI refresh, and a false result
        ///         hides the page from both the sidebar and main content.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的可见性谓词；设置界面刷新时会重新求值，结果为 false 时在侧边栏及主内容中隐藏页面。
        ///     </para>
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional enabled-state predicate. A false result dims the page and disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的启用状态谓词；结果为 false 时页面会变暗且不可交互。</para>
        /// </summary>
        public Func<bool>? EnabledWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the standard actions enabled for the page context menu.</para>
        ///     <para xml:lang="zh-CN">获取页面上下文菜单启用的标准操作。</para>
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the host surfaces on which the page appears in the sidebar and main content.</para>
        ///     <para xml:lang="zh-CN">获取此页面会出现在侧边栏及主内容中的宿主界面。</para>
        /// </summary>
        public ModSettingsHostSurface VisibleOnHostSurfaces { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the host surfaces on which this page's interactive controls are read-only.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取此页面交互控件处于只读状态的宿主界面。</para>
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the sidebar row is visible only while this page or one of its descendants is active.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取侧边栏条目是否仅在此页面或其后代页面处于活动状态时可见。</para>
        /// </summary>
        public bool SidebarVisibleOnlyWhenActive { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the assembly that registered the page, if known.</para>
        ///     <para xml:lang="zh-CN">获取注册此页面的程序集（如已知）。</para>
        /// </summary>
        public Assembly? SourceAssembly { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the sidebar may use <see cref="SourceAssembly" /> to locate ModManager presentation
        ///         metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取侧边栏是否可以通过 <see cref="SourceAssembly" /> 查找 ModManager 展示元数据。
        ///     </para>
        /// </summary>
        public bool UseSourceAssemblyManifestLookup { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents one immutable, optionally collapsible group of ordered settings entries.
    ///     </para>
    ///     <para xml:lang="zh-CN">表示包含有序设置条目的不可变节，并可选择支持折叠。</para>
    /// </summary>
    public sealed class ModSettingsSection
    {
        internal ModSettingsSection(
            string id,
            ModSettingsText? title,
            ModSettingsText? description,
            bool isCollapsible,
            bool startCollapsed,
            IReadOnlyList<ModSettingsEntryDefinition> entries,
            Func<bool>? visibleWhen = null,
            Func<bool>? enabledWhen = null,
            ModSettingsMenuCapabilities menuCapabilities = ModSettingsMenuCapabilities.All,
            ModSettingsHostSurface visibleOnHostSurfaces = ModSettingsHostSurface.All,
            ModSettingsHostSurface readOnlyOnHostSurfaces = ModSettingsHostSurface.None)
        {
            Id = id;
            Title = title;
            Description = description;
            IsCollapsible = isCollapsible;
            StartCollapsed = startCollapsed;
            Entries = Array.AsReadOnly(
                [.. entries ?? throw new ArgumentNullException(nameof(entries))]);
            VisibleWhen = visibleWhen;
            EnabledWhen = enabledWhen;
            MenuCapabilities = menuCapabilities;
            VisibleOnHostSurfaces = visibleOnHostSurfaces;
            ReadOnlyOnHostSurfaces = readOnlyOnHostSurfaces;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable section ID within its page.</para>
        ///     <para xml:lang="zh-CN">获取节在所属页面内的稳定 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional section title; a section without one is rendered as a flat group.</para>
        ///     <para xml:lang="zh-CN">获取可选的节标题；没有标题的节会呈现为平铺分组。</para>
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized description displayed below the title.</para>
        ///     <para xml:lang="zh-CN">获取标题下方显示的可选本地化说明。</para>
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the user can collapse the section.</para>
        ///     <para xml:lang="zh-CN">获取用户是否可以折叠此节。</para>
        /// </summary>
        public bool IsCollapsible { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the section initially starts collapsed when <see cref="IsCollapsible" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取启用 <see cref="IsCollapsible" /> 时此节是否初始折叠。</para>
        /// </summary>
        public bool StartCollapsed { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of the entries in display order.</para>
        ///     <para xml:lang="zh-CN">获取按显示顺序排列的条目不可变快照。</para>
        /// </summary>
        public IReadOnlyList<ModSettingsEntryDefinition> Entries { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional visibility predicate. A false result hides the section and its sidebar shortcut.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的可见性谓词；结果为 false 时隐藏此节及其侧边栏快捷入口。</para>
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional enabled-state predicate. A false result dims the section and disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的启用状态谓词；结果为 false 时节会变暗且不可交互。</para>
        /// </summary>
        public Func<bool>? EnabledWhen { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the standard actions enabled for the section context menu.</para>
        ///     <para xml:lang="zh-CN">获取节上下文菜单启用的标准操作。</para>
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the host surfaces on which the section is visible.</para>
        ///     <para xml:lang="zh-CN">获取此节可见的宿主界面。</para>
        /// </summary>
        public ModSettingsHostSurface VisibleOnHostSurfaces { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the host surfaces on which this section's entries are read-only. This mask is combined with the
        ///         owning page's mask.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取此节条目处于只读状态的宿主界面；该掩码会与所属页面的掩码合并。</para>
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; }
    }
}
