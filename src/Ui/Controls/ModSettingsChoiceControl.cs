using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;
using Array = System.Array;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A stepper choice editor that cycles through labeled values with previous and next buttons.</para>
    ///     <para xml:lang="zh-CN">通过上一个和下一个按钮循环选择带标签值的步进式选项编辑器。</para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The stored option-value type.</para>
    ///     <para xml:lang="zh-CN">所存储选项值的类型。</para>
    /// </typeparam>
    public sealed partial class ModSettingsChoiceControl<TValue> : Control
    {
        private readonly Action<TValue>? _onChanged;
        private StyleBoxFlat _centerStyle = RitsuShellChromeStyles.CreateSurfaceStyle();
        private int _currentIndex;
        private TValue? _currentValue;
        private Label? _label;
        private Button? _nextButton;
        private (TValue Value, string Label)[] _optionsWithValues = [];
        private Button? _previousButton;
        private bool _suppressCallbacks;

        /// <summary>
        ///     <para xml:lang="en">Creates a stepper from labeled values and an initial selection.</para>
        ///     <para xml:lang="zh-CN">根据带标签值和初始选中值创建步进式选项编辑器。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The labeled values available to the editor.</para>
        ///     <para xml:lang="zh-CN">编辑器可用的带标签值。</para>
        /// </param>
        /// <param name="currentValue">
        ///     <para xml:lang="en">The initial value; the first option is used when no value matches.</para>
        ///     <para xml:lang="zh-CN">初始值；没有匹配项时使用第一个选项。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked after the user steps to another value.</para>
        ///     <para xml:lang="zh-CN">用户切换到其他值后调用的回调。</para>
        /// </param>
        public ModSettingsChoiceControl(
            IReadOnlyList<(TValue Value, string Label)> options,
            TValue currentValue,
            Action<TValue> onChanged)
        {
            _optionsWithValues = [.. options];
            _currentValue = currentValue;
            _onChanged = onChanged;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.choice.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Choice.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;

            _previousButton = new ModSettingsMiniButton("<", () => Shift(-1))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.choice.layout.stepButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize,
                        RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            AddChild(_previousButton);

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
            AddChild(label);
            _label = label;

            _nextButton = new ModSettingsMiniButton(">", () => Shift(1))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.choice.layout.stepButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize,
                        RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            AddChild(_nextButton);
            SyncAvailability();
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured stepper for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的步进控件。</para>
        /// </summary>
        public ModSettingsChoiceControl()
        {
        }

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_optionsWithValues.Length == 0)
            {
                RefreshCurrentLabel();
                LayoutChildren();
                return;
            }

            var startingIndex = Array.FindIndex(_optionsWithValues,
                option => EqualityComparer<TValue>.Default.Equals(option.Value, _currentValue));
            if (startingIndex < 0)
                startingIndex = 0;
            _currentIndex = startingIndex;
            RefreshCurrentLabel();
            LayoutChildren();
        }

        /// <inheritdoc />
        public override void _Notification(int what)
        {
            base._Notification(what);
            if (what == (int)NotificationResized)
            {
                LayoutChildren();
                return;
            }

            if (what != (int)NotificationThemeChanged)
                return;
            _centerStyle = RitsuShellChromeStyles.CreateSurfaceStyle();
            LayoutChildren();
            QueueRedraw();
        }

        /// <inheritdoc />
        public override Vector2 _GetMinimumSize()
        {
            var buttonMin = ResolveStepButtonMinSize();
            var centerMin = ResolveCenterMinSize();
            var separation = ResolveRowSeparation();
            var rowMin = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.choice.layout.rowMinSize",
                new(RitsuShellTheme.Current.Metric.Choice.RowMinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));
            return new(
                Math.Max(rowMin.X, buttonMin.X * 2f + separation * 2f + centerMin.X),
                Math.Max(rowMin.Y, Math.Max(buttonMin.Y, centerMin.Y)));
        }

        /// <inheritdoc />
        public override void _Draw()
        {
            DrawStyleBox(_centerStyle, GetCenterRect());
        }

        private void Shift(int delta)
        {
            if (_optionsWithValues.Length == 0)
                return;

            _currentIndex = (_currentIndex + delta + _optionsWithValues.Length) % _optionsWithValues.Length;
            RefreshCurrentLabel();
            if (!_suppressCallbacks)
                InvokeOnChanged(_optionsWithValues[_currentIndex].Value);
        }

        private void InvokeOnChanged(TValue value)
        {
            _onChanged?.Invoke(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Selects a matching option without invoking the change callback; unmatched values leave the
        ///         selection unchanged.
        ///     </para>
        ///     <para xml:lang="zh-CN">选择匹配项而不调用变更回调；值不匹配时保持当前选项不变。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to select.</para>
        ///     <para xml:lang="zh-CN">要选择的值。</para>
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
            try
            {
                _currentIndex = index;
                RefreshCurrentLabel();
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces every option and selects the matching value, or the first option, without invoking the
        ///         callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">替换全部选项，并在不调用回调的情况下选择匹配值；没有匹配项时选择第一个选项。</para>
        /// </summary>
        /// <param name="options">
        ///     <para xml:lang="en">The replacement labeled values; an empty list disables the stepper.</para>
        ///     <para xml:lang="zh-CN">用于替换的带标签值；空列表会禁用步进控件。</para>
        /// </param>
        /// <param name="selectedValue">
        ///     <para xml:lang="en">The value to select after replacement.</para>
        ///     <para xml:lang="zh-CN">替换后要选择的值。</para>
        /// </param>
        public void SetOptions(IReadOnlyList<(TValue Value, string Label)> options, TValue selectedValue)
        {
            _optionsWithValues = [.. options];
            _currentValue = selectedValue;
            _currentIndex = 0;

            if (_optionsWithValues.Length > 0)
            {
                var matched = Array.FindIndex(_optionsWithValues,
                    option => EqualityComparer<TValue>.Default.Equals(option.Value, selectedValue));
                _currentIndex = matched >= 0 ? matched : 0;
            }

            SyncAvailability();
            RefreshCurrentLabel();
        }

        private void RefreshCurrentLabel()
        {
            if (_label == null)
                return;
            if (_optionsWithValues.Length == 0)
            {
                _label.Text = RitsuModuleLocalization.Get("choice.noAvailableOptions", "No available options");
                return;
            }

            _label.Text = _optionsWithValues[_currentIndex].Label;
        }

        private void SyncAvailability()
        {
            var disabled = _optionsWithValues.Length == 0;
            if (_previousButton != null)
                _previousButton.Disabled = disabled;
            if (_nextButton != null)
                _nextButton.Disabled = disabled;
        }

        private void LayoutChildren()
        {
            if (_previousButton == null || _nextButton == null || _label == null)
                return;

            var buttonMin = ResolveStepButtonMinSize();
            var y = Math.Max(0f, (Size.Y - buttonMin.Y) * 0.5f);
            _previousButton.Position = new(0f, y);
            _previousButton.Size = buttonMin;
            _nextButton.Position = new(Math.Max(0f, Size.X - buttonMin.X), y);
            _nextButton.Size = buttonMin;

            var centerRect = GetCenterRect();
            var margins = GetCenterMargins();
            _label.Position = new(centerRect.Position.X + margins.Left, centerRect.Position.Y + margins.Top);
            _label.Size = new(
                Math.Max(0f, centerRect.Size.X - margins.Left - margins.Right),
                Math.Max(0f, centerRect.Size.Y - margins.Top - margins.Bottom));
        }

        private Rect2 GetCenterRect()
        {
            var buttonMin = ResolveStepButtonMinSize();
            var centerMin = ResolveCenterMinSize();
            var separation = ResolveRowSeparation();
            var x = buttonMin.X + separation;
            var width = Math.Max(centerMin.X, Size.X - buttonMin.X * 2f - separation * 2f);
            var height = Math.Max(centerMin.Y, Size.Y);
            var y = Math.Max(0f, (Size.Y - height) * 0.5f);
            return new(x, y, width, height);
        }

        private BoxEdges GetCenterMargins()
        {
            return new(
                Mathf.RoundToInt(_centerStyle.ContentMarginLeft),
                Mathf.RoundToInt(_centerStyle.ContentMarginTop),
                Mathf.RoundToInt(_centerStyle.ContentMarginRight),
                Mathf.RoundToInt(_centerStyle.ContentMarginBottom));
        }

        private static Vector2 ResolveStepButtonMinSize()
        {
            return RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.choice.layout.stepButton.minSize",
                new(RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize,
                    RitsuShellTheme.Current.Metric.Entry.MiniStepperButtonSize));
        }

        private static Vector2 ResolveCenterMinSize()
        {
            return RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.choice.layout.center.minSize",
                new(RitsuShellTheme.Current.Metric.Choice.CenterMinWidth,
                    RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight));
        }

        private static int ResolveRowSeparation()
        {
            return RitsuShellThemeLayoutResolver.ResolveInt("components.choice.layout.rowSeparation", 6);
        }
    }
}
