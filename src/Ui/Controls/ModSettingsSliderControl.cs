using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A themed numeric slider with a text editor. Create and use it on the Godot main thread.</para>
    ///     <para xml:lang="zh-CN">带文本编辑器的主题数值滑块。请在 Godot 主线程创建和使用。</para>
    /// </summary>
    public sealed partial class ModSettingsSliderControl : Control
    {
        private readonly double _bindingValueAtConstruct;
        private readonly Func<double, string>? _formatter;
        private readonly Action<double>? _onChanged;
        private NControllerManager? _hookedControllerManager;
        private HSlider? _slider;
        private bool _suppressCallbacks;
        private LineEdit? _valueEdit;

        /// <summary>
        ///     <para xml:lang="en">Creates and configures the control on the Godot main thread.</para>
        ///     <para xml:lang="zh-CN">在 Godot 主线程创建并配置此控件。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">Finite initial value; it is clamped and snapped to the range.</para>
        ///     <para xml:lang="zh-CN">有限初始值；会被限制在范围内并按步进对齐。</para>
        /// </param>
        /// <param name="minValue">
        ///     <para xml:lang="en">Finite inclusive minimum, at most maxValue.</para>
        ///     <para xml:lang="zh-CN">有限的范围下限，不得大于 maxValue。</para>
        /// </param>
        /// <param name="maxValue">
        ///     <para xml:lang="en">Finite inclusive maximum, at least minValue.</para>
        ///     <para xml:lang="zh-CN">有限的范围上限，不得小于 minValue。</para>
        /// </param>
        /// <param name="step">
        ///     <para xml:lang="en">Finite nonnegative step; zero disables snapping.</para>
        ///     <para xml:lang="zh-CN">有限的非负步进；零表示不进行步进对齐。</para>
        /// </param>
        /// <param name="formatter">
        ///     <para xml:lang="en">
        ///         Display formatter, invoked on the main thread. Recoverable failures fall back to invariant
        ///         numeric text.
        ///     </para>
        ///     <para xml:lang="zh-CN">在主线程调用的显示格式器。可恢复的失败会回退为固定区域数值文本。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">Called on the main thread after a user value change; exceptions propagate.</para>
        ///     <para xml:lang="zh-CN">用户修改值后在主线程调用；异常会继续传播。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">A required reference argument is null.</para>
        ///     <para xml:lang="zh-CN">必需的引用参数为 null。</para>
        /// </exception>
        public ModSettingsSliderControl(
            double initialValue,
            double minValue,
            double maxValue,
            double step,
            Func<double, string> formatter,
            Action<double> onChanged)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            ArgumentNullException.ThrowIfNull(onChanged);
            if (!double.IsFinite(initialValue))
                throw new ArgumentOutOfRangeException(nameof(initialValue));
            if (!double.IsFinite(minValue))
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (!double.IsFinite(maxValue) || maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            if (!double.IsFinite(step) || step < 0)
                throw new ArgumentOutOfRangeException(nameof(step));
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
            slider.AddThemeStyleboxOverride("slider", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area", CreateSliderStyle(false));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", CreateSliderStyle(true));
            AddChild(slider);
            _slider = slider;
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes the control for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化此控件。</para>
        /// </summary>
        public ModSettingsSliderControl()
        {
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            ApplySliderMouseFilterForInputMode();
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

        private void OnControllerUiModeChanged()
        {
            ApplySliderMouseFilterForInputMode();
        }

        private void ApplySliderMouseFilterForInputMode()
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
            RefreshValueLabel(value);
            InvokeOnChanged(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets the displayed value, clamps and snaps it to the configured range, without invoking the
        ///         change callback. An unconfigured control ignores the value.
        ///     </para>
        ///     <para xml:lang="zh-CN">设置显示值，并按已配置范围进行限制和步进对齐，不调用变更回调。未配置的控件会忽略此值。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">Finite value to display.</para>
        ///     <para xml:lang="zh-CN">要显示的有限数值。</para>
        /// </param>
        public void SetValue(double value)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_slider == null)
                return;

            var min = _slider.MinValue;
            var max = _slider.MaxValue;
            var normalized = NormalizeSliderValue(value, min, max, _slider.Step);

            _suppressCallbacks = true;
            try
            {
                _slider.Value = normalized;
                var actual = _slider.Value;
                RefreshValueLabel(actual);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private static double NormalizeSliderValue(double value, double minValue, double maxValue, double step)
        {
            var v = Math.Clamp(value, minValue, maxValue);
            if (step > 0d)
                v = Mathf.Snapped(v, step);
            return v;
        }

        private void InvokeOnChanged(double value)
        {
            _onChanged?.Invoke(value);
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
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModSettingsSliderControl] Formatter failed for value '{value}'; using invariant fallback: {ex}");
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
}
