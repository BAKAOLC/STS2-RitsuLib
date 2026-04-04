using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Base type for one settings row: stable <see cref="Id" />, label, optional description, and UI factory hook.
    /// </summary>
    public abstract class ModSettingsEntryDefinition
    {
        /// <summary>
        ///     Initializes <see cref="Id" />, <see cref="Label" />, and <see cref="Description" />.
        /// </summary>
        protected ModSettingsEntryDefinition(string id, ModSettingsText label, ModSettingsText? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(label);

            Id = id;
            Label = label;
            Description = description;
        }

        /// <summary>
        ///     Unique entry id within its section (used for chrome clipboard and anchors).
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Primary label or body text depending on entry kind.
        /// </summary>
        public ModSettingsText Label { get; }

        /// <summary>
        ///     Optional secondary description shown in the UI.
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     When non-null, the entry row is hidden while the predicate returns false (re-evaluated on UI refresh).
        /// </summary>
        public virtual Func<bool>? VisibilityPredicate => null;

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

    /// <summary>
    ///     Boolean on/off toggle bound to <see cref="Binding" />.
    /// </summary>
    public sealed class ToggleModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<bool> binding,
        ModSettingsText? description,
        Func<bool>? visibilityPredicate = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Backing binding for the toggle.
        /// </summary>
        public IModSettingsValueBinding<bool> Binding { get; } = binding;

        /// <inheritdoc />
        public override Func<bool>? VisibilityPredicate => visibilityPredicate;

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

    /// <summary>
    ///     Floating-point slider with range and optional formatter (<see cref="double" /> domain).
    /// </summary>
    public sealed class SliderModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<double> binding,
        double minValue,
        double maxValue,
        double step,
        Func<double, string>? valueFormatter,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Backing binding for the slider value.
        /// </summary>
        public IModSettingsValueBinding<double> Binding { get; } = binding;

        /// <summary>
        ///     Minimum slider value (inclusive).
        /// </summary>
        public double MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum slider value (inclusive).
        /// </summary>
        public double MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        /// </summary>
        public double Step { get; } = step;

        /// <summary>
        ///     Optional formatter for the displayed value string.
        /// </summary>
        public Func<double, string>? ValueFormatter { get; } = valueFormatter;

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

    /// <summary>
    ///     Internal <see cref="float" /> slider entry (legacy pipeline). Only produced by the obsolete
    ///     <c>ModSettingsSectionBuilder.AddSlider</c> overload taking <see cref="IModSettingsValueBinding{T}" /> of
    ///     <see cref="float" />; separate from <see cref="SliderModSettingsEntryDefinition" /> to avoid float/double
    ///     drift and refresh feedback loops.
    /// </summary>
    public sealed class FloatSliderModSettingsEntryDefinition(
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
        /// <summary>
        ///     Backing binding for the slider value.
        /// </summary>
        public IModSettingsValueBinding<float> Binding { get; } = binding;

        /// <summary>
        ///     Minimum slider value (inclusive).
        /// </summary>
        public float MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum slider value (inclusive).
        /// </summary>
        public float MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        /// </summary>
        public float Step { get; } = step;

        /// <summary>
        ///     Optional formatter for the displayed value string.
        /// </summary>
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
            return ModSettingsUiFactory.CreateFloatSliderEntry(context, this);
        }
    }

    /// <summary>
    ///     Discrete choice control over <typeparamref name="TValue" /> with fixed <see cref="Options" />.
    /// </summary>
    public sealed class ChoiceModSettingsEntryDefinition<TValue>(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<TValue> binding,
        IReadOnlyList<ModSettingsChoiceOption<TValue>> options,
        ModSettingsChoicePresentation presentation,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Backing binding for the selected option value.
        /// </summary>
        public IModSettingsValueBinding<TValue> Binding { get; } = binding;

        /// <summary>
        ///     Ordered choices shown in the UI.
        /// </summary>
        public IReadOnlyList<ModSettingsChoiceOption<TValue>> Options { get; } = options;

        /// <summary>
        ///     Visual presentation (stepper, dropdown, etc.).
        /// </summary>
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

    /// <summary>
    ///     Color picker bound to a string (e.g. hex or engine serialization).
    /// </summary>
    public sealed class ColorModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Backing binding for the color string.
        /// </summary>
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

    /// <summary>
    ///     Shared base for single-line and multiline string entries.
    /// </summary>
    public abstract class StringFieldModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? placeholder,
        int? maxLength,
        ModSettingsText? description) : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Backing binding for the text value.
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        /// <summary>
        ///     Placeholder shown when empty.
        /// </summary>
        public ModSettingsText? Placeholder { get; } = placeholder;

        /// <summary>
        ///     Maximum character count when set.
        /// </summary>
        public int? MaxLength { get; } = maxLength;

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
    }

    /// <summary>
    ///     Single-line text field.
    /// </summary>
    public sealed class StringModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? placeholder,
        int? maxLength,
        ModSettingsText? description)
        : StringFieldModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateStringLineEntry(context, this);
        }
    }

    /// <summary>
    ///     Multiline text field.
    /// </summary>
    public sealed class MultilineStringModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        ModSettingsText? placeholder,
        int? maxLength,
        ModSettingsText? description)
        : StringFieldModSettingsEntryDefinition(id, label, binding, placeholder, maxLength, description)
    {
        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateStringMultilineEntry(context, this);
        }
    }

    /// <summary>
    ///     Key binding capture row writing a string token to <see cref="Binding" />.
    /// </summary>
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
        /// <summary>
        ///     Backing binding for the serialized key string.
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        /// <summary>
        ///     When true, modifier+key combinations are allowed.
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     When true, modifier-only shortcuts are allowed.
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     When true, left/right modifier sides are distinguished.
        /// </summary>
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

    /// <summary>
    ///     Non-persisted button that invokes <see cref="Action" />.
    /// </summary>
    public sealed class ButtonModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText buttonText,
        Action action,
        ModSettingsButtonTone tone,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Caption on the button control.
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        /// <summary>
        ///     Callback when the button is activated.
        /// </summary>
        public Action Action { get; } = action;

        /// <summary>
        ///     Visual emphasis (normal, primary, danger).
        /// </summary>
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateButtonEntry(context, this);
        }
    }

    /// <summary>
    ///     Button that receives <see cref="IModSettingsUiActionHost" /> so callbacks can refresh the pane after async work
    ///     (e.g. native file dialogs).
    /// </summary>
    public sealed class HostContextButtonModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText buttonText,
        Action<IModSettingsUiActionHost> action,
        ModSettingsButtonTone tone,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Caption on the button control.
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        /// <summary>
        ///     Callback when the button is activated; use <see cref="IModSettingsUiActionHost.RequestRefresh" /> after
        ///     mutating bindings outside the control graph.
        /// </summary>
        public Action<IModSettingsUiActionHost> Action { get; } = action;

        /// <summary>
        ///     Visual emphasis (normal, primary, danger).
        /// </summary>
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateHostContextButtonEntry(context, this);
        }
    }

    /// <summary>
    ///     Section heading without a control.
    /// </summary>
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

    /// <summary>
    ///     Read-only rich text block; <see cref="ModSettingsEntryDefinition.Label" /> is the main body and
    ///     <see cref="ModSettingsEntryDefinition.Description" /> is an optional subtitle.
    /// </summary>
    public sealed class ParagraphModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description,
        float? maxBodyHeight = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Maximum height of the paragraph body before scrolling.
        /// </summary>
        public float? MaxBodyHeight { get; } = maxBodyHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateParagraphEntry(context, this);
        }
    }

    /// <summary>
    ///     Static image preview from <see cref="TextureProvider" />.
    /// </summary>
    public sealed class ImageModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        Func<Texture2D?> textureProvider,
        float previewHeight,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Lazy texture source for the preview.
        /// </summary>
        public Func<Texture2D?> TextureProvider { get; } = textureProvider;

        /// <summary>
        ///     Height of the preview area in pixels.
        /// </summary>
        public float PreviewHeight { get; } = previewHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateImageEntry(context, this);
        }
    }
}
