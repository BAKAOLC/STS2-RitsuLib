using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a reorderable editor for a bound list of <typeparamref name="TItem" /> values, with optional
    ///         structured clipboard support for each item.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <typeparamref name="TItem" /> 绑定列表的可重排编辑器，并可为各列表项提供结构化剪贴板支持。
    ///     </para>
    /// </summary>
    public sealed class ListModSettingsEntryDefinition<TItem>(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<List<TItem>> binding,
        Func<TItem> createItem,
        Func<TItem, ModSettingsText> itemLabel,
        Func<TItem, ModSettingsText?>? itemDescription,
        Func<ModSettingsListItemContext<TItem>, Control>? itemEditorFactory,
        IStructuredModSettingsValueAdapter<TItem>? itemDataAdapter,
        ModSettingsText addButtonText,
        ModSettingsText? description,
        bool collapsibleItems,
        bool startItemsCollapsed,
        Func<ModSettingsListItemContext<TItem>, Control?>? itemHeaderAccessoryFactory)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the list binding. A binding without structured data support is wrapped with a list adapter.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取列表绑定。不支持结构化数据的绑定会由列表适配器包装。</para>
        /// </summary>
        public IModSettingsValueBinding<List<TItem>> Binding { get; } =
            binding is null
                ? throw new ArgumentNullException(nameof(binding))
                : binding is IStructuredModSettingsValueBinding<List<TItem>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List(itemDataAdapter));

        /// <summary>
        ///     <para xml:lang="en">Gets the factory invoked to create an item when the add button is pressed.</para>
        ///     <para xml:lang="zh-CN">获取按下添加按钮时用于创建列表项的工厂。</para>
        /// </summary>
        public Func<TItem> CreateItem { get; } =
            createItem ?? throw new ArgumentNullException(nameof(createItem));

        /// <summary>
        ///     <para xml:lang="en">Gets the display-label resolver for each list item.</para>
        ///     <para xml:lang="zh-CN">获取各列表项的显示标签解析器。</para>
        /// </summary>
        public Func<TItem, ModSettingsText> ItemLabel { get; } =
            itemLabel ?? throw new ArgumentNullException(nameof(itemLabel));

        /// <summary>
        ///     <para xml:lang="en">Gets the optional display-description resolver for each list item.</para>
        ///     <para xml:lang="zh-CN">获取各列表项的可选显示说明解析器。</para>
        /// </summary>
        public Func<TItem, ModSettingsText?>? ItemDescription { get; } = itemDescription;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional factory for a list item's detail editor. When absent, the UI uses its default item
        ///         layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取列表项详情编辑器的可选工厂；未提供时，界面使用默认的列表项布局。
        ///     </para>
        /// </summary>
        public Func<ModSettingsListItemContext<TItem>, Control>? ItemEditorFactory { get; } = itemEditorFactory;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional structured-data adapter used for individual items. When absent, the list adapter
        ///         uses its JSON-based default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取单个列表项使用的可选结构化数据适配器；未提供时，列表适配器使用默认的 JSON 实现。
        ///     </para>
        /// </summary>
        public IStructuredModSettingsValueAdapter<TItem>? ItemDataAdapter { get; } = itemDataAdapter;

        /// <summary>
        ///     <para xml:lang="en">Gets the localized text displayed on the add button.</para>
        ///     <para xml:lang="zh-CN">获取添加按钮显示的本地化文本。</para>
        /// </summary>
        public ModSettingsText AddButtonText { get; } =
            addButtonText ?? throw new ArgumentNullException(nameof(addButtonText));

        /// <summary>
        ///     <para xml:lang="en">Gets whether each list item can collapse its detail editor.</para>
        ///     <para xml:lang="zh-CN">获取各列表项是否可以折叠其详情编辑器。</para>
        /// </summary>
        public bool CollapsibleItems { get; } = collapsibleItems;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether item detail editors initially start collapsed when <see cref="CollapsibleItems" /> is
        ///         enabled.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取启用 <see cref="CollapsibleItems" /> 时列表项详情编辑器是否初始折叠。
        ///     </para>
        /// </summary>
        public bool StartItemsCollapsed { get; } = startItemsCollapsed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional factory for compact controls placed in the always-visible item header.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取在始终可见的列表项标题栏中放置紧凑控件的可选工厂。</para>
        /// </summary>
        public Func<ModSettingsListItemContext<TItem>, Control?>? ItemHeaderAccessoryFactory { get; } =
            itemHeaderAccessoryFactory;

        internal override bool CanResetToDefault => BindingCanResetToDefault(Binding);

        internal override void CollectChromeBindingSnapshots(
            Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
            ModSettingsClipboardData.AddChromeBindingSnapshot(target, Id, Binding);
        }

        internal override bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            var adapter = ModSettingsUiFactory.ResolveClipboardAdapter(Binding);
            if (!ModSettingsClipboardData.TryApplySerializedValueToBinding(Binding, adapter, snap, out var v))
                return false;
            Binding.Write(v);
            host.MarkDirty(Binding);
            return true;
        }

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateListEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an integer slider over an inclusive range with discrete steps.</para>
    ///     <para xml:lang="zh-CN">定义在闭区间内按离散步长调整数值的整数滑块。</para>
    /// </summary>
    public sealed class IntSliderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<int> binding,
        int minValue,
        int maxValue,
        int step,
        Func<int, string>? valueFormatter,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the binding that stores the integer value.</para>
        ///     <para xml:lang="zh-CN">获取存储整数值的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<int> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive minimum value.</para>
        ///     <para xml:lang="zh-CN">获取闭区间的最小值。</para>
        /// </summary>
        public int MinValue { get; } = minValue;

        /// <summary>
        ///     <para xml:lang="en">Gets the inclusive maximum value.</para>
        ///     <para xml:lang="zh-CN">获取闭区间的最大值。</para>
        /// </summary>
        public int MaxValue { get; } =
            maxValue >= minValue
                ? maxValue
                : throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be >= minValue.");

        /// <summary>
        ///     <para xml:lang="en">Gets the positive step between selectable values.</para>
        ///     <para xml:lang="zh-CN">获取可选数值之间的正步长。</para>
        /// </summary>
        public int Step { get; } =
            step > 0
                ? step
                : throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be > 0.");

        /// <summary>
        ///     <para xml:lang="en">Gets the optional formatter for the displayed value.</para>
        ///     <para xml:lang="zh-CN">获取显示数值使用的可选格式化器。</para>
        /// </summary>
        public Func<int, string>? ValueFormatter { get; } = valueFormatter;

        internal override bool CanResetToDefault => BindingCanResetToDefault(Binding);

        internal override void CollectChromeBindingSnapshots(
            Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
            ModSettingsClipboardData.AddChromeBindingSnapshot(target, Id, Binding);
        }

        internal override bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            var adapter = ModSettingsUiFactory.ResolveClipboardAdapter(Binding);
            if (!ModSettingsClipboardData.TryApplySerializedValueToBinding(Binding, adapter, snap, out var v))
                return false;
            Binding.Write(v);
            host.MarkDirty(Binding);
            return true;
        }

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateIntSliderEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a navigation row that opens another registered settings page.</para>
    ///     <para xml:lang="zh-CN">定义用于打开另一个已注册设置页面的导航行。</para>
    /// </summary>
    public sealed class SubpageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        string targetPageId,
        ModSettingsText buttonText,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        internal override string VisibilityTargetPageId => TargetPageId;

        /// <summary>
        ///     <para xml:lang="en">Gets the destination page ID.</para>
        ///     <para xml:lang="zh-CN">获取目标页面 ID。</para>
        /// </summary>
        public string TargetPageId { get; } =
            !string.IsNullOrWhiteSpace(targetPageId)
                ? targetPageId
                : throw new ArgumentException("The target page ID cannot be null or whitespace.", nameof(targetPageId));

        /// <summary>
        ///     <para xml:lang="en">Gets the text displayed on the navigation control.</para>
        ///     <para xml:lang="zh-CN">获取导航控件显示的文本。</para>
        /// </summary>
        public ModSettingsText ButtonText { get; } =
            buttonText ?? throw new ArgumentNullException(nameof(buttonText));

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSubpageEntry(context, this);
        }
    }
}
