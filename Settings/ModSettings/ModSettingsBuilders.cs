using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Fluent builder for a registered mod settings page: metadata, optional parent page, and sections.
    /// </summary>
    public sealed class ModSettingsPageBuilder
    {
        private readonly HashSet<string> _sectionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ModSettingsSection> _sections = [];

        private int? _modSidebarOrder;
        private Func<bool>? _pageVisibleWhen;

        /// <summary>
        ///     Initializes a builder for mod <paramref name="modId" />; <paramref name="pageId" /> defaults to the mod id when
        ///     null or whitespace.
        /// </summary>
        public ModSettingsPageBuilder(string modId, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ModId = modId;
            PageId = string.IsNullOrWhiteSpace(pageId) ? modId : pageId;
        }

        /// <summary>
        ///     Owning mod identifier.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Stable page id (used for navigation and chrome clipboard).
        /// </summary>
        public string PageId { get; }

        /// <summary>
        ///     When set, this page appears as a child of the given parent page id.
        /// </summary>
        public string? ParentPageId { get; private set; }

        /// <summary>
        ///     Localized title shown in tabs and headers.
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     Optional subtitle or long description for the page.
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     Display name for the mod in the settings sidebar (separate from page titles).
        /// </summary>
        public ModSettingsText? ModDisplayName { get; private set; }

        /// <summary>
        ///     Ordering among sibling pages (lower first).
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        ///     Nests this page under <paramref name="parentPageId" /> in the UI hierarchy.
        /// </summary>
        public ModSettingsPageBuilder AsChildOf(string parentPageId)
        {
            ParentPageId = parentPageId;
            return this;
        }

        /// <summary>
        ///     Sets the page title.
        /// </summary>
        public ModSettingsPageBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     Sets the page description.
        /// </summary>
        public ModSettingsPageBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     Sets the mod display name in the sidebar and registers it with <see cref="ModSettingsRegistry" /> on
        ///     <see cref="Build" />.
        /// </summary>
        public ModSettingsPageBuilder WithModDisplayName(ModSettingsText displayName)
        {
            ModDisplayName = displayName;
            return this;
        }

        /// <summary>
        ///     Sets <see cref="SortOrder" />.
        /// </summary>
        public ModSettingsPageBuilder WithSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            return this;
        }

        /// <summary>
        ///     Registers <see cref="ModSettingsRegistry.RegisterModSidebarOrder" /> for <see cref="ModId" /> when this page
        ///     is built (repeat calls from the same mod should use the same value).
        /// </summary>
        public ModSettingsPageBuilder WithModSidebarOrder(int order)
        {
            _modSidebarOrder = order;
            return this;
        }

        /// <summary>
        ///     Hides the page in the sidebar and main content when <paramref name="predicate" /> returns false (re-evaluated
        ///     on settings UI refresh).
        /// </summary>
        public ModSettingsPageBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _pageVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Adds a section built by <paramref name="configure" />; <paramref name="id" /> must be unique on this page.
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
                _pageVisibleWhen
            );
        }
    }

    /// <summary>
    ///     Fluent builder for a settings section: collapsible chrome and typed entries (toggles, sliders, lists, etc.).
    /// </summary>
    public sealed class ModSettingsSectionBuilder
    {
        private readonly List<ModSettingsEntryDefinition> _entries = [];
        private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);
        private Func<bool>? _sectionVisibleWhen;

        internal ModSettingsSectionBuilder(string id)
        {
            Id = id;
        }

        /// <summary>
        ///     Stable section id within the page.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Optional section heading.
        /// </summary>
        public ModSettingsText? Title { get; private set; }

        /// <summary>
        ///     Optional body text under the title.
        /// </summary>
        public ModSettingsText? Description { get; private set; }

        /// <summary>
        ///     When true, the section can be collapsed in the UI.
        /// </summary>
        public bool IsCollapsible { get; private set; }

        /// <summary>
        ///     Initial collapsed state when <see cref="IsCollapsible" /> is true.
        /// </summary>
        public bool StartCollapsed { get; private set; }

        /// <summary>
        ///     Sets <see cref="Title" />.
        /// </summary>
        public ModSettingsSectionBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     Sets <see cref="Description" />.
        /// </summary>
        public ModSettingsSectionBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        ///     Marks the section collapsible; optionally starts collapsed.
        /// </summary>
        public ModSettingsSectionBuilder Collapsible(bool startCollapsed = false)
        {
            IsCollapsible = true;
            StartCollapsed = startCollapsed;
            return this;
        }

        /// <summary>
        ///     Hides the section (and its sidebar shortcut) while <paramref name="predicate" /> is false.
        /// </summary>
        public ModSettingsSectionBuilder WithVisibleWhen(Func<bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _sectionVisibleWhen = predicate;
            return this;
        }

        /// <summary>
        ///     Adds a non-interactive header row.
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
        ///     Adds a preview image resolved by <paramref name="textureProvider" />.
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
                addButtonText ?? ModSettingsText.I18N(ModSettingsLocalization.Instance, "button.add", "Add"),
                description));
            return this;
        }

        /// <summary>
        ///     Adds a boolean toggle.
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
        ///     Adds a color picker bound to a string (serialized color).
        /// </summary>
        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ColorModSettingsEntryDefinition(id, label, binding, description));
            return this;
        }

        /// <summary>
        ///     Adds a single-line string field.
        /// </summary>
        public ModSettingsSectionBuilder AddString(
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
                new StringModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description));
            return this;
        }

        /// <summary>
        ///     Adds a multiline string field.
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
            AddEntry(id,
                new KeyBindingModSettingsEntryDefinition(id, label, binding, allowModifierCombos, allowModifierOnly,
                    distinguishModifierSides, description));
            return this;
        }

        /// <summary>
        ///     Adds a button that runs <paramref name="action" /> (no persisted value).
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

        internal ModSettingsSection Build()
        {
            return _entries.Count == 0
                ? throw new InvalidOperationException($"Settings section '{Id}' has no entries.")
                : new(Id, Title, Description, IsCollapsible, StartCollapsed, _entries.ToArray(), _sectionVisibleWhen);
        }

        private void AddEntry(string id, ModSettingsEntryDefinition entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            if (!_entryIds.Add(id))
                throw new InvalidOperationException($"Duplicate settings entry id '{id}' in section '{Id}'.");

            _entries.Add(entry);
        }
    }
}
