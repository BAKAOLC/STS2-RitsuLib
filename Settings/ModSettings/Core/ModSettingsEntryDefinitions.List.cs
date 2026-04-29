using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Reorderable list editor for a bound list of <typeparamref name="TItem" /> with optional structured clipboard per
    ///     item.
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
        /// </summary>
        public IModSettingsValueBinding<List<TItem>> Binding { get; } =
            binding is IStructuredModSettingsValueBinding<List<TItem>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List(itemDataAdapter));

        /// <summary>
        ///     Factory for a new row when Add is pressed.
        /// </summary>
        public Func<TItem> CreateItem { get; } = createItem;

        /// <summary>
        ///     Row title resolver.
        /// </summary>
        public Func<TItem, ModSettingsText> ItemLabel { get; } = itemLabel;

        /// <summary>
        ///     Optional per-row description.
        /// </summary>
        public Func<TItem, ModSettingsText?>? ItemDescription { get; } = itemDescription;

        /// <summary>
        ///     Custom editor for one row; when null, a default layout is used.
        /// </summary>
        public Func<ModSettingsListItemContext<TItem>, Control>? ItemEditorFactory { get; } = itemEditorFactory;

        /// <summary>
        ///     Adapter for item clipboard when not using JSON defaults.
        /// </summary>
        public IStructuredModSettingsValueAdapter<TItem>? ItemDataAdapter { get; } = itemDataAdapter;

        /// <summary>
        ///     Localized label for the add button.
        /// </summary>
        public ModSettingsText AddButtonText { get; } = addButtonText;

        /// <summary>
        ///     When true, each list item can collapse its detail editor body.
        /// </summary>
        public bool CollapsibleItems { get; } = collapsibleItems;

        /// <summary>
        ///     Initial collapsed state when <see cref="CollapsibleItems" /> is true.
        /// </summary>
        public bool StartItemsCollapsed { get; } = startItemsCollapsed;

        /// <summary>
        ///     Optional factory for compact controls rendered in the always-visible item header.
        /// </summary>
        public Func<ModSettingsListItemContext<TItem>, Control?>? ItemHeaderAccessoryFactory { get; } =
            itemHeaderAccessoryFactory;

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
    }

    /// <summary>
    ///     Integer range slider with discrete steps.
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
        /// </summary>
        public IModSettingsValueBinding<int> Binding { get; } = binding;

        /// <summary>
        ///     Minimum value (inclusive).
        /// </summary>
        public int MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum value (inclusive).
        /// </summary>
        public int MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        /// </summary>
        public int Step { get; } = step;

        /// <summary>
        ///     Optional display formatter.
        /// </summary>
        public Func<int, string>? ValueFormatter { get; } = valueFormatter;

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
    }

    /// <summary>
    ///     Navigation row that opens another registered settings page.
    /// </summary>
    public sealed class SubpageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        string targetPageId,
        ModSettingsText buttonText,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Destination page id.
        /// </summary>
        public string TargetPageId { get; } = targetPageId;

        /// <summary>
        ///     Label shown on the navigation control.
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSubpageEntry(context, this);
        }
    }
}
