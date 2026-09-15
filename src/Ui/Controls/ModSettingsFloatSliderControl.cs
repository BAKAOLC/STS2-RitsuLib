using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Compatibility slider for the obsolete float-binding overload; Godot renders double values while
    ///         normalization and callbacks remain in <see cref="float" /> space.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于已弃用浮点绑定重载的兼容滑块；Godot 以双精度值呈现，但规范化和回调仍使用 <see cref="float" />。</para>
    /// </summary>
    public sealed partial class ModSettingsFloatSliderControl : Control
    {
        private readonly float _bindingValueAtConstruct;
        private readonly Func<float, string>? _formatter;
        private readonly Action<float>? _onChanged;
        private NControllerManager? _hookedControllerManagerFloat;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

        /// <summary>
        ///     <para xml:lang="en">Creates a float-backed slider with an editable formatted value field.</para>
        ///     <para xml:lang="zh-CN">创建带可编辑格式化数值字段的单精度浮点滑块。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">The initial value, normalized to the configured range and step.</para>
        ///     <para xml:lang="zh-CN">初始值；会按配置的范围和步长进行规范化。</para>
        /// </param>
        /// <param name="minValue">
        ///     <para xml:lang="en">The inclusive minimum value.</para>
        ///     <para xml:lang="zh-CN">允许的最小值（含该值）。</para>
        /// </param>
        /// <param name="maxValue">
        ///     <para xml:lang="en">The inclusive maximum value.</para>
        ///     <para xml:lang="zh-CN">允许的最大值（含该值）。</para>
        /// </param>
        /// <param name="step">
        ///     <para xml:lang="en">The positive increment used to snap values.</para>
        ///     <para xml:lang="zh-CN">用于吸附数值的正数步长。</para>
        /// </param>
        /// <param name="formatter">
        ///     <para xml:lang="en">Formats effective slider values for the text field.</para>
        ///     <para xml:lang="zh-CN">用于将滑块的实际值格式化到文本字段。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked when user input changes the effective value.</para>
        ///     <para xml:lang="zh-CN">用户输入改变实际值时调用的回调。</para>
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
            MouseFilter = MouseFilterEnum.Ignore;

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
                Scrollable = false,
                MinValue = minValue,
                MaxValue = maxValue,
                Step = step,
                Value = normalizedInitial,
            };
            slider.AddThemeStyleboxOverride("slider", CreateFloatSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area", CreateFloatSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", CreateFloatSliderStyle(true));
            AddChild(slider);
            _slider = slider;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured slider for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的滑块。</para>
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
            ApplyFloatSliderMouseFilterForInputMode();
            LayoutChildren();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what == (int)NotificationResized)
                LayoutChildren();
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            var valueMin = _valueEdit?.GetCombinedMinimumSize() ?? Vector2.Zero;
            var trackMin = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.slider.layout.track.minSize",
                new(RitsuShellTheme.Current.Metric.Slider.TrackMinWidth,
                    RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight));
            var separation = RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.rowSeparation", 8);
            var rowMin = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.slider.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Slider.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            return new(
                Math.Max(rowMin.X, valueMin.X + separation + trackMin.X),
                Math.Max(rowMin.Y, Math.Max(valueMin.Y, trackMin.Y)));
        }

        private void OnFloatSliderControllerUiModeChanged()
        {
            ApplyFloatSliderMouseFilterForInputMode();
        }

        private void ApplyFloatSliderMouseFilterForInputMode()
        {
            if (_slider == null)
                return;

            var blockMouse = Sts2InputCompat.IsUsingDirectionalNavigation;
            _slider.MouseFilter = blockMouse ? MouseFilterEnum.Ignore : MouseFilterEnum.Pass;
        }

        private void LayoutChildren()
        {
            if (_valueEdit == null || _slider == null)
                return;

            var separation = RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.rowSeparation", 8);
            var valueMin = _valueEdit.GetCombinedMinimumSize();
            var trackMin = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.slider.layout.track.minSize",
                new(RitsuShellTheme.Current.Metric.Slider.TrackMinWidth,
                    RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight));
            var topMargin = RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.top", 4);
            var bottomMargin =
                RitsuShellThemeLayoutResolver.ResolveInt("components.slider.layout.track.margin.bottom", 4);
            var contentHeight = Math.Max(valueMin.Y, trackMin.Y);
            var valueY = Math.Max(0f, (Size.Y - valueMin.Y) * 0.5f);
            _valueEdit.Position = new(0f, valueY);
            _valueEdit.Size = valueMin;

            var trackX = valueMin.X + separation;
            var trackW = Math.Max(0f, Size.X - trackX);
            var trackH = Math.Max(0f, contentHeight - topMargin - bottomMargin);
            var trackY = Math.Max(0f, (Size.Y - contentHeight) * 0.5f) + topMargin;
            _slider.Position = new(trackX, trackY);
            _slider.Size = new(Math.Max(trackW, trackMin.X), trackH);
        }

        private void OnSliderValueChanged(double value)
        {
            if (_suppressCallbacks)
                return;
            var f = (float)value;
            RefreshValueLabel(f);
            InvokeOnChanged(f);
        }

        /// <summary>
        ///     <para xml:lang="en">Normalizes and displays a value without invoking the change callback.</para>
        ///     <para xml:lang="zh-CN">规范化并显示一个值，而不调用变更回调。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to normalize and display.</para>
        ///     <para xml:lang="zh-CN">要规范化并显示的值。</para>
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
            try
            {
                _slider.Value = normalized;
                var actual = (float)_slider.Value;
                RefreshValueLabel(actual);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private static float NormalizeSliderValue(float value, float minValue, float maxValue, float step)
        {
            var v = Mathf.Clamp(value, minValue, maxValue);
            if (step > 0f)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private void InvokeOnChanged(float value)
        {
            _onChanged?.Invoke(value);
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
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsFloatSliderControl] Formatter failed for value '{value}'; using invariant fallback: {ex}");
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
}
