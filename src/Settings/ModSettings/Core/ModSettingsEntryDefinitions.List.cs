using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Reorderable list editor for a bound list of <typeparamref name="TItem" /> with optional structured clipboard per
    ///     item.
    ///     用于 <typeparamref name="TItem" /> 绑定列表的可重排列表编辑器，每个条目可选支持结构化剪贴板。
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
        ///     List binding; wrapped with a list adapter when the inner binding is not already structured.
        ///     列表绑定；当内部绑定尚非结构化绑定时，会用列表适配器包装。
        /// </summary>
        public IModSettingsValueBinding<List<TItem>> Binding { get; } =
            binding is IStructuredModSettingsValueBinding<List<TItem>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List(itemDataAdapter));

        /// <summary>
        ///     Factory for a new row when Add is pressed.
        ///     按下 Add 时用于创建新行的工厂。
        /// </summary>
        public Func<TItem> CreateItem { get; } = createItem;

        /// <summary>
        ///     Row title resolver.
        ///     行标题解析器。
        /// </summary>
        public Func<TItem, ModSettingsText> ItemLabel { get; } = itemLabel;

        /// <summary>
        ///     Optional per-row description.
        ///     每行的可选描述。
        /// </summary>
        public Func<TItem, ModSettingsText?>? ItemDescription { get; } = itemDescription;

        /// <summary>
        ///     Custom editor for one row; when null, a default layout is used.
        ///     单行的自定义编辑器；为 null 时使用默认布局。
        /// </summary>
        public Func<ModSettingsListItemContext<TItem>, Control>? ItemEditorFactory { get; } = itemEditorFactory;

        /// <summary>
        ///     Adapter for item clipboard when not using JSON defaults.
        ///     不使用默认 JSON 行为时，用于条目剪贴板的适配器。
        /// </summary>
        public IStructuredModSettingsValueAdapter<TItem>? ItemDataAdapter { get; } = itemDataAdapter;

        /// <summary>
        ///     Localized label for the add button.
        ///     添加按钮的本地化标签。
        /// </summary>
        public ModSettingsText AddButtonText { get; } = addButtonText;

        /// <summary>
        ///     When true, each list item can collapse its detail editor body.
        ///     为 true 时，每个列表条目都可以折叠其详情编辑器正文。
        /// </summary>
        public bool CollapsibleItems { get; } = collapsibleItems;

        /// <summary>
        ///     Initial collapsed state when <see cref="CollapsibleItems" /> is true.
        ///     <see cref="CollapsibleItems" /> 为 true 时的初始折叠状态。
        /// </summary>
        public bool StartItemsCollapsed { get; } = startItemsCollapsed;

        /// <summary>
        ///     Optional factory for compact controls rendered in the always-visible item header.
        ///     可选工厂，用于在始终可见的条目标题栏中渲染紧凑控件。
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
    ///     Integer range slider with discrete steps.
    ///     带离散步进的整数范围滑条。
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
        ///     Backing binding for the integer value.
        ///     整数值的后端绑定。
        /// </summary>
        public IModSettingsValueBinding<int> Binding { get; } = binding;

        /// <summary>
        ///     Minimum value (inclusive).
        ///     最小值（含）。
        /// </summary>
        public int MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum value (inclusive).
        ///     最大值（含）。
        /// </summary>
        public int MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        ///     有效值之间的步进。
        /// </summary>
        public int Step { get; } = step;

        /// <summary>
        ///     Optional display formatter.
        ///     可选显示格式化器。
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
    ///     Navigation row that opens another registered settings page.
    ///     打开另一个已注册设置页的导航行。
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
        ///     Destination page id.
        ///     目标页面 id。
        /// </summary>
        public string TargetPageId { get; } = targetPageId;

        /// <summary>
        ///     Label shown on the navigation control.
        ///     导航控件上显示的标签。
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSubpageEntry(context, this);
        }
    }
}
