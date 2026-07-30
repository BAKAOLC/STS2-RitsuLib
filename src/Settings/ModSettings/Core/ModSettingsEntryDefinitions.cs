using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the identity, display text, availability, menu capabilities, and UI construction contract for
    ///         one settings row.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义单个设置行的标识、显示文本、可用状态、菜单能力及界面构建契约。</para>
    /// </summary>
    public abstract class ModSettingsEntryDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes an entry definition with a stable ID, primary label, and optional description.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用稳定 ID、主要标签及可选说明初始化条目定义。</para>
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
        ///     <para xml:lang="en">Gets the actions exposed by this entry's context menu.</para>
        ///     <para xml:lang="zh-CN">获取此条目上下文菜单公开的操作。</para>
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; internal set; } = ModSettingsMenuCapabilities.All;

        /// <summary>
        ///     <para xml:lang="en">Gets the host surfaces on which this entry's interactive controls are read-only.</para>
        ///     <para xml:lang="zh-CN">获取此条目的交互控件处于只读状态的宿主界面。</para>
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; internal set; } = ModSettingsHostSurface.None;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the entry ID, which is unique within its section and is used by clipboard snapshots and UI
        ///         anchors.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取条目 ID；该 ID 在所属节内唯一，并用于剪贴板快照及界面锚点。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the primary label or body text, depending on the entry type.</para>
        ///     <para xml:lang="zh-CN">获取主要标签或正文文本，具体含义取决于条目类型。</para>
        /// </summary>
        public ModSettingsText Label { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional secondary description displayed by the UI.</para>
        ///     <para xml:lang="zh-CN">获取界面显示的可选次级说明。</para>
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional visibility predicate. It is re-evaluated on UI refresh, and a false result hides
        ///         the row.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的可见性谓词；界面刷新时会重新求值，结果为 false 时隐藏该行。</para>
        /// </summary>
        public virtual Func<bool>? VisibilityPredicate => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional enabled-state predicate. It is re-evaluated on UI refresh, and a false result
        ///         dims the row and disables interaction.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的启用状态谓词；界面刷新时会重新求值，结果为 false 时该行会变暗且不可交互。
        ///     </para>
        /// </summary>
        public virtual Func<bool>? EnabledPredicate => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional destination page whose visibility also controls this entry. Entry decorators must
        ///         preserve this value.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取可选的目标页面；该页面的可见性也会控制此条目，且条目装饰器必须保留该值。</para>
        /// </summary>
        internal virtual string? VisibilityTargetPageId => null;

        internal virtual bool CanResetToDefault => false;

        internal abstract Control CreateControl(ModSettingsUiContext context);

        internal virtual void CollectChromeBindingSnapshots(Dictionary<string, ModSettingsChromeBindingSnapshot> target)
        {
        }

        internal virtual bool TryPasteChromeBindingSnapshot(ModSettingsChromeBindingSnapshot snap,
            IModSettingsUiActionHost host)
        {
            return false;
        }

        internal virtual bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return false;
        }

        private protected static bool BindingCanResetToDefault<TValue>(IModSettingsValueBinding<TValue> binding)
        {
            return binding is IDefaultModSettingsValueBinding<TValue>;
        }

        private protected static bool TryResetBindingToDefault<TValue>(IModSettingsValueBinding<TValue> binding,
            IModSettingsUiActionHost host)
        {
            if (binding is not IDefaultModSettingsValueBinding<TValue> defaults)
                return false;

            binding.Write(defaults.CreateDefaultValue());
            host.MarkDirty(binding);
            return true;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an on/off toggle backed by <see cref="Binding" />.</para>
    ///     <para xml:lang="zh-CN">定义由 <see cref="Binding" /> 支持的开关控件。</para>
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
        ///     <para xml:lang="en">Gets the binding that stores the toggle state.</para>
        ///     <para xml:lang="zh-CN">获取存储开关状态的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<bool> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <inheritdoc />
        public override Func<bool>? VisibilityPredicate => visibilityPredicate;

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
            return ModSettingsUiFactory.CreateToggleEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a <see cref="double" /> slider over an inclusive range with a positive step and an optional
    ///         display formatter.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义在闭区间内按正步长调整数值的 <see cref="double" /> 滑块，并可指定显示格式化器。</para>
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
        ///     <para xml:lang="en">Gets the binding that stores the slider value.</para>
        ///     <para xml:lang="zh-CN">获取存储滑块数值的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<double> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets the finite inclusive minimum value.</para>
        ///     <para xml:lang="zh-CN">获取有限的闭区间最小值。</para>
        /// </summary>
        public double MinValue { get; } =
            double.IsFinite(minValue)
                ? minValue
                : throw new ArgumentOutOfRangeException(nameof(minValue), "Slider minValue must be finite.");

        /// <summary>
        ///     <para xml:lang="en">Gets the finite inclusive maximum value.</para>
        ///     <para xml:lang="zh-CN">获取有限的闭区间最大值。</para>
        /// </summary>
        public double MaxValue { get; } =
            !double.IsFinite(maxValue)
                ? throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be finite.")
                : maxValue >= minValue
                    ? maxValue
                    : throw new ArgumentOutOfRangeException(nameof(maxValue),
                        "Slider maxValue must be >= minValue.");

        /// <summary>
        ///     <para xml:lang="en">Gets the finite positive step between selectable values.</para>
        ///     <para xml:lang="zh-CN">获取可选数值之间有限的正步长。</para>
        /// </summary>
        public double Step { get; } =
            double.IsFinite(step) && step > 0d
                ? step
                : throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be finite and > 0.");

        /// <summary>
        ///     <para xml:lang="en">Gets the optional formatter for the displayed value.</para>
        ///     <para xml:lang="zh-CN">获取显示数值使用的可选格式化器。</para>
        /// </summary>
        public Func<double, string>? ValueFormatter { get; } = valueFormatter;

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
            return ModSettingsUiFactory.CreateSliderEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the legacy <see cref="float" /> slider used by the obsolete
    ///         <c>ModSettingsSectionBuilder.AddSlider</c> overload. Its separate value path avoids float-to-double
    ///         conversion drift and refresh feedback loops.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义已过时 <c>ModSettingsSectionBuilder.AddSlider</c> 重载使用的旧版 <see cref="float" />
    ///         滑块。独立的数值路径可避免 float 与 double 转换造成的偏差及刷新反馈循环。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the binding that stores the slider value.</para>
        ///     <para xml:lang="zh-CN">获取存储滑块数值的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<float> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets the finite inclusive minimum value.</para>
        ///     <para xml:lang="zh-CN">获取有限的闭区间最小值。</para>
        /// </summary>
        public float MinValue { get; } =
            float.IsFinite(minValue)
                ? minValue
                : throw new ArgumentOutOfRangeException(nameof(minValue), "Slider minValue must be finite.");

        /// <summary>
        ///     <para xml:lang="en">Gets the finite inclusive maximum value.</para>
        ///     <para xml:lang="zh-CN">获取有限的闭区间最大值。</para>
        /// </summary>
        public float MaxValue { get; } =
            !float.IsFinite(maxValue)
                ? throw new ArgumentOutOfRangeException(nameof(maxValue), "Slider maxValue must be finite.")
                : maxValue >= minValue
                    ? maxValue
                    : throw new ArgumentOutOfRangeException(nameof(maxValue),
                        "Slider maxValue must be >= minValue.");

        /// <summary>
        ///     <para xml:lang="en">Gets the finite positive step between selectable values.</para>
        ///     <para xml:lang="zh-CN">获取可选数值之间有限的正步长。</para>
        /// </summary>
        public float Step { get; } =
            float.IsFinite(step) && step > 0f
                ? step
                : throw new ArgumentOutOfRangeException(nameof(step), "Slider step must be finite and > 0.");

        /// <summary>
        ///     <para xml:lang="en">Gets the optional formatter for the displayed value.</para>
        ///     <para xml:lang="zh-CN">获取显示数值使用的可选格式化器。</para>
        /// </summary>
        public Func<float, string>? ValueFormatter { get; } = valueFormatter;

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
            return ModSettingsUiFactory.CreateFloatSliderEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a discrete choice control over <typeparamref name="TValue" /> using either fixed
    ///         <see cref="Options" /> or a dynamic <see cref="OptionsProvider" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <typeparamref name="TValue" /> 的离散选项控件，可使用固定 <see cref="Options" /> 或动态
    ///         <see cref="OptionsProvider" />。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the binding that stores the selected option value.</para>
        ///     <para xml:lang="zh-CN">获取存储所选选项值的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<TValue> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an immutable snapshot of the initial ordered options. When <see cref="OptionsProvider" /> is
        ///         absent, this is the complete fixed option set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取初始有序选项的不可变快照；未提供 <see cref="OptionsProvider" /> 时，此快照即为完整的固定选项集。
        ///     </para>
        /// </summary>
        public IReadOnlyList<ModSettingsChoiceOption<TValue>> Options { get; } =
            ValidateAndSnapshotOptions(options, nameof(options));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional provider used to re-evaluate available options on settings UI refresh and before
        ///         a drop-down list opens.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的选项提供器；设置界面刷新以及下拉列表展开前会通过该提供器重新计算可用选项。
        ///     </para>
        /// </summary>
        public Func<IReadOnlyList<ModSettingsChoiceOption<TValue>>>? OptionsProvider { get; internal set; }

        internal IReadOnlyList<ModSettingsChoiceOption<TValue>> ResolveOptions()
        {
            if (OptionsProvider == null)
                return Options;

            var options = OptionsProvider()
                          ?? throw new InvalidOperationException(
                              $"Dynamic choice setting '{Id}' returned a null option list.");
            return ValidateAndSnapshotOptions(options, nameof(OptionsProvider));
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the visual presentation used for the choices.</para>
        ///     <para xml:lang="zh-CN">获取选项使用的视觉呈现方式。</para>
        /// </summary>
        public ModSettingsChoicePresentation Presentation { get; } = presentation;

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
            return ModSettingsUiFactory.CreateChoiceEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }

        private static IReadOnlyList<ModSettingsChoiceOption<TValue>> ValidateAndSnapshotOptions(
            IReadOnlyList<ModSettingsChoiceOption<TValue>>? options,
            string parameterName)
        {
            ArgumentNullException.ThrowIfNull(options, parameterName);

            var snapshot = options.ToArray();
            if (snapshot.Any(option => option.Label == null))
                throw new ArgumentException("Choice option labels cannot be null.", parameterName);

            return Array.AsReadOnly(snapshot);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a color picker backed by a serialized string, such as a hexadecimal color or engine
    ///         serialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义由序列化字符串支持的颜色选择器，例如十六进制颜色或引擎序列化文本。</para>
    /// </summary>
    public sealed class ColorModSettingsEntryDefinition : ModSettingsEntryDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes the binary-compatible four-parameter form, equivalent to <c>EditAlpha=true</c> and
        ///         <c>EditIntensity=false</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始化保持二进制兼容的四参数形式，等同于 <c>EditAlpha=true</c>、<c>EditIntensity=false</c>。
        ///     </para>
        /// </summary>
        public ColorModSettingsEntryDefinition(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description)
            : this(id, label, binding, description, true, false)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes a color entry with explicit alpha and intensity editing options.</para>
        ///     <para xml:lang="zh-CN">使用明确的透明度及强度编辑选项初始化颜色条目。</para>
        /// </summary>
        public ColorModSettingsEntryDefinition(
            string id,
            ModSettingsText label,
            IModSettingsValueBinding<string> binding,
            ModSettingsText? description,
            bool editAlpha,
            bool editIntensity)
            : base(id, label, description)
        {
            ArgumentNullException.ThrowIfNull(binding);
            Binding = binding;
            EditAlpha = editAlpha;
            EditIntensity = editIntensity;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the binding that stores the serialized color string.</para>
        ///     <para xml:lang="zh-CN">获取存储序列化颜色字符串的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the picker exposes alpha-channel editing, corresponding to BaseLib
        ///         <c>ConfigColorPickerAttribute.EditAlpha</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取颜色选择器是否提供透明度通道编辑，对应 BaseLib 的
        ///         <c>ConfigColorPickerAttribute.EditAlpha</c>。
        ///     </para>
        /// </summary>
        public bool EditAlpha { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the Godot picker enables HDR intensity editing. BaseLib applies this option only to
        ///         <see cref="Godot.Color" /> properties.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 Godot 颜色选择器是否启用 HDR 强度编辑；BaseLib 仅对 <see cref="Godot.Color" /> 属性应用此选项。
        ///     </para>
        /// </summary>
        public bool EditIntensity { get; }

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
            return ModSettingsUiFactory.CreateColorEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides the shared binding, placeholder, and length contract for text entries.</para>
    ///     <para xml:lang="zh-CN">为文本条目提供共享的绑定、占位文本及长度契约。</para>
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
        ///     <para xml:lang="en">Gets the binding that stores the text value.</para>
        ///     <para xml:lang="zh-CN">获取存储文本值的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets the optional placeholder displayed while the field is empty.</para>
        ///     <para xml:lang="zh-CN">获取字段为空时显示的可选占位文本。</para>
        /// </summary>
        public ModSettingsText? Placeholder { get; } = placeholder;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional positive maximum character count.</para>
        ///     <para xml:lang="zh-CN">获取可选的正数最大字符数。</para>
        /// </summary>
        public int? MaxLength { get; } =
            maxLength is null or >= 1
                ? maxLength
                : throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be null or >= 1.");

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

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a single-line text field.</para>
    ///     <para xml:lang="zh-CN">定义单行文本字段。</para>
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
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional predicate that controls invalid-state styling for the current text. A false result
        ///         does not block committing the value and mirrors ModConfig text-input validator styling.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用于控制当前文本无效状态样式的可选谓词；结果为 false 不会阻止提交该值，其行为与 ModConfig
        ///         文本输入校验器的样式提示一致。
        ///     </para>
        /// </summary>
        public Func<string, bool>? ValueValidationVisual { get; init; }

        internal Func<string, bool>? ValueValidationCommit { get; init; }

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateStringLineEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a multiline text field.</para>
    ///     <para xml:lang="zh-CN">定义多行文本字段。</para>
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
    ///     <para xml:lang="en">Defines a keyboard binding capture row that writes a normalized token to <see cref="Binding" />.</para>
    ///     <para xml:lang="zh-CN">定义将规范化标记写入 <see cref="Binding" /> 的键盘绑定捕获行。</para>
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
        ///     <para xml:lang="en">Gets the binding that stores the serialized keyboard binding.</para>
        ///     <para xml:lang="zh-CN">获取存储序列化键盘绑定的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets whether modifier-and-key combinations can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许捕获修饰键与普通按键的组合。</para>
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a modifier key by itself can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许单独捕获修饰键。</para>
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     <para xml:lang="en">Gets whether left and right modifier keys are distinguished.</para>
        ///     <para xml:lang="zh-CN">获取是否区分左侧与右侧修饰键。</para>
        /// </summary>
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

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
            return ModSettingsUiFactory.CreateKeyBindingEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines an input binding capture row that writes either a normalized keyboard token or an
    ///         <c>action:&lt;name&gt;</c> token to <see cref="Binding" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义将规范化键盘标记或 <c>action:&lt;name&gt;</c> 标记写入 <see cref="Binding" /> 的输入绑定捕获行。
    ///     </para>
    /// </summary>
    public sealed class InputBindingModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<string> binding,
        bool allowModifierCombos,
        bool allowModifierOnly,
        bool distinguishModifierSides,
        bool allowActionBindings,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the binding that stores the serialized input binding.</para>
        ///     <para xml:lang="zh-CN">获取存储序列化输入绑定的绑定。</para>
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } =
            binding ?? throw new ArgumentNullException(nameof(binding));

        /// <summary>
        ///     <para xml:lang="en">Gets whether modifier-and-key combinations can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许捕获修饰键与普通按键的组合。</para>
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a modifier key by itself can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许单独捕获修饰键。</para>
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     <para xml:lang="en">Gets whether left and right modifier keys are distinguished.</para>
        ///     <para xml:lang="zh-CN">获取是否区分左侧与右侧修饰键。</para>
        /// </summary>
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

        /// <summary>
        ///     <para xml:lang="en">Gets whether Godot and Slay the Spire 2 input actions can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许捕获 Godot 及《杀戮尖塔 2》输入动作。</para>
        /// </summary>
        public bool AllowActionBindings { get; } = allowActionBindings;

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
            return ModSettingsUiFactory.CreateInputBindingEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a keyboard binding capture row that stores multiple normalized bindings in
    ///         <see cref="Binding" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义在 <see cref="Binding" /> 中存储多个规范化绑定的键盘绑定捕获行。
    ///     </para>
    /// </summary>
    public sealed class MultiKeyBindingModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        IModSettingsValueBinding<List<string>> binding,
        bool allowModifierCombos,
        bool allowModifierOnly,
        bool distinguishModifierSides,
        ModSettingsText? description)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the binding that stores the normalized keyboard binding list. Bindings without structured
        ///         data support are wrapped with a list adapter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取存储规范化键盘绑定列表的绑定；不支持结构化数据的绑定会由列表适配器包装。
        ///     </para>
        /// </summary>
        public IModSettingsValueBinding<List<string>> Binding { get; } =
            binding is null
                ? throw new ArgumentNullException(nameof(binding))
                : binding is IStructuredModSettingsValueBinding<List<string>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List<string>());

        /// <summary>
        ///     <para xml:lang="en">Gets whether modifier-and-key combinations can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许捕获修饰键与普通按键的组合。</para>
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     <para xml:lang="en">Gets whether a modifier key by itself can be captured.</para>
        ///     <para xml:lang="zh-CN">获取是否允许单独捕获修饰键。</para>
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     <para xml:lang="en">Gets whether left and right modifier keys are distinguished while recording.</para>
        ///     <para xml:lang="zh-CN">获取录制绑定时是否区分左侧与右侧修饰键。</para>
        /// </summary>
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

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
            return ModSettingsUiFactory.CreateMultiKeyBindingEntry(context, this);
        }

        internal override bool TryResetToDefault(IModSettingsUiActionHost host)
        {
            return TryResetBindingToDefault(Binding, host);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a button that invokes <see cref="Action" /> without storing a setting value.</para>
    ///     <para xml:lang="zh-CN">定义调用 <see cref="Action" /> 且不存储设置值的按钮。</para>
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
        ///     <para xml:lang="en">Gets the text displayed on the button.</para>
        ///     <para xml:lang="zh-CN">获取按钮显示的文本。</para>
        /// </summary>
        public ModSettingsText ButtonText { get; } =
            buttonText ?? throw new ArgumentNullException(nameof(buttonText));

        /// <summary>
        ///     <para xml:lang="en">Gets the callback invoked when the button is activated.</para>
        ///     <para xml:lang="zh-CN">获取激活按钮时调用的回调。</para>
        /// </summary>
        public Action Action { get; } = action ?? throw new ArgumentNullException(nameof(action));

        /// <summary>
        ///     <para xml:lang="en">Gets the button's semantic visual tone.</para>
        ///     <para xml:lang="zh-CN">获取按钮的语义视觉色调。</para>
        /// </summary>
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateButtonEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a button whose callback receives <see cref="IModSettingsUiActionHost" />, allowing it to request
    ///         a refresh after deferred work such as a native file dialog.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义回调可接收 <see cref="IModSettingsUiActionHost" /> 的按钮，以便在原生文件对话框等延迟工作完成后请求刷新。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the text displayed on the button.</para>
        ///     <para xml:lang="zh-CN">获取按钮显示的文本。</para>
        /// </summary>
        public ModSettingsText ButtonText { get; } =
            buttonText ?? throw new ArgumentNullException(nameof(buttonText));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the callback invoked when the button is activated. It can call
        ///         <see cref="IModSettingsUiActionHost.RequestRefresh" /> after changing values outside the current
        ///         control graph.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取激活按钮时调用的回调；在当前控件树之外更改数值后，可调用
        ///         <see cref="IModSettingsUiActionHost.RequestRefresh" />。
        ///     </para>
        /// </summary>
        public Action<IModSettingsUiActionHost> Action { get; } =
            action ?? throw new ArgumentNullException(nameof(action));

        /// <summary>
        ///     <para xml:lang="en">Gets the button's semantic visual tone.</para>
        ///     <para xml:lang="zh-CN">获取按钮的语义视觉色调。</para>
        /// </summary>
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateHostContextButtonEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a non-interactive heading within a settings section.</para>
    ///     <para xml:lang="zh-CN">定义设置节内不带交互控件的标题。</para>
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
    ///     <para xml:lang="en">
    ///         Defines a read-only rich-text block whose <see cref="ModSettingsEntryDefinition.Label" /> is the body
    ///         and whose <see cref="ModSettingsEntryDefinition.Description" /> is an optional secondary description.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义只读富文本块；<see cref="ModSettingsEntryDefinition.Label" /> 为正文，
    ///         <see cref="ModSettingsEntryDefinition.Description" /> 为可选次级说明。
    ///     </para>
    /// </summary>
    public sealed class ParagraphModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText? description,
        float? maxBodyHeight = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the optional maximum body height. A finite positive value enables a scrolling viewport;
        ///         otherwise the body is uncapped.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的正文最大高度；有限正值会启用滚动视口，其他值表示不限制高度。
        ///     </para>
        /// </summary>
        public float? MaxBodyHeight { get; } = maxBodyHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateParagraphEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a read-only information card with a title, optional subtitle, and rich-text body.</para>
    ///     <para xml:lang="zh-CN">定义包含标题、可选副标题及富文本正文的只读信息卡。</para>
    /// </summary>
    public sealed class InfoCardModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText body,
        ModSettingsText? description = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the rich-text body displayed inside the card.</para>
        ///     <para xml:lang="zh-CN">获取信息卡内显示的富文本正文。</para>
        /// </summary>
        public ModSettingsText Body { get; } = body ?? throw new ArgumentNullException(nameof(body));

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateInfoCardEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a read-only runtime hotkey summary with descriptive text on the left and binding chips on the
    ///         right.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义只读的运行时热键摘要，左侧显示说明文本，右侧显示绑定标签。</para>
    /// </summary>
    public sealed class RuntimeHotkeySummaryModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText body,
        IReadOnlyList<ModSettingsText> bindings,
        ModSettingsText? idSuffix = null)
        : ModSettingsEntryDefinition(id, label, idSuffix)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the descriptive body displayed below the title.</para>
        ///     <para xml:lang="zh-CN">获取标题下方显示的说明正文。</para>
        /// </summary>
        public ModSettingsText Body { get; } = body ?? throw new ArgumentNullException(nameof(body));

        /// <summary>
        ///     <para xml:lang="en">Gets an immutable snapshot of the binding labels shown in the right column.</para>
        ///     <para xml:lang="zh-CN">获取右侧栏所显示绑定标签的不可变快照。</para>
        /// </summary>
        public IReadOnlyList<ModSettingsText> Bindings { get; } =
            ValidateAndSnapshotBindings(bindings);

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateRuntimeHotkeySummaryEntry(context, this);
        }

        private static IReadOnlyList<ModSettingsText> ValidateAndSnapshotBindings(
            IReadOnlyList<ModSettingsText>? bindings)
        {
            ArgumentNullException.ThrowIfNull(bindings);

            var snapshot = bindings.ToArray();
            if (snapshot.Any(binding => binding == null))
                throw new ArgumentException("Runtime hotkey binding labels cannot be null.", nameof(bindings));

            return Array.AsReadOnly(snapshot);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an image preview sourced from <see cref="TextureProvider" /> when it is created.</para>
    ///     <para xml:lang="zh-CN">定义创建时由 <see cref="TextureProvider" /> 提供内容的图像预览。</para>
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
        ///     <para xml:lang="en">Gets the texture provider invoked when the preview is created.</para>
        ///     <para xml:lang="zh-CN">获取创建预览时调用的纹理提供器。</para>
        /// </summary>
        public Func<Texture2D?> TextureProvider { get; } =
            textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));

        /// <summary>
        ///     <para xml:lang="en">Gets the finite positive preview height in pixels.</para>
        ///     <para xml:lang="zh-CN">获取以像素为单位的有限正数预览高度。</para>
        /// </summary>
        public float PreviewHeight { get; } =
            float.IsFinite(previewHeight) && previewHeight > 0f
                ? previewHeight
                : throw new ArgumentOutOfRangeException(nameof(previewHeight),
                    "Image previewHeight must be finite and > 0.");

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateImageEntry(context, this);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines a custom settings row created by a caller-provided control factory.</para>
    ///     <para xml:lang="zh-CN">定义由调用方提供的控件工厂创建的自定义设置行。</para>
    /// </summary>
    public sealed class CustomModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        Func<IModSettingsUiActionHost, Control> controlFactory,
        ModSettingsText? description,
        Func<bool>? visibilityPredicate = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the factory that creates the row control.</para>
        ///     <para xml:lang="zh-CN">获取创建行控件的工厂。</para>
        /// </summary>
        public Func<IModSettingsUiActionHost, Control> ControlFactory { get; } =
            controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));

        /// <inheritdoc />
        public override Func<bool>? VisibilityPredicate => visibilityPredicate;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateCustomEntry(context, this);
        }
    }
}
