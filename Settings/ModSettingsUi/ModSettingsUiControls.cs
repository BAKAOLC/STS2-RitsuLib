using System.Globalization;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Helpers;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    internal sealed partial class ModSettingsToggleControl : Button
    {
        private readonly bool _initialValue;
        private readonly Action<bool>? _onChanged;
        private bool _isOn;

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

        public ModSettingsToggleControl()
        {
        }

        public override void _Ready()
        {
            _isOn = _initialValue;
            ApplyVisualState();
        }

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
            ReleaseFocus();
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
                ? new Color(0.52f, 0.87f, 0.69f, 0.95f)
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
                    ? new Color(borderColor.R, borderColor.G, borderColor.B, 0.42f)
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
                FocusMode = FocusModeEnum.Click,
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
    internal sealed partial class ModSettingsFloatSliderControl : HBoxContainer
    {
        private readonly float _bindingValueAtConstruct;
        private readonly Func<float, string>? _formatter;
        private readonly Action<float>? _onChanged;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

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
                FocusMode = FocusModeEnum.Click,
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

        public ModSettingsFloatSliderControl()
        {
        }

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
        }

        private void OnSliderValueChanged(double value)
        {
            if (_suppressCallbacks)
                return;
            var f = (float)value;
            RefreshValueLabel(f);
            _onChanged?.Invoke(f);
        }

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

    internal sealed partial class ModSettingsChoiceControl<TValue> : HBoxContainer
    {
        private readonly TValue? _currentValue;
        private readonly Action<TValue>? _onChanged;
        private readonly (TValue Value, string Label)[]? _optionsWithValues;
        private int _currentIndex;
        private Label? _label;
        private bool _suppressCallbacks;

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

        public ModSettingsChoiceControl()
        {
        }

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

    internal sealed partial class ModSettingsDropdownChoiceControl<TValue> : HBoxContainer
    {
        private readonly Action<TValue>? _onChanged;
        private readonly (TValue Value, string Label)[]? _optionsWithValues;
        private OptionButton? _optionButton;
        private bool _suppressCallbacks;

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

            var dropdown = new OptionButton
            {
                CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth,
                    ModSettingsUiMetrics.EntryValueMinHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
                FitToLongestItem = false,
            };
            dropdown.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            dropdown.AddThemeFontSizeOverride("font_size", 17);
            dropdown.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            dropdown.AddThemeColorOverride("font_hover_color", new(1f, 1f, 1f));
            dropdown.AddThemeColorOverride("font_pressed_color", new(1f, 1f, 1f));
            dropdown.AddThemeColorOverride("font_focus_color", new(1f, 1f, 1f));
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(dropdown);
            AddChild(dropdown);
            _optionButton = dropdown;

            PopulateOptions(currentValue);
        }

        public ModSettingsDropdownChoiceControl()
        {
        }

        public override void _Ready()
        {
            if (_optionButton == null)
                return;

            _optionButton.ItemSelected += OnItemSelected;
            if (_optionButton.GetPopup() is not { } popup) return;
            ModSettingsUiControlTheming.ApplyPopupMenuListTheme(popup, 17);
        }

        public void SetValue(TValue value)
        {
            PopulateOptions(value);
        }

        private void PopulateOptions(TValue currentValue)
        {
            if (_optionButton == null || _optionsWithValues == null)
                return;

            _suppressCallbacks = true;
            _optionButton.Clear();
            var selectedIndex = 0;

            for (var i = 0; i < _optionsWithValues.Length; i++)
            {
                var option = _optionsWithValues[i];
                _optionButton.AddItem(option.Label, i);
                if (EqualityComparer<TValue>.Default.Equals(option.Value, currentValue))
                    selectedIndex = i;
            }

            if (_optionsWithValues.Length > 0)
                _optionButton.Select(selectedIndex);
            _suppressCallbacks = false;
        }

        private void OnItemSelected(long index)
        {
            if (_suppressCallbacks || _optionsWithValues == null)
                return;

            var optionIndex = (int)index;
            if (optionIndex < 0 || optionIndex >= _optionsWithValues.Length)
                return;

            _onChanged?.Invoke(_optionsWithValues[optionIndex].Value);
        }
    }

    internal sealed partial class ModSettingsColorControl : HBoxContainer
    {
        private readonly Action<string>? _onChanged;
        private LineEdit? _hexEdit;
        private ColorPickerButton? _pickerButton;
        private bool _suppressCallbacks;

        public ModSettingsColorControl(string initialValue, Action<string> onChanged)
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
                EditAlpha = true,
            };
            ModSettingsUiControlTheming.ApplyColorPickerSwatchButtonChrome(pickerButton);
            AddChild(pickerButton);
            _pickerButton = pickerButton;

            var hexEdit = new LineEdit
            {
                PlaceholderText = "#RRGGBBAA",
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
                picker.EditAlpha = true;
                picker.PresetsVisible = true;
                picker.SamplerVisible = true;
                picker.DeferredMode = false;
            }

            ApplyFromHex(initialValue, false);
        }

        public ModSettingsColorControl()
        {
        }

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
            }

            if (_pickerButton != null)
                _pickerButton.ColorChanged += color => ApplyColor(color, true);
        }

        public void SetValue(string value)
        {
            ApplyFromHex(value, false);
        }

        private void ApplyFromHex(string text, bool notify)
        {
            if (!TryParseColor(text, out var color))
            {
                ApplyColor(ReadCurrentColor(), false);
                return;
            }

            ApplyColor(color, notify);
        }

        private void ApplyColor(Color color, bool notify)
        {
            if (_suppressCallbacks)
                return;

            _suppressCallbacks = true;
            _pickerButton?.Color = color;
            _hexEdit?.Text = FormatColor(color);
            _suppressCallbacks = false;

            if (notify)
                _onChanged?.Invoke(FormatColor(color));
        }

        private Color ReadCurrentColor()
        {
            return _pickerButton?.Color ?? new(1f, 215f / 255f, 64f / 255f);
        }

        private static bool TryParseColor(string text, out Color color)
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

        private static string FormatColor(Color color)
        {
            return
                $"#{Mathf.RoundToInt(color.R * 255f):X2}{Mathf.RoundToInt(color.G * 255f):X2}{Mathf.RoundToInt(color.B * 255f):X2}{Mathf.RoundToInt(color.A * 255f):X2}";
        }
    }

    internal sealed partial class ModSettingsKeyBindingControl : VBoxContainer
    {
        private readonly bool _allowModifierCombos;
        private readonly bool _allowModifierOnly;
        private readonly bool _distinguishModifierSides;
        private readonly Action<string>? _onChanged;
        private Button? _captureButton;
        private bool _capturing;
        private string _currentValue = string.Empty;
        private Label? _hintLabel;

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

        public ModSettingsKeyBindingControl()
        {
        }

        public override void _Ready()
        {
            if (_captureButton != null)
                _captureButton.Pressed += BeginCapture;
        }

        public void SetValue(string value)
        {
            _currentValue = value;
            if (!_capturing)
                RefreshText();
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!_capturing || @event is not InputEventKey { Pressed: true } keyEvent || keyEvent.IsEcho())
                return;

            GetViewport().SetInputAsHandled();

            switch (keyEvent.Keycode)
            {
                case Key.Escape:
                    _capturing = false;
                    RefreshText();
                    return;
                case Key.Backspace or Key.Delete:
                    ApplyBinding(string.Empty, true);
                    _capturing = false;
                    return;
            }

            var binding = FormatKeyBinding(keyEvent, _allowModifierCombos, _allowModifierOnly,
                _distinguishModifierSides);
            if (string.IsNullOrWhiteSpace(binding))
                return;

            ApplyBinding(binding, true);
            _capturing = false;
        }

        private void BeginCapture()
        {
            _capturing = true;
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
            _captureButton?.Text = _capturing
                ? ModSettingsLocalization.Get("keybinding.capturing", "Press combination...")
                : string.IsNullOrWhiteSpace(_currentValue)
                    ? ModSettingsLocalization.Get("keybinding.unbound", "Unbound")
                    : _currentValue;
            _hintLabel?.Text = _capturing
                ? ModSettingsLocalization.Get("keybinding.hint.capturing",
                    "Press a key combination. Esc cancels, Backspace/Delete clears.")
                : _allowModifierCombos
                    ? _allowModifierOnly
                        ? ModSettingsLocalization.Get("keybinding.hint.combo",
                            "Click to record. Supports key combinations.")
                        : ModSettingsLocalization.Get("keybinding.hint.comboNonModifier",
                            "Click to record. Supports key combinations and requires a non-modifier key.")
                    : ModSettingsLocalization.Get("keybinding.hint.single", "Click to record a single key.");
        }

        private static string FormatKeyBinding(InputEventKey keyEvent, bool allowModifierCombos, bool allowModifierOnly,
            bool distinguishModifierSides)
        {
            var parts = new List<string>();
            if (allowModifierCombos && keyEvent.CtrlPressed)
                parts.Add("Ctrl");
            if (allowModifierCombos && keyEvent.AltPressed)
                parts.Add("Alt");
            if (allowModifierCombos && keyEvent.ShiftPressed)
                parts.Add("Shift");
            if (allowModifierCombos && keyEvent.MetaPressed)
                parts.Add("Meta");

            if (!allowModifierOnly && IsModifierKey(keyEvent.Keycode))
                return string.Empty;

            if (!IsModifierKey(keyEvent.Keycode) || parts.Count == 0)
                parts.Add(GetRecordedKeyName(keyEvent, distinguishModifierSides));

            if (!allowModifierCombos && IsModifierKey(keyEvent.Keycode))
                return GetRecordedKeyName(keyEvent, distinguishModifierSides);

            return string.Join('+', parts);
        }

        /// <summary>
        ///     Uses <see cref="InputEventKey.PhysicalKeycode" /> when <paramref name="distinguishModifierSides" /> is true
        ///     so Left Ctrl / Right Shift etc. are distinguished; otherwise uses the logical <see cref="InputEventKey.Keycode" />.
        /// </summary>
        private static string GetRecordedKeyName(InputEventKey keyEvent, bool distinguishModifierSides)
        {
            var code = distinguishModifierSides ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            if (code == Key.None)
                code = keyEvent.Keycode;
            return code.ToString();
        }

        private static bool IsModifierKey(Key key)
        {
            return key is Key.Shift or Key.Ctrl or Key.Alt or Key.Meta;
        }
    }

    internal sealed partial class ModSettingsActionsButton : MenuButton
    {
        private readonly IReadOnlyList<ModSettingsMenuAction> _actions;
        private readonly Action? _afterAction;
        private PopupMenu? _popup;
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
        }

        public ModSettingsActionsButton()
        {
            _actions = [];
        }

        public override void _Ready()
        {
            var popup = GetPopup();
            _popup = popup;
            Callable.From(RefreshPopupItems).CallDeferred();
            popup.PopupHide += () => _preferredPopupPosition = null;

            popup.AboutToPopup += () =>
            {
                RefreshPopupItems();
                ClampPopupToViewport();
            };

            popup.IdPressed += id =>
            {
                if (id < 0 || id >= _actions.Count)
                    return;

                var action = _actions[(int)id];
                if (!action.IsEnabled())
                    return;

                action.Action();
                _afterAction?.Invoke();
            };
        }

        public void OpenAt(Vector2 globalPosition)
        {
            _preferredPopupPosition = new Vector2I(
                Mathf.RoundToInt(globalPosition.X),
                Mathf.RoundToInt(globalPosition.Y));
            _popup ??= GetPopup();
            RefreshPopupItems();
            _popup.Popup();
            ClampPopupToViewport();
        }

        private void RefreshPopupItems()
        {
            _popup ??= GetPopup();
            var popup = _popup;
            if (popup == null)
                return;

            popup.Clear();
            ModSettingsUiControlTheming.ApplyPopupMenuListTheme(popup, 18);
            for (var i = 0; i < _actions.Count; i++)
            {
                popup.AddItem(_actions[i].Label, i);
                popup.SetItemDisabled(i, !_actions[i].IsEnabled());
            }
        }

        private void ClampPopupToViewport()
        {
            _popup ??= GetPopup();
            var popup = _popup;
            if (popup == null)
                return;

            var viewportRect = GetViewportRect();
            var size = popup.Size;
            var position = _preferredPopupPosition ?? popup.Position;

            var minX = Mathf.RoundToInt(viewportRect.Position.X);
            var maxX = Mathf.RoundToInt(Math.Max(viewportRect.Position.X, viewportRect.End.X - size.X));
            var minY = Mathf.RoundToInt(viewportRect.Position.Y);
            var maxY = Mathf.RoundToInt(Math.Max(viewportRect.Position.Y, viewportRect.End.Y - size.Y));

            popup.Position = new(
                Mathf.Clamp(position.X, minX, maxX),
                Mathf.Clamp(position.Y, minY, maxY));
        }
    }

    internal sealed partial class ModSettingsMiniButton : Button
    {
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
            AddThemeStyleboxOverride("normal", CreateStyle(false, false));
            AddThemeStyleboxOverride("hover", CreateStyle(true, false));
            AddThemeStyleboxOverride("pressed", CreateStyle(true, false));
            AddThemeStyleboxOverride("focus", CreateStyle(true, false));
            AddThemeStyleboxOverride("disabled", CreateStyle(false, true));
            Pressed += () =>
            {
                action();
                ReleaseFocus();
            };
        }

        public ModSettingsMiniButton()
        {
        }

        private static StyleBoxFlat CreateStyle(bool highlighted, bool disabled)
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
    }

    internal sealed partial class ModSettingsDragHandle : Button
    {
        private readonly Func<Dictionary>? _dragDataProvider;

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

        public override Variant _GetDragData(Vector2 atPosition)
        {
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
            _dropSlots.Clear();
            _rows.FreeChildren();

            var items = _entry.Binding.Read();
            _countLabel?.Text = string.Format(
                ModSettingsLocalization.Get("list.count", "{0} items"),
                items.Count);

            _emptyState?.Visible = items.Count == 0;

            _rows.AddChild(RegisterDropSlot(new(this, 0), 0));
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                _rows.AddChild(CreateRow(index, item, items.Count));
                _rows.AddChild(RegisterDropSlot(new(this, index + 1), index + 1));
            }
        }

        private Control CreateRow(int index, TItem item, int itemCount)
        {
            var itemContext = new ModSettingsListItemContext<TItem>(
                UiContext,
                CreateItemBinding(index),
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
                _entry.ItemEditorFactory?.Invoke(itemContext));
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

        private ModSettingsListDropSlot<TItem> RegisterDropSlot(ModSettingsListDropSlot<TItem> slot, int index)
        {
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

        public ModSettingsListDropSlot(ModSettingsListControl<TItem> owner, int targetIndex)
        {
            _owner = owner;
            TargetIndex = targetIndex;

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
        private readonly ModSettingsListControl<TItem> _owner;

        public ModSettingsListItemCard(
            ModSettingsListControl<TItem> owner,
            int index,
            string title,
            string? subtitle,
            ModSettingsListItemContext<TItem> itemContext,
            Control? editorContent)
        {
            _owner = owner;
            _index = index;
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

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            header.AddThemeConstantOverride("separation", 8);
            root.AddChild(header);

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

            var actions = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            actions.AddThemeConstantOverride("separation", 8);
            header.AddChild(actions);
            var actionsButton = new ModSettingsActionsButton(
                ModSettingsUiFactory.BuildListItemMenuActions(owner.UiContext, itemContext),
                itemContext.RequestRefresh);
            actionsButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            actions.AddChild(actionsButton);
            ModSettingsUiFactory.AttachContextMenuTargets(this, outer, actionsButton);

            if (editorContent == null) return;
            var editorSurface = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            editorSurface.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListEditorSurfaceStyle());
            root.AddChild(editorSurface);
            editorSurface.AddChild(editorContent);
        }

        public ModSettingsListItemCard()
        {
            _owner = null!;
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

    internal sealed partial class ModSettingsCollapsibleHeaderButton : Button
    {
        private readonly Action? _action;
        private readonly string? _subtitle;
        private readonly string _title = string.Empty;
        private Label? _arrowLabel;
        private bool _selected;
        private Label? _subtitleLabel;
        private Label? _titleLabel;

        public ModSettingsCollapsibleHeaderButton(string title, string? subtitle, Action action)
        {
            _title = title;
            _subtitle = subtitle;
            _action = action;

            FocusMode = FocusModeEnum.Click;
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

            Pressed += () =>
            {
                _action?.Invoke();
                ReleaseFocus();
            };
        }

        public ModSettingsCollapsibleHeaderButton()
        {
        }

        public override void _Ready()
        {
            _titleLabel?.Text = _title;
            _subtitleLabel?.Text = _subtitle ?? string.Empty;
            ApplySelectedState();
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
            _arrowLabel?.Text = _selected ? "▼" : "▶";
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
        }

        private void ApplyCollapsedState()
        {
            _content?.Visible = !_collapsed;
            _toggle?.SetSelected(!_collapsed);
        }
    }

    internal enum ModSettingsSidebarItemKind
    {
        ModGroup,
        Page,
        Section,
        Utility,
    }

    internal sealed partial class ModSettingsSidebarButton : Button
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

            Pressed += () =>
            {
                action?.Invoke();
                ReleaseFocus();
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

    internal partial class ModSettingsTextButton : Button
    {
        private readonly string? _text;
        private readonly ModSettingsButtonTone _tone;
        private bool _selected;

        public ModSettingsTextButton(string text, ModSettingsButtonTone tone, Action? action)
        {
            _text = text;
            _tone = tone;

            Text = text;
            CustomMinimumSize = new(ModSettingsUiFactory.EntryControlWidth, ModSettingsUiMetrics.EntryValueMinHeight);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.Click;
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
            Pressed += () =>
            {
                action?.Invoke();
                ReleaseFocus();
            };
        }

        public ModSettingsTextButton()
        {
        }

        public override void _Ready()
        {
            Text = _text ?? string.Empty;
            ApplyVisualState();
        }

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
