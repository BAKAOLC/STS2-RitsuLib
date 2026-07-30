using System.Reflection;
using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds a mod settings page by configuring its metadata, hierarchy, host-surface behavior, and sections.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于配置元数据、页面层级、宿主界面行为及节的模组设置页面构建器。</para>
    /// </summary>
    public sealed class ModSettingsPageBuilder
    {
        private readonly HashSet<string> _sectionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ModSettingsSection> _sections = [];
        private bool _hideDescription;

        private ModSettingsMenuCapabilities _menuCapabilities = ModSettingsMenuCapabilities.All;

        private int? _modSidebarOrder;
        private Func<bool>? _pageEnabledWhen;
        private ModSettingsHostSurface _pageReadOnlyOnHostSurfaces = ModSettingsHostSurface.None;
        private ModSettingsHostSurface _pageVisibleOnHostSurfaces = ModSettingsHostSurface.All;
        private Func<bool>? _pageVisibleWhen;
        private bool _sidebarVisibleOnlyWhenActive;
        private bool _useSourceAssemblyManifest = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a builder for <paramref name="modId" />. A null or whitespace
        ///         <paramref name="pageId" /> defaults to the mod ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modId" /> 初始化构建器；<paramref name="pageId" /> 为 null 或空白时默认使用模组 ID。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder(string modId, string? pageId = null)
            : this(modId, pageId, null)
        {
        }

        internal ModSettingsPageBuilder(string modId, string? pageId, Assembly? sourceAssembly)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ModId = modId;
            PageId = string.IsNullOrWhiteSpace(pageId) ? modId : pageId;
            SourceAssembly = sourceAssembly;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the owning mod ID.</para>
        ///     <para xml:lang="zh-CN">获取所属模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable page ID used for navigation and page-level clipboard snapshots.</para>
        ///     <para xml:lang="zh-CN">获取用于导航及页面级剪贴板快照的稳定页面 ID。</para>
        /// </summary>
        public string PageId { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the assembly that registered the page, if known. It can be used to locate the owning ModManager
        ///         manifest for sidebar presentation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取注册此页面的程序集（如已知）；该程序集可用于查找所属 ModManager 清单并生成侧边栏展示信息。
        ///     </para>
        /// </summary>
        public Assembly? SourceAssembly { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional ID of the page under which this page is nested.</para>
        ///     <para xml:lang="zh-CN">获取此页面所隶属父页面的可选 ID。</para>
        /// </summary>
        public string? ParentPageId { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized page title displayed in navigation and headers.</para>
        ///     <para xml:lang="zh-CN">获取在导航及标题区域显示的可选本地化页面标题。</para>
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized page description.</para>
        ///     <para xml:lang="zh-CN">获取可选的本地化页面说明。</para>
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional mod display name used by the settings sidebar.</para>
        ///     <para xml:lang="zh-CN">获取设置侧边栏使用的可选模组显示名称。</para>
        /// </summary>
        public ModSettingsText? ModDisplayName { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the sort order among sibling pages; lower values appear first.</para>
        ///     <para xml:lang="zh-CN">获取同级页面之间的排序值；数值较小的页面排在前面。</para>
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Nests the page under <paramref name="parentPageId" /> in the settings hierarchy.</para>
        ///     <para xml:lang="zh-CN">在设置页面层级中将此页面置于 <paramref name="parentPageId" /> 之下。</para>
        /// </summary>
        public ModSettingsPageBuilder AsChildOf(string parentPageId)
        {
            ParentPageId = parentPageId;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the localized page title.</para>
        ///     <para xml:lang="zh-CN">设置本地化页面标题。</para>
        /// </summary>
        public ModSettingsPageBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the localized page description.</para>
        ///     <para xml:lang="zh-CN">设置本地化页面说明。</para>
        /// </summary>
        public ModSettingsPageBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Controls whether the page header omits its description and the manifest-description fallback.
        ///     </para>
        ///     <para xml:lang="zh-CN">控制页面标题区域是否隐藏说明并禁用清单说明回退。</para>
        /// </summary>
        public ModSettingsPageBuilder WithDescriptionHidden(bool hidden = true)
        {
            _hideDescription = hidden;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the mod display name that <see cref="Build" /> registers with
        ///         <see cref="ModSettingsRegistry" /> for the sidebar.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置模组显示名称；<see cref="Build" /> 会将其注册到 <see cref="ModSettingsRegistry" /> 供侧边栏使用。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder WithModDisplayName(ModSettingsText displayName)
        {
            ModDisplayName = displayName;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets <see cref="SortOrder" />.</para>
        ///     <para xml:lang="zh-CN">设置 <see cref="SortOrder" />。</para>
        /// </summary>
        public ModSettingsPageBuilder WithSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the mod-group sidebar order that <see cref="Build" /> registers for <see cref="ModId" />.
        ///         Repeated registrations for the same mod must use the same value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置 <see cref="Build" /> 为 <see cref="ModId" /> 注册的模组分组侧边栏排序值；同一模组的重复注册必须使用相同值。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder WithModSidebarOrder(int order)
        {
            _modSidebarOrder = order;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a predicate that is re-evaluated on settings UI refresh. A false result hides the page from
        ///         both the sidebar and main content.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置在设置界面刷新时重新求值的谓词；结果为 false 时，在侧边栏及主内容中隐藏此页面。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _pageVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a predicate whose false result dims the page and disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置启用状态谓词；结果为 false 时页面会变暗且不可交互。</para>
        /// </summary>
        public ModSettingsPageBuilder WithEnabledWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _pageEnabledWhen = predicate;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the host surfaces on which the page is visible. Pages are visible on all surfaces by default.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置此页面可见的宿主界面；默认在所有宿主界面中可见。</para>
        /// </summary>
        public ModSettingsPageBuilder WithVisibleOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _pageVisibleOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Makes the sidebar item visible only while this page or one of its descendants is active.
        ///     </para>
        ///     <para xml:lang="zh-CN">使侧边栏条目仅在此页面或其后代页面处于活动状态时可见。</para>
        /// </summary>
        public ModSettingsPageBuilder WithSidebarVisibleOnlyWhenActive()
        {
            _sidebarVisibleOnlyWhenActive = true;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Controls whether the sidebar may use <see cref="SourceAssembly" /> to locate the ModManager manifest
        ///         for this page's mod group. Lookup is enabled by default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         控制侧边栏是否可以通过 <see cref="SourceAssembly" /> 查找此页面所属模组分组的 ModManager 清单；默认启用。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder WithSourceAssemblyManifestLookup(bool enabled = true)
        {
            _useSourceAssemblyManifest = enabled;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Disables sidebar manifest lookup through this page's source assembly.</para>
        ///     <para xml:lang="zh-CN">禁用通过此页面来源程序集进行的侧边栏清单查找。</para>
        /// </summary>
        public ModSettingsPageBuilder WithoutSourceAssemblyManifestLookup()
        {
            return WithSourceAssemblyManifestLookup(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the host surfaces on which the page's value controls are read-only. This mask is combined with
        ///         each section's read-only mask.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置此页面数值控件处于只读状态的宿主界面；该掩码会与各节的只读掩码合并。</para>
        /// </summary>
        public ModSettingsPageBuilder WithReadOnlyOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _pageReadOnlyOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the context-menu actions exposed for the page itself.</para>
        ///     <para xml:lang="zh-CN">设置页面自身上下文菜单公开的操作。</para>
        /// </summary>
        public ModSettingsPageBuilder WithMenuCapabilities(ModSettingsMenuCapabilities capabilities)
        {
            _menuCapabilities = capabilities;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures and adds a section. <paramref name="id" /> must be unique within this page,
        ///         case-insensitively.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置并添加节；<paramref name="id" /> 在此页面内必须不区分大小写地保持唯一。
        ///     </para>
        /// </summary>
        public ModSettingsPageBuilder AddSection(string id, Action<ModSettingsSectionBuilder> configure)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(configure);

            if (!_sectionIds.Add(id))
                throw new InvalidOperationException($"Duplicate settings section id '{id}' for mod '{ModId}'.");

            var builder = new ModSettingsSectionBuilder(id);
            configure(builder);
            _sections.Add(builder.Build());
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Materializes the page and performs pending mod-display and sidebar-order registrations. At least one
        ///         section must have been added.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         生成页面并执行待处理的模组显示名称及侧边栏排序注册；必须已添加至少一个节。
        ///     </para>
        /// </summary>
        public ModSettingsPage Build()
        {
            if (_sections.Count == 0)
                throw new InvalidOperationException($"Settings page '{PageId}' for mod '{ModId}' has no sections.");

            if (ModDisplayName != null)
                ModSettingsRegistry.RegisterModDisplayName(ModId, ModDisplayName);

            if (_modSidebarOrder is { } modOrder)
                ModSettingsRegistry.RegisterModSidebarOrder(ModId, modOrder);

            return new(
                ModId,
                PageId,
                ParentPageId,
                Title,
                Description,
                SortOrder,
                [.. _sections],
                _pageVisibleWhen,
                _pageEnabledWhen,
                _menuCapabilities,
                _pageVisibleOnHostSurfaces,
                _pageReadOnlyOnHostSurfaces,
                _sidebarVisibleOnlyWhenActive,
                _hideDescription,
                SourceAssembly,
                _useSourceAssemblyManifest
            );
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds a settings section by configuring its display, availability, host-surface behavior, and typed
    ///         entries.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于配置显示内容、可用状态、宿主界面行为及类型化条目的设置节构建器。</para>
    /// </summary>
    public sealed class ModSettingsSectionBuilder
    {
        private readonly List<ModSettingsEntryDefinition> _entries = [];
        private readonly Dictionary<string, Func<bool>> _entryEnabledWhen = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<bool>> _entryVisibleWhen = new(StringComparer.OrdinalIgnoreCase);

        private ModSettingsMenuCapabilities _menuCapabilities = ModSettingsMenuCapabilities.All;

        private Func<bool>? _sectionEnabledWhen;

        private ModSettingsHostSurface _sectionReadOnlyOnHostSurfaces = ModSettingsHostSurface.None;
        private ModSettingsHostSurface _sectionVisibleOnHostSurfaces = ModSettingsHostSurface.All;

        private Func<bool>? _sectionVisibleWhen;

        internal ModSettingsSectionBuilder(string id)
        {
            Id = id;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the stable section ID within its page.</para>
        ///     <para xml:lang="zh-CN">获取节在所属页面内的稳定 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized section title.</para>
        ///     <para xml:lang="zh-CN">获取可选的本地化节标题。</para>
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional localized description displayed below the title.</para>
        ///     <para xml:lang="zh-CN">获取标题下方显示的可选本地化说明。</para>
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the section can be collapsed.</para>
        ///     <para xml:lang="zh-CN">获取此节是否可以折叠。</para>
        /// </summary>
        public bool IsCollapsible { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the section initially starts collapsed when <see cref="IsCollapsible" /> is enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取启用 <see cref="IsCollapsible" /> 时此节是否初始折叠。</para>
        /// </summary>
        public bool StartCollapsed { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Sets the localized section title.</para>
        ///     <para xml:lang="zh-CN">设置本地化节标题。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the localized section description.</para>
        ///     <para xml:lang="zh-CN">设置本地化节说明。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Makes the section collapsible and optionally starts it collapsed.</para>
        ///     <para xml:lang="zh-CN">使此节可折叠，并可选择让其初始处于折叠状态。</para>
        /// </summary>
        public ModSettingsSectionBuilder Collapsible(bool startCollapsed = false)
        {
            IsCollapsible = true;
            StartCollapsed = startCollapsed;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a predicate whose false result hides the section and its sidebar shortcut.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置可见性谓词；结果为 false 时隐藏此节及其侧边栏快捷入口。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _sectionVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a predicate whose false result dims the section and disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置启用状态谓词；结果为 false 时节会变暗且不可交互。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithEnabledWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _sectionEnabledWhen = predicate;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the host surfaces on which the section is visible. Sections are visible on all surfaces by
        ///         default.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置此节可见的宿主界面；默认在所有宿主界面中可见。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithVisibleOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _sectionVisibleOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the host surfaces on which this section's value controls are read-only. This mask is combined
        ///         with the owning page's mask.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置此节数值控件处于只读状态的宿主界面；该掩码会与所属页面的掩码合并。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithReadOnlyOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _sectionReadOnlyOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the context-menu actions exposed for the section itself.</para>
        ///     <para xml:lang="zh-CN">设置节自身上下文菜单公开的操作。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithMenuCapabilities(ModSettingsMenuCapabilities capabilities)
        {
            _menuCapabilities = capabilities;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a non-interactive heading row.</para>
        ///     <para xml:lang="zh-CN">添加不带交互控件的标题行。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddHeader(
            string id,
            ModSettingsText label,
            ModSettingsText? description = null)
        {
            AddEntry(id, new HeaderModSettingsEntryDefinition(id, label, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a read-only rich-text paragraph with an optional maximum body height for scrolling.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加只读富文本段落，并可指定启用滚动的正文最大高度。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddParagraph(
            string id,
            ModSettingsText text,
            ModSettingsText? description = null,
            float? maxBodyHeight = null)
        {
            AddEntry(id, new ParagraphModSettingsEntryDefinition(id, text, description, maxBodyHeight));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a read-only information card with a title, optional subtitle, and rich-text body.</para>
        ///     <para xml:lang="zh-CN">添加包含标题、可选副标题及富文本正文的只读信息卡。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddInfoCard(
            string id,
            ModSettingsText label,
            ModSettingsText body,
            ModSettingsText? description = null)
        {
            AddEntry(id, new InfoCardModSettingsEntryDefinition(id, label, body, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a read-only runtime hotkey summary with descriptive text on the left and binding labels on the
        ///         right.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加只读的运行时热键摘要，左侧显示说明文本，右侧显示绑定标签。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddRuntimeHotkeySummary(
            string id,
            ModSettingsText label,
            ModSettingsText body,
            IReadOnlyList<ModSettingsText> bindings,
            ModSettingsText? idSuffix = null)
        {
            AddEntry(id, new RuntimeHotkeySummaryModSettingsEntryDefinition(id, label, body, bindings, idSuffix));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds an image preview resolved from <paramref name="textureProvider" /> when created.</para>
        ///     <para xml:lang="zh-CN">添加创建时通过 <paramref name="textureProvider" /> 解析的图像预览。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddImage(
            string id,
            ModSettingsText label,
            Func<Texture2D?> textureProvider,
            float previewHeight = 160f,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(textureProvider);
            if (!float.IsFinite(previewHeight) || previewHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(previewHeight),
                    "Image previewHeight must be finite and > 0.");

            AddEntry(id, new ImageModSettingsEntryDefinition(id, label, textureProvider, previewHeight, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds an editable list backed by <paramref name="binding" />, using
        ///         <paramref name="itemEditorFactory" /> for each item when provided.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加由 <paramref name="binding" /> 支持的可编辑列表；提供 <paramref name="itemEditorFactory" />
        ///         时使用该工厂创建各列表项编辑器。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddList<TItem>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<TItem>> binding,
            Func<TItem> createItem,
            Func<TItem, ModSettingsText> itemLabel,
            Func<TItem, ModSettingsText?>? itemDescription = null,
            Func<ModSettingsListItemContext<TItem>, Control>? itemEditorFactory = null,
            IStructuredModSettingsValueAdapter<TItem>? itemDataAdapter = null,
            ModSettingsText? addButtonText = null,
            ModSettingsText? description = null)
        {
            return AddList(
                id,
                label,
                binding,
                createItem,
                itemLabel,
                itemDescription,
                itemEditorFactory,
                itemDataAdapter,
                addButtonText,
                description,
                false,
                false,
                null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds an editable list backed by <paramref name="binding" />, with optional collapsible item cards and
        ///         compact controls in each item header.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加由 <paramref name="binding" /> 支持的可编辑列表，并可启用可折叠列表项卡片及标题栏紧凑控件。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddList<TItem>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<TItem>> binding,
            Func<TItem> createItem,
            Func<TItem, ModSettingsText> itemLabel,
            Func<TItem, ModSettingsText?>? itemDescription,
            Func<ModSettingsListItemContext<TItem>, Control>? itemEditorFactory,
            IStructuredModSettingsValueAdapter<TItem>? itemDataAdapter,
            ModSettingsText? addButtonText,
            ModSettingsText? description,
            bool collapsibleItems,
            bool startItemsCollapsed,
            Func<ModSettingsListItemContext<TItem>, Control?>? itemHeaderAccessoryFactory)
        {
            ArgumentNullException.ThrowIfNull(createItem);
            ArgumentNullException.ThrowIfNull(itemLabel);
            AddEntry(id, new ListModSettingsEntryDefinition<TItem>(
                id,
                label,
                binding,
                createItem,
                itemLabel,
                itemDescription,
                itemEditorFactory,
                itemDataAdapter,
                addButtonText ?? ModSettingsLocalization.Text("button.add", "Add"),
                description,
                collapsibleItems,
                startItemsCollapsed,
                itemHeaderAccessoryFactory));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a boolean toggle.</para>
        ///     <para xml:lang="zh-CN">添加布尔开关。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddToggle(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<bool> binding,
            ModSettingsText? description = null,
            Func<bool>? visibleWhen = null)
        {
            AddEntry(id, new ToggleModSettingsEntryDefinition(id, label, binding, description, visibleWhen));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds an integer slider over an inclusive range with a positive step.</para>
        ///     <para xml:lang="zh-CN">添加在闭区间内按正步长调整数值的整数滑块。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddIntSlider(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<int> binding,
            int minValue,
            int maxValue,
            int step = 1,
            Func<int, string>? valueFormatter = null,
            ModSettingsText? description = null)
        {
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

            AddEntry(id, new IntSliderModSettingsEntryDefinition(
                id,
                label,
                binding,
                minValue,
                maxValue,
                step,
                valueFormatter,
                description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a <see cref="double" /> slider over a finite inclusive range with a finite positive step.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加在有限闭区间内按有限正步长调整数值的 <see cref="double" /> 滑块。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddSlider(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<double> binding,
            double minValue,
            double maxValue,
            double step = 1d,
            Func<double, string>? valueFormatter = null,
            ModSettingsText? description = null)
        {
            if (!double.IsFinite(minValue))
                throw new ArgumentOutOfRangeException(nameof(minValue), "Slider minValue must be finite.");

            if (!double.IsFinite(maxValue))
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be finite.");

            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (!double.IsFinite(step) || step <= 0d)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be finite and > 0.");

            AddEntry(id, new SliderModSettingsEntryDefinition(
                id,
                label,
                binding,
                minValue,
                maxValue,
                step,
                valueFormatter,
                description));
            return this;
        }

        internal ModSettingsSectionBuilder AddFloatSlider(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<float> binding,
            float minValue,
            float maxValue,
            float step = 1f,
            Func<float, string>? valueFormatter = null,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(binding);
            if (!float.IsFinite(minValue))
                throw new ArgumentOutOfRangeException(nameof(minValue), "Slider minValue must be finite.");

            if (!float.IsFinite(maxValue))
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be finite.");

            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (!float.IsFinite(step) || step <= 0f)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be finite and > 0.");

            AddEntry(id, new FloatSliderModSettingsEntryDefinition(
                id,
                label,
                binding,
                minValue,
                maxValue,
                step,
                valueFormatter,
                description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a non-empty fixed option set using the specified <paramref name="presentation" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用指定的 <paramref name="presentation" /> 添加非空的固定选项集。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddChoice<TValue>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<TValue> binding,
            IEnumerable<ModSettingsChoiceOption<TValue>> options,
            ModSettingsText? description = null,
            ModSettingsChoicePresentation presentation = ModSettingsChoicePresentation.Stepper)
        {
            ArgumentNullException.ThrowIfNull(options);
            var materializedOptions = options.ToArray();
            if (materializedOptions.Length == 0)
                throw new InvalidOperationException($"Choice setting '{id}' requires at least one option.");

            AddEntry(id, new ChoiceModSettingsEntryDefinition<TValue>(
                id,
                label,
                binding,
                materializedOptions,
                presentation,
                description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds options that are re-evaluated on settings UI refresh and immediately before a drop-down list
        ///         opens. An empty result temporarily disables the control without changing the bound value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加在设置界面刷新及下拉列表展开前重新计算的选项；空结果会暂时禁用控件，但不会更改绑定值。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The stable entry ID within the section.</para>
        ///     <para xml:lang="zh-CN">条目在节内的稳定 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The visible row label.</para>
        ///     <para xml:lang="zh-CN">可见的行标签。</para>
        /// </param>
        /// <param name="binding">
        ///     <para xml:lang="en">The binding that stores the selected value.</para>
        ///     <para xml:lang="zh-CN">存储所选值的绑定。</para>
        /// </param>
        /// <param name="optionsProvider">
        ///     <para xml:lang="en">
        ///         The provider invoked when the entry is added, when the control is created, on UI refresh, and before
        ///         a drop-down list opens.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加条目、创建控件、界面刷新以及下拉列表展开前调用的选项提供器。
        ///     </para>
        /// </param>
        /// <param name="description">
        ///     <para xml:lang="en">The optional secondary description.</para>
        ///     <para xml:lang="zh-CN">可选的次级说明。</para>
        /// </param>
        /// <param name="presentation">
        ///     <para xml:lang="en">The visual presentation for the choices.</para>
        ///     <para xml:lang="zh-CN">选项使用的视觉呈现方式。</para>
        /// </param>
        public ModSettingsSectionBuilder AddDynamicChoice<TValue>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<TValue> binding,
            Func<IReadOnlyList<ModSettingsChoiceOption<TValue>>> optionsProvider,
            ModSettingsText? description = null,
            ModSettingsChoicePresentation presentation = ModSettingsChoicePresentation.Stepper)
        {
            ArgumentNullException.ThrowIfNull(optionsProvider);
            var options = optionsProvider()
                          ?? throw new InvalidOperationException(
                              $"Dynamic choice setting '{id}' returned a null option list.");

            var entry = new ChoiceModSettingsEntryDefinition<TValue>(
                id,
                label,
                binding,
                options,
                presentation,
                description)
            {
                OptionsProvider = optionsProvider,
            };
            AddEntry(id, entry);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a fixed choice control containing every value of <typeparamref name="TEnum" />, with optional
        ///         custom labels.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加包含 <typeparamref name="TEnum" /> 所有枚举值的固定选项控件，并可指定自定义标签。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddEnumChoice<TEnum>(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<TEnum> binding,
            Func<TEnum, ModSettingsText>? optionLabelFactory = null,
            ModSettingsText? description = null,
            ModSettingsChoicePresentation presentation = ModSettingsChoicePresentation.Stepper)
            where TEnum : struct, Enum
        {
            optionLabelFactory ??= value => ModSettingsText.Literal(value.ToString());

            return AddChoice(
                id,
                label,
                binding,
                Enum.GetValues<TEnum>()
                    .Select(value => new ModSettingsChoiceOption<TEnum>(value, optionLabelFactory(value))),
                description,
                presentation);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds the binary-compatible serialized-string color picker with alpha editing enabled and intensity
        ///         editing disabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加保持二进制兼容的序列化字符串颜色选择器；启用透明度编辑并禁用强度编辑。
        ///     </para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The stable entry ID within the section.</para>
        ///     <para xml:lang="zh-CN">条目在节内的稳定 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The visible row label.</para>
        ///     <para xml:lang="zh-CN">可见的行标签。</para>
        /// </param>
        /// <param name="binding">
        ///     <para xml:lang="en">
        ///         The binding that stores the serialized color; hexadecimal strings are preferred.
        ///     </para>
        ///     <para xml:lang="zh-CN">存储序列化颜色的绑定；建议使用十六进制字符串。</para>
        /// </param>
        /// <param name="description">
        ///     <para xml:lang="en">The optional secondary description.</para>
        ///     <para xml:lang="zh-CN">可选的次级说明。</para>
        /// </param>
        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description = null)
        {
            return AddColor(id, label, binding, description, true, false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a serialized-string color picker with explicit alpha and intensity editing options.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加由序列化字符串支持的颜色选择器，并明确指定透明度及强度编辑选项。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The stable entry ID within the section.</para>
        ///     <para xml:lang="zh-CN">条目在节内的稳定 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The visible row label.</para>
        ///     <para xml:lang="zh-CN">可见的行标签。</para>
        /// </param>
        /// <param name="binding">
        ///     <para xml:lang="en">
        ///         The binding that stores the serialized color; hexadecimal strings are preferred.
        ///     </para>
        ///     <para xml:lang="zh-CN">存储序列化颜色的绑定；建议使用十六进制字符串。</para>
        /// </param>
        /// <param name="description">
        ///     <para xml:lang="en">The optional secondary description.</para>
        ///     <para xml:lang="zh-CN">可选的次级说明。</para>
        /// </param>
        /// <param name="editAlpha">
        ///     <para xml:lang="en">Whether the picker allows editing the alpha channel.</para>
        ///     <para xml:lang="zh-CN">颜色选择器是否允许编辑透明度通道。</para>
        /// </param>
        /// <param name="editIntensity">
        ///     <para xml:lang="en">
        ///         Whether the picker enables HDR intensity editing through Godot
        ///         <c>ColorPicker.EditIntensity</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         颜色选择器是否通过 Godot <c>ColorPicker.EditIntensity</c> 启用 HDR 强度编辑。
        ///     </para>
        /// </param>
        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description,
            bool editAlpha,
            bool editIntensity)
        {
            AddEntry(id,
                new ColorModSettingsEntryDefinition(id, label, binding, description, editAlpha, editIntensity));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a single-line text field.</para>
        ///     <para xml:lang="zh-CN">添加单行文本字段。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddString(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? placeholder = null,
            int? maxLength = null,
            ModSettingsText? description = null)
        {
            return AddString(id, label, binding, placeholder, maxLength, description, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a single-line text field with optional invalid-state styling. A false validation result does
        ///         not block committing the value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加可提供无效状态样式的单行文本字段；校验结果为 false 不会阻止提交该值。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddString(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? placeholder,
            int? maxLength,
            ModSettingsText? description,
            Func<string, bool>? valueValidationVisual)
        {
            return AddString(id, label, binding, placeholder, maxLength, description, valueValidationVisual, null);
        }

        internal ModSettingsSectionBuilder AddString(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? placeholder,
            int? maxLength,
            ModSettingsText? description,
            Func<string, bool>? valueValidationVisual,
            Func<string, bool>? valueValidationCommit)
        {
            if (maxLength is < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be null or >= 1.");

            AddEntry(id,
                new StringModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description)
                {
                    ValueValidationVisual = valueValidationVisual,
                    ValueValidationCommit = valueValidationCommit,
                });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a multiline text field.</para>
        ///     <para xml:lang="zh-CN">添加多行文本字段。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddMultilineString(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? placeholder = null,
            int? maxLength = null,
            ModSettingsText? description = null)
        {
            if (maxLength is < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be null or >= 1.");

            AddEntry(id,
                new MultilineStringModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a keyboard binding capture row.</para>
        ///     <para xml:lang="zh-CN">添加键盘绑定捕获行。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddKeyBinding(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            bool allowModifierCombos = true,
            bool allowModifierOnly = true,
            bool distinguishModifierSides = false,
            ModSettingsText? description = null)
        {
            var entry = new KeyBindingModSettingsEntryDefinition(id, label, binding, allowModifierCombos,
                allowModifierOnly, distinguishModifierSides, description)
            {
                MenuCapabilities = ModSettingsMenuCapabilities.Copy | ModSettingsMenuCapabilities.ResetToDefault,
            };
            AddEntry(id, entry);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds an input binding capture row that can record keyboard shortcuts and, when enabled, Godot or
        ///         Slay the Spire 2 input actions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加输入绑定捕获行，可记录键盘快捷键，并可选择允许记录 Godot 或《杀戮尖塔 2》输入动作。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddInputBinding(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            bool allowModifierCombos = true,
            bool allowModifierOnly = true,
            bool distinguishModifierSides = false,
            bool allowActionBindings = true,
            ModSettingsText? description = null)
        {
            var entry = new InputBindingModSettingsEntryDefinition(id, label, binding, allowModifierCombos,
                allowModifierOnly, distinguishModifierSides, allowActionBindings, description)
            {
                MenuCapabilities = ModSettingsMenuCapabilities.Copy | ModSettingsMenuCapabilities.ResetToDefault,
            };
            AddEntry(id, entry);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a keyboard binding capture row that stores multiple bindings. Callers must explicitly opt in
        ///         by passing <paramref name="allowMultipleBindings" /> as true.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加可存储多个绑定的键盘绑定捕获行；调用方必须将 <paramref name="allowMultipleBindings" />
        ///         显式设为 true。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddKeyBinding(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<string>> binding,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
            bool allowMultipleBindings,
            bool allowModifierCombos = true,
            bool allowModifierOnly = true,
            bool distinguishModifierSides = false,
            ModSettingsText? description = null)
        {
            if (!allowMultipleBindings)
                throw new InvalidOperationException(
                    "List<string> key binding rows require allowMultipleBindings=true to opt into native multi-binding support.");

            var entry = new MultiKeyBindingModSettingsEntryDefinition(id, label, binding, allowModifierCombos,
                allowModifierOnly, distinguishModifierSides, description)
            {
                MenuCapabilities = ModSettingsMenuCapabilities.Copy | ModSettingsMenuCapabilities.ResetToDefault,
            };
            AddEntry(id, entry);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a button that invokes <paramref name="action" /> without storing a setting value.
        ///     </para>
        ///     <para xml:lang="zh-CN">添加调用 <paramref name="action" /> 且不存储设置值的按钮。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddButton(
            string id,
            ModSettingsText label,
            ModSettingsText buttonText,
            Action action,
            ModSettingsButtonTone tone = ModSettingsButtonTone.Normal,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(action);
            AddEntry(id, new ButtonModSettingsEntryDefinition(id, label, buttonText, action, tone, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a button whose <paramref name="action" /> receives the settings UI host, allowing it to request
        ///         a refresh after deferred work.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         添加将设置界面宿主传给 <paramref name="action" /> 的按钮，以便回调在延迟工作完成后请求刷新。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder AddButton(
            string id,
            ModSettingsText label,
            ModSettingsText buttonText,
            Action<IModSettingsUiActionHost> action,
            ModSettingsButtonTone tone = ModSettingsButtonTone.Normal,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(action);
            AddEntry(id,
                new HostContextButtonModSettingsEntryDefinition(id, label, buttonText, action, tone, description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a row that navigates to the registered page <paramref name="targetPageId" />.</para>
        ///     <para xml:lang="zh-CN">添加导航到已注册页面 <paramref name="targetPageId" /> 的行。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddSubpage(
            string id,
            ModSettingsText label,
            string targetPageId,
            ModSettingsText? buttonText = null,
            ModSettingsText? description = null)
        {
            AddEntry(id,
                new SubpageModSettingsEntryDefinition(
                    id,
                    label,
                    targetPageId,
                    buttonText ?? ModSettingsText.Literal(">"),
                    description));
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Adds a custom row created by <paramref name="controlFactory" />.</para>
        ///     <para xml:lang="zh-CN">添加由 <paramref name="controlFactory" /> 创建的自定义行。</para>
        /// </summary>
        public ModSettingsSectionBuilder AddCustom(
            string id,
            ModSettingsText label,
            Func<IModSettingsUiActionHost, Control> controlFactory,
            ModSettingsText? description = null,
            Func<bool>? visibleWhen = null)
        {
            ArgumentNullException.ThrowIfNull(controlFactory);
            AddEntry(id, new CustomModSettingsEntryDefinition(id, label, controlFactory, description, visibleWhen));
            return this;
        }

        internal ModSettingsSection Build()
        {
            return _entries.Count == 0
                ? throw new InvalidOperationException($"Settings section '{Id}' has no entries.")
                : new(Id, Title, Description, IsCollapsible, StartCollapsed, BuildEntries(), _sectionVisibleWhen,
                    _sectionEnabledWhen,
                    _menuCapabilities, _sectionVisibleOnHostSurfaces, _sectionReadOnlyOnHostSurfaces);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the context-menu actions exposed for one previously added entry.</para>
        ///     <para xml:lang="zh-CN">设置先前已添加条目所公开的上下文菜单操作。</para>
        /// </summary>
        public ModSettingsSectionBuilder ConfigureEntryMenu(string id, ModSettingsMenuCapabilities capabilities)
        {
            var entry = _entries.FirstOrDefault(existing =>
                            string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException(
                            $"Settings entry '{id}' does not exist in section '{Id}'.");
            entry.MenuCapabilities = capabilities;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the host surfaces on which one previously added entry's interactive controls are read-only.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置先前已添加条目的交互控件处于只读状态的宿主界面。</para>
        /// </summary>
        public ModSettingsSectionBuilder WithEntryReadOnlyOnHostSurfaces(string id,
            ModSettingsHostSurface surfaces)
        {
            var entry = _entries.FirstOrDefault(existing =>
                            string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException(
                            $"Settings entry '{id}' does not exist in section '{Id}'.");
            entry.ReadOnlyOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets a visibility predicate for one previously added entry. It applies to every entry type and is
        ///         re-evaluated on settings UI refresh.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为先前已添加的条目设置可见性谓词；该谓词适用于所有条目类型，并会在设置界面刷新时重新求值。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder WithEntryVisibleWhen(string id, Func<bool> predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(predicate);

            if (!_entryIds.Contains(id))
                throw new InvalidOperationException($"Settings entry '{id}' does not exist in section '{Id}'.");

            _entryVisibleWhen[id] = predicate;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets an enabled-state predicate for one previously added entry. A false result dims the row and
        ///         disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为先前已添加的条目设置启用状态谓词；结果为 false 时该行会变暗且不可交互。
        ///     </para>
        /// </summary>
        public ModSettingsSectionBuilder WithEntryEnabledWhen(string id, Func<bool> predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(predicate);

            if (!_entryIds.Contains(id))
                throw new InvalidOperationException($"Settings entry '{id}' does not exist in section '{Id}'.");

            _entryEnabledWhen[id] = predicate;
            return this;
        }

        private void AddEntry(string id, ModSettingsEntryDefinition entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            if (!_entryIds.Add(id))
                throw new InvalidOperationException($"Duplicate settings entry id '{id}' in section '{Id}'.");

            _entries.Add(entry);
        }

        private ModSettingsEntryDefinition[] BuildEntries()
        {
            var result = new ModSettingsEntryDefinition[_entries.Count];
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (_entryEnabledWhen.TryGetValue(entry.Id, out var enabledPredicate))
                    entry = new ModSettingsEntryEnabledWrapper(entry, enabledPredicate)
                    {
                        MenuCapabilities = entry.MenuCapabilities,
                        ReadOnlyOnHostSurfaces = entry.ReadOnlyOnHostSurfaces,
                    };
                if (_entryVisibleWhen.TryGetValue(entry.Id, out var visibilityPredicate))
                {
                    var wrapped = new ModSettingsEntryVisibilityWrapper(entry, visibilityPredicate)
                    {
                        MenuCapabilities = entry.MenuCapabilities,
                        ReadOnlyOnHostSurfaces = entry.ReadOnlyOnHostSurfaces,
                    };
                    result[i] = wrapped;
                    continue;
                }

                result[i] = entry;
            }

            return result;
        }
    }
}
