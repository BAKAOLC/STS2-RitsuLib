namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Binding source strategy for reflection settings members.
    ///     反射设置成员的绑定来源策略。
    /// </summary>
    public enum ModSettingsReflectionBindingSource
    {
        /// <summary>
        ///     Use framework default strategy.
        ///     使用框架默认策略。
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Persist under global scope store.
        ///     持久化到全局作用域存储。
        /// </summary>
        Global = 1,

        /// <summary>
        ///     Persist under profile scope store.
        ///     持久化到 profile 作用域存储。
        /// </summary>
        Profile = 2,

        /// <summary>
        ///     Persist under run-sidecar scope.
        ///     持久化到 run-sidecar 作用域。
        /// </summary>
        RunSidecar = 3,

        /// <summary>
        ///     In-memory only.
        ///     仅保存在内存中。
        /// </summary>
        InMemory = 4,

        /// <summary>
        ///     Caller-provided read/write/save callbacks.
        ///     调用方提供的读 / 写 / 保存回调。
        /// </summary>
        Callback = 5,

        /// <summary>
        ///     Project from a parent callback binding.
        ///     从父回调绑定投影。
        /// </summary>
        Project = 6,
    }

    /// <summary>
    ///     Declares reflection binding strategy for an annotated field/property.
    ///     为带注解的字段 / 属性声明反射绑定策略。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsBindingAttribute : Attribute
    {
        /// <summary>
        ///     Binding source strategy.
        ///     绑定来源策略。
        /// </summary>
        public ModSettingsReflectionBindingSource Source { get; init; } = ModSettingsReflectionBindingSource.Auto;

        /// <summary>
        ///     Optional persistent data key override.
        ///     可选的持久化数据 key 覆盖。
        /// </summary>
        public string? DataKey { get; init; }

        /// <summary>
        ///     Optional callback read method name for callback/project sources.
        ///     callback / project 来源的可选读取回调方法名。
        /// </summary>
        public string? ReadUsing { get; init; }

        /// <summary>
        ///     Optional callback write method name for callback/project sources.
        ///     callback / project 来源的可选写入回调方法名。
        /// </summary>
        public string? WriteUsing { get; init; }

        /// <summary>
        ///     Optional callback save method name for callback/project sources.
        ///     callback / project 来源的可选保存回调方法名。
        /// </summary>
        public string? SaveUsing { get; init; }

        /// <summary>
        ///     Optional method name that returns a default value for this binding.
        ///     返回此绑定默认值的可选方法名。
        /// </summary>
        public string? DefaultUsing { get; init; }

        /// <summary>
        ///     Optional method name that returns an <c>IStructuredModSettingsValueAdapter&lt;T&gt;</c>.
        ///     返回 <c>IStructuredModSettingsValueAdapter&lt;T&gt;</c> 的可选方法名。
        /// </summary>
        public string? AdapterUsing { get; init; }

        /// <summary>
        ///     Parent read callback method name for projection source.
        ///     投影来源的父级读取回调方法名。
        /// </summary>
        public string? ProjectParentReadUsing { get; init; }

        /// <summary>
        ///     Parent write callback method name for projection source.
        ///     投影来源的父级写入回调方法名。
        /// </summary>
        public string? ProjectParentWriteUsing { get; init; }

        /// <summary>
        ///     Optional parent save callback method name for projection source.
        ///     投影来源的可选父级保存回调方法名。
        /// </summary>
        public string? ProjectParentSaveUsing { get; init; }

        /// <summary>
        ///     Projection getter callback method name (<c>TParent -&gt; TValue</c>).
        ///     投影 getter 回调方法名（<c>TParent -&gt; TValue</c>）。
        /// </summary>
        public string? ProjectGetUsing { get; init; }

        /// <summary>
        ///     Projection setter callback method name (<c>(TParent, TValue) -&gt; TParent</c>).
        ///     投影 setter 回调方法名（<c>(TParent, TValue) -&gt; TParent</c>）。
        /// </summary>
        public string? ProjectSetUsing { get; init; }

        /// <summary>
        ///     Optional projected child data-key suffix.
        ///     可选的投影子数据 key 后缀。
        /// </summary>
        public string? ProjectDataKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for title/description text that can resolve from literal, i18n, or LocString.
    ///     标题 / 描述文本的共享槽位，可从字面值、i18n 或 LocString 解析。
    /// </summary>
    public abstract class ModSettingsTitleDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     Optional provider method name that returns <see cref="Utils.I18N" /> for this attribute.
        ///     为此 attribute 返回 <see cref="Utils.I18N" /> 的可选提供器方法名。
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     Optional title text.
        ///     可选标题文本。
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Title" />.
        ///     <see cref="Title" /> 的可选 i18n key。
        /// </summary>
        public string? TitleKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Title" />.
        ///     <see cref="Title" /> 的可选 LocString table。
        /// </summary>
        public string? TitleLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Title" />.
        ///     <see cref="Title" /> 的可选 LocString key。
        /// </summary>
        public string? TitleLocKey { get; init; }

        /// <summary>
        ///     Optional description text.
        ///     可选描述文本。
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 i18n key。
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 LocString table。
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 LocString key。
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for label/description text that can resolve from literal, i18n, or LocString.
    ///     标签 / 描述文本的共享槽位，可从字面值、i18n 或 LocString 解析。
    /// </summary>
    public abstract class ModSettingsLabelDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     Optional provider method name that returns <see cref="Utils.I18N" /> for this attribute.
        ///     为此 attribute 返回 <see cref="Utils.I18N" /> 的可选提供器方法名。
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     Optional label text.
        ///     可选标签文本。
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Label" />.
        ///     <see cref="Label" /> 的可选 i18n key。
        /// </summary>
        public string? LabelKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Label" />.
        ///     <see cref="Label" /> 的可选 LocString table。
        /// </summary>
        public string? LabelLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Label" />.
        ///     <see cref="Label" /> 的可选 LocString key。
        /// </summary>
        public string? LabelLocKey { get; init; }

        /// <summary>
        ///     Optional description text.
        ///     可选描述文本。
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 i18n key。
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 LocString table。
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Description" />.
        ///     <see cref="Description" /> 的可选 LocString key。
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     Shared slots for common ordered entries with visibility predicate.
    ///     带可见性 predicate 的通用有序条目的共享槽位。
    /// </summary>
    public abstract class ModSettingsOrderedEntryAttribute : ModSettingsLabelDescriptionTextAttribute
    {
        /// <summary>
        ///     Entry order within section.
        ///     entry 在 section 内的排序。
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     Optional visibility method name.
        ///     可选的可见性方法名。
        /// </summary>
        public string? VisibleWhen { get; init; }
    }

    /// <summary>
    ///     Marks a type as an attribute-driven reflection settings page provider.
    ///     将类型标记为 attribute 驱动的反射设置页面提供器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModSettingsPageAttribute(string modId, string? pageId = null)
        : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     Owning mod id.
        ///     所属 mod id。
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Stable page id; defaults to mod id when omitted.
        ///     稳定的 page id；省略时默认使用 mod id。
        /// </summary>
        public string? PageId { get; } = pageId;

        /// <summary>
        ///     Page sort order among siblings.
        ///     page 在同级页面中的排序。
        /// </summary>
        public int SortOrder { get; init; }

        /// <summary>
        ///     Optional parent page id for nested navigation.
        ///     嵌套导航使用的可选父页面 id。
        /// </summary>
        public string? ParentPageId { get; init; }

        /// <summary>
        ///     Optional mod display name in sidebar grouping.
        ///     sidebar 分组中显示的可选 mod 名称。
        /// </summary>
        public string? ModDisplayName { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ModDisplayName" />.
        ///     <see cref="ModDisplayName" /> 的可选 i18n key。
        /// </summary>
        public string? ModDisplayNameKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ModDisplayName" />.
        ///     <see cref="ModDisplayName" /> 的可选 LocString table。
        /// </summary>
        public string? ModDisplayNameLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ModDisplayName" />.
        ///     <see cref="ModDisplayName" /> 的可选 LocString key。
        /// </summary>
        public string? ModDisplayNameLocKey { get; init; }

        /// <summary>
        ///     Optional sidebar group order for the mod.
        ///     mod 在 sidebar 分组中的可选排序。
        /// </summary>
        public int? ModSidebarOrder { get; init; }
    }

    /// <summary>
    ///     Declares one section in a reflection settings page.
    ///     声明反射设置页面中的一个 section。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ModSettingsSectionAttribute(string id) : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     Stable section id.
        ///     稳定的 section id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Whether the section is collapsible.
        ///     此 section 是否可折叠。
        /// </summary>
        public bool IsCollapsible { get; init; }

        /// <summary>
        ///     Initial collapsed state when collapsible.
        ///     可折叠时的初始折叠状态。
        /// </summary>
        public bool StartCollapsed { get; init; }

        /// <summary>
        ///     Section sort order on the page.
        ///     section 在页面中的排序。
        /// </summary>
        public int SortOrder { get; init; }
    }

    /// <summary>
    ///     Declares a boolean toggle entry.
    ///     声明布尔 toggle 条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsToggleAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     Declares a floating-point slider entry.
    ///     声明浮点 slider 条目。
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
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Minimum slider value.
        ///     slider 的最小值。
        /// </summary>
        public double Min { get; } = min;

        /// <summary>
        ///     Maximum slider value.
        ///     slider 的最大值。
        /// </summary>
        public double Max { get; } = max;

        /// <summary>
        ///     Slider step.
        ///     slider 步长。
        /// </summary>
        public double Step { get; } = step;
    }

    /// <summary>
    ///     Declares an integer slider entry.
    ///     声明整数 slider 条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsIntSliderAttribute(string id, string sectionId, int min, int max, int step = 1)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Minimum slider value.
        ///     slider 的最小值。
        /// </summary>
        public int Min { get; } = min;

        /// <summary>
        ///     Maximum slider value.
        ///     slider 的最大值。
        /// </summary>
        public int Max { get; } = max;

        /// <summary>
        ///     Slider step.
        ///     slider 步长。
        /// </summary>
        public int Step { get; } = step;
    }

    /// <summary>
    ///     Declares a single-line text entry.
    ///     声明单行文本条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsStringAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional placeholder text.
        ///     可选 placeholder 文本。
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 i18n key。
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 LocString table。
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 LocString key。
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     Optional max length; zero means unset.
        ///     可选最大长度；0 表示未设置。
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>
        ///     Optional validation method name.
        ///     可选校验方法名。
        /// </summary>
        public string? ValidateUsing { get; init; }
    }

    /// <summary>
    ///     Declares a multiline text entry.
    ///     声明多行文本条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsMultilineStringAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional placeholder text.
        ///     可选 placeholder 文本。
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 i18n key。
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 LocString table。
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Placeholder" />.
        ///     <see cref="Placeholder" /> 的可选 LocString key。
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     Optional max length; zero means unset.
        ///     可选最大长度；0 表示未设置。
        /// </summary>
        public int MaxLength { get; init; }
    }

    /// <summary>
    ///     Declares a color picker entry.
    ///     声明颜色选择器条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsColorAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Whether alpha channel editing is enabled.
        ///     是否启用 alpha 通道编辑。
        /// </summary>
        public bool EditAlpha { get; init; } = true;

        /// <summary>
        ///     Whether HDR/intensity editing is enabled.
        ///     是否启用 HDR / intensity 编辑。
        /// </summary>
        public bool EditIntensity { get; init; }
    }

    /// <summary>
    ///     Declares a key binding entry (single or multi).
    ///     声明按键绑定条目（单绑定或多绑定）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsKeyBindingAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Whether modifier+key combinations are allowed.
        ///     是否允许 modifier + key 组合。
        /// </summary>
        public bool AllowModifierCombos { get; init; } = true;

        /// <summary>
        ///     Whether modifier-only bindings are allowed.
        ///     是否允许仅 modifier 的绑定。
        /// </summary>
        public bool AllowModifierOnly { get; init; } = true;

        /// <summary>
        ///     Whether left/right modifier keys are distinguished.
        ///     是否区分左右 modifier 键。
        /// </summary>
        public bool DistinguishModifierSides { get; init; }

        /// <summary>
        ///     Whether this represents multi-binding mode.
        ///     是否表示多绑定模式。
        /// </summary>
        public bool Multiple { get; init; }
    }

    /// <summary>
    ///     Declares a choice entry (string or enum).
    ///     声明 choice 条目（string 或 enum）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsChoiceAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional string option values.
        ///     可选的字符串选项值。
        /// </summary>
        public string[]? Options { get; init; }

        /// <summary>
        ///     Optional labels parallel to <see cref="Options" />.
        ///     与 <see cref="Options" /> 一一对应的可选标签。
        /// </summary>
        public string[]? OptionLabels { get; init; }

        /// <summary>
        ///     Optional i18n keys parallel to <see cref="Options" />.
        ///     与 <see cref="Options" /> 一一对应的可选 i18n key。
        /// </summary>
        public string[]? OptionLabelKeys { get; init; }

        /// <summary>
        ///     Optional LocString table for option labels.
        ///     选项标签使用的可选 LocString table。
        /// </summary>
        public string? OptionLabelLocTable { get; init; }

        /// <summary>
        ///     Optional LocString keys parallel to <see cref="Options" />.
        ///     与 <see cref="Options" /> 一一对应的可选 LocString key。
        /// </summary>
        public string[]? OptionLabelLocKeys { get; init; }

        /// <summary>
        ///     Choice presentation mode.
        ///     选项展示模式。
        /// </summary>
        public ModSettingsChoicePresentation Presentation { get; init; } = ModSettingsChoicePresentation.Stepper;
    }

    /// <summary>
    ///     Declares a button action entry.
    ///     声明按钮动作条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsButtonAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional button text override.
        ///     可选的按钮文本覆盖。
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 i18n key。
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 LocString table。
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 LocString key。
        /// </summary>
        public string? ButtonTextLocKey { get; init; }

        /// <summary>
        ///     Button tone.
        ///     按钮色调。
        /// </summary>
        public ModSettingsButtonTone Tone { get; init; } = ModSettingsButtonTone.Normal;

        /// <summary>
        ///     Whether the target method expects host context.
        ///     目标方法是否需要 host context。
        /// </summary>
        public bool UseHostContext { get; init; }
    }

    /// <summary>
    ///     Declares a paragraph display entry.
    ///     声明段落显示条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsParagraphAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的 entry id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional static text override.
        ///     可选的静态文本覆盖。
        /// </summary>
        public string? Text { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Text" />.
        ///     <see cref="Text" /> 的可选 i18n key。
        /// </summary>
        public string? TextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Text" />.
        ///     <see cref="Text" /> 的可选 LocString 表。
        /// </summary>
        public string? TextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Text" />.
        ///     <see cref="Text" /> 的可选 LocString key。
        /// </summary>
        public string? TextLocKey { get; init; }

        /// <summary>
        ///     Optional max body height.
        ///     可选正文最大高度。
        /// </summary>
        public float MaxBodyHeight { get; init; }
    }

    /// <summary>
    ///     Declares a header display entry.
    ///     声明一个标题显示条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsHeaderAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     Declares an info-card display entry.
    ///     声明一个信息卡显示条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsInfoCardAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional card body text override.
        ///     可选的卡片正文文本覆盖。
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 i18n key。
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 LocString 表。
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 LocString key。
        /// </summary>
        public string? BodyLocKey { get; init; }
    }

    /// <summary>
    ///     Declares a runtime hotkey-summary display entry.
    ///     声明一个运行时热键摘要显示条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsRuntimeHotkeySummaryAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Optional body text override.
        ///     可选的正文文本覆盖。
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 i18n key。
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 LocString 表。
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="Body" />.
        ///     <see cref="Body" /> 的可选 LocString key。
        /// </summary>
        public string? BodyLocKey { get; init; }

        /// <summary>
        ///     Hotkey chips to display.
        ///     要显示的热键标签。
        /// </summary>
        public string[] Bindings { get; init; } = [];

        /// <summary>
        ///     Optional i18n keys parallel to <see cref="Bindings" />.
        ///     与 <see cref="Bindings" /> 并行的可选 i18n key。
        /// </summary>
        public string[]? BindingKeys { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="Bindings" />.
        ///     <see cref="Bindings" /> 的可选 LocString 表。
        /// </summary>
        public string? BindingLocTable { get; init; }

        /// <summary>
        ///     Optional LocString keys parallel to <see cref="Bindings" />.
        ///     与 <see cref="Bindings" /> 并行的可选 LocString key。
        /// </summary>
        public string[]? BindingLocKeys { get; init; }

        /// <summary>
        ///     Optional id suffix displayed in UI.
        ///     UI 中显示的可选 id 后缀。
        /// </summary>
        public string? IdSuffix { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="IdSuffix" />.
        ///     <see cref="IdSuffix" /> 的可选 i18n key。
        /// </summary>
        public string? IdSuffixKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="IdSuffix" />.
        ///     <see cref="IdSuffix" /> 的可选 LocString 表。
        /// </summary>
        public string? IdSuffixLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="IdSuffix" />.
        ///     <see cref="IdSuffix" /> 的可选 LocString key。
        /// </summary>
        public string? IdSuffixLocKey { get; init; }
    }

    /// <summary>
    ///     Declares an image display entry.
    ///     声明一个图片显示条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsImageAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Preview height in pixels.
        ///     预览高度（像素）。
        /// </summary>
        public float PreviewHeight { get; init; } = 160f;
    }

    /// <summary>
    ///     Declares a subpage navigation entry.
    ///     声明一个子页面导航条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsSubpageAttribute(string id, string sectionId, string targetPageId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     Destination page id.
        ///     目标页面 id。
        /// </summary>
        public string TargetPageId { get; } = targetPageId;

        /// <summary>
        ///     Optional button text override.
        ///     可选的按钮文本覆盖。
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     Optional i18n key for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 i18n key。
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     Optional LocString table for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 LocString 表。
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     Optional LocString key for <see cref="ButtonText" />.
        ///     <see cref="ButtonText" /> 的可选 LocString key。
        /// </summary>
        public string? ButtonTextLocKey { get; init; }
    }

    /// <summary>
    ///     Declares a custom-control entry.
    ///     声明一个自定义控件条目。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsCustomEntryAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     Stable entry id.
        ///     稳定的条目 id。
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     Target section id.
        ///     目标 section id。
        /// </summary>
        public string SectionId { get; } = sectionId;
    }
}
