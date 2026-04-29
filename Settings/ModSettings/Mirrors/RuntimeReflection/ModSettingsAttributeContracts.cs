namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Binding source strategy for reflection settings members.
    /// </summary>
    public enum ModSettingsReflectionBindingSource
    {
        /// <summary>
        ///     Use framework default strategy.
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Persist under global scope store.
        /// </summary>
        Global = 1,

        /// <summary>
        ///     Persist under profile scope store.
        /// </summary>
        Profile = 2,

        /// <summary>
        ///     Persist under run-sidecar scope.
        /// </summary>
        RunSidecar = 3,

        /// <summary>
        ///     In-memory only.
        /// </summary>
        InMemory = 4,

        /// <summary>
        ///     Caller-provided read/write/save callbacks.
        /// </summary>
        Callback = 5,

        /// <summary>
        ///     Project from a parent callback binding.
        /// </summary>
        Project = 6,
    }

    /// <summary>
    ///     Save trigger policy after writes.
    /// </summary>
    public enum ModSettingsReflectionSavePolicy
    {
        /// <summary>
        ///     Auto-save after each write.
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Write only; save is external/manual.
        /// </summary>
        Manual = 1,
    }

    /// <summary>
    ///     Declares reflection binding strategy for an annotated field/property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsBindingAttribute : Attribute
    {
        /// <summary>
        ///     Binding source strategy.
        /// </summary>
        public ModSettingsReflectionBindingSource Source { get; init; } = ModSettingsReflectionBindingSource.Auto;

        /// <summary>
        ///     Save trigger policy after writes.
        /// </summary>
        public ModSettingsReflectionSavePolicy SavePolicy { get; init; } = ModSettingsReflectionSavePolicy.Auto;

        /// <summary>
        ///     Optional persistent data key override.
        /// </summary>
        public string? DataKey { get; init; }

        /// <summary>
        ///     Optional callback read method name for callback/project sources.
        /// </summary>
        public string? ReadUsing { get; init; }

        /// <summary>
        ///     Optional callback write method name for callback/project sources.
        /// </summary>
        public string? WriteUsing { get; init; }

        /// <summary>
        ///     Optional callback save method name for callback/project sources.
        /// </summary>
        public string? SaveUsing { get; init; }

        /// <summary>
        ///     Optional method name that returns a default value for this binding.
        /// </summary>
        public string? DefaultUsing { get; init; }

        /// <summary>
        ///     Optional method name that returns an <c>IStructuredModSettingsValueAdapter&lt;T&gt;</c>.
        /// </summary>
        public string? AdapterUsing { get; init; }

        /// <summary>
        ///     Parent read callback method name for projection source.
        /// </summary>
        public string? ProjectParentReadUsing { get; init; }

        /// <summary>
        ///     Parent write callback method name for projection source.
        /// </summary>
        public string? ProjectParentWriteUsing { get; init; }

        /// <summary>
        ///     Optional parent save callback method name for projection source.
        /// </summary>
        public string? ProjectParentSaveUsing { get; init; }

        /// <summary>
        ///     Projection getter callback method name (<c>TParent -&gt; TValue</c>).
        /// </summary>
        public string? ProjectGetUsing { get; init; }

        /// <summary>
        ///     Projection setter callback method name (<c>(TParent, TValue) -&gt; TParent</c>).
        /// </summary>
        public string? ProjectSetUsing { get; init; }

        /// <summary>
        ///     Optional projected child data-key suffix.
        /// </summary>
        public string? ProjectDataKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for title/description text that can resolve from literal, i18n, or LocString.
    /// </summary>
    public abstract class ModSettingsTitleDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     Optional provider method name that returns <see cref="Utils.I18N" /> for this attribute.
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     Optional title text.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Title" />.
        /// </summary>
        public string? TitleKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Title" />.
        /// </summary>
        public string? TitleLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Title" />.
        /// </summary>
        public string? TitleLocKey { get; init; }

        /// <summary>
        ///     Optional description text.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Description" />.
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Description" />.
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Description" />.
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for label/description text that can resolve from literal, i18n, or LocString.
    /// </summary>
    public abstract class ModSettingsLabelDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     Optional provider method name that returns <see cref="Utils.I18N" /> for this attribute.
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     Optional label text.
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Label" />.
        /// </summary>
        public string? LabelKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Label" />.
        /// </summary>
        public string? LabelLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Label" />.
        /// </summary>
        public string? LabelLocKey { get; init; }

        /// <summary>
        ///     Optional description text.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Description" />.
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Description" />.
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Description" />.
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for common ordered entries with visibility predicate.
    /// </summary>
    public abstract class ModSettingsOrderedEntryAttribute : ModSettingsLabelDescriptionTextAttribute
    {
        /// <summary>
        ///     Entry order within section.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Optional visibility method name.
        /// </summary>
        public string? VisibleWhen { get; init; }
    }

    /// <summary>
    ///     Marks a type as an attribute-driven reflection settings page provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModSettingsPageAttribute(string modId, string? pageId = null)
        : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     Owning mod id.
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Stable page id; defaults to mod id when omitted.
        /// </summary>
        public string? PageId { get; } = pageId;

        /// <summary>
        ///     Page sort order among siblings.
        /// </summary>
        public int SortOrder { get; init; }

        /// <summary>
        ///     Optional parent page id for nested navigation.
        /// </summary>
        public string? ParentPageId { get; init; }

        /// <summary>
        ///     Optional mod display name in sidebar grouping.
        /// </summary>
        public string? ModDisplayName { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ModDisplayName" />.
        /// </summary>
        public string? ModDisplayNameKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ModDisplayName" />.
        /// </summary>
        public string? ModDisplayNameLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ModDisplayName" />.
        /// </summary>
        public string? ModDisplayNameLocKey { get; init; }

        /// <summary>
        ///     Optional sidebar group order for the mod.
        /// </summary>
        public int? ModSidebarOrder { get; init; }
    }

    /// <summary>
    ///     Declares one section in a reflection settings page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ModSettingsSectionAttribute(string id) : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     Stable section id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Whether the section is collapsible.
        /// </summary>
        public bool IsCollapsible { get; init; }

        /// <summary>
        ///     Initial collapsed state when collapsible.
        /// </summary>
        public bool StartCollapsed { get; init; }

        /// <summary>
        ///     Section sort order on the page.
        /// </summary>
        public int SortOrder { get; init; }
    }

    /// <summary>
    ///     Declares a boolean toggle entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsToggleAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     Declares a floating-point slider entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsSliderAttribute(
        string id,
        string sectionId,
        double min,
        double max,
        double step = 1d)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Minimum slider value.
        /// </summary>
        public double Min { get; } = min;

        /// <summary>
        ///     Maximum slider value.
        /// </summary>
        public double Max { get; } = max;

        /// <summary>
        ///     Slider step.
        /// </summary>
        public double Step { get; } = step;
    }

    /// <summary>
    ///     Declares an integer slider entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsIntSliderAttribute(string id, string sectionId, int min, int max, int step = 1)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Minimum slider value.
        /// </summary>
        public int Min { get; } = min;

        /// <summary>
        ///     Maximum slider value.
        /// </summary>
        public int Max { get; } = max;

        /// <summary>
        ///     Slider step.
        /// </summary>
        public int Step { get; } = step;
    }

    /// <summary>
    ///     Declares a single-line text entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsStringAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional placeholder text.
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     Optional max length; zero means unset.
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>
        ///     Optional validation method name.
        /// </summary>
        public string? ValidateUsing { get; init; }
    }

    /// <summary>
    ///     Declares a multiline text entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsMultilineStringAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional placeholder text.
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Placeholder" />.
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     Optional max length; zero means unset.
        /// </summary>
        public int MaxLength { get; init; }
    }

    /// <summary>
    ///     Declares a color picker entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsColorAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Whether alpha channel editing is enabled.
        /// </summary>
        public bool EditAlpha { get; init; } = true;

        /// <summary>
        ///     Whether HDR/intensity editing is enabled.
        /// </summary>
        public bool EditIntensity { get; init; }
    }

    /// <summary>
    ///     Declares a key binding entry (single or multi).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsKeyBindingAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Whether modifier+key combinations are allowed.
        /// </summary>
        public bool AllowModifierCombos { get; init; } = true;

        /// <summary>
        ///     Whether modifier-only bindings are allowed.
        /// </summary>
        public bool AllowModifierOnly { get; init; } = true;

        /// <summary>
        ///     Whether left/right modifier keys are distinguished.
        /// </summary>
        public bool DistinguishModifierSides { get; init; }

        /// <summary>
        ///     Whether this represents multi-binding mode.
        /// </summary>
        public bool Multiple { get; init; }
    }

    /// <summary>
    ///     Declares a choice entry (string or enum).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsChoiceAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional string option values.
        /// </summary>
        public string[]? Options { get; init; }

        /// <summary>
        ///     Optional labels parallel to <see cref="Options" />.
        /// </summary>
        public string[]? OptionLabels { get; init; }

        /// <summary>
        ///     Optional i18n keys parallel to <see cref="Options" />.
        /// </summary>
        public string[]? OptionLabelKeys { get; init; }

        /// <summary>
        ///     Optional LocString table for option labels.
        /// </summary>
        public string? OptionLabelLocTable { get; init; }

        /// <summary>
        ///     Optional LocString keys parallel to <see cref="Options" />.
        /// </summary>
        public string[]? OptionLabelLocKeys { get; init; }

        /// <summary>
        ///     Choice presentation mode.
        /// </summary>
        public ModSettingsChoicePresentation Presentation { get; init; } = ModSettingsChoicePresentation.Stepper;
    }

    /// <summary>
    ///     Declares a button action entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsButtonAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional button text override.
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextLocKey { get; init; }

        /// <summary>
        ///     Button tone.
        /// </summary>
        public ModSettingsButtonTone Tone { get; init; } = ModSettingsButtonTone.Normal;

        /// <summary>
        ///     Whether the target method expects host context.
        /// </summary>
        public bool UseHostContext { get; init; }
    }

    /// <summary>
    ///     Declares a paragraph display entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsParagraphAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional static text override.
        /// </summary>
        public string? Text { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Text" />.
        /// </summary>
        public string? TextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Text" />.
        /// </summary>
        public string? TextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Text" />.
        /// </summary>
        public string? TextLocKey { get; init; }

        /// <summary>
        ///     Optional max body height.
        /// </summary>
        public float MaxBodyHeight { get; init; }
    }

    /// <summary>
    ///     Declares a header display entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsHeaderAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     Declares an info-card display entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsInfoCardAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional card body text override.
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Body" />.
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Body" />.
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Body" />.
        /// </summary>
        public string? BodyLocKey { get; init; }
    }

    /// <summary>
    ///     Declares a runtime hotkey-summary display entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsRuntimeHotkeySummaryAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional body text override.
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Body" />.
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Body" />.
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Body" />.
        /// </summary>
        public string? BodyLocKey { get; init; }

        /// <summary>
        ///     Hotkey chips to display.
        /// </summary>
        public string[] Bindings { get; init; } = [];

        /// <summary>
        ///     Optional i18n keys parallel to <see cref="Bindings" />.
        /// </summary>
        public string[]? BindingKeys { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Bindings" />.
        /// </summary>
        public string? BindingLocTable { get; init; }

        /// <summary>
        ///     Optional LocString keys parallel to <see cref="Bindings" />.
        /// </summary>
        public string[]? BindingLocKeys { get; init; }

        /// <summary>
        ///     Optional id suffix displayed in UI.
        /// </summary>
        public string? IdSuffix { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="IdSuffix" />.
        /// </summary>
        public string? IdSuffixKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="IdSuffix" />.
        /// </summary>
        public string? IdSuffixLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="IdSuffix" />.
        /// </summary>
        public string? IdSuffixLocKey { get; init; }
    }

    /// <summary>
    ///     Declares an image display entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsImageAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Preview height in pixels.
        /// </summary>
        public float PreviewHeight { get; init; } = 160f;
    }

    /// <summary>
    ///     Declares a subpage navigation entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsSubpageAttribute(string id, string sectionId, string targetPageId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Destination page id.
        /// </summary>
        public string TargetPageId { get; } = targetPageId;

        /// <summary>
        ///     Optional button text override.
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ButtonText" />.
        /// </summary>
        public string? ButtonTextLocKey { get; init; }
    }

    /// <summary>
    ///     Declares a custom-control entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsCustomEntryAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        /// </summary>
        public string SectionId { get; } = sectionId;
    }
}
