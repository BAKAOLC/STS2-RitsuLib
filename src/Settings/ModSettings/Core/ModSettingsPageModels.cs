using System.Reflection;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Chrome menu actions that can be exposed for pages, sections, and entries.
    ///     可为页面、section 和条目暴露的 chrome 菜单操作。
    /// </summary>
    [Flags]
    public enum ModSettingsMenuCapabilities
    {
        /// <summary>
        ///     No chrome menu actions are exposed.
        ///     不暴露任何 chrome 菜单操作。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Allows copying the current value or subtree.
        ///     允许复制当前值或子树。
        /// </summary>
        Copy = 1 << 0,

        /// <summary>
        ///     Allows pasting compatible clipboard content.
        ///     允许粘贴兼容的剪贴板内容。
        /// </summary>
        Paste = 1 << 1,

        /// <summary>
        ///     Allows restoring the value to its default.
        ///     允许将值恢复为默认值。
        /// </summary>
        ResetToDefault = 1 << 2,

        /// <summary>
        ///     Exposes all standard chrome menu actions.
        ///     暴露所有标准 chrome 菜单操作。
        /// </summary>
        All = Copy | Paste | ResetToDefault,
    }

    /// <summary>
    ///     One logical settings page (sidebar entry + sections).
    ///     一个逻辑设置页（侧边栏条目加 sections）。
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
            Sections = sections;
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
        ///     Owning mod id.
        ///     所属 Mod id。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Stable page id within the mod.
        ///     Mod 内稳定的页面 id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Optional parent for nested navigation; null for a root page.
        ///     嵌套导航的可选父页面；根页面为 null。
        /// </summary>
        public string? ParentPageId { get; }

        /// <summary>
        ///     Page title in the chrome.
        ///     chrome 中显示的页面标题。
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     Optional overview shown above the first section.
        ///     显示在第一个 section 上方的可选概览。
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     When true, the page header suppresses both explicit page text and manifest description fallback.
        ///     为 true 时，页面标题栏会隐藏显式页面描述和 manifest 描述回退。
        /// </summary>
        public bool HideDescription { get; }

        /// <summary>
        ///     Lower values appear earlier among sibling pages (same <see cref="ModId" /> and
        ///     <see cref="ParentPageId" />). Use <see cref="ModSettingsRegistry.RegisterPageSortOrder" /> to adjust without
        ///     rebuilding the page.
        ///     在兄弟页面（相同 <see cref="ModId" /> 和
        ///     <see cref="ParentPageId" />）中，值越小越靠前。使用 <see cref="ModSettingsRegistry.RegisterPageSortOrder" /> 可在不
        ///     重建页面的情况下调整。
        /// </summary>
        public int SortOrder { get; }

        /// <summary>
        ///     Section list in display order.
        ///     按显示顺序排列的 section 列表。
        /// </summary>
        public IReadOnlyList<ModSettingsSection> Sections { get; }

        /// <summary>
        ///     When non-null, sidebar and main page chrome hide this page when the predicate returns false. Refreshed on
        ///     settings UI refresh.
        ///     非 null 时，当谓词返回 false，侧边栏和主页面 chrome 会隐藏此页面；设置 UI 刷新时重新计算。
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     When non-null, all controls on this page are disabled (dimmed, non-interactive) while the predicate returns false.
        ///     非 null 时，当谓词返回 false，此页面上的所有控件都会禁用（变暗且不可交互）。
        /// </summary>
        public Func<bool>? EnabledWhen { get; }

        /// <summary>
        ///     Built-in actions enabled for the page-level actions menu.
        ///     页面级操作菜单启用的内置操作。
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }

        /// <summary>
        ///     Host surfaces where this page appears in the sidebar and content. Defaults to
        ///     <see cref="ModSettingsHostSurface.All" />.
        ///     此页面会出现在侧边栏和内容中的宿主界面。默认值为
        ///     <see cref="ModSettingsHostSurface.All" />。
        /// </summary>
        public ModSettingsHostSurface VisibleOnHostSurfaces { get; }

        /// <summary>
        ///     Host surfaces where interactive controls on this page are forced read-only (dimmed, no writes).
        ///     此页面上的交互控件被强制只读的宿主 surface（变暗且不写入）。
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; }

        /// <summary>
        ///     When true, this page's sidebar row is shown only while the selected page is this page or one of its
        ///     descendants.
        ///     为 true 时，仅当当前选中页面是此页面或它的子页面时，才显示此页面的侧边栏行。
        /// </summary>
        public bool SidebarVisibleOnlyWhenActive { get; }

        /// <summary>
        ///     Assembly that registered this page, when available.
        ///     注册此页面的程序集（如果可用）。
        /// </summary>
        public Assembly? SourceAssembly { get; }

        /// <summary>
        ///     Whether the sidebar may use <see cref="SourceAssembly" /> to resolve presentation metadata from ModManager.
        ///     侧边栏是否可以使用 <see cref="SourceAssembly" /> 从 ModManager 解析展示元数据。
        /// </summary>
        public bool UseSourceAssemblyManifestLookup { get; }
    }

    /// <summary>
    ///     Grouped block of entries (optionally collapsible).
    ///     条目的分组块（可选可折叠）。
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
            Entries = entries;
            VisibleWhen = visibleWhen;
            EnabledWhen = enabledWhen;
            MenuCapabilities = menuCapabilities;
            VisibleOnHostSurfaces = visibleOnHostSurfaces;
            ReadOnlyOnHostSurfaces = readOnlyOnHostSurfaces;
        }

        /// <summary>
        ///     Stable section id within the page.
        ///     页面内稳定的 section id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Section header; null for a flat list without a title bar.
        ///     section 标题；对于不带标题栏的平铺列表为 null。
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     Optional prose under the title.
        ///     标题下方的可选说明文字。
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     When true, the section can be collapsed by the user.
        ///     为 true 时，用户可以折叠此 section。
        /// </summary>
        public bool IsCollapsible { get; }

        /// <summary>
        ///     Initial collapsed state when <see cref="IsCollapsible" /> is true.
        ///     <see cref="IsCollapsible" /> 为 true 时的初始折叠状态。
        /// </summary>
        public bool StartCollapsed { get; }

        /// <summary>
        ///     Entries in display order.
        ///     按显示顺序排列的条目。
        /// </summary>
        public IReadOnlyList<ModSettingsEntryDefinition> Entries { get; }

        /// <summary>
        ///     When non-null, the section (and its sidebar shortcut) is hidden while the predicate is false.
        ///     非 null 时，当谓词为 false，此 section（及其侧边栏快捷入口）会隐藏。
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     When non-null, all controls in this section are disabled (dimmed, non-interactive) while the predicate is false.
        ///     非 null 时，当谓词为 false，此 section 内所有控件都会禁用（变暗且不可交互）。
        /// </summary>
        public Func<bool>? EnabledWhen { get; }

        /// <summary>
        ///     Built-in actions enabled for the section-level actions menu.
        ///     section 级操作菜单启用的内置操作。
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }

        /// <summary>
        ///     Host surfaces where this section is shown. Defaults to <see cref="ModSettingsHostSurface.All" />.
        ///     显示此 section 的宿主界面。默认值为 <see cref="ModSettingsHostSurface.All" />。
        /// </summary>
        public ModSettingsHostSurface VisibleOnHostSurfaces { get; }

        /// <summary>
        ///     Host surfaces where entries in this section are read-only (combined with the owning page mask).
        ///     此 section 内条目只读的宿主 surface（会与所属页面掩码组合）。
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; }
    }
}
