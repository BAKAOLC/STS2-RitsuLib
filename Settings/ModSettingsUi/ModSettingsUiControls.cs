using System.Globalization;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.RuntimeInput;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Standard On/Off toggle control used by mod settings entries.
    /// </summary>
    public sealed partial class ModSettingsToggleControl : ModSettingsGamepadCompatibleButton
    {
        private readonly bool _initialValue;
        private readonly Action<bool>? _onChanged;
        private bool _isOn;

        /// <summary>
        ///     Creates a toggle control with an initial value and change callback.
        /// </summary>
        /// <param name="initialValue">Whether the toggle starts enabled.</param>
        /// <param name="onChanged">Callback invoked after the value changes.</param>
        public ModSettingsToggleControl(bool initialValue, Action<bool> onChanged)
        {
            _initialValue = initialValue;
            _onChanged = onChanged;

            CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 18);
            AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            AddThemeColorOverride("font_hover_color", Colors.White);
            AddThemeColorOverride("font_pressed_color", Colors.White);
            AddThemeColorOverride("font_focus_color", Colors.White);
            Pressed += ToggleValue;
        }

        /// <summary>
        ///     Creates the toggle control for Godot scene instantiation.
        /// </summary>
        public ModSettingsToggleControl()
        {
        }

        /// <summary>
        ///     Initializes the visual state after the control enters the scene tree.
        /// </summary>
        public override void _Ready()
        {
            _isOn = _initialValue;
            ApplyVisualState();
        }

        /// <summary>
        ///     Sets the current toggle value without recreating the control.
        /// </summary>
        /// <param name="value">The value to display.</param>
        public void SetValue(bool value)
        {
            _isOn = value;
            ApplyVisualState();
        }

        private void ToggleValue()
        {
            _isOn = !_isOn;
            ApplyVisualState();
            _onChanged?.Invoke(_isOn);
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
                ? new(0.52f, 0.87f, 0.69f, 0.95f)
                : new Color(0.34f, 0.46f, 0.58f, 0.45f);
            var borderW = hovered ? 3 : 2;
            return new()
            {
                BgColor = on
                    ? new(0.18f, 0.42f, 0.31f, 0.98f)
                    : hovered
                        ? new(0.18f, 0.22f, 0.28f, 0.98f)
                        : new Color(0.12f, 0.15f, 0.19f, 0.98f),
                BorderColor = borderColor,
                BorderWidthLeft = borderW,
                BorderWidthTop = borderW,
                BorderWidthRight = borderW,
                BorderWidthBottom = borderW,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = hovered
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : new Color(0f, 0f, 0f, 0.12f),
                ShadowSize = hovered ? 7 : 2,
                ContentMarginLeft = 14,
                ContentMarginTop = 8,
                ContentMarginRight = 14,
                ContentMarginBottom = 8,
            };
        }

        private static StyleBoxFlat CreateDisabledStyle()
        {
            return new()
            {
                BgColor = new(0.10f, 0.10f, 0.12f, 0.7f),
                BorderColor = new(0.25f, 0.25f, 0.28f, 0.35f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 14,
                ContentMarginTop = 8,
                ContentMarginRight = 14,
                ContentMarginBottom = 8,
            };
        }
    }

    internal sealed partial class ModSettingsSliderControl : HBoxContainer
    {
        private const double SliderEpsilon = 1e-9;

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

            CustomMinimumSize = new(ModSettingsUiMetrics.SliderRowMinWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Alignment = AlignmentMode.Center;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 8);

            var valueEdit = new LineEdit
            {
                Name = "SliderValue",
                CustomMinimumSize = new(ModSettingsUiMetrics.SliderValueFieldWidth,
                    ModSettingsUiMetrics.SliderValueFieldHeight),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Alignment = HorizontalAlignment.Center,
                SelectAllOnFocus = true,
                CaretBlink = true,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(valueEdit,
                ModSettingsUiResources.KreonRegular);
            AddChild(valueEdit);
            _valueEdit = valueEdit;

            var sliderPanel = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(ModSettingsUiMetrics.SliderTrackMinWidth,
                    ModSettingsUiMetrics.SliderValueFieldHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sliderPanel.AddThemeConstantOverride("margin_top", 4);
            sliderPanel.AddThemeConstantOverride("margin_bottom", 4);
            AddChild(sliderPanel);

            var normalizedInitial = NormalizeSliderValue(initialValue, minValue, maxValue, step);
            var slider = new HSlider
            {
                Name = "Slider",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(0f, 24f),
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
            _slider.DragEnded += _ => _slider.ReleaseFocus();
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
            _onChanged?.Invoke(value);
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

            if (!IsApproxEqual(value, actual))
                _onChanged?.Invoke(actual);
        }

        private static double NormalizeSliderValue(double value, double minValue, double maxValue, double step)
        {
            var v = Math.Clamp(value, minValue, maxValue);
            if (step > 0d)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private static bool IsApproxEqual(double a, double b)
        {
            return Math.Abs(a - b) <= SliderEpsilon * Math.Max(1d, Math.Max(Math.Abs(a), Math.Abs(b)));
        }

        private void SyncBindingToCanonicalSliderValue(double bindingClaimed)
        {
            if (_slider == null)
                return;

            var onSlider = _slider.Value;
            if (!IsApproxEqual(bindingClaimed, onSlider))
                _onChanged?.Invoke(onSlider);
        }

        private void RefreshValueLabel(double value)
        {
            if (_valueEdit == null || _formatter == null)
                return;
            _valueEdit.Text = _formatter(value);
        }

        private void OnValueSubmitted(string text)
        {
            TryApplyTypedValue(text);
            _valueEdit?.ReleaseFocus();
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
            return new()
            {
                BgColor = highlighted
                    ? new(0.48f, 0.73f, 0.92f, 0.95f)
                    : new Color(0.26f, 0.34f, 0.43f, 0.98f),
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 8,
                ContentMarginTop = 6,
                ContentMarginRight = 8,
                ContentMarginBottom = 6,
            };
        }
    }

    /// <summary>
    ///     Legacy <see cref="float" /> slider row: Godot <see cref="HSlider" /> still uses <see cref="double" /> values,
    ///     but comparisons and binding I/O stay in <see cref="float" /> space to match obsolete
    ///     <c>AddSlider(..., IModSettingsValueBinding&lt;float&gt;, ...)</c> mods without double bridges.
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
        /// </summary>
        /// <param name="initialValue">The starting value shown by the slider.</param>
        /// <param name="minValue">The minimum allowed value.</param>
        /// <param name="maxValue">The maximum allowed value.</param>
        /// <param name="step">The slider increment.</param>
        /// <param name="formatter">Formats committed values for the text field.</param>
        /// <param name="onChanged">Invoked when the effective value changes.</param>
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

            CustomMinimumSize = new(ModSettingsUiMetrics.SliderRowMinWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Alignment = AlignmentMode.Center;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 8);

            var valueEdit = new LineEdit
            {
                Name = "SliderValue",
                CustomMinimumSize = new(ModSettingsUiMetrics.SliderValueFieldWidth,
                    ModSettingsUiMetrics.SliderValueFieldHeight),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Alignment = HorizontalAlignment.Center,
                SelectAllOnFocus = true,
                CaretBlink = true,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(valueEdit,
                ModSettingsUiResources.KreonRegular);
            AddChild(valueEdit);
            _valueEdit = valueEdit;

            var sliderPanel = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(ModSettingsUiMetrics.SliderTrackMinWidth,
                    ModSettingsUiMetrics.SliderValueFieldHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sliderPanel.AddThemeConstantOverride("margin_top", 4);
            sliderPanel.AddThemeConstantOverride("margin_bottom", 4);
            AddChild(sliderPanel);

            var normalizedInitial = NormalizeSliderValue(initialValue, minValue, maxValue, step);
            var slider = new HSlider
            {
                Name = "Slider",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(0f, 24f),
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
            _slider.DragEnded += _ => _slider.ReleaseFocus();
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
            _onChanged?.Invoke(f);
        }

        /// <summary>
        ///     Replaces the current slider value without leaving stale formatted text behind.
        /// </summary>
        /// <param name="value">The value to apply.</param>
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

            if (!Mathf.IsEqualApprox(value, actual))
                _onChanged?.Invoke(actual);
        }

        private static float NormalizeSliderValue(float value, float minValue, float maxValue, float step)
        {
            var v = Mathf.Clamp(value, minValue, maxValue);
            if (step > 0f)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private void SyncBindingToCanonicalSliderValue(float bindingClaimed)
        {
            if (_slider == null)
                return;

            var onSlider = (float)_slider.Value;
            if (!Mathf.IsEqualApprox(bindingClaimed, onSlider))
                _onChanged?.Invoke(onSlider);
        }

        private void RefreshValueLabel(float value)
        {
            if (_valueEdit == null || _formatter == null)
                return;
            _valueEdit.Text = _formatter(value);
        }

        private void OnValueSubmitted(string text)
        {
            TryApplyTypedValue(text);
            _valueEdit?.ReleaseFocus();
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
            return new()
            {
                BgColor = highlighted
                    ? new(0.48f, 0.73f, 0.92f, 0.95f)
                    : new Color(0.26f, 0.34f, 0.43f, 0.98f),
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 8,
                ContentMarginTop = 6,
                ContentMarginRight = 8,
                ContentMarginBottom = 6,
            };
        }
    }

    /// <summary>
    ///     Stepper-style choice editor that cycles through labeled values.
    /// </summary>
    /// <typeparam name="TValue">The stored option value type.</typeparam>
    public sealed partial class ModSettingsChoiceControl<TValue> : HBoxContainer
    {
        private readonly TValue? _currentValue;
        private readonly Action<TValue>? _onChanged;
        private readonly (TValue Value, string Label)[]? _optionsWithValues;
        private int _currentIndex;
        private Label? _label;
        private bool _suppressCallbacks;

        /// <summary>
        ///     Creates a stepper-style choice editor.
        /// </summary>
        /// <param name="options">The labeled values available to the editor.</param>
        /// <param name="currentValue">The value selected initially.</param>
        /// <param name="onChanged">Invoked after the user picks a different value.</param>
        public ModSettingsChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = options.ToArray();
            _currentValue = currentValue;
            _onChanged = onChanged;

            CustomMinimumSize = new(ModSettingsUiMetrics.ChoiceRowMinWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            Alignment = AlignmentMode.Center;
            AddThemeConstantOverride("separation", 6);

            AddChild(new ModSettingsMiniButton("<", () => Shift(-1))
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.MiniStepperButtonSize,
                    ModSettingsUiMetrics.MiniStepperButtonSize),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });

            var center = new PanelContainer
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.ChoiceCenterMinWidth,
                    ModSettingsUiMetrics.SliderValueFieldHeight),
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
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            label.AddThemeFontSizeOverride("font_size", 17);
            label.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            center.AddChild(label);
            _label = label;

            AddChild(new ModSettingsMiniButton(">", () => Shift(1))
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.MiniStepperButtonSize,
                    ModSettingsUiMetrics.MiniStepperButtonSize),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsChoiceControl()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_optionsWithValues == null)
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
            if (_optionsWithValues == null || _optionsWithValues.Length == 0)
                return;

            _currentIndex = (_currentIndex + delta + _optionsWithValues.Length) % _optionsWithValues.Length;
            RefreshCurrentLabel();
            if (!_suppressCallbacks)
                _onChanged?.Invoke(_optionsWithValues[_currentIndex].Value);
        }

        /// <summary>
        ///     Selects the matching option without triggering the external change callback.
        /// </summary>
        /// <param name="value">The value to select.</param>
        public void SetValue(TValue value)
        {
            if (_optionsWithValues == null)
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

        private void RefreshCurrentLabel()
        {
            if (_optionsWithValues == null || _label == null)
                return;
            _label.Text = _optionsWithValues[_currentIndex].Label;
        }
    }

    /// <summary>
    ///     Dropdown-style choice editor for labeled values.
    /// </summary>
    /// <typeparam name="TValue">The stored option value type.</typeparam>
    public sealed partial class ModSettingsDropdownChoiceControl<TValue> : HBoxContainer
    {
        private const float DropListMinWidth = 200f;
        private const float RowHeight = 38f;

        private readonly Action<TValue>? _onChanged;
        private readonly (TValue Value, string Label)[]? _optionsWithValues;
        private readonly System.Collections.Generic.Dictionary<int, ModSettingsMiniButton> _rowButtonCache = [];
        private readonly List<ModSettingsMiniButton> _rowButtons = [];
        private Control? _backdrop;
        private VBoxContainer? _dropList;
        private bool _dropOpen;
        private PanelContainer? _dropPanel;
        private ModSettingsGamepadCompatibleButton? _faceButton;
        private int _selectedIndex;
        private bool _suppressCallbacks;

        /// <summary>
        ///     Creates a dropdown-style choice editor.
        /// </summary>
        /// <param name="options">The labeled values available to the editor.</param>
        /// <param name="currentValue">The value selected initially.</param>
        /// <param name="onChanged">Invoked after the user picks a different value.</param>
        public ModSettingsDropdownChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = options.ToArray();
            _onChanged = onChanged;

            CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth, ModSettingsUiMetrics.EntryValueMinHeight);
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
                CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth,
                    ModSettingsUiMetrics.EntryValueMinHeight),
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
            face.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            face.AddThemeFontSizeOverride("font_size", 17);
            face.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            face.AddThemeColorOverride("font_hover_color", new(1f, 1f, 1f));
            face.AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            face.AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(face);
            face.Pressed += OnFacePressed;
            AddChild(face);
            _faceButton = face;

            RefreshFaceLabel();
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsDropdownChoiceControl()
        {
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

        /// <inheritdoc />
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

        /// <summary>
        ///     Selects the matching option without reopening the dropdown or firing the external callback.
        /// </summary>
        /// <param name="value">The value to select.</param>
        public void SetValue(TValue value)
        {
            if (_optionsWithValues == null || _faceButton == null)
                return;

            var idx = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, value));
            if (idx < 0)
                return;

            _suppressCallbacks = true;
            _selectedIndex = idx;
            RefreshFaceLabel();
            _suppressCallbacks = false;
        }

        private void OnFacePressed()
        {
            if (_faceButton == null || _faceButton.Disabled || _optionsWithValues == null ||
                _optionsWithValues.Length == 0)
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

            _dropList = new()
            {
                Name = "ChoiceDropdownList",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _dropList.AddThemeConstantOverride("separation", 8);
            _dropPanel.AddChild(_dropList);
        }

        private void OnBackdropGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                CloseDropdown();
        }

        private void OpenDropdown()
        {
            if (_dropPanel == null || _dropList == null || _backdrop == null || _optionsWithValues == null ||
                _optionsWithValues.Length == 0)
                return;

            RebuildListRows();
            if (_rowButtons.Count == 0)
                return;

            _dropOpen = true;
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
            LayoutDropdownInViewport();
            _backdrop.Visible = true;
            _dropPanel.Visible = true;
            WireRowFocusNeighbors();
            Callable.From(GrabSelectedRowFocus).CallDeferred();
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            _dropOpen = false;
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            if (_backdrop != null)
                _backdrop.Visible = false;
            if (_dropPanel != null)
                _dropPanel.Visible = false;

            if (_faceButton != null && IsInstanceValid(_faceButton) && _faceButton.IsVisibleInTree())
                _faceButton.GrabFocus();
        }

        private void RebuildListRows()
        {
            if (_dropList == null || _optionsWithValues == null)
                return;

            _rowButtons.Clear();
            var liveIndexes = Enumerable.Range(0, _optionsWithValues.Length).ToHashSet();
            foreach (var staleIndex in _rowButtonCache.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
            {
                if (_rowButtonCache.TryGetValue(staleIndex, out var staleRow) && IsInstanceValid(staleRow))
                    staleRow.QueueFree();
                _rowButtonCache.Remove(staleIndex);
            }

            var panelMinW = DropListMinWidth;
            if (_faceButton != null)
                panelMinW = Mathf.Max(panelMinW, _faceButton.CustomMinimumSize.X);

            for (var i = 0; i < _optionsWithValues.Length; i++)
            {
                var index = i;
                var opt = _optionsWithValues[i];
                if (!_rowButtonCache.TryGetValue(index, out var row) || !IsInstanceValid(row))
                {
                    row = new(opt.Label, () => ActivateRow(index))
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        Alignment = HorizontalAlignment.Left,
                    };
                    row.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
                    row.AddThemeFontSizeOverride("font_size", 18);
                    _rowButtonCache[index] = row;
                }

                row.Text = opt.Label;
                row.CustomMinimumSize = new(panelMinW - 24f, RowHeight);
                row.TooltipText = string.Empty;
                row.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
                row.AddThemeColorOverride("font_hover_color", Colors.White);
                row.AddThemeColorOverride("font_pressed_color", Colors.White);
                row.AddThemeStyleboxOverride("normal", ModSettingsMiniButton.CreateStyle(false));
                row.AddThemeStyleboxOverride("hover", ModSettingsMiniButton.CreateStyle(true));
                row.AddThemeStyleboxOverride("pressed", ModSettingsMiniButton.CreatePressedStyle());
                row.AddThemeStyleboxOverride("focus", ModSettingsMiniButton.CreateFocusStyle());
                if (index == _selectedIndex)
                {
                    row.TooltipText = ModSettingsLocalization.Get("choice.dropdown.currentRow",
                        "This option is the active setting (shown on the closed control).");
                    row.AddThemeColorOverride("font_color", new(0.95f, 0.98f, 1f));
                    row.AddThemeColorOverride("font_hover_color", Colors.White);
                    row.AddThemeColorOverride("font_pressed_color", Colors.White);
                    row.AddThemeStyleboxOverride("normal", CreateDropdownCurrentRowNormal());
                    row.AddThemeStyleboxOverride("hover", CreateDropdownCurrentRowHover());
                    row.AddThemeStyleboxOverride("pressed", CreateDropdownCurrentRowPressed());
                    row.AddThemeStyleboxOverride("focus", CreateDropdownCurrentRowFocus());
                }

                if (row.GetParent() != _dropList)
                    _dropList.AddChild(row);
                _dropList.MoveChild(row, i);
                _rowButtons.Add(row);
            }

            if (_dropPanel != null)
                _dropPanel.CustomMinimumSize = new(panelMinW, 0f);
        }

        private void ActivateRow(int index)
        {
            if (_suppressCallbacks || _optionsWithValues == null)
                return;

            if (index < 0 || index >= _optionsWithValues.Length)
                return;

            _selectedIndex = index;
            RefreshFaceLabel();
            _onChanged?.Invoke(_optionsWithValues[index].Value);
            CloseDropdown();
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
            if (_faceButton == null || _optionsWithValues == null || _optionsWithValues.Length == 0)
                return;
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
            return new()
            {
                BgColor = new(0.14f, 0.26f, 0.32f, 0.98f),
                BorderColor = new(0.45f, 0.72f, 0.86f, 0.85f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 10,
                ContentMarginTop = 5,
                ContentMarginRight = 10,
                ContentMarginBottom = 5,
            };
        }

        private static StyleBoxFlat CreateDropdownCurrentRowHover()
        {
            var s = CreateDropdownCurrentRowNormal();
            s.BgColor = new(0.17f, 0.30f, 0.36f, 0.99f);
            return s;
        }

        private static StyleBoxFlat CreateDropdownCurrentRowPressed()
        {
            var s = CreateDropdownCurrentRowNormal();
            s.BgColor = new(0.19f, 0.34f, 0.40f, 0.99f);
            return s;
        }

        private static StyleBoxFlat CreateDropdownCurrentRowFocus()
        {
            return new()
            {
                BgColor = new(0.18f, 0.32f, 0.38f, 0.99f),
                BorderColor = new(0.85f, 0.94f, 1f, 0.98f),
                BorderWidthLeft = 3,
                BorderWidthTop = 3,
                BorderWidthRight = 3,
                BorderWidthBottom = 3,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 9,
                ContentMarginTop = 4,
                ContentMarginRight = 9,
                ContentMarginBottom = 4,
            };
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

        private void GrabSelectedRowFocus()
        {
            if (_rowButtons.Count == 0)
                return;
            var idx = Mathf.Clamp(_selectedIndex, 0, _rowButtons.Count - 1);
            _rowButtons[idx].GrabFocus();
        }

        private void LayoutDropdownInViewport()
        {
            if (_backdrop == null || _dropPanel == null || _faceButton == null)
                return;

            var vr = GetViewport().GetVisibleRect();
            _backdrop.GlobalPosition = vr.Position;
            _backdrop.Size = vr.Size;

            _dropPanel.ResetSize();
            var panelSize = _dropPanel.GetCombinedMinimumSize();
            var gr = _faceButton.GetGlobalRect();
            var desiredTopLeft = new Vector2(gr.Position.X, gr.End.Y);

            var maxX = vr.End.X - panelSize.X;
            var maxY = vr.End.Y - panelSize.Y;
            desiredTopLeft = new(
                Mathf.Clamp(desiredTopLeft.X, vr.Position.X, maxX),
                Mathf.Clamp(desiredTopLeft.Y, vr.Position.Y, maxY));
            _dropPanel.GlobalPosition = desiredTopLeft;
        }
    }

    /// <summary>
    ///     Color editor with a visible swatch picker and editable hex value.
    /// </summary>
    public sealed partial class ModSettingsColorControl : HBoxContainer
    {
        private readonly Action<string?>? _onChanged;
        private LineEdit? _hexEdit;
        private string _lastCommitted = string.Empty;
        private ColorPickerButton? _pickerButton;
        private bool _pickerChangedWhileOpen;
        private bool _suppressCallbacks;
        private Color _unsetPreviewColor = new(1f, 215f / 255f, 64f / 255f);

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
        /// </summary>
        /// <param name="initialValue">The initial color string (hex, HTML, or BaseLib <c>[r,g,b,a]</c>), or null/empty.</param>
        /// <param name="onChanged">Callback invoked after the committed color value changes.</param>
        /// <param name="editAlpha">Whether the swatch popup exposes alpha editing.</param>
        /// <param name="editIntensity">Whether the popup exposes intensity (HDR) editing.</param>
        public ModSettingsColorControl(string? initialValue, Action<string?> onChanged, bool editAlpha,
            bool editIntensity)
        {
            _onChanged = onChanged;

            CustomMinimumSize = new(ModSettingsUiMetrics.ColorRowMinWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            Alignment = AlignmentMode.Center;
            AddThemeConstantOverride("separation", 8);

            var pickerButton = new ColorPickerButton
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.ColorSwatchSize, ModSettingsUiMetrics.ColorSwatchSize),
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
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.SliderValueFieldHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(hexEdit, ModSettingsUiResources.KreonBold);
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
        /// </summary>
        public ModSettingsColorControl()
        {
        }

        /// <summary>
        ///     Current hex text shown by the editor, or an empty string when the color is unset.
        /// </summary>
        public string ValueText => _hexEdit?.Text ?? _lastCommitted;

        /// <summary>
        ///     Serializes <paramref name="color" /> as an 8-digit hex string for settings storage / display.
        /// </summary>
        public static string FormatStoredColorString(Color color)
        {
            return FormatColorValue(color);
        }

        /// <summary>
        ///     Parses hex, Godot HTML, or BaseLib-style <c>[r, g, b, a]</c> component lists (invariant floats).
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
        /// </summary>
        public override void _Ready()
        {
            if (_hexEdit != null)
            {
                _hexEdit.TextSubmitted += text =>
                {
                    ApplyFromHex(text, true);
                    _hexEdit.ReleaseFocus();
                };
                _hexEdit.FocusExited += () => ApplyFromHex(_hexEdit.Text, true);
                ModSettingsFocusChrome.AttachControllerSelectionReticle(_hexEdit);
            }

            if (_pickerButton == null) return;
            _pickerButton.PopupClosed += OnPickerPopupClosed;
            _pickerButton.ColorChanged += OnPickerColorChanged;
            ModSettingsFocusChrome.AttachControllerSelectionReticle(_pickerButton);
        }

        /// <summary>
        ///     Updates the displayed value without recreating the control.
        /// </summary>
        /// <param name="value">The hex color to display, or null/empty to leave the field unset.</param>
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
                _onChanged?.Invoke(null);
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
                _onChanged?.Invoke(formatted);
        }

        private void RestoreCurrentPresentation()
        {
            ApplyFromHex(_lastCommitted, false);
        }

        private void OnPickerColorChanged(Color color)
        {
            _pickerChangedWhileOpen = true;
            ApplyColor(color, false);
            _onChanged?.Invoke(FormatColorValue(color));
        }

        private void OnPickerPopupClosed()
        {
            _pickerChangedWhileOpen = false;
            _pickerButton?.ReleaseFocus();
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
    /// </summary>
    public sealed partial class ModSettingsKeyBindingControl : VBoxContainer
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
        /// </summary>
        /// <param name="initialValue">The binding shown initially.</param>
        /// <param name="allowModifierCombos">Whether modifier combinations are allowed.</param>
        /// <param name="allowModifierOnly">Whether modifier-only bindings are allowed.</param>
        /// <param name="distinguishModifierSides">Whether left and right modifier keys are recorded separately.</param>
        /// <param name="onChanged">Invoked after the binding changes.</param>
        public ModSettingsKeyBindingControl(string initialValue, bool allowModifierCombos, bool allowModifierOnly,
            bool distinguishModifierSides, Action<string> onChanged)
        {
            _allowModifierCombos = allowModifierCombos;
            _allowModifierOnly = allowModifierOnly;
            _distinguishModifierSides = distinguishModifierSides;
            _onChanged = onChanged;
            _currentValue = initialValue;

            CustomMinimumSize = new(ModSettingsUiMetrics.KeybindingBlockWidth, 80f);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 8);

            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            row.AddThemeConstantOverride("separation", 6);
            AddChild(row);

            var captureButton = new Button
            {
                CustomMinimumSize = new(ModSettingsUiMetrics.KeybindingCaptureMinWidth,
                    ModSettingsUiMetrics.EntryValueMinHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            captureButton.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            captureButton.AddThemeFontSizeOverride("font_size", 17);
            captureButton.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(captureButton);
            row.AddChild(captureButton);
            _captureButton = captureButton;

            row.AddChild(new ModSettingsMiniButton(ModSettingsLocalization.Get("button.clear", "Clear"),
                () => ApplyBinding(string.Empty, true))
            {
                CustomMinimumSize = new(64f, ModSettingsUiMetrics.EntryValueMinHeight),
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
            hint.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            hint.AddThemeFontSizeOverride("font_size", ModSettingsUiMetrics.KeybindingHintFontSize);
            hint.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshText();
            SetProcessUnhandledKeyInput(true);
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsKeyBindingControl()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_captureButton != null)
                _captureButton.Pressed += BeginCapture;
        }

        /// <summary>
        ///     Replaces the captured binding without starting capture mode.
        /// </summary>
        /// <param name="value">The binding to display.</param>
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
                _onChanged?.Invoke(value);
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

    internal sealed partial class ModSettingsActionsButton : ModSettingsGamepadCompatibleButton
    {
        private const float DropMinWidth = 260f;
        private const float RowHeight = 38f;

        private readonly IReadOnlyList<ModSettingsMenuAction> _actions;
        private readonly Action? _afterAction;
        private readonly System.Collections.Generic.Dictionary<int, ModSettingsMiniButton> _rowButtonCache = [];
        private readonly List<ModSettingsMiniButton> _rowButtons = [];
        private Control? _backdrop;
        private VBoxContainer? _dropList;
        private bool _dropOpen;
        private PanelContainer? _dropPanel;
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
            CustomMinimumSize = new(36f, 32f);
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 18);
            AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextSecondary);
            AddThemeColorOverride("font_hover_color", new(0.98f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
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

        public override void _Ready()
        {
            base._Ready();
            BuildDropdownShell();
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
            _preferredPopupPosition = new Vector2I(
                Mathf.RoundToInt(globalPosition.X),
                Mathf.RoundToInt(globalPosition.Y));
            if (_dropOpen)
            {
                LayoutDropdownInViewport();
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

        private void BuildDropdownShell()
        {
            _backdrop = new()
            {
                Name = "ActionsMenuBackdrop",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                TopLevel = true,
                ZIndex = 900,
            };
            _backdrop.SetAnchorsPreset(LayoutPreset.TopLeft);
            _backdrop.GuiInput += OnBackdropGuiInput;
            AddChild(_backdrop);

            _dropPanel = new()
            {
                Name = "ActionsMenuPanel",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
                TopLevel = true,
                ZIndex = 901,
                CustomMinimumSize = new(DropMinWidth, 0f),
            };
            _dropPanel.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            AddChild(_dropPanel);

            _dropList = new()
            {
                Name = "ActionsMenuList",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _dropList.AddThemeConstantOverride("separation", 8);
            _dropPanel.AddChild(_dropList);
        }

        private void OnBackdropGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                CloseDropdown();
        }

        private void OpenDropdown()
        {
            if (_actions.Count == 0 || _dropPanel == null || _dropList == null || _backdrop == null)
                return;

            RebuildMenuRows();
            if (_rowButtons.Count == 0)
                return;

            _dropOpen = true;
            SetProcessInput(true);
            SetProcessUnhandledInput(true);
            LayoutDropdownInViewport();
            _backdrop.Visible = true;
            _dropPanel.Visible = true;
            WireRowFocusNeighbors();
            Callable.From(GrabFirstEnabledRow).CallDeferred();
        }

        private void CloseDropdown()
        {
            if (!_dropOpen)
                return;

            _dropOpen = false;
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            _preferredPopupPosition = null;
            if (_backdrop != null)
                _backdrop.Visible = false;
            if (_dropPanel != null)
                _dropPanel.Visible = false;

            if (IsInstanceValid(this) && IsVisibleInTree())
                GrabFocus();
        }

        private void RebuildMenuRows()
        {
            if (_dropList == null)
                return;

            _rowButtons.Clear();
            var liveIndexes = Enumerable.Range(0, _actions.Count).ToHashSet();
            foreach (var staleIndex in _rowButtonCache.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
            {
                if (_rowButtonCache.TryGetValue(staleIndex, out var staleRow) && IsInstanceValid(staleRow))
                    staleRow.QueueFree();
                _rowButtonCache.Remove(staleIndex);
            }

            for (var i = 0; i < _actions.Count; i++)
            {
                var index = i;
                var def = _actions[i];
                if (!_rowButtonCache.TryGetValue(index, out var row) || !IsInstanceValid(row))
                {
                    row = new(def.Label, () => ActivateRow(index))
                    {
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                        Alignment = HorizontalAlignment.Left,
                    };
                    row.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
                    row.AddThemeFontSizeOverride("font_size", 18);
                    _rowButtonCache[index] = row;
                }

                row.Text = def.Label;
                row.CustomMinimumSize = new(DropMinWidth - 24f, RowHeight);
                row.Disabled = !def.IsEnabled();
                if (row.GetParent() != _dropList)
                    _dropList.AddChild(row);
                _dropList.MoveChild(row, i);
                _rowButtons.Add(row);
            }
        }

        private void ActivateRow(int index)
        {
            if (index < 0 || index >= _actions.Count)
                return;

            var def = _actions[index];
            if (!def.IsEnabled())
                return;

            def.Action();
            _afterAction?.Invoke();
            CloseDropdown();
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

        private void LayoutDropdownInViewport()
        {
            if (_backdrop == null || _dropPanel == null)
                return;

            var vr = GetViewport().GetVisibleRect();
            _backdrop.GlobalPosition = vr.Position;
            _backdrop.Size = vr.Size;

            _dropPanel.ResetSize();
            var panelSize = _dropPanel.GetCombinedMinimumSize();
            Vector2 desiredTopLeft;
            if (_preferredPopupPosition.HasValue)
            {
                desiredTopLeft = new(_preferredPopupPosition.Value.X, _preferredPopupPosition.Value.Y);
            }
            else
            {
                var gr = GetGlobalRect();
                desiredTopLeft = new(gr.End.X - panelSize.X, gr.End.Y);
            }

            var maxX = vr.End.X - panelSize.X;
            var maxY = vr.End.Y - panelSize.Y;
            desiredTopLeft = new(
                Mathf.Clamp(desiredTopLeft.X, vr.Position.X, maxX),
                Mathf.Clamp(desiredTopLeft.Y, vr.Position.Y, maxY));
            _dropPanel.GlobalPosition = desiredTopLeft;
        }
    }

    /// <summary>
    ///     Multi-keybinding capture editor used by native settings pages.
    /// </summary>
    public sealed partial class ModSettingsMultiKeyBindingControl : VBoxContainer
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
        /// </summary>
        /// <param name="initialValues">Initial bindings shown by the control.</param>
        /// <param name="allowModifierCombos">Whether modifier combinations are allowed.</param>
        /// <param name="allowModifierOnly">Whether modifier-only bindings are allowed.</param>
        /// <param name="distinguishModifierSides">Whether left/right modifier keys are recorded separately.</param>
        /// <param name="onChanged">Invoked after the normalized binding list changes.</param>
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
            AddThemeConstantOverride("separation", 8);

            var bindingsList = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            bindingsList.AddThemeConstantOverride("separation", 6);
            AddChild(bindingsList);
            _bindingsList = bindingsList;

            var hint = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            hint.AddThemeFontSizeOverride("font_size", ModSettingsUiMetrics.KeybindingHintFontSize);
            hint.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshPresentation();
            SetProcessUnhandledKeyInput(true);
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsMultiKeyBindingControl()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
        }

        /// <summary>
        ///     Replaces the displayed binding list without starting capture mode.
        /// </summary>
        /// <param name="values">The bindings to display.</param>
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
                _onChanged?.Invoke(_values.ToList());
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
                empty.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
                empty.AddThemeFontSizeOverride("font_size", 15);
                empty.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelSecondary);
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
            row.AddThemeConstantOverride("separation", 6);

            var primaryButton = new Button
            {
                Text = primaryText,
                CustomMinimumSize = new(ModSettingsUiMetrics.KeybindingCaptureMinWidth,
                    ModSettingsUiMetrics.EntryValueMinHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            primaryButton.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            primaryButton.AddThemeFontSizeOverride("font_size", 17);
            primaryButton.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(primaryButton);
            primaryButton.Pressed += primaryAction;
            row.AddChild(primaryButton);

            row.AddChild(new ModSettingsMiniButton(secondaryText, secondaryAction)
            {
                CustomMinimumSize = new(86f, ModSettingsUiMetrics.EntryValueMinHeight),
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
    /// </summary>
    public sealed partial class ModSettingsMiniButton : ModSettingsGamepadCompatibleButton
    {
        /// <summary>
        ///     Creates a compact button with the standard mini-button chrome.
        /// </summary>
        /// <param name="text">The button label.</param>
        /// <param name="action">Invoked when the button is pressed.</param>
        public ModSettingsMiniButton(string text, Action action)
        {
            Text = text;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 17);
            AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            AddThemeColorOverride("font_hover_color", Colors.White);
            AddThemeColorOverride("font_pressed_color", Colors.White);
            AddThemeStyleboxOverride("normal", CreateStyle(false));
            AddThemeStyleboxOverride("hover", CreateStyle(true));
            AddThemeStyleboxOverride("pressed", CreateStyle(true));
            AddThemeStyleboxOverride("focus", CreateStyle(true));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, true));
            Pressed += action;
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsMiniButton()
        {
        }

        /// <summary>
        ///     Creates the standard mini-button surface for normal, hover, focus, and disabled states.
        /// </summary>
        public static StyleBoxFlat CreateStyle(bool highlighted, bool disabled = false)
        {
            return new()
            {
                BgColor = disabled
                    ? new(0.11f, 0.14f, 0.18f, 0.82f)
                    : highlighted
                        ? new(0.17f, 0.28f, 0.34f, 0.98f)
                        : new Color(0.12f, 0.16f, 0.21f, 0.96f),
                BorderColor = disabled
                    ? new(0.28f, 0.36f, 0.43f, 0.40f)
                    : highlighted
                        ? new(0.60f, 0.82f, 0.92f, 0.78f)
                        : new Color(0.38f, 0.54f, 0.66f, 0.40f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 10,
                ContentMarginTop = 5,
                ContentMarginRight = 10,
                ContentMarginBottom = 5,
            };
        }

        /// <summary>
        ///     Creates the pressed-state style for mini buttons.
        /// </summary>
        public static StyleBoxFlat CreatePressedStyle()
        {
            return CreateStyle(true);
        }

        /// <summary>
        ///     Creates the focus-state style for mini buttons.
        /// </summary>
        public static StyleBoxFlat CreateFocusStyle()
        {
            return CreateStyle(true);
        }
    }

    internal sealed partial class ModSettingsDragHandle : Button
    {
        private readonly Func<Dictionary>? _dragDataProvider;
        private NControllerManager? _hookedControllerManagerDrag;

        public ModSettingsDragHandle(string indexLabel, Func<Dictionary> dragDataProvider)
        {
            _dragDataProvider = dragDataProvider;

            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            CustomMinimumSize = new(52f, 0f);
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
            content.AddThemeConstantOverride("separation", 3);
            AddChild(content);

            var number = new Label
            {
                Text = indexLabel,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            number.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            number.AddThemeFontSizeOverride("font_size", 17);
            number.AddThemeColorOverride("font_color", new(0.96f, 0.98f, 1f));
            content.AddChild(number);

            var grip = new Label
            {
                Text = "::::",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            grip.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            grip.AddThemeFontSizeOverride("font_size", 14);
            grip.AddThemeColorOverride("font_color", new(0.78f, 0.88f, 0.94f, 0.95f));
            content.AddChild(grip);

            var hint = new Label
            {
                Text = ModSettingsLocalization.Get("list.dragShort", "Drag"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            hint.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            hint.AddThemeFontSizeOverride("font_size", 12);
            hint.AddThemeColorOverride("font_color", new(0.80f, 0.89f, 0.94f, 0.90f));
            content.AddChild(hint);
        }

        public ModSettingsDragHandle()
        {
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
                CustomMinimumSize = new(48f, ModSettingsUiMetrics.EntryValueMinHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            preview.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListItemCardStyle(true));
            SetDragPreview(preview);
            return Variant.From(_dragDataProvider());
        }

        private static StyleBoxFlat CreateRailStyle(bool highlighted)
        {
            return new()
            {
                BgColor = highlighted
                    ? new(0.16f, 0.28f, 0.36f, 0.98f)
                    : new Color(0.12f, 0.20f, 0.27f, 0.96f),
                BorderColor = highlighted
                    ? new(0.65f, 0.86f, 0.94f, 0.88f)
                    : new Color(0.40f, 0.60f, 0.71f, 0.58f),
                BorderWidthLeft = 0,
                BorderWidthTop = 0,
                BorderWidthRight = 1,
                BorderWidthBottom = 0,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 6,
                ContentMarginTop = 8,
                ContentMarginRight = 6,
                ContentMarginBottom = 8,
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
        private VBoxContainer? _rows;

        public ModSettingsListControl(ModSettingsUiContext context, ListModSettingsEntryDefinition<TItem> entry)
        {
            UiContext = context;
            _entry = entry;

            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 10);
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
            root.AddThemeConstantOverride("separation", 10);
            shell.AddChild(root);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation", 10);
            root.AddChild(header);

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation", 3);
            header.AddChild(textColumn);

            textColumn.AddChild(ModSettingsUiFactory.CreateRefreshableSectionTitle(UiContext,
                () => ModSettingsUiFactory.ResolveEntryLabelDisplay(_entry.Label)));

            var descriptionLabel = ModSettingsUiFactory.CreateRefreshableDescriptionLabel(UiContext,
                () => ModSettingsUiContext.ResolveBindingDescriptionBody(_entry.Description));
            textColumn.AddChild(descriptionLabel);

            textColumn.AddChild(ModSettingsUiFactory.CreatePersistenceScopeTag(_entry.Binding));

            var summary = new PanelContainer
            {
                CustomMinimumSize = new(96f, 32f),
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
            countLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            countLabel.AddThemeFontSizeOverride("font_size", 15);
            countLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            summary.AddChild(countLabel);
            _countLabel = countLabel;

            var addButton = new ModSettingsTextButton(ModSettingsUiContext.Resolve(_entry.AddButtonText),
                ModSettingsButtonTone.Accent,
                () => Mutate(items => items.Add(_entry.CreateItem())))
            {
                CustomMinimumSize = new(152f, ModSettingsUiMetrics.EntryValueMinHeight),
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
            bodyContent.AddThemeConstantOverride("separation", 6);
            body.AddChild(bodyContent);

            _rows = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _rows.AddThemeConstantOverride("separation", 6);
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
            emptyLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            emptyLabel.AddThemeFontSizeOverride("font_size", 16);
            emptyLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelSecondary);
            emptyState.AddChild(emptyLabel);
            _emptyState = emptyState;

            ModSettingsUiFactory.RegisterRefreshWhenAlive(UiContext, this, RebuildRows);
            RebuildRows();
        }

        private void RebuildRows()
        {
            if (_rows == null || !IsInstanceValid(this))
                return;

            ClearActiveDropSlot();
            var items = _entry.Binding.Read();
            if (_countLabel != null)
                _countLabel.Text = string.Format(
                    ModSettingsLocalization.Get("list.count", "{0} items"),
                    items.Count);

            if (_emptyState != null)
                _emptyState.Visible = items.Count == 0;

            var liveIndexes = Enumerable.Range(0, items.Count).ToHashSet();
            foreach (var staleIndex in _rowCards.Keys.Where(index => !liveIndexes.Contains(index)).ToArray())
            {
                if (_rowCards.TryGetValue(staleIndex, out var staleRow) && IsInstanceValid(staleRow))
                    staleRow.QueueFree();
                _rowCards.Remove(staleIndex);
            }

            foreach (var staleSlot in _dropSlots.Keys.Where(index => index > items.Count).ToArray())
            {
                if (_dropSlots.TryGetValue(staleSlot, out var slot) && IsInstanceValid(slot))
                    slot.QueueFree();
                _dropSlots.Remove(staleSlot);
            }

            var childOrder = 0;
            for (var slotIndex = 0; slotIndex <= items.Count; slotIndex++)
            {
                var dropSlot = EnsureDropSlot(slotIndex);
                if (dropSlot.GetParent() != _rows)
                    _rows.AddChild(dropSlot);
                _rows.MoveChild(dropSlot, childOrder++);

                if (slotIndex >= items.Count)
                    continue;

                var row = CreateRow(slotIndex, items[slotIndex], items.Count);
                _rowCards[slotIndex] = row;
                if (row.GetParent() != _rows)
                    _rows.AddChild(row);
                _rows.MoveChild(row, childOrder++);
            }

            _rows.ResetSize();
            _rows.QueueSort();
        }

        private Control CreateRow(int index, TItem item, int itemCount)
        {
            var itemContext = new ModSettingsListItemContext<TItem>(
                UiContext,
                CreateItemBinding(index),
                $"{_entry.Id}[{index}]",
                index,
                itemCount,
                item,
                updatedItem => Mutate(items => items[index] = updatedItem),
                index > 0 ? () => Mutate(items => MoveItem(items, index, index - 1)) : null,
                index < itemCount - 1 ? () => Mutate(items => MoveItem(items, index, index + 1)) : null,
                () => Mutate(items => DuplicateItem(items, index)),
                () => Mutate(items => items.RemoveAt(index)),
                UiContext.RequestRefresh);

            return new ModSettingsListItemCard<TItem>(
                this,
                index,
                ModSettingsUiFactory.ResolveEntryLabelDisplay(_entry.ItemLabel(item)),
                _entry.ItemDescription?.Invoke(item) is { } description
                    ? ModSettingsUiContext.Resolve(description)
                    : null,
                itemContext,
                _entry.ItemEditorFactory?.Invoke(itemContext),
                _entry.CollapsibleItems,
                _entry.StartItemsCollapsed,
                _entry.ItemHeaderAccessoryFactory?.Invoke(itemContext));
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
            CustomMinimumSize = new(0f, 8f);
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
            return new()
            {
                BgColor = highlighted
                    ? new(0.55f, 0.80f, 0.90f, 0.95f)
                    : new Color(0f, 0f, 0f, 0f),
                BorderColor = highlighted
                    ? new(0.75f, 0.90f, 0.96f, 0.98f)
                    : new Color(0f, 0f, 0f, 0f),
                BorderWidthTop = highlighted ? 1 : 0,
                BorderWidthBottom = highlighted ? 1 : 0,
                BorderWidthLeft = 0,
                BorderWidthRight = 0,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 0,
                ContentMarginTop = highlighted ? 1 : 0,
                ContentMarginRight = 0,
                ContentMarginBottom = highlighted ? 1 : 0,
            };
        }
    }

    internal sealed partial class ModSettingsListItemCard<TItem> : PanelContainer
    {
        private readonly int _index;
        private readonly bool _isCollapsible;
        private readonly ModSettingsListItemContext<TItem>? _itemContext;
        private readonly ModSettingsListControl<TItem> _owner;
        private bool _collapsed;
        private PanelContainer? _editorSurface;
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
            _index = index;
            _itemContext = itemContext;
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
            outer.AddThemeConstantOverride("separation", 8);
            AddChild(outer);

            outer.AddChild(new ModSettingsDragHandle((index + 1).ToString(), () => owner.CreateDragData(index)));

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddThemeConstantOverride("separation", 8);
            outer.AddChild(root);

            var headerRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            headerRow.AddThemeConstantOverride("separation", 8);
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
                header.AddThemeConstantOverride("separation", 8);
                headerRow.AddChild(header);

                var textColumn = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                textColumn.AddThemeConstantOverride("separation", 2);
                header.AddChild(textColumn);

                textColumn.AddChild(ModSettingsUiFactory.CreateSectionTitle(title));
                if (!string.IsNullOrWhiteSpace(subtitle))
                    textColumn.AddChild(ModSettingsUiFactory.CreateInlineDescription(subtitle));
            }

            var actions = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            actions.AddThemeConstantOverride("separation", 8);
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
        }

        private void ToggleCollapsed()
        {
            if (!_isCollapsible)
                return;
            _collapsed = !_collapsed;
            _itemContext?.SetRowState("collapsed", _collapsed);
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

            _owner.PreviewDropAtIndex(atPosition.Y < Size.Y * 0.5f ? _index : _index + 1);
            return true;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _owner.DropAtIndex(data, atPosition.Y < Size.Y * 0.5f ? _index : _index + 1);
        }
    }

    internal sealed partial class ModSettingsCollapsibleHeaderButton : ModSettingsGamepadCompatibleButton
    {
        private readonly Action? _action;
        private readonly string? _subtitle;
        private readonly string _title = string.Empty;
        private Label? _arrowLabel;
        private MarginContainer? _measureFrame;
        private bool _selected;
        private Label? _subtitleLabel;
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
            CustomMinimumSize = new(0f, string.IsNullOrWhiteSpace(subtitle) ? 56f : 84f);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddThemeStyleboxOverride("normal", CreateHeaderStyle(false, false));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(false, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(false, true));

            var frame = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
            };
            frame.SetAnchorsPreset(LayoutPreset.FullRect);
            frame.OffsetLeft = 0;
            frame.OffsetTop = 0;
            frame.OffsetRight = 0;
            frame.OffsetBottom = 0;
            frame.AddThemeConstantOverride("margin_left", 14);
            frame.AddThemeConstantOverride("margin_top", 10);
            frame.AddThemeConstantOverride("margin_right", 14);
            frame.AddThemeConstantOverride("margin_bottom", 10);
            AddChild(frame);
            _measureFrame = frame;

            var root = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            root.AddThemeConstantOverride("separation", 12);
            frame.AddChild(root);

            var arrowLabel = new Label
            {
                CustomMinimumSize = new(28f, 28f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            arrowLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            arrowLabel.AddThemeFontSizeOverride("font_size", 21);
            arrowLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextSecondary);
            root.AddChild(arrowLabel);
            _arrowLabel = arrowLabel;

            var textColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            textColumn.AddThemeConstantOverride("separation", 2);
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
            titleLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            titleLabel.AddThemeFontSizeOverride("font_size", 24);
            titleLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            textColumn.AddChild(titleLabel);
            _titleLabel = titleLabel;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var subtitleLabel = new Label
                {
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    ClipText = false,
                };
                subtitleLabel.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
                subtitleLabel.AddThemeFontSizeOverride("font_size", 17);
                subtitleLabel.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelSecondary);
                textColumn.AddChild(subtitleLabel);
                _subtitleLabel = subtitleLabel;
            }

            Pressed += () => _action?.Invoke();
        }

        public ModSettingsCollapsibleHeaderButton()
        {
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

        private void ApplySelectedState()
        {
            AddThemeStyleboxOverride("normal", CreateHeaderStyle(_selected, false));
            AddThemeStyleboxOverride("hover", CreateHeaderStyle(_selected, true));
            AddThemeStyleboxOverride("pressed", CreateHeaderStyle(true, true));
            AddThemeStyleboxOverride("focus", CreateHeaderStyle(_selected, true));
            if (_arrowLabel != null)
                _arrowLabel.Text = _selected ? "▼" : "▶";
        }

        private static StyleBoxFlat CreateHeaderStyle(bool selected, bool hovered)
        {
            return new()
            {
                BgColor = selected
                    ? new(0.14f, 0.19f, 0.24f, 0.96f)
                    : hovered
                        ? new(0.12f, 0.17f, 0.22f, 0.96f)
                        : new Color(0.10f, 0.14f, 0.19f, 0.94f),
                BorderColor = selected
                    ? new(0.55f, 0.70f, 0.80f, 0.72f)
                    : new Color(0.37f, 0.48f, 0.57f, 0.52f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
            };
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
            AddThemeConstantOverride("separation", 8);
        }

        public ModSettingsCollapsibleSection()
        {
        }

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
            cardContent.AddThemeConstantOverride("separation", 8);
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
                headerRow.AddThemeConstantOverride("separation", 10);
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

            _content = new() { MouseFilter = MouseFilterEnum.Ignore };
            _content.AddThemeConstantOverride("separation", 8);
            if (_contentControls != null)
                foreach (var control in _contentControls)
                    _content.AddChild(control);
            cardContent.AddChild(_content);

            _collapsed = _startCollapsed;
            ApplyCollapsedState();
        }

        private void ToggleCollapsed()
        {
            _collapsed = !_collapsed;
            ApplyCollapsedState();
            if (!_collapsed)
                Callable.From(EnsureExpandedSectionVisible).CallDeferred();
        }

        private void ApplyCollapsedState()
        {
            if (_content != null)
                _content.Visible = !_collapsed;
            _toggle?.SetSelected(!_collapsed);
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
            CustomMinimumSize = new(0f, kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 62f,
                ModSettingsSidebarItemKind.Page => 48f,
                ModSettingsSidebarItemKind.Section => 38f,
                _ => 44f,
            });
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            Alignment = HorizontalAlignment.Left;
            IconAlignment = HorizontalAlignment.Left;

            AddThemeFontOverride("font", kind == ModSettingsSidebarItemKind.ModGroup
                ? ModSettingsUiResources.KreonBold
                : ModSettingsUiResources.KreonRegular);
            AddThemeFontSizeOverride("font_size", kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => 22,
                ModSettingsSidebarItemKind.Page => 19,
                ModSettingsSidebarItemKind.Section => 16,
                _ => 17,
            });
            AddThemeColorOverride("font_color", kind == ModSettingsSidebarItemKind.Section
                ? ModSettingsUiPalette.SidebarSection
                : ModSettingsUiPalette.LabelPrimary);
            AddThemeColorOverride("font_hover_color", new(0.98f, 1f, 1f));
            AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));

            AddThemeStyleboxOverride("normal", CreateStyle(false, false, _kind, _indentLevel));
            AddThemeStyleboxOverride("hover", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("focus", CreateStyle(false, true, _kind, _indentLevel));
            AddThemeStyleboxOverride("disabled", CreateDisabledStyle());

            Pressed += () => action?.Invoke();
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
                    ? new(0.17f, 0.28f, 0.36f, 0.99f)
                    : hovered
                        ? new(0.14f, 0.23f, 0.30f, 0.98f)
                        : new Color(0.11f, 0.18f, 0.24f, 0.97f),
                ModSettingsSidebarItemKind.Section => selected
                    ? new(0.12f, 0.22f, 0.29f, 0.98f)
                    : hovered
                        ? new(0.10f, 0.18f, 0.24f, 0.95f)
                        : new Color(0.07f, 0.11f, 0.16f, 0.92f),
                ModSettingsSidebarItemKind.Utility => selected
                    ? new(0.16f, 0.24f, 0.31f, 0.98f)
                    : hovered
                        ? new(0.12f, 0.19f, 0.26f, 0.97f)
                        : new Color(0.09f, 0.14f, 0.20f, 0.95f),
                _ => selected
                    ? new(0.15f, 0.25f, 0.32f, 0.985f)
                    : hovered
                        ? new(0.11f, 0.19f, 0.26f, 0.975f)
                        : new Color(0.08f, 0.13f, 0.19f, 0.94f),
            };

            var border = kind switch
            {
                ModSettingsSidebarItemKind.ModGroup => selected
                    ? new(0.72f, 0.88f, 0.95f, 0.90f)
                    : new Color(0.47f, 0.63f, 0.73f, 0.62f),
                ModSettingsSidebarItemKind.Section => selected
                    ? new(0.56f, 0.80f, 0.90f, 0.84f)
                    : new Color(0.27f, 0.42f, 0.52f, 0.45f),
                _ => selected
                    ? new(0.63f, 0.82f, 0.92f, 0.86f)
                    : new Color(0.41f, 0.56f, 0.67f, 0.56f),
            };

            var leftBorder = selected
                ? kind == ModSettingsSidebarItemKind.Section ? 3 : 4
                : kind == ModSettingsSidebarItemKind.ModGroup
                    ? 2
                    : 1;

            return new()
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthLeft = leftBorder,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = new(0f, 0f, 0f, 0.18f),
                ShadowSize = kind == ModSettingsSidebarItemKind.ModGroup ? 4 : 2,
                ContentMarginLeft = (kind == ModSettingsSidebarItemKind.Section ? 14 : 18) + indentLevel * 14,
                ContentMarginTop = kind == ModSettingsSidebarItemKind.Section ? 8 : 10,
                ContentMarginRight = kind == ModSettingsSidebarItemKind.Section ? 14 : 18,
                ContentMarginBottom = kind == ModSettingsSidebarItemKind.Section ? 8 : 10,
            };
        }

        internal static StyleBoxFlat CreateDisabledStyle()
        {
            return new()
            {
                BgColor = new(0.09f, 0.10f, 0.12f, 0.7f),
                BorderColor = new(0.24f, 0.27f, 0.30f, 0.4f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ContentMarginLeft = 14,
                ContentMarginTop = 8,
                ContentMarginRight = 14,
                ContentMarginBottom = 8,
            };
        }
    }

    /// <summary>
    ///     Textured action button using the standard settings button chrome.
    /// </summary>
    public partial class ModSettingsTextButton : ModSettingsGamepadCompatibleButton
    {
        private readonly string? _text;
        private readonly ModSettingsButtonTone _tone;
        private bool _selected;

        /// <summary>
        ///     Creates a standard settings action button.
        /// </summary>
        /// <param name="text">The button label.</param>
        /// <param name="tone">The visual tone applied to the button.</param>
        /// <param name="action">Invoked when the button is pressed.</param>
        public ModSettingsTextButton(string text, ModSettingsButtonTone tone, Action? action)
        {
            _text = text;
            _tone = tone;

            Text = text;
            CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            Flat = false;
            ClipText = true;
            AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            AddThemeFontSizeOverride("font_size", 18);
            AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            AddThemeColorOverride("font_hover_color", Colors.White);
            AddThemeColorOverride("font_pressed_color", Colors.White);
            AddThemeColorOverride("font_focus_color", Colors.White);
            ApplyVisualState();
            Pressed += () => action?.Invoke();
        }

        /// <summary>
        ///     Godot serialization constructor.
        /// </summary>
        public ModSettingsTextButton()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            Text = _text ?? string.Empty;
            ApplyVisualState();
        }

        /// <summary>
        ///     Updates the selected visual state used by segmented button groups and previews.
        /// </summary>
        /// <param name="selected">Whether the button should render as selected.</param>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            AddThemeStyleboxOverride("normal", CreateStyle(_selected, false, _tone));
            AddThemeStyleboxOverride("hover", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, true, _tone));
            AddThemeStyleboxOverride("focus", CreateStyle(_selected, true, _tone));
            AddThemeStyleboxOverride("disabled", ModSettingsSidebarButton.CreateDisabledStyle());
        }

        private static StyleBoxFlat CreateStyle(bool selected, bool hovered, ModSettingsButtonTone tone)
        {
            var borderColor = tone switch
            {
                ModSettingsButtonTone.Accent => new(0.53f, 0.76f, 0.86f, 0.86f),
                ModSettingsButtonTone.Danger => new(0.87f, 0.48f, 0.46f, 0.84f),
                _ => new Color(0.45f, 0.60f, 0.70f, 0.60f),
            };

            var backgroundColor = tone switch
            {
                ModSettingsButtonTone.Accent => selected || hovered
                    ? new(0.14f, 0.27f, 0.33f, 0.985f)
                    : new Color(0.10f, 0.21f, 0.26f, 0.97f),
                ModSettingsButtonTone.Danger => selected || hovered
                    ? new(0.29f, 0.12f, 0.12f, 0.985f)
                    : new Color(0.23f, 0.095f, 0.10f, 0.97f),
                _ => selected || hovered ? new(0.13f, 0.21f, 0.28f, 0.985f) : new Color(0.10f, 0.16f, 0.22f, 0.97f),
            };

            var shadowSize = hovered ? 7 : 2;
            var shadowColor = hovered
                ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                : new Color(0f, 0f, 0f, 0.12f);

            return new()
            {
                BgColor = backgroundColor,
                BorderColor = borderColor,
                BorderWidthLeft = hovered ? 2 : 1,
                BorderWidthTop = hovered ? 2 : 1,
                BorderWidthRight = hovered ? 2 : 1,
                BorderWidthBottom = hovered ? 2 : 1,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = shadowColor,
                ShadowSize = shadowSize,
                ContentMarginLeft = 14,
                ContentMarginTop = 8,
                ContentMarginRight = 14,
                ContentMarginBottom = 8,
            };
        }
    }
}
