namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Chrome menu actions that can be exposed for pages, sections, and entries.
    /// </summary>
    [Flags]
    public enum ModSettingsMenuCapabilities
    {
        /// <summary>
        ///     No chrome menu actions are exposed.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Allows copying the current value or subtree.
        /// </summary>
        Copy = 1 << 0,

        /// <summary>
        ///     Allows pasting compatible clipboard content.
        /// </summary>
        Paste = 1 << 1,

        /// <summary>
        ///     Allows restoring the value to its default.
        /// </summary>
        ResetToDefault = 1 << 2,

        /// <summary>
        ///     Exposes all standard chrome menu actions.
        /// </summary>
        All = Copy | Paste | ResetToDefault,
    }

    /// <summary>
    ///     One logical settings page (sidebar entry + sections).
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
            ModSettingsMenuCapabilities menuCapabilities = ModSettingsMenuCapabilities.Copy |
                                                           ModSettingsMenuCapabilities.Paste)
        {
            ModId = modId;
            Id = id;
            ParentPageId = parentPageId;
            Title = title;
            Description = description;
            SortOrder = sortOrder;
            Sections = sections;
            VisibleWhen = visibleWhen;
            MenuCapabilities = menuCapabilities;
        }

        /// <summary>
        ///     Owning mod id.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Stable page id within the mod.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Optional parent for nested navigation; null for a root page.
        /// </summary>
        public string? ParentPageId { get; }

        /// <summary>
        ///     Page title in the chrome.
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     Optional overview shown above the first section.
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     Lower values appear earlier among sibling pages (same <see cref="ModId" /> and
        ///     <see cref="ParentPageId" />). Use <see cref="ModSettingsRegistry.RegisterPageSortOrder" /> to adjust without
        ///     rebuilding the page.
        /// </summary>
        public int SortOrder { get; }

        /// <summary>
        ///     Section list in display order.
        /// </summary>
        public IReadOnlyList<ModSettingsSection> Sections { get; }

        /// <summary>
        ///     When non-null, sidebar and main page chrome hide this page when the predicate returns false. Refreshed on
        ///     settings UI refresh.
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     Built-in actions enabled for the page-level actions menu.
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }
    }

    /// <summary>
    ///     Grouped block of entries (optionally collapsible).
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
            ModSettingsMenuCapabilities menuCapabilities = ModSettingsMenuCapabilities.Copy |
                                                           ModSettingsMenuCapabilities.Paste)
        {
            Id = id;
            Title = title;
            Description = description;
            IsCollapsible = isCollapsible;
            StartCollapsed = startCollapsed;
            Entries = entries;
            VisibleWhen = visibleWhen;
            MenuCapabilities = menuCapabilities;
        }

        /// <summary>
        ///     Stable section id within the page.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Section header; null for a flat list without a title bar.
        /// </summary>
        public ModSettingsText? Title { get; }

        /// <summary>
        ///     Optional prose under the title.
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     When true, the section can be collapsed by the user.
        /// </summary>
        public bool IsCollapsible { get; }

        /// <summary>
        ///     Initial collapsed state when <see cref="IsCollapsible" /> is true.
        /// </summary>
        public bool StartCollapsed { get; }

        /// <summary>
        ///     Entries in display order.
        /// </summary>
        public IReadOnlyList<ModSettingsEntryDefinition> Entries { get; }

        /// <summary>
        ///     When non-null, the section (and its sidebar shortcut) is hidden while the predicate is false.
        /// </summary>
        public Func<bool>? VisibleWhen { get; }

        /// <summary>
        ///     Built-in actions enabled for the section-level actions menu.
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; }
    }
}
