using System.Globalization;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Ui.Shell.Theme;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    internal interface IModSettingsTransientPopupOwner
    {
        void ForceCloseTransientUi();
    }

    /// <summary>
    ///     Implemented by entry controls that consume directional (up/down) input while in an active mode — an
    ///     open dropdown/actions menu, or a key-binding control recording input. The submenu's live focus
    ///     navigator skips controls whose ancestor claims directional input so the control's own handling wins.
    ///     由那些在激活模式下消费方向(上/下)输入的条目控件实现——展开的下拉/操作菜单,或正在录制输入的按键绑定控件。子菜单的
    ///     实时焦点导航器会跳过其祖先声明占用方向输入的控件,让该控件自身的处理生效。
    /// </summary>
    internal interface IModSettingsDirectionalInputClaimant
    {
        bool ClaimsDirectionalInput { get; }
    }

    /// <summary>
    ///     Standard On/Off toggle control used by mod settings entries.
    ///     mod 设置条目使用的标准 On/Off 切换控件。
    /// </summary>
    public sealed partial class ModSettingsToggleControl : ModSettingsGamepadCompatibleButton
    {
        private bool _initialValue;
        private bool _isOn;
        private Action<bool>? _onChanged;

        /// <summary>
        ///     Creates a toggle control with an initial value and change callback.
        ///     创建带初始值和变更回调的切换控件。
        /// </summary>
        /// <param name="initialValue">
        ///     Whether the toggle starts enabled.
        ///     切换控件初始是否启用。
        /// </param>
        /// <param name="onChanged">
        ///     Callback invoked after the value changes.
        ///     值变化后调用的回调。
        /// </param>
        public ModSettingsToggleControl(bool initialValue, Action<bool>? onChanged)
        {
            _initialValue = initialValue;
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.toggle.layout.entry.minSize",
                new(ModSettingsUiFactory.EntryControlWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            Pressed += ToggleValue;
        }

        /// <summary>
        ///     Creates the toggle control for Godot scene instantiation.
        ///     创建用于 Godot 场景实例化的切换控件。
        /// </summary>
        public ModSettingsToggleControl()
        {
        }

        internal void BindValue(bool value, Action<bool>? onChanged)
        {
            _initialValue = value;
            _onChanged = onChanged;
            SetValue(value);
        }

        internal void ClearBinding()
        {
            _onChanged = null;
            this.ReleaseFocusIfInsideTree();
            Disabled = false;
            ProcessMode = ProcessModeEnum.Inherit;
            Modulate = Colors.White;
        }

        /// <summary>
        ///     Initializes the visual state after the control enters the scene tree.
        ///     控件进入场景树后初始化视觉状态。
        /// </summary>
        public override void _Ready()
        {
            _isOn = _initialValue;
            ApplyVisualState();
        }

        /// <summary>
        ///     Sets the current toggle value without recreating the control.
        ///     设置当前切换值而不重新创建控件。
        /// </summary>
        /// <param name="value">
        ///     The value to display.
        ///     要显示的值。
        /// </param>
        public void SetValue(bool value)
        {
            _isOn = value;
            ApplyVisualState();
        }

        private void ToggleValue()
        {
            _isOn = !_isOn;
            ApplyVisualState();
            InvokeOnChangedSafely(_isOn);
        }

        private void InvokeOnChangedSafely(bool value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsToggleControl] onChanged failed: {ex.Message}");
            }
        }

        private void ApplyVisualState()
        {
            Text = _isOn
                ? ModSettingsLocalization.Get("toggle.on", "On")
                : ModSettingsLocalization.Get("toggle.off", "Off");
            AddThemeStyleboxOverride("normal", CreateStyle(_isOn, false));
            AddThemeStyleboxOverride("hover", CreateStyle(_isOn, true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateStyle(_isOn, true));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());
        }

        private static StyleBoxFlat CreateStyle(bool on, bool hovered)
        {
            var borderColor = on
                ? RitsuShellTheme.Current.Component.Toggle.On.Border
                : RitsuShellTheme.Current.Component.Toggle.Off.Border;
            var normalBorder = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidth", 2);
            var hoverBorder =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthHover", 3);
            var border = hovered ? hoverBorder : normalBorder;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.toggle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.bottom", 8));
            var shadowSize = hovered
                ? RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSizeHover", 7)
                : RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSize", 2);
            return new()
            {
                BgColor = on
                    ? RitsuShellTheme.Current.Component.Toggle.On.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.Toggle.OffHover.Bg
                        : RitsuShellTheme.Current.Component.Toggle.Off.Bg,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = hovered
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : RitsuShellTheme.Current.Component.Toggle.Shadow,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateDisabledStyle()
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthDisabled", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.toggle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.bottom", 8));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Toggle.Disabled.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Toggle.Disabled.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }

    internal sealed partial class ModSettingsSliderControl : HBoxContainer
    {
        private readonly double _bindingValueAtConstruct;
        private readonly Func<double, string>? _formatter;
        private readonly Action<double>? _onChanged;
        private NControllerManager? _hookedControllerManager;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

        public ModSettingsSliderControl(
            double initialValue,
            double minValue,
            double maxValue,
            double step,
            Func<double, string> formatter,
            Action<double> onChanged)
        {
            _formatter = formatter;
            _onChanged = onChanged;
            _bindingValueAtConstruct = initialValue;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.slider.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Slider.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Alignment = AlignmentMode.Center;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.rowSeparation", 8));

            var valueEdit = new LineEdit
            {
                Name = "SliderValue",
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.valueField.minSize",
                    new(RitsuShellTheme.Current.Metric.Slider.ValueFieldWidth,
                        RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Alignment = HorizontalAlignment.Center,
                SelectAllOnFocus = true,
                CaretBlink = true,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(valueEdit,
                RitsuShellTheme.Current.Font.Body);
            AddChild(valueEdit);
            _valueEdit = valueEdit;

            var sliderPanel = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.track.minSize",
                    new(RitsuShellTheme.Current.Metric.Slider.TrackMinWidth,
                        RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sliderPanel.AddThemeConstantOverride("margin_top",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.top", 4));
            sliderPanel.AddThemeConstantOverride("margin_bottom",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.bottom", 4));
            AddChild(sliderPanel);

            var normalizedInitial = NormalizeSliderValue(initialValue, minValue, maxValue, step);
            var slider = new HSlider
            {
                Name = "Slider",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.track.barMinSize",
                    new(0f, 24f)),
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Pass,
                MinValue = minValue,
                MaxValue = maxValue,
                Step = step,
                Value = normalizedInitial,
            };
            slider.AddThemeStyleboxOverride("slider", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", CreateSliderStyle(true));
            sliderPanel.AddChild(slider);
            _slider = slider;
        }

        public ModSettingsSliderControl()
        {
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedControllerManager = NControllerManager.Instance;
            if (_hookedControllerManager != null)
            {
                _hookedControllerManager.ControllerDetected += OnControllerUiModeChanged;
                _hookedControllerManager.MouseDetected += OnControllerUiModeChanged;
            }

            ApplySliderMouseFilterForInputMode();
        }

        public override void _ExitTree()
        {
            if (_hookedControllerManager != null)
            {
                _hookedControllerManager.ControllerDetected -= OnControllerUiModeChanged;
                _hookedControllerManager.MouseDetected -= OnControllerUiModeChanged;
                _hookedControllerManager = null;
            }

            base._ExitTree();
        }

        public override void _Ready()
        {
            if (_slider == null)
                return;

            RefreshValueLabel(_slider.Value);
            _slider.ValueChanged += OnSliderValueChanged;
            _slider.DragEnded += _ => _slider.ReleaseFocusIfInsideTree();
            if (_valueEdit == null) return;
            _valueEdit.TextSubmitted += OnValueSubmitted;
            _valueEdit.FocusExited += OnValueFocusExited;

            SyncBindingToCanonicalSliderValue(_bindingValueAtConstruct);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_slider);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_valueEdit);
            ApplySliderMouseFilterForInputMode();
        }

        private void OnControllerUiModeChanged()
        {
            ApplySliderMouseFilterForInputMode();
        }

        private void ApplySliderMouseFilterForInputMode()
        {
            if (_slider == null)
                return;

            var blockMouse = NControllerManager.Instance?.IsUsingController == true;
            _slider.MouseFilter = blockMouse ? MouseFilterEnum.Ignore : MouseFilterEnum.Pass;
        }

        private void OnSliderValueChanged(double value)
        {
            if (_suppressCallbacks)
                return;
            RefreshValueLabel(value);
            InvokeOnChangedSafely(value);
        }

        public void SetValue(double value)
        {
            if (_slider == null)
                return;

            var min = _slider.MinValue;
            var max = _slider.MaxValue;
            var normalized = NormalizeSliderValue(value, min, max, _slider.Step);

            _suppressCallbacks = true;
            _slider.Value = normalized;
            var actual = _slider.Value;
            RefreshValueLabel(actual);
            _suppressCallbacks = false;
        }

        private static double NormalizeSliderValue(double value, double minValue, double maxValue, double step)
        {
            var v = Math.Clamp(value, minValue, maxValue);
            if (step > 0d)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private void InvokeOnChangedSafely(double value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsSliderControl] onChanged failed: {ex.Message}");
            }
        }

        private void SyncBindingToCanonicalSliderValue(double bindingClaimed)
        {
            if (_slider == null)
                return;
            SetValue(bindingClaimed);
        }

        private void RefreshValueLabel(double value)
        {
            if (_valueEdit == null || _formatter == null)
                return;

            try
            {
                _valueEdit.Text = _formatter(value);
            }
            catch
            {
                _valueEdit.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        private void OnValueSubmitted(string text)
        {
            TryApplyTypedValue(text);
            _valueEdit.ReleaseFocusIfInsideTree();
        }

        private void OnValueFocusExited()
        {
            if (_valueEdit != null)
                TryApplyTypedValue(_valueEdit.Text);
        }

        private void TryApplyTypedValue(string text)
        {
            if (_slider == null)
                return;

            if (!double.TryParse(text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(text, out value))
            {
                RefreshValueLabel(_slider.Value);
                return;
            }

            value = NormalizeSliderValue(value, _slider.MinValue, _slider.MaxValue, _slider.Step);
            _slider.Value = value;
        }

        private static StyleBoxFlat CreateSliderStyle(bool highlighted)
        {
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.slider.layout.grabber.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.slider.layout.grabber.padding", 8);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.top", 6),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.bottom", 6));
            var bg = highlighted
                ? ResolveSliderColor("components.slider.track.highlight",
                    RitsuShellTheme.Current.Component.Slider.GrabHighlight)
                : ResolveSliderColor("components.slider.track.bg", RitsuShellTheme.Current.Component.Slider.GrabShadow);
            return new()
            {
                BgColor = bg,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static Color ResolveSliderColor(string path, Color fallback)
        {
            return RitsuShellTheme.Current.TryGetColor(path, out var color) ? color : fallback;
        }
    }

    /// <summary>
    ///     Legacy <see cref="float" /> slider row: Godot <see cref="HSlider" /> still uses <see cref="double" /> values,
    ///     but comparisons and binding I/O stay in <see cref="float" /> space to match obsolete
    ///     <c>AddSlider(..., IModSettingsValueBinding&lt;float&gt;, ...)</c> mods without double bridges.
    ///     旧版 <see cref="float" /> 滑块行：Godot <see cref="HSlider" /> 仍使用 <see cref="double" /> 值，
    ///     但比较和 binding I/O 保持在 <see cref="float" /> 空间，以匹配不带 double 桥接的过时
    ///     <c>AddSlider(..., IModSettingsValueBinding&lt;float&gt;, ...)</c> mod。
    /// </summary>
    public sealed partial class ModSettingsFloatSliderControl : HBoxContainer
    {
        private readonly float _bindingValueAtConstruct;
        private readonly Func<float, string>? _formatter;
        private readonly Action<float>? _onChanged;
        private NControllerManager? _hookedControllerManagerFloat;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

        /// <summary>
        ///     Creates a float-backed slider editor.
        ///     创建以 float 支持的滑块编辑器。
        /// </summary>
        /// <param name="initialValue">
        ///     The starting value shown by the slider.
        ///     滑块显示的起始值。
        /// </param>
        /// <param name="minValue">
        ///     The minimum allowed value.
        ///     允许的最小值。
        /// </param>
        /// <param name="maxValue">
        ///     The maximum allowed value.
        ///     允许的最大值。
        /// </param>
        /// <param name="step">
        ///     The slider increment.
        ///     滑块增量。
        /// </param>
        /// <param name="formatter">
        ///     Formats committed values for the text field.
        ///     为文本字段格式化已提交值。
        /// </param>
        /// <param name="onChanged">
        ///     Invoked when the effective value changes.
        ///     有效值变化时调用。
        /// </param>
        public ModSettingsFloatSliderControl(
            float initialValue,
            float minValue,
            float maxValue,
            float step,
            Func<float, string> formatter,
            Action<float> onChanged)
        {
            _formatter = formatter;
            _onChanged = onChanged;
            _bindingValueAtConstruct = initialValue;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.slider.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Slider.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Alignment = AlignmentMode.Center;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.rowSeparation", 8));

            var valueEdit = new LineEdit
            {
                Name = "SliderValue",
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.valueField.minSize",
                    new(RitsuShellTheme.Current.Metric.Slider.ValueFieldWidth,
                        RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Alignment = HorizontalAlignment.Center,
                SelectAllOnFocus = true,
                CaretBlink = true,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(valueEdit,
                RitsuShellTheme.Current.Font.Body);
            AddChild(valueEdit);
            _valueEdit = valueEdit;

            var sliderPanel = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.track.minSize",
                    new(RitsuShellTheme.Current.Metric.Slider.TrackMinWidth,
                        RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sliderPanel.AddThemeConstantOverride("margin_top",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.top", 4));
            sliderPanel.AddThemeConstantOverride("margin_bottom",
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.bottom", 4));
            AddChild(sliderPanel);

            var normalizedInitial = NormalizeSliderValue(initialValue, minValue, maxValue, step);
            var slider = new HSlider
            {
                Name = "Slider",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.slider.layout.track.barMinSize",
                    new(0f, 24f)),
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Pass,
                MinValue = minValue,
                MaxValue = maxValue,
                Step = step,
                Value = normalizedInitial,
            };
            slider.AddThemeStyleboxOverride("slider", CreateFloatSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area", CreateFloatSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", CreateFloatSliderStyle(true));
            sliderPanel.AddChild(slider);
            _slider = slider;
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsFloatSliderControl()
        {
        }

        /// <inheritdoc />
        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedControllerManagerFloat = NControllerManager.Instance;
            if (_hookedControllerManagerFloat != null)
            {
                _hookedControllerManagerFloat.ControllerDetected += OnFloatSliderControllerUiModeChanged;
                _hookedControllerManagerFloat.MouseDetected += OnFloatSliderControllerUiModeChanged;
            }

            ApplyFloatSliderMouseFilterForInputMode();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (_hookedControllerManagerFloat != null)
            {
                _hookedControllerManagerFloat.ControllerDetected -= OnFloatSliderControllerUiModeChanged;
                _hookedControllerManagerFloat.MouseDetected -= OnFloatSliderControllerUiModeChanged;
                _hookedControllerManagerFloat = null;
            }

            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_slider == null)
                return;

            RefreshValueLabel((float)_slider.Value);
            _slider.ValueChanged += OnSliderValueChanged;
            _slider.DragEnded += _ => _slider.ReleaseFocusIfInsideTree();
            if (_valueEdit == null) return;
            _valueEdit.TextSubmitted += OnValueSubmitted;
            _valueEdit.FocusExited += OnValueFocusExited;

            SyncBindingToCanonicalSliderValue(_bindingValueAtConstruct);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_slider);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_valueEdit);
            ApplyFloatSliderMouseFilterForInputMode();
        }

        private void OnFloatSliderControllerUiModeChanged()
        {
            ApplyFloatSliderMouseFilterForInputMode();
        }

        private void ApplyFloatSliderMouseFilterForInputMode()
        {
            if (_slider == null)
                return;

            var blockMouse = NControllerManager.Instance?.IsUsingController == true;
            _slider.MouseFilter = blockMouse ? MouseFilterEnum.Ignore : MouseFilterEnum.Pass;
        }

        private void OnSliderValueChanged(double value)
        {
            if (_suppressCallbacks)
                return;
            var f = (float)value;
            RefreshValueLabel(f);
            InvokeOnChangedSafely(f);
        }

        /// <summary>
        ///     Replaces the current slider value without leaving stale formatted text behind.
        ///     替换当前滑块值，且不留下过时的格式化文本。
        /// </summary>
        /// <param name="value">
        ///     The value to apply.
        ///     要应用的值。
        /// </param>
        public void SetValue(float value)
        {
            if (_slider == null)
                return;

            var min = (float)_slider.MinValue;
            var max = (float)_slider.MaxValue;
            var step = (float)_slider.Step;
            var normalized = NormalizeSliderValue(value, min, max, step);

            _suppressCallbacks = true;
            _slider.Value = normalized;
            var actual = (float)_slider.Value;
            RefreshValueLabel(actual);
            _suppressCallbacks = false;
        }

        private static float NormalizeSliderValue(float value, float minValue, float maxValue, float step)
        {
            var v = Mathf.Clamp(value, minValue, maxValue);
            if (step > 0f)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private void InvokeOnChangedSafely(float value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsFloatSliderControl] onChanged failed: {ex.Message}");
            }
        }

        private void SyncBindingToCanonicalSliderValue(float bindingClaimed)
        {
            if (_slider == null)
                return;
            SetValue(bindingClaimed);
        }

        private void RefreshValueLabel(float value)
        {
            if (_valueEdit == null || _formatter == null)
                return;

            try
            {
                _valueEdit.Text = _formatter(value);
            }
            catch
            {
                _valueEdit.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        private void OnValueSubmitted(string text)
        {
            TryApplyTypedValue(text);
            _valueEdit.ReleaseFocusIfInsideTree();
        }

        private void OnValueFocusExited()
        {
            if (_valueEdit != null)
                TryApplyTypedValue(_valueEdit.Text);
        }

        private void TryApplyTypedValue(string text)
        {
            if (_slider == null)
                return;

            if (!float.TryParse(text, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var value) &&
                !float.TryParse(text, out value))
            {
                RefreshValueLabel((float)_slider.Value);
                return;
            }

            value = NormalizeSliderValue(value, (float)_slider.MinValue, (float)_slider.MaxValue,
                (float)_slider.Step);
            _slider.Value = value;
        }

        private static StyleBoxFlat CreateFloatSliderStyle(bool highlighted)
        {
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.slider.layout.grabber.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.slider.layout.grabber.padding", 8);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.top", 6),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.grabber.padding.bottom", 6));
            var bg = highlighted
                ? ResolveSliderColor("components.slider.track.highlight",
                    RitsuShellTheme.Current.Component.Slider.GrabHighlight)
                : ResolveSliderColor("components.slider.track.bg", RitsuShellTheme.Current.Component.Slider.GrabShadow);
            return new()
            {
                BgColor = bg,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static Color ResolveSliderColor(string path, Color fallback)
        {
            return RitsuShellTheme.Current.TryGetColor(path, out var color) ? color : fallback;
        }
    }

    /// <summary>
    ///     Stepper-style choice editor that cycles through labeled values.
    ///     在带标签值之间循环的步进式选项编辑器。
    /// </summary>
    /// <typeparam name="TValue">
    ///     The stored option value type.
    ///     存储的选项值类型。
    /// </typeparam>
    public sealed partial class ModSettingsChoiceControl<TValue> : HBoxContainer
    {
        private readonly Action<TValue>? _onChanged;
        private int _currentIndex;
        private TValue? _currentValue;
        private Label? _label;
        private (TValue Value, string Label)[] _optionsWithValues = [];
        private bool _suppressCallbacks;

        /// <summary>
        ///     Creates a stepper-style choice editor.
        ///     创建步进式选项编辑器。
        /// </summary>
        /// <param name="options">
        ///     The labeled values available to the editor.
        ///     编辑器可用的带标签值。
        /// </param>
        /// <param name="currentValue">
        ///     The value selected initially.
        ///     初始选中的值。
        /// </param>
        /// <param name="onChanged">
        ///     Invoked after the user picks a different value.
        ///     用户选择不同值后调用。
        /// </param>
        public ModSettingsChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = options.ToArray();
            _currentValue = currentValue;
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.choice.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Choice.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            Alignment = AlignmentMode.Center;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.choice.layout.rowSeparation", 6));

            AddChild(new ModSettingsMiniButton("<", () => Shift(-1))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.choice.layout.stepButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize,
                        RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });

            var center = new PanelContainer
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.choice.layout.center.minSize",
                    new(RitsuShellTheme.Current.Metric.Choice.CenterMinWidth,
                        RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            center.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateSurfaceStyle());
            AddChild(center);

            var label = new Label
            {
                Name = "Label",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.Off,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                ClipText = true,
            };
            label.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            label.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            center.AddChild(label);
            _label = label;

            AddChild(new ModSettingsMiniButton(">", () => Shift(1))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.choice.layout.stepButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize,
                        RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsChoiceControl()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_optionsWithValues.Length == 0)
                return;

            var startingIndex = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, _currentValue));
            if (startingIndex < 0)
                startingIndex = 0;
            _currentIndex = startingIndex;
            RefreshCurrentLabel();
        }

        private void Shift(int delta)
        {
            if (_optionsWithValues.Length == 0)
                return;

            _currentIndex = (_currentIndex + delta + _optionsWithValues.Length) % _optionsWithValues.Length;
            RefreshCurrentLabel();
            if (!_suppressCallbacks)
                InvokeOnChangedSafely(_optionsWithValues[_currentIndex].Value);
        }

        private void InvokeOnChangedSafely(TValue value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsChoiceControl] onChanged failed: {ex.Message}");
            }
        }

        /// <summary>
        ///     Selects the matching option without triggering the external change callback.
        ///     选择匹配选项而不触发外部变更回调。
        /// </summary>
        /// <param name="value">
        ///     The value to select.
        ///     要选择的值。
        /// </param>
        public void SetValue(TValue value)
        {
            if (_optionsWithValues.Length == 0)
                return;
            var index = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, value));
            if (index < 0)
                return;
            _suppressCallbacks = true;
            _currentIndex = index;
            RefreshCurrentLabel();
            _suppressCallbacks = false;
        }

        /// <summary>
        ///     Replaces all choices and updates the selected value without invoking callbacks.
        ///     替换所有选项并更新选中值，且不调用回调。
        /// </summary>
        public void SetOptions(IReadOnlyList<(TValue Value, string Label)> options, TValue selectedValue)
        {
            _optionsWithValues = options.ToArray();
            _currentValue = selectedValue;
            _currentIndex = 0;

            if (_optionsWithValues.Length > 0)
            {
                var matched = Array.FindIndex(_optionsWithValues,
                    option => EqualityComparer<TValue>.Default.Equals(option.Value, selectedValue));
                _currentIndex = matched >= 0 ? matched : 0;
            }

            RefreshCurrentLabel();
        }

        private void RefreshCurrentLabel()
        {
            if (_optionsWithValues.Length == 0 || _label == null)
                return;
            _label.Text = _optionsWithValues[_currentIndex].Label;
        }
    }

    /// <summary>
    ///     Dropdown-style choice editor for labeled values.
    ///     用于带标签值的下拉式选项编辑器。
    /// </summary>
    /// <typeparam name="TValue">
    ///     The stored option value type.
    ///     存储的选项值类型。
    /// </typeparam>
    public sealed partial class ModSettingsDropdownChoiceControl<TValue> : HBoxContainer,
        IModSettingsTransientPopupOwner, IModSettingsDirectionalInputClaimant
    {
        private const float DropListMinWidth = 200f;
        private const float RowHeight = 38f;
        private const int DropdownVirtualOverscanRows = 2;
        private const float DropdownViewportEdgeMargin = 16f;

        private readonly Action<TValue>? _onChanged;
        private readonly List<ModSettingsMiniButton> _rowButtons = [];
        private readonly List<ModSettingsMiniButton> _virtualRowPool = [];
        private int _activePoolCount;
        private Control? _backdrop;
        private float _cachedDropdownBodyH;
        private float _dropdownListSeparation;
        private float _dropdownPanelMinWidth;
        private float _dropdownRowStride;
        private bool _dropdownScrollWired;
        private float _dropdownUniformRowLayoutWidth;
        private bool _dropOpen;
        private PanelContainer? _dropPanel;
        private ScrollContainer? _dropScroll;
        private ModSettingsGamepadCompatibleButton? _faceButton;
        private (TValue Value, string Label)[] _optionsWithValues = [];
        private int _selectedIndex;
        private int[] _slotOptionIndex = [];
        private bool _suppressCallbacks;
        private Control? _virtualContent;

        /// <summary>
        ///     Creates a dropdown-style choice editor.
        ///     创建下拉式选项编辑器。
        /// </summary>
        /// <param name="options">
        ///     The labeled values available to the editor.
        ///     编辑器可用的带标签值。
        /// </param>
        /// <param name="currentValue">
        ///     The value selected initially.
        ///     初始选中的值。
        /// </param>
        /// <param name="onChanged">
        ///     Invoked after the user picks a different value.
        ///     用户选择不同值后调用。
        /// </param>
        public ModSettingsDropdownChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = options.ToArray();
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dropdown.layout.entry.minSize",
                new(ModSettingsUiFactory.EntryControlWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;

            _selectedIndex = 0;
            for (var i = 0; i < _optionsWithValues.Length; i++)
                if (EqualityComparer<TValue>.Default.Equals(_optionsWithValues[i].Value, currentValue))
                {
                    _selectedIndex = i;
                    break;
                }

            var face = new ModSettingsGamepadCompatibleButton
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.dropdown.layout.face.minSize",
                    new(ModSettingsUiFactory.EntryControlWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
                Flat = false,
                Disabled = _optionsWithValues.Length == 0,
                Alignment = HorizontalAlignment.Left,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            face.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            face.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            face.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            face.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Color.White);
            face.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Color.White);
            face.AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Color.White);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(face);
            face.Pressed += OnFacePressed;
            AddChild(face);
            _faceButton = face;

            RefreshFaceLabel();
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsDropdownChoiceControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _dropOpen;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            CloseDropdown();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            BuildDropdownShell();
            ApplyFaceDropdownChrome();
            RefreshFaceLabel();
        }

        /// <inheritdoc />
        public override void _ExitTree()
        {
            if (_dropOpen)
                CloseDropdown();
            base._ExitTree();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what != NotificationThemeChanged)
                return;

            if (_dropScroll != null && IsInstanceValid(_dropScroll))
                ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_dropScroll);
            if (_dropPanel != null && IsInstanceValid(_dropPanel))
                _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            if (!_dropOpen || _optionsWithValues.Length == 0)
                return;

            SyncDropdownVirtualContentWidthToShelf();
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
        }

        /// <inheritdoc />
        public override void _Input(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho())
            {
                if (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack))
                {
                    CloseDropdown();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (@event.IsActionPressed("ui_up"))
                {
                    if (TryNavigateVirtualDropdownByDirection(-1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
                else if (@event.IsActionPressed("ui_down"))
                {
                    if (TryNavigateVirtualDropdownByDirection(1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
            }

            base._Input(@event);
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho())
            {
                if (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack))
                {
                    CloseDropdown();
                    GetViewport()?.SetInputAsHandled();
                    return;
                }

                if (@event.IsActionPressed("ui_up"))
                {
                    if (TryNavigateVirtualDropdownByDirection(-1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
                else if (@event.IsActionPressed("ui_down"))
                {
                    if (TryNavigateVirtualDropdownByDirection(1))
                    {
                        GetViewport()?.SetInputAsHandled();
                        return;
                    }
                }
            }

            base._UnhandledInput(@event);
        }

        /// <summary>
        ///     Selects the matching option without reopening the dropdown or firing the external callback.
        ///     选择匹配选项而不重新打开下拉菜单，也不触发外部回调。
        /// </summary>
        /// <param name="value">
        ///     The value to select.
        ///     要选择的值。
        /// </param>
        public void SetValue(TValue value)
        {
            if (_optionsWithValues.Length == 0 || _faceButton == null)
                return;

            var idx = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, value));
            if (idx < 0)
                return;

            _suppressCallbacks = true;
            _selectedIndex = idx;
            RefreshFaceLabel();
            if (_dropOpen)
            {
                SyncVirtualDropdownRows();
                WireRowFocusNeighbors();
            }

            _suppressCallbacks = false;
        }

        /// <summary>
        ///     Replaces all dropdown options and updates the selected value without firing callbacks.
        ///     替换所有下拉选项并更新选中值，且不触发回调。
        /// </summary>
        public void SetOptions(IReadOnlyList<(TValue Value, string Label)> options, TValue selectedValue)
        {
            _optionsWithValues = options.ToArray();
            _selectedIndex = 0;
            for (var i = 0; i < _optionsWithValues.Length; i++)
                if (EqualityComparer<TValue>.Default.Equals(_optionsWithValues[i].Value, selectedValue))
                {
                    _selectedIndex = i;
                    break;
                }

            if (_faceButton != null)
                _faceButton.Disabled = _optionsWithValues.Length == 0;
            RefreshFaceLabel();
            if (_dropOpen)
                RebuildListRows();
        }

        private void OnFacePressed()
        {
            if (_faceButton == null || _faceButton.Disabled || _optionsWithValues.Length == 0)
                return;

            if (_dropOpen)
                CloseDropdown();
            else
                OpenDropdown();
        }

        private void BuildDropdownShell()
        {
            _backdrop = new()
            {
                Name = "ChoiceDropdownBackdrop",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                TopLevel = true,
                ZIndex = 880,
            };
            _backdrop.SetAnchorsPreset(LayoutPreset.TopLeft);
            _backdrop.GuiInput += OnBackdropGuiInput;
            AddChild(_backdrop);

            _dropPanel = new()
            {
                Name = "ChoiceDropdownPanel",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                TopLevel = true,
                ZIndex = 881,
            };
            _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            AddChild(_dropPanel);

            _dropScroll = new()
            {
                Name = "ChoiceDropdownScroll",
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                // Dropdown width should follow content, not the viewport.
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                ClipContents = false,
            };
            _dropPanel.AddChild(_dropScroll);

            _virtualContent = new()
            {
                Name = "ChoiceDropdownVirtualContent",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                ClipContents = false,
            };
            _dropScroll.AddChild(_virtualContent);
            ModSettingsUiControlTheming.ApplySettingsScrollContainerThemeForDropdownList(_dropScroll);
        }

        private void OnBackdropGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                CloseDropdown();
        }

        private void OpenDropdown()
        {
            if (_dropPanel == null || _virtualContent == null || _backdrop == null || _optionsWithValues.Length == 0)
                return;

            _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            RebuildListRows();
            if (_activePoolCount == 0 || _dropScroll == null)
                return;

            TryWireDropdownScroll();
            _dropOpen = true;
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
            SyncVirtualDropdownRows();
            LayoutDropdownInViewport();
            _backdrop.Visible = true;
            _dropPanel.Visible = true;
            WireRowFocusNeighbors();
            Callable.From(GrabSelectedRowFocus).CallDeferred();
            Callable.From(TryFinalizeDropdownLayoutAfterScrollResolved).CallDeferred();
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            _dropOpen = false;
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            TryUnwireDropdownScroll();
            if (_faceButton != null && IsInstanceValid(_faceButton))
                _faceButton.FocusNeighborBottom = null;

            if (_backdrop != null)
                _backdrop.Visible = false;
            if (_dropPanel != null)
                _dropPanel.Visible = false;

            if (_faceButton != null && IsInstanceValid(_faceButton) && _faceButton.IsVisibleInTree())
                _faceButton.GrabFocus();
        }

        private void RebuildListRows()
        {
            if (_virtualContent == null || _dropScroll == null)
                return;

            _rowButtons.Clear();
            _dropdownListSeparation =
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.listSeparation", 8);
            _dropdownRowStride = RowHeight + _dropdownListSeparation;

            var vr = GetViewport().GetVisibleRect();
            var shell = GetDropdownListShellHorizontalInset();
            var faceOuter = ComputeDropdownOuterMinWidthProvisional();

            var n = _optionsWithValues.Length;
            var totalContentH = GetTotalDropdownVirtualContentHeight(n);
            _cachedDropdownBodyH =
                Mathf.Min(totalContentH, EstimateMaxDropdownViewportListHeight(vr));

            if (n == 0)
            {
                _dropdownUniformRowLayoutWidth = 0f;
                _dropdownPanelMinWidth = faceOuter;
                var shelfEmpty = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
                _virtualContent.CustomMinimumSize = new(shelfEmpty, Mathf.Max(totalContentH, RowHeight));
                _activePoolCount = 0;
                _slotOptionIndex = [];
                HideExtraVirtualRows(0);
                if (_dropPanel != null)
                    _dropPanel.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.panel.minSize",
                        new(_dropdownPanelMinWidth, 0f));

                _dropScroll!.CustomMinimumSize = new(shelfEmpty, RowHeight);
                return;
            }

            _dropdownUniformRowLayoutWidth = ComputeUniformDropdownListRowLayoutWidth(vr, n);
            _dropdownPanelMinWidth = Mathf.Max(faceOuter, _dropdownUniformRowLayoutWidth + shell);
            var shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            if (_dropdownUniformRowLayoutWidth > shelfW + 0.5f)
            {
                _dropdownPanelMinWidth = _dropdownUniformRowLayoutWidth + shell;
                shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            }

            _virtualContent.CustomMinimumSize = new(shelfW, Mathf.Max(totalContentH, RowHeight));

            var poolSlots = Mathf.Min(n,
                Mathf.Max(
                    DropdownVirtualOverscanRows * 2 + 1,
                    Mathf.CeilToInt(_cachedDropdownBodyH / Mathf.Max(_dropdownRowStride, 0.001f))
                    + 1 + DropdownVirtualOverscanRows * 2));

            EnsureVirtualDropdownRows(poolSlots);
            _slotOptionIndex = new int[_virtualRowPool.Count];
            Array.Fill(_slotOptionIndex, -1);

            ApplyDropdownScrollViewportSizing(_cachedDropdownBodyH);

            if (_dropPanel != null)
                _dropPanel.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.dropdown.layout.panel.minSize",
                    new(_dropdownPanelMinWidth, 0f));

            _activePoolCount = poolSlots;

            HideExtraVirtualRows(poolSlots);
            ScrollDropdownContentToShowIndex(_selectedIndex);
        }

        private float ComputeDropdownOuterMinWidthProvisional()
        {
            return _faceButton == null
                ? DropListMinWidth
                : Mathf.Max(Mathf.Max(DropListMinWidth, _faceButton.Size.X), _faceButton.CustomMinimumSize.X);
        }

        private float GetDropdownListShellHorizontalInset()
        {
            if (_dropPanel?.GetThemeStylebox("panel") is StyleBoxFlat sb)
                return sb.ContentMarginLeft + sb.ContentMarginRight;

            var pad = RitsuShellThemeLayoutResolver.ResolveEdges("components.listShell.layout.padding", 12);
            return pad.Left + pad.Right;
        }

        private float GetDropdownInnerScrollMinWidth(float outerMinWidth)
        {
            var inset = GetDropdownListShellHorizontalInset();
            return Mathf.Max(1f, outerMinWidth - inset);
        }

        private void ApplyDropdownOuterWidthFromViewport(Rect2 vr)
        {
            if (_faceButton == null || _dropPanel == null || _dropScroll == null || _virtualContent == null)
                return;

            var gr = _faceButton.GetGlobalRect();
            var faceW = gr.Size.X > 2f ? gr.Size.X : Mathf.Max(_faceButton.Size.X, _faceButton.CustomMinimumSize.X);
            var maxOuter = Mathf.Max(DropListMinWidth, vr.Size.X - DropdownViewportEdgeMargin * 2f);
            var faceBased = Mathf.Clamp(Mathf.Max(DropListMinWidth, faceW), DropListMinWidth, maxOuter);
            var newOuter = Mathf.Min(maxOuter, Mathf.Max(faceBased, _dropdownPanelMinWidth));

            if (Mathf.IsEqualApprox(newOuter, _dropdownPanelMinWidth))
                return;

            _dropdownPanelMinWidth = newOuter;
            var shelf = GetDropdownInnerScrollMinWidth(newOuter);
            var hContent = _virtualContent.CustomMinimumSize.Y;
            _virtualContent.CustomMinimumSize = new(shelf, hContent);
            _dropScroll.CustomMinimumSize = new(shelf, _dropScroll.CustomMinimumSize.Y);
            _dropPanel.CustomMinimumSize = new(newOuter, 0f);
            if (_dropOpen)
                SyncVirtualDropdownRows();
        }

        private void SyncDropdownVirtualContentWidthToShelf()
        {
            if (_virtualContent == null)
                return;

            var shelfW = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            _virtualContent.CustomMinimumSize = new(shelfW, _virtualContent.CustomMinimumSize.Y);
        }

        private float GetDropdownRowHorizontalChromeWidth()
        {
            var pad = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.padding", 10);
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.borderWidth", 1);
            return pad.Left + pad.Right + border.Left + border.Right;
        }

        private static float MeasureDropdownLabelDrawableWidth(string label, Font font, int fontSize,
            float horizontalChrome)
        {
            if (string.IsNullOrEmpty(label))
                return horizontalChrome;

            if (font is null)
                return horizontalChrome + label.Length * 8f;

            return horizontalChrome + font.GetStringSize(label, HorizontalAlignment.Left, -1f, fontSize).X;
        }

        private float ComputeMaxDropdownOptionDrawableWidth(int optionCount)
        {
            if (optionCount <= 0)
                return DropListMinWidth;

            var font = RitsuShellTheme.Current.Font.Body;
            var fontSize = RitsuShellTheme.Current.Metric.FontSize.PopupRow;
            var chrome = GetDropdownRowHorizontalChromeWidth();
            var maxW = 0f;
            for (var i = 0; i < optionCount; i++)
            {
                var label = _optionsWithValues[i].Label ?? string.Empty;
                maxW = Mathf.Max(maxW, MeasureDropdownLabelDrawableWidth(label, font, fontSize, chrome));
            }

            return Mathf.Max(maxW, DropListMinWidth);
        }

        private float ResolveDropdownListInnerMaxUniformWidth(Rect2 vr)
        {
            const float edge = DropdownViewportEdgeMargin * 2f;
            var frac = RitsuShellThemeLayoutResolver.ResolveFloat(
                "components.dropdown.layout.list.maxUniformContentWidthFraction", 0.92f);
            var shell = GetDropdownListShellHorizontalInset();
            var fromViewport = Mathf.Max(DropListMinWidth, vr.Size.X * frac - edge - shell);
            var hard = RitsuShellThemeLayoutResolver.ResolveFloat(
                "components.dropdown.layout.list.maxUniformContentWidth", 0f);
            return hard > 1f ? Mathf.Max(DropListMinWidth, Mathf.Min(fromViewport, hard)) : fromViewport;
        }

        private float ComputeUniformDropdownListRowLayoutWidth(Rect2 vr, int optionCount)
        {
            var raw = ComputeMaxDropdownOptionDrawableWidth(optionCount);
            var cap = ResolveDropdownListInnerMaxUniformWidth(vr);
            return Mathf.Max(DropListMinWidth, Mathf.Min(raw, cap));
        }

        private void TryFinalizeDropdownLayoutAfterScrollResolved()
        {
            if (!_dropOpen || _dropScroll == null || _virtualContent == null)
                return;

            _dropScroll.QueueSort();
            var inner = _dropScroll.Size.X;
            if (inner < 2f && _dropScroll.GetGlobalRect().Size.X > 2f)
                inner = _dropScroll.GetGlobalRect().Size.X;

            if (inner < 2f || _dropdownUniformRowLayoutWidth <= 0f)
                return;

            if (_dropdownUniformRowLayoutWidth <= inner + 0.5f)
                return;

            _dropdownUniformRowLayoutWidth = inner;
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
        }

        private static float RowTopOffset(int rowIndex, float rowStride)
        {
            return rowIndex * rowStride;
        }

        private float GetTotalDropdownVirtualContentHeight(int optionCount)
        {
            return optionCount == 0 ? 0f : optionCount * RowHeight + (optionCount - 1) * _dropdownListSeparation;
        }

        private static float EstimateMaxDropdownViewportListHeight(Rect2 visibleRect)
        {
            const float edge = DropdownViewportEdgeMargin * 2f;
            return Mathf.Max(RowHeight, visibleRect.Size.Y * 0.5f - edge);
        }

        private static float ClampToOrderedRange(float value, float a, float b)
        {
            var min = Mathf.Min(a, b);
            var max = Mathf.Max(a, b);
            return Mathf.Clamp(value, min, max);
        }

        private void ApplyDropdownScrollViewportSizing(float bodyH)
        {
            if (_dropScroll == null)
                return;

            var h = Mathf.Max(RowHeight, bodyH);
            var shelf = GetDropdownInnerScrollMinWidth(_dropdownPanelMinWidth);
            _dropScroll.CustomMinimumSize = new(shelf, h);
            _cachedDropdownBodyH = h;
            SyncDropdownVirtualContentWidthToShelf();
        }

        private void EnsureVirtualDropdownRows(int poolSlots)
        {
            if (_virtualContent == null)
                return;

            while (_virtualRowPool.Count < poolSlots)
            {
                var slotIndex = _virtualRowPool.Count;
                var row = new ModSettingsMiniButton(string.Empty, () => OnVirtualPoolRowActivated(slotIndex))
                {
                    Alignment = HorizontalAlignment.Left,
                };
                row.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                row.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PopupRow);
                row.SetAnchorsPreset(LayoutPreset.TopLeft);
                row.FocusEntered += () => _dropScroll?.EnsureControlVisible(row);
                _virtualContent.AddChild(row);
                _virtualRowPool.Add(row);
            }
        }

        private void HideExtraVirtualRows(int firstHiddenIndex)
        {
            for (var i = firstHiddenIndex; i < _virtualRowPool.Count; i++)
            {
                var row = _virtualRowPool[i];
                row.Visible = false;
                row.FocusMode = FocusModeEnum.None;
            }
        }

        private void OnVirtualPoolRowActivated(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotOptionIndex.Length)
                return;

            var optIndex = _slotOptionIndex[slotIndex];
            if (optIndex < 0)
                return;

            ActivateRow(optIndex);
        }

        private void TryWireDropdownScroll()
        {
            if (_dropdownScrollWired || _dropScroll == null)
                return;

            _dropScroll.GetVScrollBar().ValueChanged += OnDropdownScrollValueChanged;
            _dropdownScrollWired = true;
        }

        private void TryUnwireDropdownScroll()
        {
            if (!_dropdownScrollWired || _dropScroll == null)
                return;

            _dropScroll.GetVScrollBar().ValueChanged -= OnDropdownScrollValueChanged;
            _dropdownScrollWired = false;
        }

        private void OnDropdownScrollValueChanged(double _)
        {
            SyncVirtualDropdownRows();
        }

        private void SyncVirtualDropdownRows()
        {
            if (_virtualContent == null || _dropScroll == null || _activePoolCount == 0)
                return;

            var n = _optionsWithValues.Length;
            if (n == 0)
                return;

            var totalH = GetTotalDropdownVirtualContentHeight(n);
            var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
            var maxScroll = Mathf.Max(0f, totalH - viewH);
            var scrollFloat = (float)_dropScroll.ScrollVertical;
            var clampedScroll = ClampToOrderedRange(scrollFloat, 0f, maxScroll);
            if (!Mathf.IsEqualApprox(scrollFloat, clampedScroll))
                _dropScroll.ScrollVertical = (int)Mathf.Round(clampedScroll);

            var scrollY = (float)_dropScroll.ScrollVertical;

            var maxAnchor = Mathf.Max(0, n - _activePoolCount);
            var anchor = Mathf.Clamp(Mathf.FloorToInt(scrollY / Mathf.Max(_dropdownRowStride, 0.001f)), 0, maxAnchor);

            // Reserve scrollbar width (and separation) when visible; otherwise rows will extend under it and get clipped.
            var shelfW = _dropScroll.Size.X > 2f ? _dropScroll.Size.X : _dropScroll.GetGlobalRect().Size.X;
            var reserve = 0f;
            var bar = _dropScroll.GetVScrollBar();
            if (bar != null && IsInstanceValid(bar) && bar.Visible)
            {
                var sep = RitsuShellThemeLayoutResolver.ResolveInt(
                    "components.dropdown.layout.scroll.scrollbarVSeparation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.scrollbar.layout.scrollbarVSeparation", 0));
                reserve = Mathf.Max(0f, bar.Size.X + sep);
            }

            var usableW = Mathf.Max(0f, shelfW - reserve);
            var rowW = _dropdownUniformRowLayoutWidth > 0f
                ? Mathf.Min(_dropdownUniformRowLayoutWidth, usableW)
                : usableW;

            _rowButtons.Clear();

            for (var slot = 0; slot < _activePoolCount; slot++)
            {
                var optIndex = anchor + slot;
                if (optIndex >= n)
                {
                    _slotOptionIndex[slot] = -1;
                    var hidden = _virtualRowPool[slot];
                    hidden.Visible = false;
                    hidden.FocusMode = FocusModeEnum.None;
                    continue;
                }

                _slotOptionIndex[slot] = optIndex;
                var row = _virtualRowPool[slot];
                row.Visible = true;
                row.FocusMode = FocusModeEnum.All;
                ResetDropdownVirtualRowState(row);
                ApplyDropdownVirtualRowPresentation(row, optIndex, rowW);
                var yTop = RowTopOffset(optIndex, _dropdownRowStride);
                row.Position = new(0f, yTop);
                row.TooltipText = string.Empty;

                if (optIndex == _selectedIndex)
                {
                    row.TooltipText = ModSettingsLocalization.Get("choice.dropdown.currentRow",
                        "This option is the active setting (shown on the closed control).");
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.DropdownRow);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeStyleboxOverride("normal", CreateDropdownCurrentRowNormal());
                    row.AddThemeStyleboxOverride("hover", CreateDropdownCurrentRowHover());
                    row.AddThemeStyleboxOverride("pressed", CreateDropdownCurrentRowPressed());
                    row.AddThemeStyleboxOverride("focus", CreateDropdownCurrentRowFocus());
                }
                else
                {
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeStyleboxOverride("normal", ModSettingsMiniButton.CreateStyle(false));
                    row.AddThemeStyleboxOverride("hover", ModSettingsMiniButton.CreateStyle(true));
                    row.AddThemeStyleboxOverride("pressed", ModSettingsMiniButton.CreatePressedStyle());
                    row.AddThemeStyleboxOverride("focus", ModSettingsMiniButton.CreateFocusStyle());
                }

                _rowButtons.Add(row);
            }

            HideExtraVirtualRows(_activePoolCount);
        }

        private static void ResetDropdownVirtualRowState(ModSettingsMiniButton row)
        {
            // Virtual rows are recycled; clear any leftover toggle/pressed state and theme overrides
            // so slot appearance always reflects the current bound option index.
            row.ToggleMode = false;
            row.ButtonPressed = false;

            row.RemoveThemeColorOverride("font_color");
            row.RemoveThemeColorOverride("font_hover_color");
            row.RemoveThemeColorOverride("font_pressed_color");

            row.RemoveThemeStyleboxOverride("normal");
            row.RemoveThemeStyleboxOverride("hover");
            row.RemoveThemeStyleboxOverride("pressed");
            row.RemoveThemeStyleboxOverride("focus");
        }

        private void ApplyDropdownVirtualRowPresentation(ModSettingsMiniButton row, int optIndex, float rowW)
        {
            var opt = _optionsWithValues[optIndex];
            // Make the actual selected option unambiguous vs hover/focus on recycled rows.
            row.Text = optIndex == _selectedIndex ? $"\u2713 {opt.Label}" : opt.Label;
            row.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dropdown.layout.row.minSize",
                new(rowW, RowHeight));
            row.Size = row.CustomMinimumSize;
        }

        private void ScrollDropdownContentToShowIndex(int index)
        {
            if (_dropScroll == null)
                return;

            var n = _optionsWithValues.Length;
            if (n == 0)
                return;

            index = Mathf.Clamp(index, 0, n - 1);
            var y = RowTopOffset(index, _dropdownRowStride);
            var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
            var totalH = GetTotalDropdownVirtualContentHeight(n);
            var maxScroll = Mathf.Max(0f, totalH - viewH);
            var scroll = (float)_dropScroll.ScrollVertical;
            if (y < scroll)
                scroll = y;
            else if (y + RowHeight > scroll + viewH)
                scroll = y + RowHeight - viewH;

            _dropScroll.ScrollVertical = (int)Mathf.Round(ClampToOrderedRange(scroll, 0f, maxScroll));
        }

        private void ActivateRow(int index)
        {
            if (_suppressCallbacks)
                return;

            if (index < 0 || index >= _optionsWithValues.Length)
                return;

            _selectedIndex = index;
            RefreshFaceLabel();
            InvokeOnChangedSafely(_optionsWithValues[index].Value);
            CloseDropdown();
        }

        private void InvokeOnChangedSafely(TValue value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsDropdownChoiceControl] onChanged failed: {ex.Message}");
            }
        }

        private void ApplyFaceDropdownChrome()
        {
            if (_faceButton == null)
                return;

            var arrow = _faceButton.GetThemeIcon("arrow", "OptionButton")
                        ?? _faceButton.GetThemeIcon("select_arrow", "Tree");
            if (arrow == null)
                return;

            _faceButton.Icon = arrow;
            _faceButton.IconAlignment = HorizontalAlignment.Right;
            _faceButton.ExpandIcon = false;
        }

        private void RefreshFaceLabel()
        {
            if (_faceButton == null)
                return;
            if (_optionsWithValues.Length == 0)
            {
                _faceButton.Text = string.Empty;
                _faceButton.TooltipText = string.Empty;
                return;
            }

            var i = Mathf.Clamp(_selectedIndex, 0, _optionsWithValues.Length - 1);
            var label = _optionsWithValues[i].Label;
            _faceButton.Text = _faceButton.Icon != null
                ? label
                : label + ModSettingsLocalization.Get("choice.dropdown.chevronGap", "  ") +
                  ModSettingsLocalization.Get("choice.dropdown.chevron", "\u25be");
            _faceButton.TooltipText = string.Format(
                ModSettingsLocalization.Get("choice.dropdown.tooltip",
                    "Opens a list to choose a value. Current: {0}"),
                label);
        }

        private static StyleBoxFlat CreateDropdownCurrentRowNormal()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.borderWidth", 2);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.dropdown.layout.currentRow.cornerRadius",
                    RitsuShellTheme.Current.Metric.Radius.Default);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.padding.bottom", 5));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Open.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateDropdownCurrentRowHover()
        {
            var s = CreateDropdownCurrentRowNormal();
            s.BgColor = RitsuShellTheme.Current.Component.Dropdown.Hover.Bg;
            return s;
        }

        private static StyleBoxFlat CreateDropdownCurrentRowPressed()
        {
            var s = CreateDropdownCurrentRowNormal();
            s.BgColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Bg;
            return s;
        }

        private static StyleBoxFlat CreateDropdownCurrentRowFocus()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.borderWidthFocus", 3);
            var cornerRadii =
                RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.dropdown.layout.currentRow.cornerRadius",
                    RitsuShellTheme.Current.Metric.Radius.Default);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.currentRow.paddingFocus", 9);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.top", 4),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.currentRow.paddingFocus.bottom",
                    4));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private void WireRowFocusNeighbors()
        {
            if (_faceButton != null)
                _faceButton.FocusNeighborBottom =
                    _rowButtons.Count > 0 ? _rowButtons[0].GetPath() : null;

            for (var i = 0; i < _rowButtons.Count; i++)
            {
                var row = _rowButtons[i];
                row.FocusNeighborLeft = row.GetPath();
                row.FocusNeighborRight = row.GetPath();

                var optIdx = LookupVirtualPoolOptionIndex(row);
                row.FocusNeighborTop = i > 0
                    ? _rowButtons[i - 1].GetPath()
                    : optIdx == 0
                        ? _faceButton?.GetPath()
                        : null;

                row.FocusNeighborBottom =
                    i < _rowButtons.Count - 1 ? _rowButtons[i + 1].GetPath() : null;
            }
        }

        private int LookupVirtualPoolOptionIndex(ModSettingsMiniButton row)
        {
            if (_virtualRowPool.Count == 0 || _slotOptionIndex.Length == 0)
                return -1;

            var limit = Mathf.Min(Mathf.Min(_activePoolCount, _slotOptionIndex.Length), _virtualRowPool.Count);
            for (var s = 0; s < limit; s++)
                if (_virtualRowPool[s] == row)
                    return _slotOptionIndex[s];

            return -1;
        }

        private bool TryNavigateVirtualDropdownByDirection(int delta)
        {
            if (!_dropOpen || GetViewport()?.GuiGetFocusOwner() is not ModSettingsMiniButton focus)
                return false;

            var optIdx = LookupVirtualPoolOptionIndex(focus);
            if (optIdx < 0)
                return false;

            var target = optIdx + delta;
            if (target < 0 || target >= _optionsWithValues.Length)
                return false;

            // Always navigate by option index. Scroll only when needed, rather than waiting for focus to
            // reach the last visible pooled row (which makes scrolling feel "late").
            if (_dropScroll != null)
            {
                var scrollY = (float)_dropScroll.ScrollVertical;
                var viewH = Mathf.Max(RowHeight, _cachedDropdownBodyH);
                var y = RowTopOffset(target, _dropdownRowStride);
                var outOfView = y < scrollY || y + RowHeight > scrollY + viewH;
                if (outOfView)
                    ScrollDropdownContentToShowIndex(target);
            }

            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
            Callable.From(() => TryGrabVirtualRowForOption(target)).CallDeferred();
            return true;
        }

        private void TryGrabVirtualRowForOption(int optionIndex)
        {
            for (var s = 0; s < _activePoolCount; s++)
            {
                if (!IsInstanceValid(_virtualRowPool[s]) ||
                    !_virtualRowPool[s].Visible ||
                    s >= _slotOptionIndex.Length)
                    continue;
                if (_slotOptionIndex[s] != optionIndex)
                    continue;
                _virtualRowPool[s].GrabFocus();
                return;
            }
        }

        private void GrabSelectedRowFocus()
        {
            if (_virtualRowPool.Count == 0)
                return;

            ScrollDropdownContentToShowIndex(_selectedIndex);
            SyncVirtualDropdownRows();
            WireRowFocusNeighbors();
            Callable.From(TryGrabVirtualRowForOptionDelegate).CallDeferred();
        }

        private void TryGrabVirtualRowForOptionDelegate()
        {
            TryGrabVirtualRowForOption(_selectedIndex);
        }

        private void LayoutDropdownInViewport()
        {
            if (_backdrop == null || _dropPanel == null || _faceButton == null)
                return;

            var vr = GetViewport().GetVisibleRect();
            _backdrop.GlobalPosition = vr.Position;
            _backdrop.Size = vr.Size;

            ApplyDropdownOuterWidthFromViewport(vr);

            var scrollBodyH =
                Mathf.Max(RowHeight, _dropScroll?.CustomMinimumSize.Y ?? _cachedDropdownBodyH);

            ApplyDropdownScrollViewportSizing(scrollBodyH);

            _dropPanel.ResetSize();
            _dropPanel.QueueSort();
            var measured = _dropPanel.GetCombinedMinimumSize();
            var panelW = Mathf.Max(_dropdownPanelMinWidth, measured.X);
            var panelSize = new Vector2(panelW, measured.Y);

            var gr = _faceButton.GetGlobalRect();
            var desiredTopLeft = new Vector2(gr.Position.X, gr.End.Y);

            var maxX = Mathf.Max(vr.Position.X, vr.End.X - panelSize.X);
            var maxY = Mathf.Max(vr.Position.Y, vr.End.Y - panelSize.Y);

            desiredTopLeft = new(
                ClampToOrderedRange(desiredTopLeft.X, vr.Position.X, maxX),
                ClampToOrderedRange(desiredTopLeft.Y, vr.Position.Y, maxY));
            _dropPanel.GlobalPosition = desiredTopLeft;
            Callable.From(TryFinalizeDropdownLayoutAfterScrollResolved).CallDeferred();
        }
    }

    /// <summary>
    ///     Color editor with a visible swatch picker and editable hex value.
    ///     带可见色块选择器和可编辑十六进制值的颜色编辑器。
    /// </summary>
    public sealed partial class ModSettingsColorControl : HBoxContainer, IModSettingsTransientPopupOwner
    {
        private readonly Action<string?>? _onChanged;
        private LineEdit? _hexEdit;
        private string _lastCommitted = string.Empty;
        private ColorPickerButton? _pickerButton;
        private bool _pickerChangedWhileOpen;
        private bool _suppressCallbacks;
        private Color _unsetPreviewColor = RitsuShellTheme.Current.Color.UnsetPreview;

        /// <summary>
        ///     Creates a color editor. 保留旧版两参数构造函数以维持 ABI 兼容。
        /// </summary>
        /// <param name="initialValue">The initial color string (hex, HTML, or BaseLib <c>[r,g,b,a]</c>), or null/empty.</param>
        /// <param name="onChanged">Callback invoked after the committed color value changes.</param>
        public ModSettingsColorControl(string? initialValue, Action<string?> onChanged)
            : this(initialValue, onChanged, true, false)
        {
        }

        /// <summary>
        ///     Creates a color editor with picker options.
        ///     创建带选择器选项的颜色编辑器。
        /// </summary>
        /// <param name="initialValue">
        ///     The initial color string (hex, HTML, or BaseLib <c>[r,g,b,a]</c>), or null/empty.
        ///     指定 initial color string (hex, HTML, or BaseLib <c>[r,g,b,a]</c>), or null/empty。
        /// </param>
        /// <param name="onChanged">
        ///     Callback invoked after the committed color value changes.
        ///     已提交颜色值变化后调用的回调。
        /// </param>
        /// <param name="editAlpha">
        ///     Whether the swatch popup exposes alpha editing.
        ///     色块弹窗是否公开 alpha 编辑。
        /// </param>
        /// <param name="editIntensity">
        ///     Whether the popup exposes intensity (HDR) editing.
        ///     弹窗是否公开强度（HDR）编辑。
        /// </param>
        public ModSettingsColorControl(string? initialValue, Action<string?> onChanged, bool editAlpha,
            bool editIntensity)
        {
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.color.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Color.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            Alignment = AlignmentMode.Center;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.color.layout.rowSeparation", 8));

            var pickerButton = new ColorPickerButton
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.color.layout.swatch.minSize",
                    new(RitsuShellTheme.Current.Metric.Color.SwatchSize,
                        RitsuShellTheme.Current.Metric.Color.SwatchSize)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All,
                EditAlpha = editAlpha,
            };
            ModSettingsUiControlTheming.ApplyColorPickerSwatchButtonChrome(pickerButton);
            AddChild(pickerButton);
            _pickerButton = pickerButton;

            var hexEdit = new LineEdit
            {
                PlaceholderText = editAlpha ? "#RRGGBBAA" : "#RRGGBB",
                SelectAllOnFocus = true,
                Alignment = HorizontalAlignment.Center,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.color.layout.valueField.minSize",
                    new(0f, RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(hexEdit,
                RitsuShellTheme.Current.Font.BodyBold);
            AddChild(hexEdit);
            _hexEdit = hexEdit;

            if (pickerButton.GetPicker() is { } picker)
            {
                picker.EditAlpha = editAlpha;
                picker.EditIntensity = editIntensity;
                picker.PresetsVisible = true;
                picker.SamplerVisible = true;
                picker.DeferredMode = false;
            }

            ApplyFromHex(initialValue, false);
        }

        /// <summary>
        ///     Creates the color editor for Godot scene instantiation.
        ///     创建用于 Godot 场景实例化的颜色编辑器。
        /// </summary>
        public ModSettingsColorControl()
        {
        }

        /// <summary>
        ///     Current hex text shown by the editor, or an empty string when the color is unset.
        ///     编辑器当前显示的 hex 文本；颜色未设置时为空字符串。
        /// </summary>
        public string ValueText => _hexEdit?.Text ?? _lastCommitted;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            if (_pickerButton?.GetPopup() is { Visible: true } popup)
                popup.Hide();
            _pickerChangedWhileOpen = false;
        }

        /// <summary>
        ///     Serializes <paramref name="color" /> as an 8-digit hex string for settings storage / display.
        ///     将 <paramref name="color" /> 序列化为 8 位十六进制字符串，用于设置存储 / 显示。
        /// </summary>
        public static string FormatStoredColorString(Color color)
        {
            return FormatColorValue(color);
        }

        /// <summary>
        ///     Parses hex, Godot HTML, or BaseLib-style <c>[r, g, b, a]</c> component lists (invariant floats).
        ///     解析 hex、Godot HTML 或 BaseLib 风格的 <c>[r, g, b, a]</c> 分量列表（固定区域性的浮点数）。
        /// </summary>
        public static bool TryDeserializeColorForSettings(string? text, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            if (TryParseHexColorString(trimmed, out color))
                return true;

            try
            {
                color = Color.FromHtml(trimmed);
                return true;
            }
            catch
            {
                // ignored
            }

            return TryParseBracketRgbaColor(trimmed, out color);
        }

        /// <summary>
        ///     Wires editor events after the control enters the scene tree.
        ///     控件进入场景树后连接编辑器事件。
        /// </summary>
        public override void _Ready()
        {
            if (_hexEdit != null)
            {
                _hexEdit.TextSubmitted += text =>
                {
                    ApplyFromHex(text, true);
                    _hexEdit.ReleaseFocusIfInsideTree();
                };
                _hexEdit.FocusExited += () => ApplyFromHex(_hexEdit.Text, true);
                ModSettingsFocusChrome.AttachControllerSelectionReticle(_hexEdit);
            }

            if (_pickerButton == null) return;
            _pickerButton.PopupClosed += OnPickerPopupClosed;
            _pickerButton.ColorChanged += OnPickerColorChanged;
            if (_pickerButton.GetPopup() is { } popup)
                popup.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_pickerButton);
        }

        /// <summary>
        ///     Updates the displayed value without recreating the control.
        ///     更新显示值而不重新创建控件。
        /// </summary>
        /// <param name="value">
        ///     The hex color to display, or null/empty to leave the field unset.
        ///     要显示的 hex 颜色；为 null/空时保持字段未设置。
        /// </param>
        public void SetValue(string? value)
        {
            ApplyFromHex(value, false);
        }

        private void ApplyFromHex(string? text, bool notify)
        {
            var trimmed = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                ApplyUnset(notify);
                return;
            }

            if (!TryDeserializeColorForSettings(trimmed, out var color))
            {
                RestoreCurrentPresentation();
                return;
            }

            ApplyColor(color, notify);
        }

        private void ApplyUnset(bool notify)
        {
            if (_suppressCallbacks)
                return;

            _suppressCallbacks = true;
            _hexEdit?.Set("text", string.Empty);
            _pickerButton?.Set("color", _unsetPreviewColor);
            _suppressCallbacks = false;
            _lastCommitted = string.Empty;

            if (notify)
                InvokeOnChangedSafely(null);
        }

        private void ApplyColor(Color color, bool notify)
        {
            if (_suppressCallbacks)
                return;

            var formatted = FormatColorValue(color);
            _suppressCallbacks = true;
            _pickerButton?.Set("color", color);
            _hexEdit?.Set("text", formatted);
            _suppressCallbacks = false;
            _lastCommitted = formatted;
            _unsetPreviewColor = color;

            if (notify)
                InvokeOnChangedSafely(formatted);
        }

        private void RestoreCurrentPresentation()
        {
            ApplyFromHex(_lastCommitted, false);
        }

        private void OnPickerColorChanged(Color color)
        {
            _pickerChangedWhileOpen = true;
            ApplyColor(color, false);
            InvokeOnChangedSafely(FormatColorValue(color));
        }

        private void InvokeOnChangedSafely(string? value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsColorControl] onChanged failed: {ex.Message}");
            }
        }

        private void OnPickerPopupClosed()
        {
            _pickerChangedWhileOpen = false;
            if (_pickerButton != null && IsInstanceValid(_pickerButton) && _pickerButton.IsVisibleInTree())
                _pickerButton.GrabFocus();
        }

        private static bool TryParseHexColorString(string text, out Color color)
        {
            var trimmed = text.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                color = default;
                return false;
            }

            if (!trimmed.StartsWith('#'))
                trimmed = $"#{trimmed}";

            var hex = trimmed[1..];
            if (hex.Length is not (3 or 4 or 6 or 8) || hex.Any(c => !Uri.IsHexDigit(c)))
            {
                color = default;
                return false;
            }

            if (hex.Length is 3 or 4)
                hex = string.Concat(hex.Select(c => new string(c, 2)));
            if (hex.Length == 6)
                hex += "FF";

            color = new(
                Convert.ToByte(hex[..2], 16) / 255f,
                Convert.ToByte(hex[2..4], 16) / 255f,
                Convert.ToByte(hex[4..6], 16) / 255f,
                Convert.ToByte(hex[6..8], 16) / 255f);
            return true;
        }

        private static bool TryParseBracketRgbaColor(string text, out Color color)
        {
            color = default;
            var s = text.Trim();
            if (s.Length < 7)
                return false;

            s = s.Trim('[', ']');
            var parts = s.Split(',');
            if (parts.Length != 4)
                return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var a)) return false;
            color = new(r, g, b, a);
            return true;
        }

        private static string FormatColorValue(Color color)
        {
            return
                $"#{Mathf.RoundToInt(color.R * 255f):X2}{Mathf.RoundToInt(color.G * 255f):X2}{Mathf.RoundToInt(color.B * 255f):X2}{Mathf.RoundToInt(color.A * 255f):X2}";
        }
    }

    /// <summary>
    ///     Keybinding capture editor used by settings pages and custom editors.
    ///     设置页面和自定义编辑器使用的按键绑定捕获编辑器。
    /// </summary>
    public sealed partial class ModSettingsKeyBindingControl : VBoxContainer, IModSettingsDirectionalInputClaimant
    {
        private readonly bool _allowModifierCombos;
        private readonly bool _allowModifierOnly;
        private readonly bool _distinguishModifierSides;
        private readonly Action<string>? _onChanged;
        private readonly HashSet<string> _pendingModifierBindings = [];
        private Button? _captureButton;
        private bool _capturing;
        private string _currentValue = string.Empty;
        private Label? _hintLabel;

        /// <summary>
        ///     Creates a keybinding capture editor.
        ///     创建按键绑定捕获编辑器。
        /// </summary>
        /// <param name="initialValue">
        ///     The binding shown initially.
        ///     初始显示的绑定。
        /// </param>
        /// <param name="allowModifierCombos">
        ///     Whether modifier combinations are allowed.
        ///     是否允许修饰键组合。
        /// </param>
        /// <param name="allowModifierOnly">
        ///     Whether modifier-only bindings are allowed.
        ///     是否允许仅修饰键的绑定。
        /// </param>
        /// <param name="distinguishModifierSides">
        ///     Whether left and right modifier keys are recorded separately.
        ///     是否分别记录左、右修饰键。
        /// </param>
        /// <param name="onChanged">
        ///     Invoked after the binding changes.
        ///     绑定变化后调用。
        /// </param>
        public ModSettingsKeyBindingControl(string initialValue, bool allowModifierCombos, bool allowModifierOnly,
            bool distinguishModifierSides, Action<string> onChanged)
        {
            _allowModifierCombos = allowModifierCombos;
            _allowModifierOnly = allowModifierOnly;
            _distinguishModifierSides = distinguishModifierSides;
            _onChanged = onChanged;
            _currentValue = initialValue;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.keybinding.layout.block.minSize",
                new(RitsuShellTheme.Current.Metric.Keybinding.BlockWidth, 80f));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.blockSeparation", 8));

            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.rowSeparation", 6));
            AddChild(row);

            var captureButton = new Button
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.captureButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Keybinding.CaptureMinWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            captureButton.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            captureButton.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            captureButton.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(captureButton);
            row.AddChild(captureButton);
            _captureButton = captureButton;

            row.AddChild(new ModSettingsMiniButton(ModSettingsLocalization.Get("button.clear", "Clear"),
                () => ApplyBinding(string.Empty, true))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.clearButton.minSize",
                    new(64f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });

            var hint = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = allowModifierCombos
                    ? ModSettingsLocalization.Get("keybinding.hint.combo",
                        "Click to record. Supports key combinations.")
                    : ModSettingsLocalization.Get("keybinding.hint.single", "Click to record a single key."),
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.Keybinding.HintFontSize);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshText();
            SetProcessUnhandledKeyInput(true);
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsKeyBindingControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _capturing;

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_captureButton != null)
                _captureButton.Pressed += BeginCapture;
        }

        /// <summary>
        ///     Replaces the captured binding without starting capture mode.
        ///     替换捕获的绑定而不启动捕获模式。
        /// </summary>
        /// <param name="value">
        ///     The binding to display.
        ///     要显示的绑定。
        /// </param>
        public void SetValue(string value)
        {
            _currentValue = value;
            if (!_capturing)
                RefreshText();
        }

        /// <inheritdoc />
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!_capturing || @event is not InputEventKey keyEvent || keyEvent.IsEcho())
                return;

            GetViewport().SetInputAsHandled();

            if (keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Escape:
                        _capturing = false;
                        _pendingModifierBindings.Clear();
                        RefreshText();
                        return;
                    case Key.Backspace or Key.Delete:
                        ApplyBinding(string.Empty, true);
                        _capturing = false;
                        _pendingModifierBindings.Clear();
                        return;
                }

                if (IsModifierKey(keyEvent.Keycode))
                {
                    if (!_allowModifierCombos && !_allowModifierOnly)
                        return;

                    _pendingModifierBindings.Add(GetRecordedKeyName(keyEvent, _distinguishModifierSides));
                    RefreshText();
                    return;
                }

                var binding = BuildBindingFromPendingModifiers(keyEvent, _allowModifierCombos,
                    _distinguishModifierSides, _pendingModifierBindings);
                if (string.IsNullOrWhiteSpace(binding))
                    return;

                ApplyBinding(binding, true);
                _capturing = false;
                _pendingModifierBindings.Clear();
                return;
            }

            if (_pendingModifierBindings.Count == 0 || !IsModifierKey(keyEvent.Keycode))
                return;

            if (_allowModifierOnly)
                ApplyBinding(string.Join('+', OrderedModifierTokens(_pendingModifierBindings)), true);

            _capturing = false;
            _pendingModifierBindings.Clear();
            RefreshText();
        }

        private void BeginCapture()
        {
            _capturing = true;
            _pendingModifierBindings.Clear();
            RefreshText();
            _captureButton?.GrabFocus();
        }

        private void ApplyBinding(string value, bool notify)
        {
            _currentValue = value;
            RefreshText();
            if (notify)
                InvokeOnChangedSafely(value);
        }

        private void InvokeOnChangedSafely(string value)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(value);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsKeyBindingControl] onChanged failed: {ex.Message}");
            }
        }

        private void RefreshText()
        {
            var pendingBindingText = _pendingModifierBindings.Count == 0
                ? string.Empty
                : string.Join('+', OrderedModifierTokens(_pendingModifierBindings));

            var captureText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? ModSettingsLocalization.Get("keybinding.capturing", "Press combination...")
                    : pendingBindingText + "+..."
                : string.IsNullOrWhiteSpace(_currentValue)
                    ? ModSettingsLocalization.Get("keybinding.unbound", "Unbound")
                    : _currentValue;
            if (_captureButton != null)
                _captureButton.Text = captureText;

            var hintText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? ModSettingsLocalization.Get("keybinding.hint.capturing",
                        "Press a key combination. Esc cancels, Backspace/Delete clears.")
                    : ModSettingsLocalization.Get("keybinding.hint.capturingPending",
                        "Modifier keys recorded. Press another key to complete, or release to keep a modifier-only binding.")
                : _allowModifierCombos
                    ? _allowModifierOnly
                        ? ModSettingsLocalization.Get("keybinding.hint.combo",
                            "Click to record. Supports key combinations.")
                        : ModSettingsLocalization.Get("keybinding.hint.comboNonModifier",
                            "Click to record. Supports key combinations and requires a non-modifier key.")
                    : ModSettingsLocalization.Get("keybinding.hint.single", "Click to record a single key.");
            if (_hintLabel != null)
                _hintLabel.Text = hintText;
        }

        internal static string BuildBindingFromPendingModifiers(InputEventKey keyEvent, bool allowModifierCombos,
            bool distinguishModifierSides, IEnumerable<string> pendingModifiers)
        {
            var parts = allowModifierCombos
                ? OrderedModifierTokens(pendingModifiers).ToList()
                : [];
            parts.Add(GetRecordedKeyName(keyEvent, distinguishModifierSides));
            return string.Join('+', parts);
        }

        /// <summary>
        ///     Uses <see cref="InputEventKey.PhysicalKeycode" /> when <paramref name="distinguishModifierSides" /> is true
        ///     so Left Ctrl / Right Shift etc. are distinguished; otherwise uses the logical <see cref="InputEventKey.Keycode" />.
        ///     当 <paramref name="distinguishModifierSides" /> 为 true 时使用 <see cref="InputEventKey.PhysicalKeycode" />，以区分 Left
        ///     Ctrl / Right Shift 等；否则使用逻辑 <see cref="InputEventKey.Keycode" />。
        /// </summary>
        internal static string GetRecordedKeyName(InputEventKey keyEvent, bool distinguishModifierSides)
        {
            var code = distinguishModifierSides ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            if (code == Key.None)
                code = keyEvent.Keycode;
            return code.ToString();
        }

        internal static IEnumerable<string> OrderedModifierTokens(IEnumerable<string> tokens)
        {
            return tokens.OrderBy(GetModifierSortOrder).ThenBy(static t => t, StringComparer.OrdinalIgnoreCase);
        }

        private static int GetModifierSortOrder(string token)
        {
            var normalized = token.ToLowerInvariant();
            if (normalized.Contains("ctrl") || normalized.Contains("control"))
                return 0;
            if (normalized.Contains("alt"))
                return 1;
            if (normalized.Contains("shift"))
                return 2;
            if (normalized.Contains("meta") || normalized.Contains("cmd") || normalized.Contains("command"))
                return 3;
            return 100;
        }

        internal static bool IsModifierKey(Key key)
        {
            var name = key.ToString().ToLowerInvariant();
            return name.Contains("shift") || name.Contains("ctrl") || name.Contains("control") ||
                   name.Contains("alt") || name.Contains("meta") || name.Contains("cmd") ||
                   name.Contains("command");
        }
    }

    internal sealed partial class ModSettingsActionsButton : ModSettingsGamepadCompatibleButton,
        IModSettingsTransientPopupOwner, IModSettingsDirectionalInputClaimant
    {
        private const float DropMinWidth = 260f;
        private const float RowHeight = 38f;

        private static readonly SharedActionsDropdown SharedDropdown = new();

        private readonly IReadOnlyList<ModSettingsMenuAction> _actions;
        private readonly Action? _afterAction;
        private bool _dropOpen;
        private Vector2I? _preferredPopupPosition;

        public ModSettingsActionsButton(IReadOnlyList<ModSettingsMenuAction> actions, Action? afterAction = null)
        {
            _actions = actions;
            _afterAction = afterAction;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            Text = ModSettingsLocalization.Get("button.actionsGlyph", "\u22ee");
            TooltipText = ModSettingsLocalization.Get("button.actionsShort", "Actions");
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.chromeMenu.layout.trigger.minSize",
                new(36f, 32f));
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichSecondary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Color.White);
            AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateChromeActionsMenuStyle(false));
            AddThemeStyleboxOverride("hover", ModSettingsUiFactory.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("pressed", ModSettingsUiFactory.CreateChromeActionsMenuStyle(true));
            AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateChromeActionsMenuStyle(true));
            Pressed += OnEllipsisPressed;
        }

        public ModSettingsActionsButton()
        {
            _actions = [];
            Pressed += OnEllipsisPressed;
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _dropOpen;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            CloseDropdown();
        }

        public override void _ExitTree()
        {
            if (_dropOpen)
                CloseDropdown();
            base._ExitTree();
        }

        public override void _Input(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
            {
                CloseDropdown();
                GetViewport()?.SetInputAsHandled();
                return;
            }

            base._Input(@event);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_dropOpen && !@event.IsEcho() &&
                (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
            {
                CloseDropdown();
                GetViewport()?.SetInputAsHandled();
                return;
            }

            base._UnhandledInput(@event);
        }

        public void OpenAt(Vector2 globalPosition)
        {
            if (Disabled || ProcessMode == ProcessModeEnum.Disabled)
                return;
            _preferredPopupPosition = new Vector2I(
                Mathf.RoundToInt(globalPosition.X),
                Mathf.RoundToInt(globalPosition.Y));
            if (_dropOpen)
            {
                SharedDropdown.LayoutInViewport(this);
                return;
            }

            OpenDropdown();
        }

        private void OnEllipsisPressed()
        {
            if (Disabled)
                return;

            if (_dropOpen)
                CloseDropdown();
            else
                OpenDropdown();
        }

        private void OpenDropdown()
        {
            if (_actions.Count == 0)
                return;

            SharedDropdown.Open(this);
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            SharedDropdown.Close(this, true);
        }

        private void OnSharedDropdownOpened()
        {
            _dropOpen = true;
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
        }

        private void OnSharedDropdownClosed(bool restoreFocus)
        {
            _dropOpen = false;
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            _preferredPopupPosition = null;

            if (restoreFocus && IsInstanceValid(this) && IsVisibleInTree())
                GrabFocus();
        }

        private static StyleBoxFlat CreateActionsRowStyle(bool highlighted)
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.Dropdown.Hover.Bg
                    : RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.Dropdown.Hover.Border
                    : RitsuShellTheme.Current.Component.Dropdown.Open.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateActionsRowPressedStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Pressed.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateActionsRowFocusStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Focus.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        private static StyleBoxFlat CreateActionsRowDisabledStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.borderWidth", 1);
            var padding =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.dropdown.layout.actionsRow.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.actionsRow.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dropdown.layout.actionsRow.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.Dropdown.Open.Bg,
                BorderColor = RitsuShellTheme.Current.Component.Dropdown.Open.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        internal void ForceCloseDropdown()
        {
            CloseDropdown();
        }

        private void ActivateRow(int index)
        {
            if (index < 0 || index >= _actions.Count)
                return;

            var def = _actions[index];
            if (!def.IsEnabled())
                return;

            try
            {
                def.Action();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsActionsButton] action failed: {ex.Message}");
            }

            if (_afterAction != null)
                try
                {
                    _afterAction();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[ModSettingsActionsButton] afterAction failed: {ex.Message}");
                }

            CloseDropdown();
        }

        private sealed class SharedActionsDropdown
        {
            private readonly System.Collections.Generic.Dictionary<int, ModSettingsMiniButton> _rowButtonCache = [];
            private readonly List<ModSettingsMiniButton> _rowButtons = [];
            private Control? _backdrop;
            private VBoxContainer? _dropList;
            private PanelContainer? _dropPanel;
            private ModSettingsActionsButton? _owner;

            internal void Open(ModSettingsActionsButton owner)
            {
                if (!IsInstanceValid(owner))
                    return;

                if (_owner != null && !IsInstanceValid(_owner))
                    _owner = null;
                else if (_owner != null && !ReferenceEquals(_owner, owner))
                    Close(_owner, false);

                EnsureShell(owner);
                if (_dropPanel == null || _dropList == null || _backdrop == null)
                    return;

                _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
                RebuildRows(owner);
                if (_rowButtons.Count == 0)
                    return;

                _owner = owner;
                owner.OnSharedDropdownOpened();
                LayoutInViewport(owner);
                _backdrop.Visible = true;
                _dropPanel.Visible = true;
                WireRowFocusNeighbors();
                Callable.From(GrabFirstEnabledRow).CallDeferred();
            }

            internal void Close(ModSettingsActionsButton owner, bool restoreFocus)
            {
                if (_owner == null || !ReferenceEquals(_owner, owner))
                    return;

                _owner = null;
                if (_backdrop != null && IsInstanceValid(_backdrop))
                    _backdrop.Visible = false;
                if (_dropPanel != null && IsInstanceValid(_dropPanel))
                    _dropPanel.Visible = false;

                if (IsInstanceValid(owner))
                    owner.OnSharedDropdownClosed(restoreFocus);
            }

            internal void LayoutInViewport(ModSettingsActionsButton owner)
            {
                if (_backdrop == null || _dropPanel == null || !IsInstanceValid(owner))
                    return;

                var vr = owner.GetViewport().GetVisibleRect();
                _backdrop.GlobalPosition = vr.Position;
                _backdrop.Size = vr.Size;

                _dropPanel.Size = Vector2.Zero;
                _dropPanel.ResetSize();
                var panelSize = _dropPanel.GetCombinedMinimumSize();
                Vector2 desiredTopLeft;
                if (owner._preferredPopupPosition.HasValue)
                {
                    desiredTopLeft = new(
                        owner._preferredPopupPosition.Value.X,
                        owner._preferredPopupPosition.Value.Y);
                }
                else
                {
                    var gr = owner.GetGlobalRect();
                    desiredTopLeft = new(gr.End.X - panelSize.X, gr.End.Y);
                }

                var maxX = Mathf.Max(vr.Position.X, vr.End.X - panelSize.X);
                var maxY = Mathf.Max(vr.Position.Y, vr.End.Y - panelSize.Y);
                desiredTopLeft = new(
                    Mathf.Clamp(desiredTopLeft.X, vr.Position.X, maxX),
                    Mathf.Clamp(desiredTopLeft.Y, vr.Position.Y, maxY));
                _dropPanel.GlobalPosition = desiredTopLeft;
            }

            private void EnsureShell(ModSettingsActionsButton owner)
            {
                if (_backdrop == null || !IsInstanceValid(_backdrop) ||
                    _dropPanel == null || !IsInstanceValid(_dropPanel) ||
                    _dropList == null || !IsInstanceValid(_dropList))
                {
                    BuildShell(owner);
                    return;
                }

                EnsureParent(_backdrop, owner);
                EnsureParent(_dropPanel, owner);
            }

            private void BuildShell(ModSettingsActionsButton owner)
            {
                _backdrop = new()
                {
                    Name = "SharedActionsMenuBackdrop",
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Stop,
                    TopLevel = true,
                    ZIndex = 900,
                };
                _backdrop.SetAnchorsPreset(LayoutPreset.TopLeft);
                _backdrop.GuiInput += OnBackdropGuiInput;
                owner.AddChild(_backdrop);

                _dropPanel = new()
                {
                    Name = "SharedActionsMenuPanel",
                    Visible = false,
                    MouseFilter = MouseFilterEnum.Stop,
                    TopLevel = true,
                    ZIndex = 901,
                    CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.actionsMenu.minSize",
                        new(DropMinWidth, 0f)),
                };
                _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
                owner.AddChild(_dropPanel);

                _dropList = new()
                {
                    Name = "SharedActionsMenuList",
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _dropList.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt(
                        "components.dropdown.layout.actionsMenu.listSeparation",
                        8));
                _dropPanel.AddChild(_dropList);
                _rowButtonCache.Clear();
                _rowButtons.Clear();
            }

            private static void EnsureParent(Node node, Node parent)
            {
                if (node.GetParent() == parent)
                    return;

                node.GetParent()?.RemoveChild(node);
                parent.AddChild(node);
            }

            private void OnBackdropGuiInput(InputEvent @event)
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } &&
                    _owner != null)
                    Close(_owner, true);
            }

            private void RebuildRows(ModSettingsActionsButton owner)
            {
                if (_dropList == null)
                    return;

                _rowButtons.Clear();
                var liveIndexes = Enumerable.Range(0, owner._actions.Count).ToHashSet();
                foreach (var staleIndex in _rowButtonCache.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
                {
                    if (_rowButtonCache.TryGetValue(staleIndex, out var staleRow) &&
                        IsInstanceValid(staleRow))
                    {
                        if (staleRow.GetParent() == _dropList)
                            _dropList.RemoveChild(staleRow);
                        staleRow.QueueFree();
                    }

                    _rowButtonCache.Remove(staleIndex);
                }

                for (var i = 0; i < owner._actions.Count; i++)
                {
                    var index = i;
                    var def = owner._actions[i];
                    if (!_rowButtonCache.TryGetValue(index, out var row) || !IsInstanceValid(row))
                    {
                        row = new(def.Label, () => _owner?.ActivateRow(index))
                        {
                            SizeFlagsHorizontal = SizeFlags.ExpandFill,
                            Alignment = HorizontalAlignment.Left,
                        };
                        row.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                        row.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PopupRow);
                        _rowButtonCache[index] = row;
                    }

                    row.Text = def.Label;
                    var rowPadX = RitsuShellThemeLayoutResolver.ResolveFloat(
                        "components.dropdown.layout.actionsRow.hInset", 24f);
                    row.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                        "components.dropdown.layout.actionsRow.minSize",
                        new(DropMinWidth - rowPadX, RowHeight));
                    row.Disabled = !def.IsEnabled();
                    row.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.DropdownRow);
                    row.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
                    row.AddThemeColorOverride("font_disabled_color", RitsuShellTheme.Current.Text.LabelSecondary);
                    row.AddThemeStyleboxOverride("normal", CreateActionsRowStyle(false));
                    row.AddThemeStyleboxOverride("hover", CreateActionsRowStyle(true));
                    row.AddThemeStyleboxOverride("pressed", CreateActionsRowPressedStyle());
                    row.AddThemeStyleboxOverride("focus", CreateActionsRowFocusStyle());
                    row.AddThemeStyleboxOverride("disabled", CreateActionsRowDisabledStyle());
                    if (row.GetParent() != _dropList)
                        _dropList.AddChild(row);
                    _dropList.MoveChild(row, i);
                    _rowButtons.Add(row);
                }

                _dropList.ResetSize();
                _dropPanel?.ResetSize();
            }

            private void WireRowFocusNeighbors()
            {
                for (var i = 0; i < _rowButtons.Count; i++)
                {
                    var row = _rowButtons[i];
                    var selfPath = row.GetPath();
                    row.FocusNeighborLeft = selfPath;
                    row.FocusNeighborRight = selfPath;
                    row.FocusNeighborTop = i > 0 ? _rowButtons[i - 1].GetPath() : null;
                    row.FocusNeighborBottom = i < _rowButtons.Count - 1 ? _rowButtons[i + 1].GetPath() : null;
                }
            }

            private void GrabFirstEnabledRow()
            {
                foreach (var row in _rowButtons.Where(row => !row.Disabled && row.IsVisibleInTree()))
                {
                    row.GrabFocus();
                    return;
                }
            }
        }
    }

    /// <summary>
    ///     Multi-keybinding capture editor used by native settings pages.
    ///     原生设置页面使用的多按键绑定捕获编辑器。
    /// </summary>
    public sealed partial class ModSettingsMultiKeyBindingControl : VBoxContainer, IModSettingsDirectionalInputClaimant
    {
        private readonly bool _allowModifierCombos;
        private readonly bool _allowModifierOnly;
        private readonly bool _distinguishModifierSides;
        private readonly Action<List<string>>? _onChanged;
        private readonly HashSet<string> _pendingModifierBindings = [];
        private VBoxContainer? _bindingsList;
        private bool _capturing;
        private int _capturingIndex = -1;
        private bool _capturingNewBinding;
        private Label? _hintLabel;
        private List<string> _values = [];

        /// <summary>
        ///     Creates a native multi-binding capture editor.
        ///     创建原生多绑定捕获编辑器。
        /// </summary>
        /// <param name="initialValues">
        ///     Initial bindings shown by the control.
        ///     控件初始显示的绑定。
        /// </param>
        /// <param name="allowModifierCombos">
        ///     Whether modifier combinations are allowed.
        ///     是否允许修饰键组合。
        /// </param>
        /// <param name="allowModifierOnly">
        ///     Whether modifier-only bindings are allowed.
        ///     是否允许仅修饰键的绑定。
        /// </param>
        /// <param name="distinguishModifierSides">
        ///     Whether left/right modifier keys are recorded separately.
        ///     是否分别记录左/右修饰键。
        /// </param>
        /// <param name="onChanged">
        ///     Invoked after the normalized binding list changes.
        ///     规范化后的绑定列表变化后调用。
        /// </param>
        public ModSettingsMultiKeyBindingControl(IEnumerable<string>? initialValues, bool allowModifierCombos,
            bool allowModifierOnly, bool distinguishModifierSides, Action<List<string>> onChanged)
        {
            _allowModifierCombos = allowModifierCombos;
            _allowModifierOnly = allowModifierOnly;
            _distinguishModifierSides = distinguishModifierSides;
            _onChanged = onChanged;
            _values = NormalizeBindings(initialValues);

            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.blockSeparation", 8));

            var bindingsList = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            bindingsList.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.listSeparation", 6));
            AddChild(bindingsList);
            _bindingsList = bindingsList;

            var hint = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.Keybinding.HintFontSize);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshPresentation();
            SetProcessUnhandledKeyInput(true);
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsMultiKeyBindingControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _capturing;

        /// <inheritdoc />
        public override void _Ready()
        {
        }

        /// <summary>
        ///     Replaces the displayed binding list without starting capture mode.
        ///     替换显示的绑定列表而不启动捕获模式。
        /// </summary>
        /// <param name="values">
        ///     The bindings to display.
        ///     要显示的绑定。
        /// </param>
        public void SetValue(IEnumerable<string>? values)
        {
            _values = NormalizeBindings(values);
            if (!_capturing)
                RefreshPresentation();
        }

        /// <inheritdoc />
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!_capturing || @event is not InputEventKey keyEvent || keyEvent.IsEcho())
                return;

            GetViewport().SetInputAsHandled();

            if (keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Escape:
                        CancelCapture();
                        return;
                    case Key.Backspace or Key.Delete:
                        if (_capturingIndex >= 0)
                            RemoveBindingAt(_capturingIndex);
                        else
                            ApplyBindings([], true);
                        _capturing = false;
                        _capturingIndex = -1;
                        _capturingNewBinding = false;
                        _pendingModifierBindings.Clear();
                        return;
                }

                if (ModSettingsKeyBindingControl.IsModifierKey(keyEvent.Keycode))
                {
                    if (!_allowModifierCombos && !_allowModifierOnly)
                        return;

                    _pendingModifierBindings.Add(ModSettingsKeyBindingControl.GetRecordedKeyName(keyEvent,
                        _distinguishModifierSides));
                    RefreshPresentation();
                    return;
                }

                var binding = ModSettingsKeyBindingControl.BuildBindingFromPendingModifiers(keyEvent,
                    _allowModifierCombos, _distinguishModifierSides, _pendingModifierBindings);
                if (string.IsNullOrWhiteSpace(binding))
                    return;

                CommitCapturedBinding(binding);
                _capturing = false;
                _capturingIndex = -1;
                _capturingNewBinding = false;
                _pendingModifierBindings.Clear();
                RefreshPresentation();
                return;
            }

            if (_pendingModifierBindings.Count == 0 || !ModSettingsKeyBindingControl.IsModifierKey(keyEvent.Keycode))
                return;

            if (_allowModifierOnly)
                CommitCapturedBinding(string.Join('+',
                    ModSettingsKeyBindingControl.OrderedModifierTokens(_pendingModifierBindings)));

            _capturing = false;
            _capturingIndex = -1;
            _capturingNewBinding = false;
            _pendingModifierBindings.Clear();
            RefreshPresentation();
        }

        private void BeginAddCapture()
        {
            _capturing = true;
            _capturingIndex = -1;
            _capturingNewBinding = true;
            _pendingModifierBindings.Clear();
            RefreshPresentation();
        }

        private void BeginCapture(int index)
        {
            _capturing = true;
            _capturingIndex = index;
            _capturingNewBinding = false;
            _pendingModifierBindings.Clear();
            RefreshPresentation();
            FocusCaptureRow(index);
        }

        private void FocusCaptureRow(int index)
        {
            if (index < 0 || !(_bindingsList?.GetChildCount() > index)) return;
            if (_bindingsList.GetChild(index) is Control row)
                row.GrabFocus();
        }

        private void CancelCapture()
        {
            _capturing = false;
            _capturingIndex = -1;
            _capturingNewBinding = false;
            _pendingModifierBindings.Clear();
            RefreshPresentation();
        }

        private void AddBinding(string value, bool notify)
        {
            var next = NormalizeBindings(_values.Append(value));
            ApplyBindings(next, notify);
        }

        private void ReplaceBindingAt(int index, string value)
        {
            if (index < 0 || index >= _values.Count)
            {
                AddBinding(value, true);
                return;
            }

            var next = _values.ToList();
            next[index] = value;
            ApplyBindings(next, true);
        }

        private void CommitCapturedBinding(string value)
        {
            if (_capturingNewBinding || _capturingIndex < 0)
                AddBinding(value, true);
            else
                ReplaceBindingAt(_capturingIndex, value);
        }

        private void RemoveBindingAt(int index)
        {
            if (index < 0 || index >= _values.Count)
                return;

            var next = _values.ToList();
            next.RemoveAt(index);
            if (_capturing)
            {
                if (_capturingIndex == index)
                {
                    _capturing = false;
                    _capturingIndex = -1;
                    _capturingNewBinding = false;
                    _pendingModifierBindings.Clear();
                }
                else if (_capturingIndex > index)
                {
                    _capturingIndex--;
                }
            }

            ApplyBindings(next, true);
        }

        private void ApplyBindings(IEnumerable<string> values, bool notify)
        {
            _values = NormalizeBindings(values);
            RefreshPresentation();
            if (notify)
                InvokeOnChangedSafely(_values.ToList());
        }

        private void InvokeOnChangedSafely(List<string> values)
        {
            if (_onChanged == null)
                return;

            try
            {
                _onChanged(values);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsMultiKeyBindingControl] onChanged failed: {ex.Message}");
            }
        }

        private void RefreshPresentation()
        {
            var pendingBindingText = _pendingModifierBindings.Count == 0
                ? string.Empty
                : string.Join('+', ModSettingsKeyBindingControl.OrderedModifierTokens(_pendingModifierBindings));

            var hintText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? ModSettingsLocalization.Get("ritsulib.keybindingMulti.capturing",
                        _capturingIndex >= 0
                            ? "Press a new key combination for the selected binding. Esc cancels, Backspace/Delete removes it."
                            : "Press a key combination to add. Esc cancels, Backspace/Delete clears all.")
                    : ModSettingsLocalization.Get("keybinding.hint.capturingPending",
                        "Modifier keys recorded. Press another key to complete, or release to keep a modifier-only binding.")
                : _allowModifierCombos
                    ? _allowModifierOnly
                        ? ModSettingsLocalization.Get("ritsulib.keybindingMulti.hint",
                            "Add one or more bindings. Use Rebind on a row to replace just that binding.")
                        : ModSettingsLocalization.Get("keybinding.hint.comboNonModifier",
                            "Click to record. Supports key combinations and requires a non-modifier key.")
                    : ModSettingsLocalization.Get("keybinding.hint.single", "Click to record a single key.");
            if (_hintLabel != null)
                _hintLabel.Text = hintText;

            RebuildBindingsList();
        }

        private void RebuildBindingsList()
        {
            if (_bindingsList == null)
                return;

            foreach (var child in _bindingsList.GetChildren())
                child.QueueFree();

            if (_values.Count == 0)
            {
                var empty = new Label
                {
                    Text = ModSettingsLocalization.Get("ritsulib.keybindingMulti.empty", "No bindings recorded."),
                    MouseFilter = MouseFilterEnum.Ignore,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                empty.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                empty.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PillCount);
                empty.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                _bindingsList.AddChild(empty);
            }

            var rowPendingBindingText = _pendingModifierBindings.Count == 0
                ? string.Empty
                : string.Join('+', ModSettingsKeyBindingControl.OrderedModifierTokens(_pendingModifierBindings));

            for (var i = 0; i < _values.Count; i++)
            {
                var bindingIndex = i;
                var isCapturingThisRow = _capturing && _capturingIndex == bindingIndex;
                var binding = _values[bindingIndex];
                var rowText = isCapturingThisRow
                    ? string.IsNullOrWhiteSpace(rowPendingBindingText)
                        ? ModSettingsLocalization.Get("keybinding.capturing", "Press combination...")
                        : rowPendingBindingText + "+..."
                    : string.IsNullOrWhiteSpace(binding)
                        ? ModSettingsLocalization.Get("keybinding.unbound", "Unbound")
                        : binding;

                _bindingsList.AddChild(CreateBindingRow(
                    rowText,
                    () => BeginCapture(bindingIndex),
                    ModSettingsLocalization.Get("button.remove", "Remove"),
                    () => RemoveBindingAt(bindingIndex)));
            }

            var addRowText = _capturing && _capturingNewBinding
                ? string.IsNullOrWhiteSpace(rowPendingBindingText)
                    ? ModSettingsLocalization.Get("keybinding.capturing", "Press combination...")
                    : rowPendingBindingText + "+..."
                : ModSettingsLocalization.Get("ritsulib.keybindingMulti.add", "Add binding");

            _bindingsList.AddChild(CreateBindingRow(
                addRowText,
                BeginAddCapture,
                ModSettingsLocalization.Get("button.clear", "Clear all"),
                () => ApplyBindings([], true)));
        }

        private HBoxContainer CreateBindingRow(string primaryText, Action primaryAction, string secondaryText,
            Action secondaryAction)
        {
            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.rowSeparation", 6));

            var primaryButton = new Button
            {
                Text = primaryText,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.multi.captureButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Keybinding.CaptureMinWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            primaryButton.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            primaryButton.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.MiniButton);
            primaryButton.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(primaryButton);
            primaryButton.Pressed += primaryAction;
            row.AddChild(primaryButton);

            row.AddChild(new ModSettingsMiniButton(secondaryText, secondaryAction)
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.multi.secondaryButton.minSize",
                    new(86f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });

            return row;
        }

        private static List<string> NormalizeBindings(IEnumerable<string>? values)
        {
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values ?? [])
            {
                if (!RuntimeHotkeyService.TryNormalizeBinding(value, out var normalizedBinding))
                    continue;
                if (seen.Add(normalizedBinding))
                    normalized.Add(normalizedBinding);
            }

            return normalized;
        }
    }

    /// <summary>
    ///     Compact button used by stepper controls, dropdown rows, and other dense settings editors.
    ///     步进控件、下拉行和其他密集设置编辑器使用的紧凑按钮。
    /// </summary>
    public sealed partial class ModSettingsMiniButton : ModSettingsGamepadCompatibleButton
    {
        /// <summary>
        ///     Creates a compact button with the standard mini-button chrome.
        ///     创建带标准迷你按钮外观的紧凑按钮。
        /// </summary>
        /// <param name="text">
        ///     The button label.
        ///     按钮标签。
        /// </param>
        /// <param name="action">
        ///     Invoked when the button is pressed.
        ///     按钮按下时调用。
        /// </param>
        public ModSettingsMiniButton(string text, Action action)
        {
            Text = text;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.MiniButton);
            AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeStyleboxOverride("normal", CreateStyle(false));
            AddThemeStyleboxOverride("hover", CreateStyle(true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true));
            AddThemeStyleboxOverride("focus", CreateStyle(true));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, true));
            Pressed += action;
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsMiniButton()
        {
        }

        /// <summary>
        ///     Creates the standard mini-button surface for normal, hover, focus, and disabled states.
        ///     为正常、悬停、焦点和禁用状态创建标准迷你按钮表面。
        /// </summary>
        public static StyleBoxFlat CreateStyle(bool highlighted, bool disabled = false)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.borderWidth", 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.stepper.layout.padding", 10);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.top", 5),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.stepper.layout.padding.bottom", 5));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.stepper.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = disabled
                    ? RitsuShellTheme.Current.Component.Stepper.Neutral.Bg
                    : highlighted
                        ? RitsuShellTheme.Current.Component.Stepper.Hover.Bg
                        : RitsuShellTheme.Current.Component.Stepper.Default.Bg,
                BorderColor = disabled
                    ? RitsuShellTheme.Current.Component.Stepper.Neutral.Border
                    : highlighted
                        ? RitsuShellTheme.Current.Component.Stepper.Hover.Border
                        : RitsuShellTheme.Current.Component.Stepper.Default.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     Creates the pressed-state style for mini buttons.
        ///     创建迷你按钮的按下状态样式。
        /// </summary>
        public static StyleBoxFlat CreatePressedStyle()
        {
            return CreateStyle(true);
        }

        /// <summary>
        ///     Creates the focus-state style for mini buttons.
        ///     创建迷你按钮的焦点状态样式。
        /// </summary>
        public static StyleBoxFlat CreateFocusStyle()
        {
            return CreateStyle(true);
        }
    }

    internal sealed partial class ModSettingsDragHandle : Button
    {
        private readonly Func<Dictionary>? _dragDataProvider;
        private readonly Func<int>? _rowIndexZeroBased;
        private NControllerManager? _hookedControllerManagerDrag;
        private Label? _indexNumberLabel;

        public ModSettingsDragHandle(Func<int> rowIndexZeroBased, Func<Dictionary> dragDataProvider)
        {
            _rowIndexZeroBased = rowIndexZeroBased;
            _dragDataProvider = dragDataProvider;

            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.dragHandle.layout.minSize",
                new(52f, 0f));
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeStyleboxOverride("normal", CreateRailStyle(false));
            AddThemeStyleboxOverride("hover", CreateRailStyle(true));
            AddThemeStyleboxOverride("pressed", CreateRailStyle(true));
            AddThemeStyleboxOverride("focus", CreateRailStyle(true));
            MouseDefaultCursorShape = CursorShape.Drag;

            var content = new VBoxContainer
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            content.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.contentSeparation", 3));
            AddChild(content);

            var number = new Label
            {
                Text = FormatDragIndexLabel(_rowIndexZeroBased()),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            number.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            number.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            number.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Number);
            content.AddChild(number);
            _indexNumberLabel = number;

            var grip = new Label
            {
                Text = "::::",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            grip.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            grip.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Grip);
            grip.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Grip);
            content.AddChild(grip);

            var hint = new Label
            {
                Text = ModSettingsLocalization.Get("list.dragShort", "Drag"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HintSmall);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.Hint);
            content.AddChild(hint);
        }

        public ModSettingsDragHandle()
        {
        }

        internal void RefreshIndexDisplay()
        {
            if (_indexNumberLabel != null && _rowIndexZeroBased != null)
                _indexNumberLabel.Text = FormatDragIndexLabel(_rowIndexZeroBased());
        }

        private static string FormatDragIndexLabel(int zeroBasedRowIndex)
        {
            return (zeroBasedRowIndex + 1).ToString();
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedControllerManagerDrag = NControllerManager.Instance;
            if (_hookedControllerManagerDrag != null)
            {
                _hookedControllerManagerDrag.ControllerDetected += ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag.MouseDetected += ApplyDragHandleMousePolicy;
            }

            ApplyDragHandleMousePolicy();
        }

        public override void _ExitTree()
        {
            if (_hookedControllerManagerDrag != null)
            {
                _hookedControllerManagerDrag.ControllerDetected -= ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag.MouseDetected -= ApplyDragHandleMousePolicy;
                _hookedControllerManagerDrag = null;
            }

            base._ExitTree();
        }

        public override void _Ready()
        {
            ModSettingsFocusChrome.AttachControllerSelectionReticle(this);
            ApplyDragHandleMousePolicy();
            base._Ready();
        }

        private void ApplyDragHandleMousePolicy()
        {
            var blockMouse = NControllerManager.Instance?.IsUsingController == true;
            MouseFilter = blockMouse ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.All;
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (NControllerManager.Instance?.IsUsingController == true)
                return default;

            if (_dragDataProvider == null)
                return default;

            var preview = new PanelContainer
            {
                CustomMinimumSize = new(48f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            preview.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(true));
            SetDragPreview(preview);
            return Variant.From(_dragDataProvider());
        }

        private static StyleBoxFlat CreateRailStyle(bool highlighted)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.dragHandle.layout.borderWidth", 0);
            border = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.top", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.right",
                    border.Right == 0 ? 1 : border.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.borderWidth.bottom", 0));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.dragHandle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.dragHandle.layout.padding", 6);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.dragHandle.layout.padding.bottom", 8));
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.DragHandle.Selected.Bg
                    : RitsuShellTheme.Current.Component.DragHandle.Default.Bg,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.DragHandle.Selected.Border
                    : RitsuShellTheme.Current.Component.DragHandle.Default.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }

    internal sealed partial class ModSettingsListControl<TItem> : VBoxContainer
    {
        private readonly string _dragToken = Guid.NewGuid().ToString("N");
        private readonly System.Collections.Generic.Dictionary<int, ModSettingsListDropSlot<TItem>> _dropSlots = [];
        private readonly ListModSettingsEntryDefinition<TItem> _entry;
        private readonly System.Collections.Generic.Dictionary<int, Control> _rowCards = [];
        private ModSettingsListDropSlot<TItem>? _activeDropSlot;
        private Label? _countLabel;
        private int _currentDragIndex = -1;
        private bool _dropCommitted;
        private PanelContainer? _emptyState;
        private List<TItem>? _listStructuralBaseline;
        private VBoxContainer? _rows;

        public ModSettingsListControl(ModSettingsUiContext context, ListModSettingsEntryDefinition<TItem> entry)
        {
            UiContext = context;
            _entry = entry;

            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.rootSeparation", 10));
        }

        public ModSettingsListControl()
        {
            UiContext = null!;
            _entry = null!;
        }

        internal ModSettingsUiContext UiContext { get; }

        public override void _Notification(int what)
        {
            if (what != NotificationDragEnd) return;
            if (!_dropCommitted && _activeDropSlot != null && _currentDragIndex >= 0)
                MoveDraggedItemTo(_activeDropSlot.TargetIndex);

            _currentDragIndex = -1;
            _dropCommitted = false;
            ClearActiveDropSlot();
        }

        public override void _Process(double delta)
        {
            if (_currentDragIndex < 0 || !Input.IsMouseButtonPressed(MouseButton.Left) || _rows == null)
                return;

            var mouse = GetViewport().GetMousePosition();
            var nearestTargetIndex = -1;
            var nearestDistance = float.MaxValue;

            foreach (var pair in _dropSlots)
            {
                var rect = pair.Value.GetGlobalRect();
                var center = rect.Position + rect.Size * 0.5f;
                var dx = mouse.X < rect.Position.X
                    ? rect.Position.X - mouse.X
                    : mouse.X > rect.End.X
                        ? mouse.X - rect.End.X
                        : 0f;
                var dy = MathF.Abs(mouse.Y - center.Y);
                var distance = dx * 0.25f + dy;
                if (!(distance < nearestDistance)) continue;
                nearestDistance = distance;
                nearestTargetIndex = pair.Key;
            }

            if (nearestTargetIndex >= 0)
                PreviewDropAtIndex(nearestTargetIndex);
        }

        public override void _Ready()
        {
            var shell = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            shell.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            AddChild(shell);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.shellSeparation", 10));
            shell.AddChild(root);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.headerSeparation", 10));
            root.AddChild(header);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.textColumnSeparation", 3));
            header.AddChild(textColumn);

            textColumn.AddChild(ModSettingsUiFactory.CreateRefreshableSectionTitle(UiContext, _entry.Label,
                () => ModSettingsUiFactory.ResolveEntryLabelDisplay(_entry.Label)));

            var descriptionLabel = ModSettingsUiFactory.CreateRefreshableDescriptionLabel(UiContext, _entry.Description,
                () => ModSettingsUiControlFactoryHelper.ResolveDescription(_entry.Description));
            textColumn.AddChild(descriptionLabel);

            var summary = new PanelContainer
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.list.layout.summaryPill.minSize",
                    new(96f, 32f)),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            summary.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            header.AddChild(summary);

            var countLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            countLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            countLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PillCount);
            countLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            summary.AddChild(countLabel);
            _countLabel = countLabel;

            var addButton = new ModSettingsTextButton(ModSettingsUiContext.Resolve(_entry.AddButtonText),
                ModSettingsButtonTone.Accent,
                () => Mutate(items => items.Add(_entry.CreateItem())))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.list.layout.addButton.minSize",
                    new(152f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            header.AddChild(addButton);

            if (ModSettingsUiFactory.CreateEntryActionsButton(UiContext, _entry.Binding) is ModSettingsActionsButton
                actionsButton)
            {
                header.AddChild(actionsButton);
                ModSettingsUiFactory.AttachContextMenuTargets(this, shell, actionsButton);
            }

            var body = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            body.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateInsetSurfaceStyle());
            root.AddChild(body);

            var bodyContent = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            bodyContent.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.bodySeparation", 6));
            body.AddChild(bodyContent);

            _rows = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _rows.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.list.layout.rowsSeparation", 6));
            bodyContent.AddChild(_rows);

            var emptyState = new PanelContainer
            {
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyState.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreatePillStyle());
            bodyContent.AddChild(emptyState);

            var emptyLabel = new Label
            {
                Text = ModSettingsLocalization.Get("list.empty", "No items yet. Add one to start editing."),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            emptyLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            emptyLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            emptyLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            emptyState.AddChild(emptyLabel);
            _emptyState = emptyState;

            ModSettingsUiFactory.RegisterRefreshWhenAlive(UiContext, this, SyncRows,
                ModSettingsUiRefreshSpec.ForBinding(_entry.Binding));
            SyncRows();
        }

        private void SyncRows()
        {
            if (_rows == null || !IsInstanceValid(this))
                return;

            ClearActiveDropSlot();
            var items = _entry.Binding.Read();
            UpdateListHeaderChrome(items);

            if (!TryIncrementalListSync(items))
                FullRebuildRows(items);

            PruneTrailingDropSlots(items);
            LayoutRowsChildOrder(items);
            UpdateStructuralBaseline(items);
            _rows?.ResetSize();
            _rows?.QueueSort();
        }

        private void UpdateListHeaderChrome(List<TItem> items)
        {
            if (_countLabel != null)
                _countLabel.Text = string.Format(
                    ModSettingsLocalization.Get("list.count", "{0} items"),
                    items.Count);

            if (_emptyState != null)
                _emptyState.Visible = items.Count == 0;
        }

        private void UpdateStructuralBaseline(List<TItem> items)
        {
            _listStructuralBaseline = CloneBindingValue(items);
        }

        private bool BaselinePrefixMatches(List<TItem> items, int prefixLength)
        {
            if (_listStructuralBaseline == null || prefixLength > _listStructuralBaseline.Count ||
                prefixLength > items.Count)
                return false;

            var comparer = EqualityComparer<TItem>.Default;
            for (var i = 0; i < prefixLength; i++)
                if (!comparer.Equals(items[i], _listStructuralBaseline[i]))
                    return false;

            return true;
        }

        private static bool RowKeysCoverRange(System.Collections.Generic.Dictionary<int, Control> rowCards, int count)
        {
            for (var i = 0; i < count; i++)
                if (!rowCards.ContainsKey(i))
                    return false;

            return true;
        }

        private bool TryIncrementalListSync(List<TItem> items)
        {
            var n = items.Count;

            if (n == _rowCards.Count && RowKeysCoverRange(_rowCards, n))
            {
                var comparer = EqualityComparer<TItem>.Default;
                List<int>? dirty = null;
                for (var i = 0; i < n; i++)
                {
                    if (!_rowCards.TryGetValue(i, out var c) || c is not ModSettingsListItemCard<TItem> card)
                        return false;
                    if (comparer.Equals(items[i], card.ItemContext.Item))
                        continue;
                    dirty ??= [];
                    dirty.Add(i);
                }

                if (dirty == null)
                {
                    for (var i = 0; i < n; i++)
                        ResyncExistingRow(i, items);
                    return true;
                }

                if (dirty.Count == n)
                    return false;

                var dirtySet = new HashSet<int>(dirty);
                foreach (var i in dirtySet)
                    ReplaceRowCardAt(i, items);

                for (var i = 0; i < n; i++)
                {
                    if (dirtySet.Contains(i))
                        continue;
                    ResyncExistingRow(i, items);
                }

                return true;
            }

            if (n == _rowCards.Count + 1
                && RowKeysCoverRange(_rowCards, n - 1)
                && !_rowCards.ContainsKey(n - 1)
                && _listStructuralBaseline != null
                && _listStructuralBaseline.Count == n - 1
                && BaselinePrefixMatches(items, n - 1))
            {
                for (var i = 0; i < n - 1; i++)
                    ResyncExistingRow(i, items);
                AppendRow(items, n - 1, n);
                return true;
            }

            if (n != _rowCards.Count - 1
                || !_rowCards.ContainsKey(n)
                || _listStructuralBaseline == null
                || _listStructuralBaseline.Count != n + 1
                || !BaselinePrefixMatches(items, n))
                return false;

            if (_rowCards.TryGetValue(n, out var trailing) && IsInstanceValid(trailing))
                DetachAndQueueFree(trailing);
            _rowCards.Remove(n);

            for (var i = 0; i < n; i++)
                ResyncExistingRow(i, items);
            return true;
        }

        private static void DetachAndQueueFree(Control node)
        {
            if (!IsInstanceValid(node))
                return;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        private void ReplaceRowCardAt(int i, List<TItem> items)
        {
            if (_rows == null)
                return;

            if (_rowCards.TryGetValue(i, out var oldRow) && IsInstanceValid(oldRow))
            {
                DetachAndQueueFree(oldRow);
                _rowCards.Remove(i);
            }

            var row = CreateRow(i, items[i], items.Count);
            _rowCards[i] = row;
            _rows.AddChild(row);
        }

        private void ResyncExistingRow(int i, List<TItem> items)
        {
            if (!_rowCards.TryGetValue(i, out var control) ||
                control is not ModSettingsListItemCard<TItem> card) return;
            var item = items[i];
            card.ItemContext.SyncRowListState(i, items.Count, item);
            var title = ModSettingsUiFactory.ResolveEntryLabelDisplay(SafeResolveItemLabel(item));
            var subtitle = SafeResolveItemDescription(item) is { } d
                ? ModSettingsUiContext.Resolve(d)
                : null;
            card.SyncRowChrome(i, title, subtitle, i == 0);
            card.QueueSort();
        }

        private void AppendRow(List<TItem> items, int index, int itemCount)
        {
            var row = CreateRow(index, items[index], itemCount);
            _rowCards[index] = row;
            if (_rows != null && row.GetParent() != _rows)
                _rows.AddChild(row);
        }

        private void PruneTrailingDropSlots(List<TItem> items)
        {
            foreach (var staleSlot in _dropSlots.Keys.Where(index => index > items.Count).ToArray())
            {
                if (_dropSlots.TryGetValue(staleSlot, out var slot) && IsInstanceValid(slot))
                    DetachAndQueueFree(slot);
                _dropSlots.Remove(staleSlot);
            }
        }

        private void LayoutRowsChildOrder(List<TItem> items)
        {
            if (_rows == null)
                return;

            var childOrder = 0;
            for (var slotIndex = 0; slotIndex <= items.Count; slotIndex++)
            {
                var dropSlot = EnsureDropSlot(slotIndex);
                if (dropSlot.GetParent() != _rows)
                    _rows.AddChild(dropSlot);
                _rows.MoveChild(dropSlot, childOrder++);

                if (slotIndex >= items.Count)
                    continue;

                if (!_rowCards.TryGetValue(slotIndex, out var row))
                    continue;

                if (row.GetParent() != _rows)
                    _rows.AddChild(row);
                _rows.MoveChild(row, childOrder++);
            }
        }

        private void FullRebuildRows(List<TItem> items)
        {
            var liveIndexes = Enumerable.Range(0, items.Count).ToHashSet();
            foreach (var staleIndex in _rowCards.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
            {
                if (_rowCards.TryGetValue(staleIndex, out var staleRow) && IsInstanceValid(staleRow))
                    DetachAndQueueFree(staleRow);
                _rowCards.Remove(staleIndex);
            }

            for (var slotIndex = 0; slotIndex < items.Count; slotIndex++)
            {
                if (_rowCards.TryGetValue(slotIndex, out var existing) && IsInstanceValid(existing))
                    DetachAndQueueFree(existing);

                var row = CreateRow(slotIndex, items[slotIndex], items.Count);
                _rowCards[slotIndex] = row;
                if (_rows != null && row.GetParent() != _rows)
                    _rows.AddChild(row);
            }
        }

        private Control CreateRow(int index, TItem item, int itemCount)
        {
            var liveIndex = new ModSettingsListItemContext<TItem>.ListRowLiveIndex { Value = index };
            var itemContext = new ModSettingsListItemContext<TItem>(
                UiContext,
                CreateItemBinding(index),
                $"{_entry.Id}[{index}]",
                liveIndex,
                itemCount,
                item,
                updatedItem => Mutate(items => items[liveIndex.Value] = updatedItem),
                () => Mutate(items =>
                {
                    if (liveIndex.Value <= 0)
                        return;
                    MoveItem(items, liveIndex.Value, liveIndex.Value - 1);
                }),
                () => Mutate(items =>
                {
                    if (liveIndex.Value >= items.Count - 1)
                        return;
                    MoveItem(items, liveIndex.Value, liveIndex.Value + 1);
                }),
                () => Mutate(items => DuplicateItem(items, liveIndex.Value)),
                () => Mutate(items => items.RemoveAt(liveIndex.Value)),
                UiContext.RequestRefresh);

            return new ModSettingsListItemCard<TItem>(
                this,
                index,
                ModSettingsUiFactory.ResolveEntryLabelDisplay(SafeResolveItemLabel(item)),
                SafeResolveItemDescription(item) is { } description
                    ? ModSettingsUiContext.Resolve(description)
                    : null,
                itemContext,
                SafeCreateItemEditor(itemContext),
                _entry.CollapsibleItems,
                _entry.StartItemsCollapsed,
                SafeCreateItemHeaderAccessory(itemContext));
        }

        private ModSettingsText SafeResolveItemLabel(TItem item)
        {
            try
            {
                return _entry.ItemLabel(item);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemLabel failed for '{_entry.Id}': {ex.Message}");
                return ModSettingsText.Literal(_entry.Id);
            }
        }

        private ModSettingsText? SafeResolveItemDescription(TItem item)
        {
            if (_entry.ItemDescription == null)
                return null;

            try
            {
                return _entry.ItemDescription(item);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemDescription failed for '{_entry.Id}': {ex.Message}");
                return null;
            }
        }

        private Control? SafeCreateItemEditor(ModSettingsListItemContext<TItem> itemContext)
        {
            if (_entry.ItemEditorFactory == null)
                return null;

            try
            {
                return _entry.ItemEditorFactory(itemContext);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemEditorFactory failed for '{_entry.Id}': {ex.Message}");
                return null;
            }
        }

        private Control? SafeCreateItemHeaderAccessory(ModSettingsListItemContext<TItem> itemContext)
        {
            if (_entry.ItemHeaderAccessoryFactory == null)
                return null;

            try
            {
                return _entry.ItemHeaderAccessoryFactory(itemContext);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsListControl] ItemHeaderAccessoryFactory failed for '{_entry.Id}': {ex.Message}");
                return null;
            }
        }

        private void Mutate(Action<List<TItem>> mutate)
        {
            var clone = CloneBindingValue(_entry.Binding.Read());
            mutate(clone);
            _entry.Binding.Write(clone);
            UiContext.MarkDirty(_entry.Binding);
            UiContext.RequestRefresh();
        }

        private IModSettingsValueBinding<TItem> CreateItemBinding(int index)
        {
            var itemAdapter = _entry.ItemDataAdapter;
            return ModSettingsBindings.Project(
                _entry.Binding,
                $"items[{index}]",
                items => items[index],
                (items, item) => ReplaceAt(items, index, item),
                itemAdapter);
        }

        internal Dictionary CreateDragData(int index)
        {
            _currentDragIndex = index;
            _dropCommitted = false;
            return new()
            {
                ["token"] = _dragToken,
                ["index"] = index,
            };
        }

        internal bool CanAcceptDrop(Variant data)
        {
            return data.VariantType == Variant.Type.Dictionary
                   && data.AsGodotDictionary().TryGetValue("token", out var token)
                   && token.AsString() == _dragToken;
        }

        internal void HandleDrop(Variant data, int targetIndex)
        {
            if (!CanAcceptDrop(data))
                return;

            var dragIndex = data.AsGodotDictionary()["index"].AsInt32();
            _dropCommitted = true;
            ClearActiveDropSlot();
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        internal void SetActiveDropSlot(ModSettingsListDropSlot<TItem>? slot, bool active)
        {
            if (!active)
            {
                if (_activeDropSlot == slot)
                    ClearActiveDropSlot();
                else
                    slot?.SetHighlighted(false);
                return;
            }

            if (_activeDropSlot != null && _activeDropSlot != slot)
                _activeDropSlot.SetHighlighted(false);

            _activeDropSlot = slot;
            _activeDropSlot?.SetHighlighted(true);
        }

        internal void ClearActiveDropSlot()
        {
            _activeDropSlot?.SetHighlighted(false);
            _activeDropSlot = null;
        }

        internal void PreviewDropAtIndex(int targetIndex)
        {
            if (_dropSlots.TryGetValue(targetIndex, out var slot))
                SetActiveDropSlot(slot, true);
        }

        internal void DropAtIndex(Variant data, int targetIndex)
        {
            HandleDrop(data, targetIndex);
        }

        private void MoveDraggedItemTo(int targetIndex)
        {
            var dragIndex = _currentDragIndex;
            if (dragIndex < 0)
                return;

            _dropCommitted = true;
            Mutate(items => MoveItemToSlot(items, dragIndex, targetIndex));
        }

        private ModSettingsListDropSlot<TItem> EnsureDropSlot(int index)
        {
            if (_dropSlots.TryGetValue(index, out var slot) && IsInstanceValid(slot))
                return slot;

            slot = new(this, index);
            _dropSlots[index] = slot;
            return slot;
        }

        private List<TItem> CloneBindingValue(List<TItem> items)
        {
            return _entry.Binding is IStructuredModSettingsValueBinding<List<TItem>> structured
                ? structured.Adapter.Clone(items)
                : items.ToList();
        }

        private static List<TItem> ReplaceAt(List<TItem> items, int index, TItem item)
        {
            var clone = items.ToList();
            clone[index] = item;
            return clone;
        }

        private void DuplicateItem(List<TItem> items, int index)
        {
            if (index < 0 || index >= items.Count)
                return;

            var item = items[index];
            if (_entry.ItemDataAdapter != null)
                item = _entry.ItemDataAdapter.Clone(item);
            items.Insert(index + 1, item);
        }

        private static void MoveItem(List<TItem> items, int from, int to)
        {
            if (from < 0 || from >= items.Count || to < 0 || to >= items.Count || from == to)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(to, item);
        }

        private static void MoveItemToSlot(List<TItem> items, int from, int slotIndex)
        {
            if (from < 0 || from >= items.Count)
                return;

            slotIndex = Mathf.Clamp(slotIndex, 0, items.Count);
            var normalizedIndex = slotIndex;
            if (from < normalizedIndex)
                normalizedIndex--;

            if (normalizedIndex == from)
                return;

            var item = items[from];
            items.RemoveAt(from);
            items.Insert(normalizedIndex, item);
        }
    }

    internal sealed partial class ModSettingsListDropSlot<TItem> : PanelContainer
    {
        private readonly ModSettingsListControl<TItem> _owner;
        private NControllerManager? _hookedDropSlotController;

        public ModSettingsListDropSlot(ModSettingsListControl<TItem> owner, int targetIndex)
        {
            _owner = owner;
            TargetIndex = targetIndex;

            FocusMode = FocusModeEnum.None;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.list.layout.dropSlot.minSize",
                new(0f, 8f));
            AddThemeStyleboxOverride("panel", CreateStyle(false));
        }

        public ModSettingsListDropSlot()
        {
            _owner = null!;
        }

        internal int TargetIndex { get; }

        public override void _EnterTree()
        {
            base._EnterTree();
            _hookedDropSlotController = NControllerManager.Instance;
            if (_hookedDropSlotController != null)
            {
                _hookedDropSlotController.ControllerDetected += ApplyDropSlotInputPolicy;
                _hookedDropSlotController.MouseDetected += ApplyDropSlotInputPolicy;
            }

            ApplyDropSlotInputPolicy();
        }

        public override void _ExitTree()
        {
            if (_hookedDropSlotController != null)
            {
                _hookedDropSlotController.ControllerDetected -= ApplyDropSlotInputPolicy;
                _hookedDropSlotController.MouseDetected -= ApplyDropSlotInputPolicy;
                _hookedDropSlotController = null;
            }

            base._ExitTree();
        }

        private void ApplyDropSlotInputPolicy()
        {
            var controller = NControllerManager.Instance?.IsUsingController == true;
            MouseFilter = controller ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            var canDrop = _owner.CanAcceptDrop(data);
            _owner.SetActiveDropSlot(this, canDrop);
            return canDrop;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _owner.HandleDrop(data, TargetIndex);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationDragEnd)
                _owner.ClearActiveDropSlot();
        }

        internal void SetHighlighted(bool highlighted)
        {
            AddThemeStyleboxOverride("panel", CreateStyle(highlighted));
        }

        private static StyleBoxFlat CreateStyle(bool highlighted)
        {
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.choiceCenter.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            BoxEdges border = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.top",
                    highlighted ? 1 : 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.right", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.borderWidth.bottom",
                    highlighted ? 1 : 0));
            BoxEdges padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.left", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.top",
                    highlighted ? 1 : 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.right", 0),
                RitsuShellThemeLayoutResolver.ResolveInt("components.choiceCenter.layout.padding.bottom",
                    highlighted ? 1 : 0));
            return new()
            {
                BgColor = highlighted
                    ? RitsuShellTheme.Current.Component.ChoiceCenter.HighlightTop
                    : RitsuShellTheme.Current.Color.Transparent,
                BorderColor = highlighted
                    ? RitsuShellTheme.Current.Component.ChoiceCenter.HighlightBottom
                    : RitsuShellTheme.Current.Color.Transparent,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }

    internal sealed partial class ModSettingsListItemCard<TItem> : PanelContainer
    {
        private readonly bool _isCollapsible;
        private readonly ModSettingsListControl<TItem> _owner;
        private bool _collapsed;
        private ModSettingsDragHandle? _dragHandle;
        private PanelContainer? _editorSurface;
        private MegaRichTextLabel? _plainSubtitleLabel;
        private MegaRichTextLabel? _plainTitleLabel;
        private ModSettingsCollapsibleHeaderButton? _toggleButton;

        public ModSettingsListItemCard(
            ModSettingsListControl<TItem> owner,
            int index,
            string title,
            string? subtitle,
            ModSettingsListItemContext<TItem> itemContext,
            Control? editorContent,
            bool collapsible,
            bool startCollapsed,
            Control? headerAccessory)
        {
            _owner = owner;
            ItemContext = itemContext;
            _isCollapsible = collapsible && editorContent != null;
            _collapsed = _isCollapsible && itemContext.GetRowState("collapsed", startCollapsed);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Stop;
            AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(index == 0));

            var outer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            outer.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.outerSeparation", 8));
            AddChild(outer);

            var drag = new ModSettingsDragHandle(() => itemContext.Index,
                () => owner.CreateDragData(itemContext.Index));
            _dragHandle = drag;
            outer.AddChild(drag);

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.rootSeparation", 8));
            outer.AddChild(root);

            var headerRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            headerRow.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.headerSeparation", 8));
            root.AddChild(headerRow);

            if (_isCollapsible)
            {
                _toggleButton = new(title, subtitle, ToggleCollapsed)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                headerRow.AddChild(_toggleButton);
            }
            else
            {
                var header = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                    Alignment = BoxContainer.AlignmentMode.Center,
                };
                header.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.headerInnerSeparation", 8));
                headerRow.AddChild(header);

                var textColumn = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                textColumn.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.textSeparation", 2));
                header.AddChild(textColumn);

                var titleLabel = ModSettingsUiFactory.CreateSectionTitle(title);
                textColumn.AddChild(titleLabel);
                _plainTitleLabel = titleLabel;

                var subtitleLabel = ModSettingsUiFactory.CreateInlineDescription(subtitle ?? string.Empty);
                subtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
                textColumn.AddChild(subtitleLabel);
                _plainSubtitleLabel = subtitleLabel;
            }

            var actions = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            actions.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.listItem.layout.actionsSeparation", 8));
            headerRow.AddChild(actions);

            if (headerAccessory != null)
                actions.AddChild(headerAccessory);

            var actionsButton = new ModSettingsActionsButton(
                ModSettingsUiFactory.BuildListItemMenuActions(owner.UiContext, itemContext),
                itemContext.RequestRefresh);
            actionsButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            actions.AddChild(actionsButton);
            ModSettingsUiFactory.AttachContextMenuTargets(this, outer, actionsButton);

            if (editorContent == null) return;
            _editorSurface = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = !_collapsed,
            };
            _editorSurface.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListEditorSurfaceStyle());
            root.AddChild(_editorSurface);
            _editorSurface.AddChild(editorContent);
            ApplyCollapsedState();
        }

        public ModSettingsListItemCard()
        {
            _owner = null!;
            ItemContext = null!;
        }

        internal ModSettingsListItemContext<TItem> ItemContext { get; }

        internal void SyncRowChrome(int logicalIndex, string title, string? subtitle, bool isFirstRow)
        {
            AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(isFirstRow));
            _dragHandle?.RefreshIndexDisplay();

            if (_isCollapsible)
            {
                _toggleButton?.SetTexts(title, subtitle);
            }
            else
            {
                _plainTitleLabel?.SetTextAutoSize(title);
                if (_plainSubtitleLabel == null) return;
                _plainSubtitleLabel.SetTextAutoSize(subtitle ?? string.Empty);
                _plainSubtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
            }
        }

        private void ToggleCollapsed()
        {
            if (!_isCollapsible)
                return;
            _collapsed = !_collapsed;
            ItemContext.SetRowState("collapsed", _collapsed);
            ApplyCollapsedState();
        }

        private void ApplyCollapsedState()
        {
            _editorSurface?.SetDeferred(CanvasItem.PropertyName.Visible, !_collapsed);
            _toggleButton?.SetSelected(!_collapsed);
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (!_owner.CanAcceptDrop(data))
                return false;

            var i = ItemContext.Index;
            _owner.PreviewDropAtIndex(atPosition.Y < Size.Y * 0.5f ? i : i + 1);
            return true;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var i = ItemContext.Index;
            _owner.DropAtIndex(data, atPosition.Y < Size.Y * 0.5f ? i : i + 1);
        }
    }

    internal sealed partial class ModSettingsCollapsibleHeaderButton : ModSettingsGamepadCompatibleButton
    {
        private readonly Action? _action;
        private Label? _arrowLabel;
        private bool _contentEnabled = true;
        private MarginContainer? _measureFrame;
        private bool _selected;
        private string? _subtitle;
        private Label? _subtitleLabel;
        private string _title = string.Empty;
        private Label? _titleLabel;

        public ModSettingsCollapsibleHeaderButton(string title, string? subtitle, Action action)
        {
            _title = title;
            _subtitle = subtitle;
            _action = action;

            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipContents = false;
            Text = string.Empty;
            CustomMinimumSize = ResolveHeaderMinSize(subtitle);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddThemeStyleboxOverride("normal", CreateHeaderStyle(false, false, true));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(false, true, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(false, true, true));

            var frame = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.SetAnchorsPreset(LayoutPreset.FullRect);
            frame.OffsetLeft = 0;
            frame.OffsetTop = 0;
            frame.OffsetRight = 0;
            frame.OffsetBottom = 0;
            var headerPadding = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.collapsible.layout.header.padding", 14);
            headerPadding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.left",
                    headerPadding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.top", 10),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.right",
                    headerPadding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.padding.bottom", 10));
            frame.AddThemeConstantOverride("margin_left", headerPadding.Left);
            frame.AddThemeConstantOverride("margin_top", headerPadding.Top);
            frame.AddThemeConstantOverride("margin_right", headerPadding.Right);
            frame.AddThemeConstantOverride("margin_bottom", headerPadding.Bottom);
            AddChild(frame);
            _measureFrame = frame;

            var root = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            root.AddThemeConstantOverride("separation", RitsuShellThemeLayoutResolver.ResolveInt(
                "components.collapsible.layout.header.separation", 12));
            frame.AddChild(root);

            var arrowLabel = new Label
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.collapsible.layout.header.arrow.minSize",
                    new(28f, 28f)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            arrowLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            arrowLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderArrow);
            arrowLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichSecondary);
            root.AddChild(arrowLabel);
            _arrowLabel = arrowLabel;

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.header.textSeparation", 2));
            root.AddChild(textColumn);

            var titleLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            titleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            titleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderTitle);
            titleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            textColumn.AddChild(titleLabel);
            _titleLabel = titleLabel;

            var subtitleLabel = new Label
            {
                Text = subtitle ?? string.Empty,
                Visible = !string.IsNullOrWhiteSpace(subtitle),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = false,
            };
            subtitleLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            subtitleLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.HeaderSubtitle);
            subtitleLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            textColumn.AddChild(subtitleLabel);
            _subtitleLabel = subtitleLabel;

            Pressed += () =>
            {
                if (_action == null)
                    return;

                try
                {
                    _action();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModSettingsCollapsibleHeaderButton] action failed: {ex.Message}");
                }
            };
        }

        public ModSettingsCollapsibleHeaderButton()
        {
        }

        internal void SetTexts(string title, string? subtitle)
        {
            _title = title;
            _subtitle = subtitle;
            if (_titleLabel != null)
                _titleLabel.Text = title;
            if (_subtitleLabel != null)
            {
                _subtitleLabel.Text = subtitle ?? string.Empty;
                _subtitleLabel.Visible = !string.IsNullOrWhiteSpace(subtitle);
            }

            CustomMinimumSize = ResolveHeaderMinSize(subtitle);
            Callable.From(UpdateMinimumSize).CallDeferred();
        }

        public override Vector2 _GetMinimumSize()
        {
            var baseMin = base._GetMinimumSize();
            if (_measureFrame == null)
                return baseMin;
            var inner = _measureFrame.GetCombinedMinimumSize();
            return new(baseMin.X, Mathf.Max(baseMin.Y, inner.Y));
        }

        public override void _Ready()
        {
            if (_titleLabel != null)
                _titleLabel.Text = _title;
            if (_subtitleLabel != null)
                _subtitleLabel.Text = _subtitle ?? string.Empty;
            ApplySelectedState();
            Callable.From(UpdateMinimumSize).CallDeferred();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplySelectedState();
        }

        internal void SetContentEnabled(bool enabled)
        {
            _contentEnabled = enabled;
            ApplySelectedState();
        }

        private void ApplySelectedState()
        {
            AddThemeStyleboxOverride("normal", CreateHeaderStyle(_selected, false, _contentEnabled));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(_selected, true, _contentEnabled));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true, _contentEnabled));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(_selected, true, _contentEnabled));
            if (_arrowLabel != null)
                _arrowLabel.Text = _selected ? "▼" : "▶";

            var opacity = _contentEnabled ? 1f : ResolveDisabledOpacityFactor();
            ApplyLabelOpacity(_arrowLabel, opacity);
            ApplyLabelOpacity(_titleLabel, opacity);
            ApplyLabelOpacity(_subtitleLabel, opacity);
        }

        private static StyleBoxFlat CreateHeaderStyle(bool selected, bool hovered, bool contentEnabled)
        {
            var border = RitsuShellThemeLayoutResolver.ResolveEdges("components.collapsible.layout.borderWidth", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.collapsible.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);

            return new()
            {
                BgColor = !contentEnabled
                    ? RitsuShellTheme.Current.Component.Collapsible.Disabled.Bg
                    : selected
                        ? RitsuShellTheme.Current.Component.Collapsible.Selected.Bg
                        : hovered
                            ? RitsuShellTheme.Current.Component.Collapsible.Hover.Bg
                            : RitsuShellTheme.Current.Component.Collapsible.Default.Bg,
                BorderColor = !contentEnabled
                    ? RitsuShellTheme.Current.Component.Collapsible.Disabled.Border
                    : selected
                        ? RitsuShellTheme.Current.Component.Collapsible.Selected.Border
                        : RitsuShellTheme.Current.Component.Collapsible.Default.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
            };
        }

        private static void ApplyLabelOpacity(Label? label, float opacity)
        {
            if (label == null)
                return;
            var m = label.Modulate;
            label.Modulate = new(m.R, m.G, m.B, opacity);
        }

        private static float ResolveDisabledOpacityFactor()
        {
            try
            {
                var v = (float)RitsuShellTheme.Current.GetDimensionDouble("semantic.state.disabled.opacity");
                if (v is > 0.05f and <= 1.0f)
                    return v;
            }
            catch
            {
                // ignored
            }

            return 0.78f;
        }

        private static Vector2 ResolveHeaderMinSize(string? subtitle)
        {
            var hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            var fallback = new Vector2(0f, hasSubtitle ? 84f : 56f);
            var path = hasSubtitle
                ? "components.collapsible.layout.header.minSizeWithSubtitle"
                : "components.collapsible.layout.header.minSize";
            return RitsuShellThemeLayoutResolver.ResolveMinSize(path, fallback);
        }
    }

    internal sealed partial class ModSettingsCollapsibleSection : VBoxContainer
    {
        private readonly Control[]? _contentControls;
        private readonly string? _description;
        private readonly ModSettingsActionsButton? _headerActions;
        private readonly string? _sectionId;
        private readonly bool _startCollapsed;
        private readonly string? _title;
        private bool _collapsed;
        private VBoxContainer? _content;
        private bool _contentEnabled = true;
        private Action? _lazyContentBuilder;
        private bool _lazyContentBuilt;
        private ModSettingsCollapsibleHeaderButton? _toggle;

        public ModSettingsCollapsibleSection(string title, string? sectionId, string? description, bool startCollapsed,
            Control[] contentControls, ModSettingsActionsButton? headerActions = null)
        {
            _title = title;
            _sectionId = sectionId;
            _description = description;
            _startCollapsed = startCollapsed;
            _contentControls = contentControls;
            _headerActions = headerActions;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.sectionSeparation", 8));
        }

        public ModSettingsCollapsibleSection()
        {
        }

        internal VBoxContainer ContentHost => _content ??= CreateContentHost();

        public override void _Ready()
        {
            if (!string.IsNullOrWhiteSpace(_sectionId))
                Name = $"Section_{_sectionId}";

            var card = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            card.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateSurfaceStyle());
            AddChild(card);

            var cardContent = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            cardContent.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.cardSeparation", 8));
            card.AddChild(cardContent);

            if (_title != null)
                _toggle = new(_title, _description, ToggleCollapsed)
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };

            if (_toggle != null || _headerActions != null)
            {
                var headerRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                    Alignment = AlignmentMode.Center,
                };
                headerRow.AddThemeConstantOverride("separation",
                    RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.headerRowSeparation", 10));
                if (_toggle != null)
                {
                    _toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    headerRow.AddChild(_toggle);
                }
                else
                {
                    headerRow.AddChild(new Control
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        MouseFilter = MouseFilterEnum.Ignore,
                    });
                }

                if (_headerActions != null)
                {
                    _headerActions.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                    headerRow.AddChild(_headerActions);
                }

                cardContent.AddChild(headerRow);
            }

            _content ??= CreateContentHost();
            if (_contentControls != null && _content.GetChildCount() == 0)
                foreach (var control in _contentControls)
                    _content.AddChild(control);
            cardContent.AddChild(_content);

            _collapsed = _startCollapsed;
            ApplyCollapsedState();
            ApplyContentEnabledState();
        }

        private void ToggleCollapsed()
        {
            _collapsed = !_collapsed;
            if (!_collapsed)
                EnsureLazyContentBuilt();
            ApplyCollapsedState();
            if (!_collapsed)
                Callable.From(EnsureExpandedSectionVisible).CallDeferred();
        }

        internal void Expand()
        {
            if (!_collapsed)
                return;

            _collapsed = false;
            EnsureLazyContentBuilt();
            ApplyCollapsedState();
        }

        internal void EnsureContentBuilt()
        {
            EnsureLazyContentBuilt();
        }

        internal void SetLazyContentBuilder(Action builder)
        {
            _lazyContentBuilder = builder;
            if (!_collapsed)
                EnsureLazyContentBuilt();
        }

        private void EnsureLazyContentBuilt()
        {
            if (_lazyContentBuilt || _lazyContentBuilder == null)
                return;

            _lazyContentBuilt = true;
            _lazyContentBuilder();
            _lazyContentBuilder = null;
            ApplyContentEnabledState();
        }

        private void ApplyCollapsedState()
        {
            if (_content != null)
                _content.Visible = !_collapsed;
            _toggle?.SetSelected(!_collapsed);
        }

        internal void SetContentEnabled(bool enabled)
        {
            _contentEnabled = enabled;
            ApplyContentEnabledState();
        }

        private void ApplyContentEnabledState()
        {
            if (_content != null)
                ModSettingsUiFactory.ApplyEnabledRecursive(_content, _contentEnabled);

            // Actions should follow the content enabled state (disabled when unavailable).
            if (_headerActions != null)
                ModSettingsUiFactory.ApplyEnabledRecursive(_headerActions, _contentEnabled);

            // Collapsing stays operable; only the content becomes disabled.
            _toggle?.SetContentEnabled(_contentEnabled);
        }

        private static VBoxContainer CreateContentHost()
        {
            var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            content.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.collapsible.layout.contentSeparation", 8));
            return content;
        }

        private void EnsureExpandedSectionVisible()
        {
            if (!IsVisibleInTree())
                return;

            var scroll = FindAncestorScrollContainer(this);
            scroll?.EnsureControlVisible(this);
        }

        private static ScrollContainer? FindAncestorScrollContainer(Node node)
        {
            for (var current = node.GetParent(); current != null; current = current.GetParent())
                if (current is ScrollContainer scroll)
                    return scroll;

            return null;
        }
    }

    internal enum ModSettingsSidebarItemKind
    {
        ModGroup,
        Page,
        Section,
        Utility,
    }

    internal sealed partial class ModSettingsSidebarButton : ModSettingsGamepadCompatibleButton
    {
        private readonly int _indentLevel;
        private readonly ModSettingsSidebarItemKind _kind;
        private readonly string? _prefix;
        private readonly string? _rawText;
        private bool _selected;

        public ModSettingsSidebarButton(string text, Action? action,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            _rawText = text;
            _indentLevel = Math.Max(0, indentLevel);
            _kind = kind;
            _prefix = prefix;
            Text = text;
            TooltipText = text;
            var minHeight = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 62f,
                ModSettingsSidebarItemKind.Page => 48f,
                ModSettingsSidebarItemKind.Section => 38f,
                _ => 44f,
            };
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.sidebar.layout.button.minSize",
                new(0f, minHeight));
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            Alignment = HorizontalAlignment.Left;
            IconAlignment = HorizontalAlignment.Left;

            AddThemeFontOverride("font", kind == ModSettingsSidebarItemKind.ModGroup
                ? RitsuShellTheme.Current.Font.BodyBold
                : RitsuShellTheme.Current.Font.Body);
            AddThemeFontSizeOverride("font_size", kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 22,
                ModSettingsSidebarItemKind.Page => 19,
                ModSettingsSidebarItemKind.Section => 16,
                _ => 17,
            });
            AddThemeColorOverride("font_color", kind == ModSettingsSidebarItemKind.Section
                ? RitsuShellTheme.Current.Text.SidebarSection
                : RitsuShellTheme.Current.Text.LabelPrimary);
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Color.White);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Color.White);

            AddThemeStyleboxOverride("normal", CreateStyle(false, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());

            Pressed += () =>
            {
                if (action == null)
                    return;

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[ModSettingsSidebarButton] action failed: {ex.Message}");
                }
            };
        }

        public ModSettingsSidebarButton()
        {
        }

        public override void _Ready()
        {
            Text = string.IsNullOrWhiteSpace(_prefix) ? _rawText ?? string.Empty : $"{_prefix}  {_rawText}";
            SetSelected(_selected);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateStyle(_selected, true, _kind, _indentLevel));
        }

        internal static StyleBoxFlat CreateStyle(bool selected, bool hovered,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            int indentLevel = 0)
        {
            var bg = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.SelectedHover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Selected.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.Mod.Bg,
                ModSettingsSidebarItemKind.Section => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.Hover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Default.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.IdleDeep.Bg,
                ModSettingsSidebarItemKind.Utility => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.UtilitySelected.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.IdleDeepHover.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.IdleDeep.Bg,
                _ => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.ModHover.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.SidebarBtn.Mod.Bg
                        : RitsuShellTheme.Current.Component.SidebarBtn.ModDeep.Bg,
            };

            var borderColor = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.SelectedHover.Border
                    : RitsuShellTheme.Current.Component.SidebarBtn.Selected.Border,
                ModSettingsSidebarItemKind.Section => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.Hover.Border
                    : RitsuShellTheme.Current.Component.SidebarBtn.Default.Border,
                _ => selected
                    ? RitsuShellTheme.Current.Component.SidebarBtn.DeepBorderHover
                    : RitsuShellTheme.Current.Component.SidebarBtn.DeepBorder,
            };

            var leftBorder = selected
                ? kind == ModSettingsSidebarItemKind.Section ? 3 : 4
                : kind == ModSettingsSidebarItemKind.ModGroup
                    ? 2
                    : 1;
            var borderWidths =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.button.borderWidth", 1);
            borderWidths = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.left",
                    leftBorder),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.top",
                    borderWidths.Top),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.right",
                    borderWidths.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.borderWidth.bottom",
                    borderWidths.Bottom));
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.sidebar.layout.button.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var shadowSize = kind == ModSettingsSidebarItemKind.ModGroup ? 4 : 2;
            shadowSize = RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.shadowSize",
                shadowSize);
            var fallbackTopBottom = kind == ModSettingsSidebarItemKind.Section ? 8 : 10;
            var fallbackRight = kind == ModSettingsSidebarItemKind.Section ? 14 : 18;
            var fallbackLeft = (kind == ModSettingsSidebarItemKind.Section ? 14 : 18) + indentLevel * 14;
            BoxEdges padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.left", fallbackLeft),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.top",
                    fallbackTopBottom),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.right",
                    fallbackRight),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.button.padding.bottom",
                    fallbackTopBottom));

            return new()
            {
                BgColor = bg,
                BorderColor = borderColor,
                BorderWidthLeft = borderWidths.Left,
                BorderWidthTop = borderWidths.Top,
                BorderWidthRight = borderWidths.Right,
                BorderWidthBottom = borderWidths.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = RitsuShellTheme.Current.Component.SidebarBtn.Shadow,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        internal static StyleBoxFlat CreateDisabledStyle()
        {
            var border =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.disabled.borderWidth", 2);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.sidebar.layout.disabled.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.sidebar.layout.disabled.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.left",
                    padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.right",
                    padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.sidebar.layout.disabled.padding.bottom", 8));
            return new()
            {
                BgColor = RitsuShellTheme.Current.Component.SidebarRail.Bg,
                BorderColor = RitsuShellTheme.Current.Component.SidebarRail.Border,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }

    /// <summary>
    ///     Textured action button using the standard settings button chrome.
    ///     使用标准设置按钮外观的纹理动作按钮。
    /// </summary>
    public partial class ModSettingsTextButton : ModSettingsGamepadCompatibleButton
    {
        private Action? _action;
        private bool _pressedHandlerAttached;
        private bool _selected;
        private string? _text;
        private ModSettingsButtonTone _tone;

        /// <summary>
        ///     Creates a standard settings action button.
        ///     创建标准设置动作按钮。
        /// </summary>
        /// <param name="text">
        ///     The button label.
        ///     按钮标签。
        /// </param>
        /// <param name="tone">
        ///     The visual tone applied to the button.
        ///     应用到按钮的视觉色调。
        /// </param>
        /// <param name="action">
        ///     Invoked when the button is pressed.
        ///     按钮按下时调用。
        /// </param>
        public ModSettingsTextButton(string text, ModSettingsButtonTone tone, Action? action)
        {
            Configure(text, tone, action);
            EnsurePressedHandlerAttached();
        }

        /// <summary>
        ///     Godot serialization constructor.
        ///     Godot 序列化构造函数。
        /// </summary>
        public ModSettingsTextButton()
        {
            EnsurePressedHandlerAttached();
        }

        internal void Configure(string text, ModSettingsButtonTone tone, Action? action)
        {
            _text = text;
            _tone = tone;
            _action = action;
            Text = text;
            Alignment = HorizontalAlignment.Center;
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.textButton.layout.minSize",
                new(ModSettingsUiFactory.EntryControlWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            AddThemeColorOverride("font_color", ResolveToneForeground(tone));
            AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            AddThemeColorOverride("font_disabled_color", RitsuShellTheme.Current.Text.LabelSecondary);
            ApplyVisualState();
        }

        internal void ClearAction()
        {
            _action = null;
            _selected = false;
            this.ReleaseFocusIfInsideTree();
            Disabled = false;
            ProcessMode = ProcessModeEnum.Inherit;
            Modulate = Colors.White;
            ApplyVisualState();
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            Text = _text ?? string.Empty;
            Alignment = HorizontalAlignment.Center;
            ApplyVisualState();
        }

        /// <summary>
        ///     Updates the selected visual state used by segmented button groups and previews.
        ///     更新分段按钮组和预览使用的选中视觉状态。
        /// </summary>
        /// <param name="selected">
        ///     Whether the button should render as selected.
        ///     按钮是否应渲染为选中状态。
        /// </param>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        private void EnsurePressedHandlerAttached()
        {
            if (_pressedHandlerAttached)
                return;
            Pressed += InvokeActionSafely;
            _pressedHandlerAttached = true;
        }

        private void InvokeActionSafely()
        {
            if (_action == null)
                return;

            try
            {
                _action();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModSettingsTextButton] action failed: {ex.Message}");
            }
        }

        private void ApplyVisualState()
        {
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _tone));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _tone));
            AddThemeStyleboxOverride("focus", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, false, _tone));
        }

        private static Color ResolveToneForeground(ModSettingsButtonTone tone)
        {
            return tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };
        }

        private static StyleBoxFlat CreateStyle(bool selected, bool hovered, ModSettingsButtonTone tone)
        {
            var borderColor = tone switch
            {
                ModSettingsButtonTone.Accent => RitsuShellTheme.Current.Component.TextButton.Accent.Fg,
                ModSettingsButtonTone.Danger => RitsuShellTheme.Current.Component.TextButton.Danger.Fg,
                _ => RitsuShellTheme.Current.Component.TextButton.Neutral.Fg,
            };

            var backgroundColor = tone switch
            {
                ModSettingsButtonTone.Accent => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Accent.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Accent.Bg,
                ModSettingsButtonTone.Danger => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Danger.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Danger.Bg,
                _ => selected || hovered
                    ? RitsuShellTheme.Current.Component.TextButton.Neutral.BgHover
                    : RitsuShellTheme.Current.Component.TextButton.Neutral.Bg,
            };

            var shadowSize = hovered ? 7 : 2;
            var shadowColor = hovered
                ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                : RitsuShellTheme.Current.Color.Shadow.Ambient;
            var normalBorder =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.textButton.layout.borderWidth", 1);
            var hoverBorder = RitsuShellThemeLayoutResolver.ResolveEdges(
                "components.textButton.layout.borderWidthHover",
                normalBorder.Left + 1);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.textButton.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.textButton.layout.padding.bottom", 8));
            var border = hovered ? hoverBorder : normalBorder;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.textButton.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);

            return new()
            {
                BgColor = backgroundColor,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                ShadowColor = shadowColor,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }
    }
}
