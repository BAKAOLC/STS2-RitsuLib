using Godot;

namespace STS2RitsuLib.Settings
{
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
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<List<TItem>> Binding { get; } =
            binding is IStructuredModSettingsValueBinding<List<TItem>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List(itemDataAdapter));

        public Func<TItem> CreateItem { get; } = createItem;
        public Func<TItem, ModSettingsText> ItemLabel { get; } = itemLabel;
        public Func<TItem, ModSettingsText?>? ItemDescription { get; } = itemDescription;
        public Func<ModSettingsListItemContext<TItem>, Control>? ItemEditorFactory { get; } = itemEditorFactory;
        public IStructuredModSettingsValueAdapter<TItem>? ItemDataAdapter { get; } = itemDataAdapter;
        public ModSettingsText AddButtonText { get; } = addButtonText;

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
        public IModSettingsValueBinding<int> Binding { get; } = binding;
        public int MinValue { get; } = minValue;
        public int MaxValue { get; } = maxValue;
        public int Step { get; } = step;
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

    public sealed class SubpageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        string targetPageId,
        ModSettingsText buttonText,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public string TargetPageId { get; } = targetPageId;
        public ModSettingsText ButtonText { get; } = buttonText;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateSubpageEntry(context, this);
        }
    }
}
