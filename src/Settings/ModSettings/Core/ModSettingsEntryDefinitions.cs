using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Base type for one settings row: stable <see cref="Id" />, label, optional description, and UI factory hook.
    ///     单个设置行的基类型：包含稳定 <c>Id</c>、标签、可选描述和 UI 工厂钩子。
    /// </summary>
    public abstract class ModSettingsEntryDefinition
    {
        /// <summary>
        ///     Initializes <see cref="Id" />, <see cref="Label" />, and <see cref="Description" />.
        ///     初始化 <see cref="Id" />、<see cref="Label" /> 和 <see cref="Description" />。
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
        ///     Chrome menu actions available for this entry.
        ///     此条目可用的 chrome 菜单操作。
        /// </summary>
        public ModSettingsMenuCapabilities MenuCapabilities { get; internal set; } = ModSettingsMenuCapabilities.All;

        /// <summary>
        ///     Host surfaces where this entry's interactive controls are forced read-only.
        ///     此条目的交互控件被强制只读的宿主 surface。
        /// </summary>
        public ModSettingsHostSurface ReadOnlyOnHostSurfaces { get; internal set; } = ModSettingsHostSurface.None;

        /// <summary>
        ///     Unique entry id within its section (used for chrome clipboard and anchors).
        ///     section 内唯一的条目 id（用于 chrome 剪贴板和锚点）。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Primary label or body text depending on entry kind.
        ///     主要标签或正文文本，具体取决于条目类型。
        /// </summary>
        public ModSettingsText Label { get; }

        /// <summary>
        ///     Optional secondary description shown in the UI.
        ///     UI 中显示的可选次级描述。
        /// </summary>
        public ModSettingsText? Description { get; }

        /// <summary>
        ///     When non-null, the entry row is hidden while the predicate returns false (re-evaluated on UI refresh).
        ///     非 null 时，当谓词返回 false 会隐藏该条目行（UI 刷新时重新计算）。
        /// </summary>
        public virtual Func<bool>? VisibilityPredicate => null;

        /// <summary>
        ///     When non-null, the entry row is disabled (dimmed, non-interactive) while the predicate returns false
        ///     (re-evaluated on UI refresh).
        ///     非 null 时，当谓词返回 false 会禁用该条目行（变暗且不可交互；UI 刷新时重新计算）。
        /// </summary>
        public virtual Func<bool>? EnabledPredicate => null;

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
    ///     Boolean on/off toggle bound to <see cref="Binding" />.
    ///     绑定到 <see cref="Binding" /> 的布尔开 / 关 toggle。
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
        ///     开关的后端绑定。
        /// </summary>
        public IModSettingsValueBinding<bool> Binding { get; } = binding;

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
    ///     Floating-point slider with range and optional formatter (<see cref="double" /> domain).
    ///     带范围和可选格式化器的浮点 slider（<see cref="double" /> 域）。
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
        ///     滑条值的后端绑定。
        /// </summary>
        public IModSettingsValueBinding<double> Binding { get; } = binding;

        /// <summary>
        ///     Minimum slider value (inclusive).
        ///     滑条最小值（含）。
        /// </summary>
        public double MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum slider value (inclusive).
        ///     滑条最大值（含）。
        /// </summary>
        public double MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        ///     有效值之间的步进。
        /// </summary>
        public double Step { get; } = step;

        /// <summary>
        ///     Optional formatter for the displayed value string.
        ///     显示值字符串的可选格式化器。
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
    ///     Internal <see cref="float" /> slider entry (legacy pipeline). Only produced by the obsolete
    ///     <c>ModSettingsSectionBuilder.AddSlider</c> overload taking <see cref="IModSettingsValueBinding{T}" /> of
    ///     <see cref="float" />; separate from <see cref="SliderModSettingsEntryDefinition" /> to avoid float/double
    ///     drift and refresh feedback loops.
    ///     内部 <see cref="float" /> slider 条目（旧版管线）。仅由已过时的
    ///     <c>ModSettingsSectionBuilder.AddSlider</c> 重载生成，该重载接收 <see cref="float" /> 的
    ///     <see cref="IModSettingsValueBinding{T}" />；它与 <see cref="SliderModSettingsEntryDefinition" /> 分离，以避免 float/double
    ///     漂移和刷新反馈循环。
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
        ///     滑条值的后端绑定。
        /// </summary>
        public IModSettingsValueBinding<float> Binding { get; } = binding;

        /// <summary>
        ///     Minimum slider value (inclusive).
        ///     滑条最小值（含）。
        /// </summary>
        public float MinValue { get; } = minValue;

        /// <summary>
        ///     Maximum slider value (inclusive).
        ///     滑条最大值（含）。
        /// </summary>
        public float MaxValue { get; } = maxValue;

        /// <summary>
        ///     Step between valid values.
        ///     有效值之间的步进。
        /// </summary>
        public float Step { get; } = step;

        /// <summary>
        ///     Optional formatter for the displayed value string.
        ///     显示值字符串的可选格式化器。
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
    ///     Discrete choice control over <typeparamref name="TValue" /> with fixed <see cref="Options" />.
    ///     基于 <typeparamref name="TValue" />、具有固定 <see cref="Options" /> 的离散 choice 控件。
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
        ///     所选选项值的后端绑定。
        /// </summary>
        public IModSettingsValueBinding<TValue> Binding { get; } = binding;

        /// <summary>
        ///     Ordered choices shown in the UI.
        ///     UI 中显示的有序选项。
        /// </summary>
        public IReadOnlyList<ModSettingsChoiceOption<TValue>> Options { get; } = options;

        /// <summary>
        ///     Visual presentation (stepper, dropdown, etc.).
        ///     视觉呈现方式（步进器、下拉菜单等）。
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
    }

    /// <summary>
    ///     Color picker bound to a string (e.g. hex or engine serialization).
    ///     Color picker bound to a string (e.g. hex 或 engine serialization).
    /// </summary>
    public sealed class ColorModSettingsEntryDefinition : ModSettingsEntryDefinition
    {
        /// <summary>
        ///     保留旧版四参数构造函数以维持二进制兼容；等价于 <c>EditAlpha=true</c>、<c>EditIntensity=false</c>。
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
        ///     Full constructor including color picker options.
        ///     完整构造函数（含拾色器选项）。
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
            Binding = binding;
            EditAlpha = editAlpha;
            EditIntensity = editIntensity;
        }

        /// <summary>
        ///     Backing binding for the color string.
        ///     颜色字符串的后备绑定。
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; }

        /// <summary>
        ///     When false, the picker does not expose the alpha channel (matches BaseLib
        ///     <c>[ConfigColorPicker(EditAlpha=false)]</c>).
        ///     为 false 时，选择器不公开 alpha 通道（匹配 BaseLib
        ///     <c>[ConfigColorPicker(EditAlpha=false)]</c>）。
        /// </summary>
        public bool EditAlpha { get; }

        /// <summary>
        ///     When true, enables HDR / intensity editing on the Godot picker (BaseLib only applies this for
        ///     <see cref="Godot.Color" /> properties).
        ///     为 true 时，在 Godot 选择器上启用 HDR / intensity 编辑（BaseLib 仅对
        ///     <see cref="Godot.Color" /> 属性应用此项）。
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
    ///     Shared base for single-line and multiline string entries.
    ///     单行和多行字符串条目的共享基类。
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
        ///     文本值的后备绑定。
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        /// <summary>
        ///     Placeholder shown when empty.
        ///     Placeholder shown 当 empty.
        /// </summary>
        public ModSettingsText? Placeholder { get; } = placeholder;

        /// <summary>
        ///     Maximum character count when set.
        ///     设置时的最大字符数。
        /// </summary>
        public int? MaxLength { get; } = maxLength;

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
    ///     Single-line text field.
    ///     单行文本字段。
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
        ///     Optional visual validation for the current text (e.g. red border when <see langword="false" />).
        ///     Does not block commits; mirrors optional ModConfig TextInput <c>Validator</c> styling.
        ///     当前文本的可选视觉校验（例如 <see langword="false" /> 时显示红色边框）。
        ///     不会阻止提交；对应可选 ModConfig TextInput <c>Validator</c> 样式。
        /// </summary>
        public Func<string, bool>? ValueValidationVisual { get; init; }

        internal Func<string, bool>? ValueValidationCommit { get; init; }

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateStringLineEntry(context, this);
        }
    }

    /// <summary>
    ///     Multiline text field.
    ///     多行文本字段。
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
    ///     将字符串 token 写入 <see cref="Binding" /> 的按键绑定捕获行。
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
        ///     序列化按键字符串的后备绑定。
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        /// <summary>
        ///     When true, modifier+key combinations are allowed.
        ///     为 true 时，modifier+key combinations are allowed。
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     When true, modifier-only shortcuts are allowed.
        ///     为 true 时，modifier-only shortcuts are allowed。
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     When true, left/right modifier sides are distinguished.
        ///     为 true 时，left/right modifier sides are distinguished。
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
    ///     Input binding capture row writing either a key token or <c>action:&lt;name&gt;</c> token to <see cref="Binding" />.
    ///     输入绑定捕获行，将按键 token 或 <c>action:&lt;name&gt;</c> token 写入 <see cref="Binding" />。
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
        ///     Backing binding for the serialized input binding string.
        ///     序列化输入绑定字符串的后备绑定。
        /// </summary>
        public IModSettingsValueBinding<string> Binding { get; } = binding;

        /// <summary>
        ///     Whether modifier+key combinations are allowed.
        ///     是否允许修饰键加按键组合。
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     Whether modifier-only shortcuts are allowed.
        ///     是否允许仅修饰键快捷键。
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     Whether left/right modifier sides are distinguished.
        ///     是否区分左/右修饰键。
        /// </summary>
        public bool DistinguishModifierSides { get; } = distinguishModifierSides;

        /// <summary>
        ///     Whether Godot/STS2 action bindings can be captured.
        ///     是否允许捕获 Godot/STS2 action 绑定。
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
    ///     Multi-key binding capture row writing a binding list to <see cref="Binding" />.
    ///     将绑定列表写入 <see cref="Binding" /> 的多按键绑定捕获行。
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
        ///     Binding that stores the normalized list of captured hotkeys.
        ///     存储已捕获热键规范化列表的绑定。
        /// </summary>
        public IModSettingsValueBinding<List<string>> Binding { get; } =
            binding is IStructuredModSettingsValueBinding<List<string>>
                ? binding
                : ModSettingsBindings.WithAdapter(binding, ModSettingsStructuredData.List<string>());

        /// <summary>
        ///     Whether modifier combinations are allowed.
        ///     表示是否 modifier combinations are allowed。
        /// </summary>
        public bool AllowModifierCombos { get; } = allowModifierCombos;

        /// <summary>
        ///     Whether modifier-only bindings are allowed.
        ///     表示是否 modifier-only bindings are allowed。
        /// </summary>
        public bool AllowModifierOnly { get; } = allowModifierOnly;

        /// <summary>
        ///     Whether left/right modifier keys are distinguished while recording.
        ///     录制时是否区分左右 modifier 键。
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
    ///     Non-persisted button that invokes <see cref="Action" />.
    ///     调用 <see cref="Action" /> 的非持久化按钮。
    ///     调用 <c>action</c> 的非持久化按钮。
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
        ///     按钮控件上的标题。
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        /// <summary>
        ///     Callback when the button is activated.
        ///     按钮激活时的回调。
        /// </summary>
        public Action Action { get; } = action;

        /// <summary>
        ///     Visual emphasis (normal, primary, danger).
        ///     视觉强调级别（normal、primary、danger）。
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
    ///     接收 <see cref="IModSettingsUiActionHost" /> 的按钮，使回调可在异步工作后刷新面板
    ///     （例如原生文件对话框）。
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
        ///     按钮控件上的标题。
        /// </summary>
        public ModSettingsText ButtonText { get; } = buttonText;

        /// <summary>
        ///     Callback when the button is activated; use <see cref="IModSettingsUiActionHost.RequestRefresh" /> after
        ///     mutating bindings outside the control graph.
        ///     按钮激活时的回调；在控件图之外修改绑定后使用
        ///     <see cref="IModSettingsUiActionHost.RequestRefresh" />。
        /// </summary>
        public Action<IModSettingsUiActionHost> Action { get; } = action;

        /// <summary>
        ///     Visual emphasis (normal, primary, danger).
        ///     视觉强调级别（normal、primary、danger）。
        /// </summary>
        public ModSettingsButtonTone Tone { get; } = tone;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateHostContextButtonEntry(context, this);
        }
    }

    /// <summary>
    ///     Section heading without a control.
    ///     不带控件的 section 标题。
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
    ///     只读富文本块；<see cref="ModSettingsEntryDefinition.Label" /> 是正文，
    ///     <see cref="ModSettingsEntryDefinition.Description" /> 是可选副标题。
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
        ///     段落正文开始滚动前的最大高度。
        /// </summary>
        public float? MaxBodyHeight { get; } = maxBodyHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateParagraphEntry(context, this);
        }
    }

    /// <summary>
    ///     Read-only information card with title, optional subtitle, and rich-text body.
    ///     Read-only information 卡牌 带有 title, 可选 subtitle, 和 rich-text body.
    /// </summary>
    public sealed class InfoCardModSettingsEntryDefinition(
        string id,
        ModSettingsText label,
        ModSettingsText body,
        ModSettingsText? description = null)
        : ModSettingsEntryDefinition(id, label, description)
    {
        /// <summary>
        ///     Main rich-text body shown inside the card.
        ///     显示在卡片内的富文本正文。
        /// </summary>
        public ModSettingsText Body { get; } = body;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateInfoCardEntry(context, this);
        }
    }

    /// <summary>
    ///     Read-only runtime hotkey summary row with left text and right binding chips.
    ///     Read-only runtime hotkey summary row 带有 left text 和 right binding chips.
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
        ///     Descriptive body text shown under the title.
        ///     显示在标题下方的描述正文。
        /// </summary>
        public ModSettingsText Body { get; } = body;

        /// <summary>
        ///     Binding chips shown in the right-hand column.
        ///     显示在右侧列中的绑定 chip。
        /// </summary>
        public IReadOnlyList<ModSettingsText> Bindings { get; } = bindings;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateRuntimeHotkeySummaryEntry(context, this);
        }
    }

    /// <summary>
    ///     Static image preview from <see cref="TextureProvider" />.
    ///     来自 <see cref="TextureProvider" /> 的静态图像预览。
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
        ///     预览用的延迟纹理来源。
        /// </summary>
        public Func<Texture2D?> TextureProvider { get; } = textureProvider;

        /// <summary>
        ///     Height of the preview area in pixels.
        ///     预览区域的高度，单位为像素。
        /// </summary>
        public float PreviewHeight { get; } = previewHeight;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateImageEntry(context, this);
        }
    }

    /// <summary>
    ///     Custom settings row built by a caller-provided control factory.
    ///     自定义 设置 row built 通过 a caller-provided control factory.
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
        ///     Factory that creates the row control.
        ///     创建行控件的工厂。
        /// </summary>
        public Func<IModSettingsUiActionHost, Control> ControlFactory { get; } = controlFactory;

        /// <inheritdoc />
        public override Func<bool>? VisibilityPredicate => visibilityPredicate;

        internal override Control CreateControl(ModSettingsUiContext context)
        {
            return ModSettingsUiFactory.CreateCustomEntry(context, this);
        }
    }
}
