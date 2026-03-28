using Godot;

namespace STS2RitsuLib.Settings
{
    public sealed class ModSettingsPageBuilder
    {
        private readonly HashSet<string> _sectionIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ModSettingsSection> _sections = [];

        public ModSettingsPageBuilder(string modId, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ModId = modId;
            PageId = string.IsNullOrWhiteSpace(pageId) ? modId : pageId;
        }

        public string ModId { get; }
        public string PageId { get; }
        public string? ParentPageId { get; private set; }
        public ModSettingsText? Title { get; private set; }
        public ModSettingsText? Description { get; private set; }
        public ModSettingsText? ModDisplayName { get; private set; }
        public int SortOrder { get; private set; }

        public ModSettingsPageBuilder AsChildOf(string parentPageId)
        {
            ParentPageId = parentPageId;
            return this;
        }

        public ModSettingsPageBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        public ModSettingsPageBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        public ModSettingsPageBuilder WithModDisplayName(ModSettingsText displayName)
        {
            ModDisplayName = displayName;
            return this;
        }

        public ModSettingsPageBuilder WithSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            return this;
        }

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

        public ModSettingsPage Build()
        {
            if (_sections.Count == 0)
                throw new InvalidOperationException($"Settings page '{PageId}' for mod '{ModId}' has no sections.");

            if (ModDisplayName != null)
                ModSettingsRegistry.RegisterModDisplayName(ModId, ModDisplayName);

            return new(
                ModId,
                PageId,
                ParentPageId,
                Title,
                Description,
                SortOrder,
                _sections.ToArray()
            );
        }
    }

    public sealed class ModSettingsSectionBuilder
    {
        private readonly List<ModSettingsEntryDefinition> _entries = [];
        private readonly HashSet<string> _entryIds = new(StringComparer.OrdinalIgnoreCase);

        internal ModSettingsSectionBuilder(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public ModSettingsText? Title { get; private set; }
        public ModSettingsText? Description { get; private set; }
        public bool IsCollapsible { get; private set; }
        public bool StartCollapsed { get; private set; }

        public ModSettingsSectionBuilder WithTitle(ModSettingsText title)
        {
            Title = title;
            return this;
        }

        public ModSettingsSectionBuilder WithDescription(ModSettingsText description)
        {
            Description = description;
            return this;
        }

        public ModSettingsSectionBuilder Collapsible(bool startCollapsed = false)
        {
            IsCollapsible = true;
            StartCollapsed = startCollapsed;
            return this;
        }

        public ModSettingsSectionBuilder AddHeader(
            string id,
            ModSettingsText label,
            ModSettingsText? description = null)
        {
            AddEntry(id, new HeaderModSettingsEntryDefinition(id, label, description));
            return this;
        }

        public ModSettingsSectionBuilder AddParagraph(
            string id,
            ModSettingsText text,
            ModSettingsText? description = null,
            float? maxBodyHeight = null)
        {
            AddEntry(id, new ParagraphModSettingsEntryDefinition(id, text, description, maxBodyHeight));
            return this;
        }

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

        public ModSettingsSectionBuilder AddToggle(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<bool> binding,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ToggleModSettingsEntryDefinition(id, label, binding, description));
            return this;
        }

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
            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

            if (step <= 0f)
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

        public ModSettingsSectionBuilder AddColor(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description = null)
        {
            AddEntry(id, new ColorModSettingsEntryDefinition(id, label, binding, description));
            return this;
        }

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
                : new(Id, Title, Description, IsCollapsible, StartCollapsed, _entries.ToArray());
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
