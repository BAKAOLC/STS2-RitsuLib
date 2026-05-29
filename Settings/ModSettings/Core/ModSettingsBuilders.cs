using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Fluent builder for a registered mod settings page: metadata, optional parent page, and sections.
    ///     已注册 Mod 设置页的流式构建器：用于配置元数据、可选父页面和 sections。
    /// </summary>
    public sealed class ModSettingsPageBuilder
    {
        private readonly HashSet<string> _sectionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ModSettingsSection> _sections = [];

        private ModSettingsMenuCapabilities _menuCapabilities = ModSettingsMenuCapabilities.Copy |
                                                                ModSettingsMenuCapabilities.Paste;

        private int? _modSidebarOrder;
        private Func<bool>? _pageEnabledWhen;
        private ModSettingsHostSurface _pageReadOnlyOnHostSurfaces = ModSettingsHostSurface.None;
        private ModSettingsHostSurface _pageVisibleOnHostSurfaces = ModSettingsHostSurface.All;
        private Func<bool>? _pageVisibleWhen;
        private bool _sidebarVisibleOnlyWhenActive;

        /// <summary>
        ///     Initializes a builder for mod <paramref name="modId" />; <paramref name="pageId" /> defaults to the mod id when
        ///     null or whitespace.
        ///     为 mod <paramref name="modId" /> 初始化构建器；当 <paramref name="pageId" /> 为 null 或空白时，
        ///     默认使用 mod id。
        /// </summary>
        public ModSettingsPageBuilder(string modId, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ModId = modId;
            PageId = string.IsNullOrWhiteSpace(pageId) ? modId : pageId;
        }

        /// <summary>
        ///     Owning mod identifier.
        ///     所属 Mod 标识符。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Stable page id (used for navigation and chrome clipboard).
        ///     稳定页面 id（用于导航和 chrome 剪贴板）。
        /// </summary>
        public string PageId { get; }

        /// <summary>
        ///     When set, this page appears as a child of the given parent page id.
        ///     设置后，此页面会作为给定父页面 id 的子页面显示。
        /// </summary>
        public string? ParentPageId { get; private set; }

        /// <summary>
        ///     Localized title shown in tabs and headers.
        ///     显示在标签页和标题区域的本地化标题。
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     Optional subtitle or long description for the page.
        ///     页面可选副标题或长描述。
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     Display name for the mod in the settings sidebar (separate from page titles).
        ///     设置侧边栏中的 Mod 显示名称（独立于页面标题）。
        /// </summary>
        public ModSettingsText? ModDisplayName { get; private set; }

        /// <summary>
        ///     Ordering among sibling pages (lower first).
        ///     同级页面之间的排序（数值越小越靠前）。
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        ///     Nests this page under <paramref name="parentPageId" /> in the UI hierarchy.
        ///     在 UI 层级中将此页面嵌套到 <paramref name="parentPageId" /> 下。
        /// </summary>
        public ModSettingsPageBuilder AsChildOf(string parentPageId)
        {
            ParentPageId = parentPageId;
            return this;
        }

        /// <summary>
        ///     Sets the page title.
        ///     设置页面标题。
        /// </summary>
        public ModSettingsPageBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     Sets the page description.
        ///     设置页面描述。
        /// </summary>
        public ModSettingsPageBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     Sets the mod display name in the sidebar and registers it with <see cref="ModSettingsRegistry" /> on
        ///     <see cref="Build" />.
        ///     设置侧边栏中的 mod 显示名称，并在 <see cref="Build" /> 时将其注册到
        ///     <see cref="ModSettingsRegistry" />。
        /// </summary>
        public ModSettingsPageBuilder WithModDisplayName(ModSettingsText displayName)
        {
            ModDisplayName = displayName;
            return this;
        }

        /// <summary>
        ///     Sets <see cref="SortOrder" />.
        ///     设置 <see cref="SortOrder" />。
        /// </summary>
        public ModSettingsPageBuilder WithSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            return this;
        }

        /// <summary>
        ///     Registers <see cref="ModSettingsRegistry.RegisterModSidebarOrder" /> for <see cref="ModId" /> when this page
        ///     is built (repeat calls from the same mod should use the same value).
        ///     构建此页面时，为 <see cref="ModId" /> 注册 <see cref="ModSettingsRegistry.RegisterModSidebarOrder" />
        ///     （同一 mod 的重复调用应使用相同值）。
        /// </summary>
        public ModSettingsPageBuilder WithModSidebarOrder(int order)
        {
            _modSidebarOrder = order;
            return this;
        }

        /// <summary>
        ///     Hides the page in the sidebar and main content when <paramref name="predicate" /> returns false (re-evaluated
        ///     on settings UI refresh).
        ///     当 <paramref name="predicate" /> 返回 false 时，在侧边栏和主内容中隐藏页面（设置 UI 刷新时
        ///     重新求值）。
        /// </summary>
        public ModSettingsPageBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _pageVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Disables the page (dimmed, non-interactive) while <paramref name="predicate" /> is false.
        ///     当 <paramref name="predicate" /> 为 false 时禁用页面（变暗且不可交互）。
        /// </summary>
        public ModSettingsPageBuilder WithEnabledWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _pageEnabledWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Limits where this page appears (main menu vs run pause vs combat pause). Defaults to all surfaces.
        ///     限制此页面出现的位置（主菜单、run 暂停、战斗暂停）。默认显示在所有 surface。
        /// </summary>
        public ModSettingsPageBuilder WithVisibleOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _pageVisibleOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     Shows this page in the sidebar only after the user navigates to this page (or one of its child pages).
        ///     仅在用户进入此页面（或它的子页面）后，才在侧边栏显示此页面。
        /// </summary>
        public ModSettingsPageBuilder WithSidebarVisibleOnlyWhenActive()
        {
            _sidebarVisibleOnlyWhenActive = true;
            return this;
        }

        /// <summary>
        ///     Host surfaces where controls on this page are read-only (combined with per-section masks).
        ///     此页面控件只读的宿主 surface（会与每个 section 的掩码组合）。
        /// </summary>
        public ModSettingsPageBuilder WithReadOnlyOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _pageReadOnlyOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     Restricts which chrome menu actions are exposed for the page itself.
        ///     限制页面自身暴露哪些 chrome 菜单操作。
        /// </summary>
        public ModSettingsPageBuilder WithMenuCapabilities(ModSettingsMenuCapabilities capabilities)
        {
            _menuCapabilities = capabilities;
            return this;
        }

        /// <summary>
        ///     Adds a section built by <paramref name="configure" />; <paramref name="id" /> must be unique on this page.
        ///     添加由 <paramref name="configure" /> 构建的 section；<paramref name="id" /> 在此页面上必须唯一。
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
        ///     Materializes the page; throws if no sections were added.
        ///     生成页面对象；如果没有添加任何 section，则抛出异常。
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
                _sections.ToArray(),
                _pageVisibleWhen,
                _pageEnabledWhen,
                _menuCapabilities,
                _pageVisibleOnHostSurfaces,
                _pageReadOnlyOnHostSurfaces,
                _sidebarVisibleOnlyWhenActive
            );
        }
    }

    /// <summary>
    ///     Fluent builder for a settings section: collapsible chrome and typed entries (toggles, sliders, lists, etc.).
    ///     设置 section 的流式构建器：配置可折叠 chrome 和类型化条目（开关、滑条、列表等）。
    /// </summary>
    public sealed class ModSettingsSectionBuilder
    {
        private readonly List<ModSettingsEntryDefinition> _entries = [];
        private readonly Dictionary<string, Func<bool>> _entryEnabledWhen = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<bool>> _entryVisibleWhen = new(StringComparer.OrdinalIgnoreCase);

        private ModSettingsMenuCapabilities _menuCapabilities = ModSettingsMenuCapabilities.Copy |
                                                                ModSettingsMenuCapabilities.Paste;

        private Func<bool>? _sectionEnabledWhen;

        private ModSettingsHostSurface _sectionReadOnlyOnHostSurfaces = ModSettingsHostSurface.None;
        private ModSettingsHostSurface _sectionVisibleOnHostSurfaces = ModSettingsHostSurface.All;

        private Func<bool>? _sectionVisibleWhen;

        internal ModSettingsSectionBuilder(string id)
        {
            Id = id;
        }

        /// <summary>
        ///     Stable section id within the page.
        ///     页面内稳定的 section id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Optional section heading.
        ///     可选 section 标题。
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     Optional body text under the title.
        ///     标题下方的可选正文。
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     When true, the section can be collapsed in the UI.
        ///     为 true 时，该 section 可在 UI 中折叠。
        /// </summary>
        public bool IsCollapsible { get; private set; }

        /// <summary>
        ///     Initial collapsed state when <see cref="IsCollapsible" /> is true.
        ///     <see cref="IsCollapsible" /> 为 true 时的初始折叠状态。
        /// </summary>
        public bool StartCollapsed { get; private set; }

        /// <summary>
        ///     Sets <see cref="Title" />.
        ///     设置 <see cref="Title" />。
        /// </summary>
        public ModSettingsSectionBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     Sets <see cref="Description" />.
        ///     设置 <see cref="Description" />。
        /// </summary>
        public ModSettingsSectionBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     Marks the section collapsible; optionally starts collapsed.
        ///     将该 section 标记为可折叠；可选地初始为折叠状态。
        /// </summary>
        public ModSettingsSectionBuilder Collapsible(bool startCollapsed = false)
        {
            IsCollapsible = true;
            StartCollapsed = startCollapsed;
            return this;
        }

        /// <summary>
        ///     Hides the section (and its sidebar shortcut) while <paramref name="predicate" /> is false.
        ///     当 <paramref name="predicate" /> 为 false 时隐藏此 section（及其侧边栏快捷入口）。
        /// </summary>
        public ModSettingsSectionBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _sectionVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Disables the section (dimmed, non-interactive) while <paramref name="predicate" /> is false.
        ///     当 <paramref name="predicate" /> 为 false 时禁用此 section（变暗且不可交互）。
        /// </summary>
        public ModSettingsSectionBuilder WithEnabledWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _sectionEnabledWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Limits where this section is shown. Defaults to all host surfaces.
        ///     限制此 section 显示的位置。默认用于所有宿主界面。
        /// </summary>
        public ModSettingsSectionBuilder WithVisibleOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _sectionVisibleOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     Host surfaces where this section’s value controls are read-only (OR’d with the owning page mask).
        ///     此 section 的值控件在哪些宿主界面中为只读（与所属页面掩码按 OR 合并）。
        /// </summary>
        public ModSettingsSectionBuilder WithReadOnlyOnHostSurfaces(ModSettingsHostSurface surfaces)
        {
            _sectionReadOnlyOnHostSurfaces = surfaces;
            return this;
        }

        /// <summary>
        ///     Restricts which chrome menu actions are exposed for the section itself.
        ///     限制该 section 自身暴露哪些 chrome 菜单动作。
        /// </summary>
        public ModSettingsSectionBuilder WithMenuCapabilities(ModSettingsMenuCapabilities capabilities)
        {
            _menuCapabilities = capabilities;
            return this;
        }

        /// <summary>
        ///     Adds a non-interactive header row.
        ///     添加非交互式标题行。
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
        ///     Adds read-only paragraph text with optional max height for scrolling.
        ///     添加只读段落文本，并可指定用于滚动的最大高度。
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
        ///     Adds a read-only information card with title, optional subtitle, and body text.
        ///     添加只读信息卡，包含标题、可选副标题和正文文本。
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
        ///     Adds a read-only runtime hotkey summary row with left text and right binding chips.
        ///     添加只读运行时热键摘要行，左侧为文本，右侧为绑定 chip。
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
        ///     Adds a preview image resolved by <paramref name="textureProvider" />.
        ///     添加由 <paramref name="textureProvider" /> 解析的预览图像。
        /// </summary>
        public ModSettingsSectionBuilder AddImage(
            string id,
            ModSettingsText label,
            Func<Texture2D?> textureProvider,
            float previewHeight = 160f,
            ModSettingsText? description = null)
        {
            ArgumentNullException.ThrowIfNull(textureProvider);
            AddEntry(id, new ImageModSettingsEntryDefinition(id, label, textureProvider, previewHeight, description));
            return this;
        }

        /// <summary>
        ///     Adds an editable list bound to <paramref name="binding" /> with per-row editor from
        ///     <paramref name="itemEditorFactory" /> or defaults.
        ///     添加绑定到 <paramref name="binding" /> 的可编辑列表，每行编辑器来自
        ///     <paramref name="itemEditorFactory" /> 或默认实现。
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
        ///     Adds an editable list bound to <paramref name="binding" /> with optional collapsible item cards and
        ///     compact header accessories.
        ///     compact header accessories.
        ///     添加绑定到 <paramref name="binding" /> 的可编辑列表，支持可选的可折叠条目卡和
        ///     紧凑标题附件。
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
        ///     Adds a boolean toggle.
        ///     添加布尔 toggle。
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
        ///     Adds an integer range slider.
        ///     添加整数范围 slider。
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
        ///     Adds a floating-point range slider (<see cref="double" /> value domain).
        ///     添加浮点范围 slider（<see cref="double" /> 值域）。
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
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0d)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

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

        /// <summary>
        ///     Legacy <see cref="float" /> overload for binary compatibility; uses a dedicated float slider entry (not
        ///     the <see cref="double" /> control path) to avoid float/double conversion feedback loops.
        ///     用于二进制兼容性的旧版 <see cref="float" /> 重载；使用专用 float slider 条目（不走
        ///     <see cref="double" /> 控件路径），以避免 float/double 转换反馈循环。
        /// </summary>
        [Obsolete(
            "Prefer AddSlider with IModSettingsValueBinding<double> and double range parameters. This overload exists only for compatibility with mods compiled against pre-double slider APIs.")]
        public ModSettingsSectionBuilder AddSlider(
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
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0f)
                throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

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
        ///     Adds a fixed set of choices (stepper, dropdown, etc. per <paramref name="presentation" />).
        ///     添加固定选项集（按 <paramref name="presentation" /> 使用 stepper、dropdown 等）。
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
        ///     Adds a choice control for enum <typeparamref name="TEnum" /> with optional per-value labels.
        ///     为 enum <typeparamref name="TEnum" /> 添加 choice 控件，可为每个值指定可选标签。
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
        ///     Adds a color picker bound to a string (serialized color). 保留旧签名以维持 ABI 兼容。
        /// </summary>
        /// <param name="id">Stable entry id within the section.</param>
        /// <param name="label">Row label.</param>
        /// <param name="binding">Backing string binding (hex preferred; see <see cref="ModSettingsColorControl" />).</param>
        /// <param name="description">Optional description body.</param>
        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description = null)
        {
            return AddColor(id, label, binding, description, true, false);
        }

        /// <summary>
        ///     Adds a color picker bound to a string (serialized color), with picker chrome options.
        ///     添加绑定到字符串（序列化颜色）的颜色选择器，并带有选择器 chrome 选项。
        /// </summary>
        /// <param name="id">
        ///     Stable entry id within the section.
        ///     section 内稳定的条目 id。
        /// </param>
        /// <param name="label">
        ///     Row label.
        ///     行标签。
        /// </param>
        /// <param name="binding">
        ///     Backing string binding (hex preferred; see <see cref="ModSettingsColorControl" />).
        /// </param>
        /// <param name="description">
        ///     Optional description body.
        ///     可选 description body.
        /// </param>
        /// <param name="editAlpha">
        ///     Whether the picker allows editing alpha.
        ///     取色器是否允许编辑 alpha。
        /// </param>
        /// <param name="editIntensity">
        ///     Whether the picker allows intensity / HDR-style values (Godot
        ///     <c>ColorPicker.EditIntensity</c>).
        ///     选择器是否允许 intensity / HDR 风格值（Godot
        ///     <c>ColorPicker.EditIntensity</c>）。
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
        ///     Adds a single-line string field.
        ///     添加单行字符串字段。
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
        ///     Adds a single-line string field with optional visual validation (invalid text shows error chrome; commits
        ///     are not blocked).
        ///     添加单行字符串字段，并可选进行视觉校验（无效文本显示错误 chrome；提交
        ///     不会被阻止）。
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
            if (maxLength is < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be null or >= 1.");

            AddEntry(id,
                new StringModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description)
                {
                    ValueValidationVisual = valueValidationVisual,
                });
            return this;
        }

        /// <summary>
        ///     Adds a multiline string field.
        ///     添加多行字符串字段。
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
        ///     Adds a key binding capture row.
        ///     添加按键绑定捕获行。
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
        ///     Adds a multi-key binding capture row. This path is native-only and must be explicitly opted into.
        ///     添加多按键绑定捕获行。此路径仅限原生，必须显式选择启用。
        /// </summary>
        public ModSettingsSectionBuilder AddKeyBinding(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<List<string>> binding,
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
        ///     Adds a button that runs <paramref name="action" /> (no persisted value).
        ///     添加运行 <paramref name="action" /> 的按钮（无持久化值）。
        ///     添加运行 <c>action</c> 的按钮（无持久化值）。
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
        ///     Adds a button that runs <paramref name="action" /> with a settings UI host (for refresh after deferred work).
        ///     添加使用设置 UI host 运行 <paramref name="action" /> 的按钮（用于延迟工作后的刷新）。
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
        ///     Adds navigation to another registered page <paramref name="targetPageId" />.
        ///     添加到另一个已注册页面 <paramref name="targetPageId" /> 的导航。
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
        ///     Adds a custom row built by <paramref name="controlFactory" />.
        ///     添加由 <paramref name="controlFactory" /> 构建的自定义行。
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
        ///     Overrides the chrome menu capabilities for one entry in this section.
        ///     覆盖此 section 中某个条目的 chrome 菜单能力。
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
        ///     Host surfaces where one entry's interactive controls are read-only.
        ///     配置某个条目的交互控件在哪些宿主界面中只读。
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

        internal ModSettingsSectionBuilder WithEntryVisibleWhen(string id, Func<bool> predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(predicate);

            if (!_entryIds.Contains(id))
                throw new InvalidOperationException($"Settings entry '{id}' does not exist in section '{Id}'.");

            _entryVisibleWhen[id] = predicate;
            return this;
        }

        /// <summary>
        ///     Disables one entry (dimmed, non-interactive) while <paramref name="predicate" /> is false.
        ///     当 <paramref name="predicate" /> 为 false 时禁用某个条目（变暗且不可交互）。
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
