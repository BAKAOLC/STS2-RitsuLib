namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Specifies how a member exposed by the runtime-reflection mirror stores its value.</para>
    ///     <para xml:lang="zh-CN">指定运行时反射镜像所公开成员的值存储方式。</para>
    /// </summary>
    public enum ModSettingsReflectionBindingSource
    {
        /// <summary>
        ///     <para xml:lang="en">Uses the framework default, which currently behaves like <see cref="Global" />.</para>
        ///     <para xml:lang="zh-CN">使用框架默认方式；当前行为与 <see cref="Global" /> 相同。</para>
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     <para xml:lang="en">Persists the value in the mod's global data store.</para>
        ///     <para xml:lang="zh-CN">将值持久化到模组的全局数据存储中。</para>
        /// </summary>
        Global = 1,

        /// <summary>
        ///     <para xml:lang="en">Persists the value in the mod's profile-scoped data store.</para>
        ///     <para xml:lang="zh-CN">将值持久化到模组的玩家档案数据存储中。</para>
        /// </summary>
        Profile = 2,

        /// <summary>
        ///     <para xml:lang="en">Keeps the value in memory for the current process only.</para>
        ///     <para xml:lang="zh-CN">仅在当前进程的内存中保存值。</para>
        /// </summary>
        InMemory = 4,

        /// <summary>
        ///     <para xml:lang="en">Uses caller-provided read, write, and optional save callbacks.</para>
        ///     <para xml:lang="zh-CN">使用调用方提供的读取、写入及可选保存回调。</para>
        /// </summary>
        Callback = 5,

        /// <summary>
        ///     <para xml:lang="en">Projects this value from a parent value managed through callbacks.</para>
        ///     <para xml:lang="zh-CN">从由回调管理的父值中投影出此值。</para>
        /// </summary>
        Project = 6,
    }

    /// <summary>
    ///     <para xml:lang="en">Configures value binding for an annotated settings field or property.</para>
    ///     <para xml:lang="zh-CN">配置带设置特性的字段或属性所使用的值绑定。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsBindingAttribute : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the storage strategy used by the binding.</para>
        ///     <para xml:lang="zh-CN">获取绑定采用的存储方式。</para>
        /// </summary>
        public ModSettingsReflectionBindingSource Source { get; init; } = ModSettingsReflectionBindingSource.Auto;

        /// <summary>
        ///     <para xml:lang="en">Gets an optional data-key override; otherwise a key derived from the declaring member is used.</para>
        ///     <para xml:lang="zh-CN">获取可选的数据键覆盖；未指定时使用由声明成员生成的键。</para>
        /// </summary>
        public string? DataKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless read method used by <see cref="ModSettingsReflectionBindingSource.Callback" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModSettingsReflectionBindingSource.Callback" /> 使用的可选无参数读取方法名。</para>
        /// </summary>
        public string? ReadUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional single-parameter write method used by <see cref="ModSettingsReflectionBindingSource.Callback" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModSettingsReflectionBindingSource.Callback" /> 使用的可选单参数写入方法名。</para>
        /// </summary>
        public string? WriteUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless save method used by <see cref="ModSettingsReflectionBindingSource.Callback" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModSettingsReflectionBindingSource.Callback" /> 使用的可选无参数保存方法名。</para>
        /// </summary>
        public string? SaveUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless method that returns the binding's default value.</para>
        ///     <para xml:lang="zh-CN">获取返回绑定默认值的可选无参数方法名。</para>
        /// </summary>
        public string? DefaultUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless method that returns an <c>IStructuredModSettingsValueAdapter&lt;T&gt;</c>.</para>
        ///     <para xml:lang="zh-CN">获取返回 <c>IStructuredModSettingsValueAdapter&lt;T&gt;</c> 的可选无参数方法名。</para>
        /// </summary>
        public string? AdapterUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the required parameterless method that reads the parent value for a projected binding.</para>
        ///     <para xml:lang="zh-CN">获取投影绑定读取父值所需的无参数方法名。</para>
        /// </summary>
        public string? ProjectParentReadUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the required single-parameter method that writes the parent value for a projected binding.</para>
        ///     <para xml:lang="zh-CN">获取投影绑定写入父值所需的单参数方法名。</para>
        /// </summary>
        public string? ProjectParentWriteUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless method that saves the parent value for a projected binding.</para>
        ///     <para xml:lang="zh-CN">获取投影绑定保存父值所用的可选无参数方法名。</para>
        /// </summary>
        public string? ProjectParentSaveUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the required projection method with signature <c>TValue (TParent)</c>.</para>
        ///     <para xml:lang="zh-CN">获取签名为 <c>TValue (TParent)</c> 的必需投影读取方法名。</para>
        /// </summary>
        public string? ProjectGetUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the required projection method with signature <c>TParent (TParent, TValue)</c>.</para>
        ///     <para xml:lang="zh-CN">获取签名为 <c>TParent (TParent, TValue)</c> 的必需投影写入方法名。</para>
        /// </summary>
        public string? ProjectSetUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the projected child key; the annotated member name is used when omitted.</para>
        ///     <para xml:lang="zh-CN">获取投影子项的键；未指定时使用带特性的成员名。</para>
        /// </summary>
        public string? ProjectDataKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides localizable title and description metadata shared by reflection attributes.</para>
    ///     <para xml:lang="zh-CN">提供反射特性共用的可本地化标题与说明元数据。</para>
    /// </summary>
    public abstract class ModSettingsTitleDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless provider method that returns the <see cref="Utils.I18N" /> instance used by this attribute.</para>
        ///     <para xml:lang="zh-CN">获取为此特性返回 <see cref="Utils.I18N" /> 实例的可选无参数提供方法名。</para>
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal title or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的标题文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the title.</para>
        ///     <para xml:lang="zh-CN">获取标题使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? TitleKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="TitleLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="TitleLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? TitleLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the title.</para>
        ///     <para xml:lang="zh-CN">获取标题使用的可选游戏本地化键。</para>
        /// </summary>
        public string? TitleLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal description or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的说明文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the description.</para>
        ///     <para xml:lang="zh-CN">获取说明使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="DescriptionLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="DescriptionLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the description.</para>
        ///     <para xml:lang="zh-CN">获取说明使用的可选游戏本地化键。</para>
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides localizable label and description metadata shared by reflection entry attributes.</para>
    ///     <para xml:lang="zh-CN">提供反射条目特性共用的可本地化标签与说明元数据。</para>
    /// </summary>
    public abstract class ModSettingsLabelDescriptionTextAttribute : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless provider method that returns the <see cref="Utils.I18N" /> instance used by this attribute.</para>
        ///     <para xml:lang="zh-CN">获取为此特性返回 <see cref="Utils.I18N" /> 实例的可选无参数提供方法名。</para>
        /// </summary>
        public string? I18NProviderUsing { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal label or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的标签文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the label.</para>
        ///     <para xml:lang="zh-CN">获取标签使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? LabelKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="LabelLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="LabelLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? LabelLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the label.</para>
        ///     <para xml:lang="zh-CN">获取标签使用的可选游戏本地化键。</para>
        /// </summary>
        public string? LabelLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal description or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的说明文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the description.</para>
        ///     <para xml:lang="zh-CN">获取说明使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? DescriptionKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="DescriptionLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="DescriptionLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? DescriptionLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the description.</para>
        ///     <para xml:lang="zh-CN">获取说明使用的可选游戏本地化键。</para>
        /// </summary>
        public string? DescriptionLocKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Adds ordering and conditional visibility metadata shared by reflection entry attributes.</para>
    ///     <para xml:lang="zh-CN">提供反射条目特性共用的排序与条件可见性元数据。</para>
    /// </summary>
    public abstract class ModSettingsOrderedEntryAttribute : ModSettingsLabelDescriptionTextAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the entry's ascending sort order within its section.</para>
        ///     <para xml:lang="zh-CN">获取条目在所属节内按升序排列时的顺序值。</para>
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parameterless Boolean method that determines whether the entry is visible.</para>
        ///     <para xml:lang="zh-CN">获取用于判断条目是否可见的可选无参数布尔方法名。</para>
        /// </summary>
        public string? VisibleWhen { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Marks a class as a runtime-reflection settings page provider.</para>
    ///     <para xml:lang="zh-CN">将类标记为运行时反射设置页面提供程序。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModSettingsPageAttribute(string modId, string? pageId = null)
        : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns the page.</para>
        ///     <para xml:lang="zh-CN">获取此页面所属模组的 ID。</para>
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     <para xml:lang="en">Gets the stable page ID; a null, empty, or whitespace value uses <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取稳定的页面 ID；值为 <see langword="null" />、空字符串或空白时使用 <see cref="ModId" />。</para>
        /// </summary>
        public string? PageId { get; } = pageId;

        /// <summary>
        ///     <para xml:lang="en">Gets the page's ascending sort order among sibling pages.</para>
        ///     <para xml:lang="zh-CN">获取页面在同级页面中按升序排列时的顺序值。</para>
        /// </summary>
        public int SortOrder { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional parent page ID used for nested navigation.</para>
        ///     <para xml:lang="zh-CN">获取用于嵌套导航的可选父页面 ID。</para>
        /// </summary>
        public string? ParentPageId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal mod name or localization fallback shown in the sidebar.</para>
        ///     <para xml:lang="zh-CN">获取侧边栏中显示的可选模组名称；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? ModDisplayName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the sidebar mod name.</para>
        ///     <para xml:lang="zh-CN">获取侧边栏模组名称使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? ModDisplayNameKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="ModDisplayNameLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="ModDisplayNameLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? ModDisplayNameLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the sidebar mod name.</para>
        ///     <para xml:lang="zh-CN">获取侧边栏模组名称使用的可选游戏本地化键。</para>
        /// </summary>
        public string? ModDisplayNameLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets an optional ascending sort order for the mod's sidebar group.</para>
        ///     <para xml:lang="zh-CN">获取模组侧边栏分组按升序排列时的可选顺序值。</para>
        /// </summary>
        public int? ModSidebarOrder { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Declares a section on a runtime-reflection settings page.</para>
    ///     <para xml:lang="zh-CN">声明运行时反射设置页面中的一个节。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ModSettingsSectionAttribute(string id) : ModSettingsTitleDescriptionTextAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable section ID referenced by entry attributes.</para>
        ///     <para xml:lang="zh-CN">获取供条目特性引用的稳定节 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the section can be collapsed.</para>
        ///     <para xml:lang="zh-CN">获取此节是否可折叠。</para>
        /// </summary>
        public bool IsCollapsible { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether a collapsible section starts collapsed.</para>
        ///     <para xml:lang="zh-CN">获取可折叠节初始时是否处于折叠状态。</para>
        /// </summary>
        public bool StartCollapsed { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the section's ascending sort order on the page.</para>
        ///     <para xml:lang="zh-CN">获取节在页面中按升序排列时的顺序值。</para>
        /// </summary>
        public int SortOrder { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a Boolean field or property as a toggle entry.</para>
    ///     <para xml:lang="zh-CN">将布尔字段或属性公开为开关条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsToggleAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a <see cref="double" /> or <see cref="float" /> field or property as a slider entry.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="double" /> 或 <see cref="float" /> 字段或属性公开为滑块条目。</para>
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
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive minimum slider value.</para>
        ///     <para xml:lang="zh-CN">获取滑块可取的最小值（含该值）。</para>
        /// </summary>
        public double Min { get; } = min;

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive maximum slider value.</para>
        ///     <para xml:lang="zh-CN">获取滑块可取的最大值（含该值）。</para>
        /// </summary>
        public double Max { get; } = max;

        /// <summary>
        ///     <para xml:lang="en">Gets the positive increment between slider values.</para>
        ///     <para xml:lang="zh-CN">获取滑块值之间的正数步长。</para>
        /// </summary>
        public double Step { get; } = step;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes an <see cref="int" /> field or property as a slider entry.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="int" /> 字段或属性公开为滑块条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsIntSliderAttribute(string id, string sectionId, int min, int max, int step = 1)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive minimum slider value.</para>
        ///     <para xml:lang="zh-CN">获取滑块可取的最小值（含该值）。</para>
        /// </summary>
        public int Min { get; } = min;

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive maximum slider value.</para>
        ///     <para xml:lang="zh-CN">获取滑块可取的最大值（含该值）。</para>
        /// </summary>
        public int Max { get; } = max;

        /// <summary>
        ///     <para xml:lang="en">Gets the positive increment between slider values.</para>
        ///     <para xml:lang="zh-CN">获取滑块值之间的正数步长。</para>
        /// </summary>
        public int Step { get; } = step;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a <see cref="string" /> field or property as a single-line text entry.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="string" /> 字段或属性公开为单行文本条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsStringAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal placeholder or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的占位文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the placeholder.</para>
        ///     <para xml:lang="zh-CN">获取占位文本使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="PlaceholderLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="PlaceholderLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the placeholder.</para>
        ///     <para xml:lang="zh-CN">获取占位文本使用的可选游戏本地化键。</para>
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the maximum character count; values less than or equal to zero leave the length unrestricted.</para>
        ///     <para xml:lang="zh-CN">获取最大字符数；值小于或等于零时不限制长度。</para>
        /// </summary>
        public int MaxLength { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional method with signature <c>bool (string)</c> used to validate the current text.</para>
        ///     <para xml:lang="zh-CN">获取用于校验当前文本、签名为 <c>bool (string)</c> 的可选方法名。</para>
        /// </summary>
        public string? ValidateUsing { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a <see cref="string" /> field or property as a multiline text entry.</para>
    ///     <para xml:lang="zh-CN">将 <see cref="string" /> 字段或属性公开为多行文本条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsMultilineStringAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal placeholder or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的占位文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? Placeholder { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the placeholder.</para>
        ///     <para xml:lang="zh-CN">获取占位文本使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? PlaceholderKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="PlaceholderLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="PlaceholderLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? PlaceholderLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the placeholder.</para>
        ///     <para xml:lang="zh-CN">获取占位文本使用的可选游戏本地化键。</para>
        /// </summary>
        public string? PlaceholderLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the maximum character count; values less than or equal to zero leave the length unrestricted.</para>
        ///     <para xml:lang="zh-CN">获取最大字符数；值小于或等于零时不限制长度。</para>
        /// </summary>
        public int MaxLength { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a color string field or property as a color-picker entry.</para>
    ///     <para xml:lang="zh-CN">将表示颜色的字符串字段或属性公开为颜色选择器条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsColorAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the picker allows editing the alpha channel.</para>
        ///     <para xml:lang="zh-CN">获取颜色选择器是否允许编辑 Alpha 通道。</para>
        /// </summary>
        public bool EditAlpha { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the picker allows HDR intensity values.</para>
        ///     <para xml:lang="zh-CN">获取颜色选择器是否允许使用 HDR 强度值。</para>
        /// </summary>
        public bool EditIntensity { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a string or string-list field or property as a key-binding entry.</para>
    ///     <para xml:lang="zh-CN">将字符串或字符串列表字段或属性公开为按键绑定条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsKeyBindingAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets whether bindings may combine a modifier with another key.</para>
        ///     <para xml:lang="zh-CN">获取绑定是否可由修饰键与其他按键组合而成。</para>
        /// </summary>
        public bool AllowModifierCombos { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a modifier key alone is accepted as a binding.</para>
        ///     <para xml:lang="zh-CN">获取是否允许仅使用修饰键作为绑定。</para>
        /// </summary>
        public bool AllowModifierOnly { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether left and right variants of modifier keys are distinct.</para>
        ///     <para xml:lang="zh-CN">获取是否区分修饰键的左侧与右侧版本。</para>
        /// </summary>
        public bool DistinguishModifierSides { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the annotated member contains multiple bindings and must be a <see cref="List{T}" /> of strings.</para>
        ///     <para xml:lang="zh-CN">获取带特性的成员是否包含多个绑定；启用时成员必须为字符串 <see cref="List{T}" />。</para>
        /// </summary>
        public bool Multiple { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a string or enumeration field or property as a choice entry.</para>
    ///     <para xml:lang="zh-CN">将字符串或枚举字段或属性公开为选项条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ModSettingsChoiceAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the available values for a string-backed choice; enumeration choices derive their values from the enum type.</para>
        ///     <para xml:lang="zh-CN">获取字符串选项可用的值；枚举选项会从枚举类型中生成可用值。</para>
        /// </summary>
        public string[]? Options { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets optional literal labels or localization fallbacks indexed in parallel with string-backed <see cref="Options" />.</para>
        ///     <para xml:lang="zh-CN">获取按字符串 <see cref="Options" /> 索引对应的可选标签文本；使用本地化来源时作为回退文本。</para>
        /// </summary>
        public string[]? OptionLabels { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets optional <see cref="Utils.I18N" /> keys indexed in parallel with string-backed <see cref="Options" />.</para>
        ///     <para xml:lang="zh-CN">获取按字符串 <see cref="Options" /> 索引对应的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string[]? OptionLabelKeys { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="OptionLabelLocKeys" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="OptionLabelLocKeys" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? OptionLabelLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets optional game localization keys indexed in parallel with string-backed <see cref="Options" />.</para>
        ///     <para xml:lang="zh-CN">获取按字符串 <see cref="Options" /> 索引对应的可选游戏本地化键。</para>
        /// </summary>
        public string[]? OptionLabelLocKeys { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the control style used to present the available choices.</para>
        ///     <para xml:lang="zh-CN">获取用于展示可用选项的控件样式。</para>
        /// </summary>
        public ModSettingsChoicePresentation Presentation { get; init; } = ModSettingsChoicePresentation.Stepper;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as a settings action button.</para>
    ///     <para xml:lang="zh-CN">将方法公开为设置操作按钮。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsButtonAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal button label or localization fallback; the method name is used when omitted.</para>
        ///     <para xml:lang="zh-CN">获取可选的按钮标签文本或本地化回退文本；未指定时使用方法名。</para>
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the button label.</para>
        ///     <para xml:lang="zh-CN">获取按钮标签使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="ButtonTextLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="ButtonTextLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the button label.</para>
        ///     <para xml:lang="zh-CN">获取按钮标签使用的可选游戏本地化键。</para>
        /// </summary>
        public string? ButtonTextLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the visual tone applied to the button.</para>
        ///     <para xml:lang="zh-CN">获取按钮采用的视觉色调。</para>
        /// </summary>
        public ModSettingsButtonTone Tone { get; init; } = ModSettingsButtonTone.Normal;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the action supplies the current <see cref="IModSettingsUiActionHost" />, allowing the method to accept it as its sole parameter.</para>
        ///     <para xml:lang="zh-CN">获取操作是否提供当前 <see cref="IModSettingsUiActionHost" />，从而允许方法将其作为唯一参数。</para>
        /// </summary>
        public bool UseHostContext { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as a paragraph display entry.</para>
    ///     <para xml:lang="zh-CN">将方法公开为段落显示条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsParagraphAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets optional literal paragraph text or a localization fallback; without configured text or a key, the annotated method supplies dynamic text.</para>
        ///     <para xml:lang="zh-CN">获取可选的段落文本或本地化回退文本；未配置文本或键时，由带特性的方法动态提供文本。</para>
        /// </summary>
        public string? Text { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the paragraph text.</para>
        ///     <para xml:lang="zh-CN">获取段落文本使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? TextKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="TextLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="TextLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? TextLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the paragraph text.</para>
        ///     <para xml:lang="zh-CN">获取段落文本使用的可选游戏本地化键。</para>
        /// </summary>
        public string? TextLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the maximum body height in pixels; values less than or equal to zero leave the height unrestricted.</para>
        ///     <para xml:lang="zh-CN">获取正文最大高度（像素）；值小于或等于零时不限制高度。</para>
        /// </summary>
        public float MaxBodyHeight { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as a header display entry.</para>
    ///     <para xml:lang="zh-CN">将方法公开为标题显示条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsHeaderAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as an information-card display entry.</para>
    ///     <para xml:lang="zh-CN">将方法公开为信息卡显示条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsInfoCardAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets optional literal body text or a localization fallback; without configured text or a key, the annotated method supplies dynamic text.</para>
        ///     <para xml:lang="zh-CN">获取可选的正文文本或本地化回退文本；未配置文本或键时，由带特性的方法动态提供文本。</para>
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the card body.</para>
        ///     <para xml:lang="zh-CN">获取信息卡正文使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="BodyLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="BodyLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the card body.</para>
        ///     <para xml:lang="zh-CN">获取信息卡正文使用的可选游戏本地化键。</para>
        /// </summary>
        public string? BodyLocKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as a runtime hotkey-summary display entry.</para>
    ///     <para xml:lang="zh-CN">将方法公开为运行时热键摘要显示条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsRuntimeHotkeySummaryAttribute(string id, string sectionId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets optional literal body text or a localization fallback; without configured text or a key, the annotated method supplies dynamic text.</para>
        ///     <para xml:lang="zh-CN">获取可选的正文文本或本地化回退文本；未配置文本或键时，由带特性的方法动态提供文本。</para>
        /// </summary>
        public string? Body { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the body text.</para>
        ///     <para xml:lang="zh-CN">获取正文文本使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? BodyKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="BodyLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="BodyLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? BodyLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the body text.</para>
        ///     <para xml:lang="zh-CN">获取正文文本使用的可选游戏本地化键。</para>
        /// </summary>
        public string? BodyLocKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the literal hotkey labels, also used as localization fallbacks.</para>
        ///     <para xml:lang="zh-CN">获取要显示的热键标签文本；使用本地化来源时也作为回退文本。</para>
        /// </summary>
        public string[] Bindings { get; init; } = [];

        /// <summary>
        ///     <para xml:lang="en">Gets optional <see cref="Utils.I18N" /> keys indexed in parallel with <see cref="Bindings" />.</para>
        ///     <para xml:lang="zh-CN">获取按 <see cref="Bindings" /> 索引对应的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string[]? BindingKeys { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="BindingLocKeys" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="BindingLocKeys" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? BindingLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets optional game localization keys indexed in parallel with <see cref="Bindings" />.</para>
        ///     <para xml:lang="zh-CN">获取按 <see cref="Bindings" /> 索引对应的可选游戏本地化键。</para>
        /// </summary>
        public string[]? BindingLocKeys { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal identifier suffix or localization fallback displayed by the entry.</para>
        ///     <para xml:lang="zh-CN">获取条目显示的可选标识符后缀文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? IdSuffix { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the identifier suffix.</para>
        ///     <para xml:lang="zh-CN">获取标识符后缀使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? IdSuffixKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="IdSuffixLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="IdSuffixLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? IdSuffixLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the identifier suffix.</para>
        ///     <para xml:lang="zh-CN">获取标识符后缀使用的可选游戏本地化键。</para>
        /// </summary>
        public string? IdSuffixLocKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a parameterless method returning a Godot texture as an image entry.</para>
    ///     <para xml:lang="zh-CN">将返回 Godot 纹理的无参数方法公开为图像条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsImageAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the positive, finite preview height in pixels.</para>
        ///     <para xml:lang="zh-CN">获取以像素为单位的有限正数预览高度。</para>
        /// </summary>
        public float PreviewHeight { get; init; } = 160f;
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method as a navigation entry that opens another settings page.</para>
    ///     <para xml:lang="zh-CN">将方法公开为打开另一设置页面的导航条目。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsSubpageAttribute(string id, string sectionId, string targetPageId)
        : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the destination settings page.</para>
        ///     <para xml:lang="zh-CN">获取目标设置页面的 ID。</para>
        /// </summary>
        public string TargetPageId { get; } = targetPageId;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional literal navigation-button label or localization fallback.</para>
        ///     <para xml:lang="zh-CN">获取可选的导航按钮标签文本；使用本地化来源时该值作为回退文本。</para>
        /// </summary>
        public string? ButtonText { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional <see cref="Utils.I18N" /> key for the navigation-button label.</para>
        ///     <para xml:lang="zh-CN">获取导航按钮标签使用的可选 <see cref="Utils.I18N" /> 键。</para>
        /// </summary>
        public string? ButtonTextKey { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization table used with <see cref="ButtonTextLocKey" />.</para>
        ///     <para xml:lang="zh-CN">获取与 <see cref="ButtonTextLocKey" /> 配合使用的可选游戏本地化表。</para>
        /// </summary>
        public string? ButtonTextLocTable { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional game localization key for the navigation-button label.</para>
        ///     <para xml:lang="zh-CN">获取导航按钮标签使用的可选游戏本地化键。</para>
        /// </summary>
        public string? ButtonTextLocKey { get; init; }
    }

    /// <summary>
    ///     <para xml:lang="en">Exposes a method returning a Godot control as a custom settings-control factory; the method may optionally accept an <see cref="IModSettingsUiActionHost" />.</para>
    ///     <para xml:lang="zh-CN">将返回 Godot 控件的方法公开为自定义设置控件工厂；该方法可以选择接收一个 <see cref="IModSettingsUiActionHost" />。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModSettingsCustomEntryAttribute(string id, string sectionId) : ModSettingsOrderedEntryAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable entry ID.</para>
        ///     <para xml:lang="zh-CN">获取稳定的条目 ID。</para>
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the section that contains the entry.</para>
        ///     <para xml:lang="zh-CN">获取容纳此条目的节 ID。</para>
        /// </summary>
        public string SectionId { get; } = sectionId;
    }
}
