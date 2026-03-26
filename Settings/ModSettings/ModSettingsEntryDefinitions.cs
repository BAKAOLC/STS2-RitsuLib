using Godot;

namespace STS2RitsuLib.Settings
{
    public abstract class ModSettingsEntryDefinition
    {
        protected ModSettingsEntryDefinition(string id, ModSettingsText label, ModSettingsText? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(label);

            Id = id;
            Label = label;
            Description = description;
        }

        public string Id { get; }
        public ModSettingsText Label { get; }
        public ModSettingsText? Description { get; }

        internal abstract Control CreateControl(ModSettingsUiContext context);

        internal virtual void CollectChromeBindingSnapshots(Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
        }

        internal virtual bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            return false;
        }
    }

    public sealed class ToggleModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<bool> binding,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<bool> Binding { get; } = binding;

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
            return ModSettingsUiFactory.CreateToggleEntry(context, this);
        }
    }

    public sealed class SliderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<float> binding,
        float minValue,
        float maxValue,
        float step,
        Func<float, string>? valueFormatter,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<float> Binding { get; } = binding;
        public float MinValue { get; } = minValue;
        public float MaxValue { get; } = maxValue;
        public float Step { get; } = step;
        public Func<float, string>? ValueFormatter { get; } = valueFormatter;

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
            return ModSettingsUiFactory.CreateSliderEntry(context, this);
        }
    }

    public sealed class ChoiceModSettingsEntryDefinition<TValue>(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<TValue> binding,
        IReadOnlyList<ModSettingsChoiceOption<TValue>> options,
        ModSettingsChoicePresentation presentation,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<TValue> Binding { get; } = binding;
        public IReadOnlyList<ModSettingsChoiceOption<TValue>> Options { get; } = options;
        public ModSettingsChoicePresentation Presentation { get; } = presentation;

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
            return ModSettingsUiFactory.CreateChoiceEntry(context, this);
        }
    }

    public sealed class ColorModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<string> Binding { get; } = binding;

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
            return ModSettingsUiFactory.CreateColorEntry(context, this);
        }
    }

    public sealed class KeyBindingModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        bool allowModifierCombos,
        bool allowModifierOnly,
        bool distinguishModifierSides,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public IModSettingsValueBinding<string> Binding { get; } = binding;
        public bool AllowModifierCombos { get; } = allowModifierCombos;
        public bool AllowModifierOnly { get; } = allowModifierOnly;
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

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
            return ModSettingsUiFactory.CreateKeyBindingEntry(context, this);
        }
    }

    public sealed class ButtonModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText buttonText,
        Action action,
        ModSettingsButtonTone tone,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public ModSettingsText ButtonText { get; } = buttonText;
        public Action Action { get; } = action;
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateButtonEntry(context, this);
        }
    }

    public sealed class HeaderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateHeaderEntry(context, this);
        }
    }

    public sealed class ParagraphModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateParagraphEntry(context, this);
        }
    }

    public sealed class ImageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        Func<Texture2D?> textureProvider,
        float previewHeight,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        public Func<Texture2D?> TextureProvider { get; } = textureProvider;
        public float PreviewHeight { get; } = previewHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateImageEntry(context, this);
        }
    }
}
