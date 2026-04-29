using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Single-line string entry backed by a <see cref="LineEdit" />.
    /// </summary>
    public sealed partial class ModSettingsStringLineControl : HBoxContainer
    {
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private readonly Func<string, bool>? _validationVisual;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;
        private StyleBoxFlat? _validationInvalidStyle;
        private StyleBoxFlat? _validationNeutralStyle;

        /// <summary>
        ///     Creates a single-line string editor.
        /// </summary>
        /// <param name="initialValue">The initial text value.</param>
        /// <param name="placeholder">Placeholder text shown when the field is empty.</param>
        /// <param name="maxLength">Optional maximum text length.</param>
        /// <param name="onChanged">Callback invoked after the committed value changes.</param>
        public ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged)
            : this(initialValue, placeholder, maxLength, onChanged, null)
        {
        }

        /// <summary>
        ///     Creates a single-line string editor with optional validation chrome (e.g. red border when the predicate
        ///     returns <see langword="false" />).
        /// </summary>
        /// <param name="initialValue">The initial text value.</param>
        /// <param name="placeholder">Placeholder text shown when the field is empty.</param>
        /// <param name="maxLength">Optional maximum text length.</param>
        /// <param name="onChanged">Callback invoked after the committed value changes.</param>
        /// <param name="validationVisual">
        ///     When non-null, invoked for the current text to choose normal vs. error styling; commits are not blocked.
        /// </param>
        public ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged, Func<string, bool>? validationVisual)
        {
            _onChanged = onChanged;
            _maxLength = maxLength;
            _validationVisual = validationVisual;
            _lastCommitted = ModSettingsStringEditorShared.ClampToMaxLength(initialValue ?? string.Empty, maxLength);

            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            CustomMinimumSize = new(ModSettingsUiMetrics.StringEntryMinWidth,
                ModSettingsUiMetrics.EntryValueMinHeight);

            var edit = new LineEdit
            {
                Text = _lastCommitted,
                PlaceholderText = placeholder ?? string.Empty,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.SliderValueFieldHeight),
                CaretBlink = true,
                SelectAllOnFocus = false,
                Alignment = HorizontalAlignment.Left,
            };
            if (maxLength is >= 1)
                edit.MaxLength = maxLength.Value;
            ModSettingsStringEditorShared.ApplyStringLineEditTheme(edit);
            edit.TextChanged += OnLineEditTextChanged;
            edit.TextSubmitted += text =>
            {
                Commit(text);
                edit.ReleaseFocus();
            };
            edit.FocusExited += () => Commit(edit.Text);
            AddChild(edit);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(edit);
            Editor = edit;
            ApplyValidationChrome(_lastCommitted);
        }

        /// <summary>
        ///     Creates the string editor for Godot scene instantiation.
        /// </summary>
        public ModSettingsStringLineControl()
        {
        }

        /// <summary>
        ///     Inner <see cref="LineEdit" />; null when instantiated via parameterless constructor (e.g. Godot tooling).
        /// </summary>
        public LineEdit? Editor { get; private set; }

        /// <summary>
        ///     Updates the displayed value without recreating the control.
        /// </summary>
        /// <param name="value">The value to display.</param>
        public void SetValue(string? value)
        {
            if (Editor == null)
                return;

            var v = ModSettingsStringEditorShared.ClampToMaxLength(value ?? string.Empty, _maxLength);
            if (v == _lastCommitted && Editor.Text == v)
                return;

            _suppressCallbacks = true;
            Editor.Text = v;
            _lastCommitted = v;
            _suppressCallbacks = false;
            ApplyValidationChrome(v);
        }

        private void OnLineEditTextChanged(string newText)
        {
            if (_suppressCallbacks)
                return;
            Commit(newText);
        }

        private void Commit(string? text)
        {
            if (_suppressCallbacks)
                return;

            var t = ModSettingsStringEditorShared.ClampToMaxLength(text ?? string.Empty, _maxLength);
            if (t == _lastCommitted)
            {
                ApplyValidationChrome(Editor?.Text ?? t);
                return;
            }

            _lastCommitted = t;
            _onChanged?.Invoke(t);
            ApplyValidationChrome(t);
        }

        private void ApplyValidationChrome(string text)
        {
            if (_validationVisual == null || Editor == null)
                return;

            bool ok;
            try
            {
                ok = _validationVisual(text);
            }
            catch
            {
                ok = false;
            }

            _validationNeutralStyle ??= new()
            {
                BgColor = new(0.15f, 0.15f, 0.15f),
                BorderColor = new(0.3f, 0.3f, 0.3f),
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3,
            };
            _validationInvalidStyle ??= new()
            {
                BgColor = new(0.2f, 0.1f, 0.1f),
                BorderColor = new(0.8f, 0.3f, 0.3f),
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3,
            };

            Editor.AddThemeStyleboxOverride("normal", ok ? _validationNeutralStyle : _validationInvalidStyle);
        }
    }

    /// <summary>
    ///     Multiline string entry backed by a <see cref="TextEdit" />.
    /// </summary>
    public sealed partial class ModSettingsStringMultilineControl : HBoxContainer
    {
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;

        /// <summary>
        ///     Creates a multiline string editor.
        /// </summary>
        /// <param name="initialValue">The initial text value.</param>
        /// <param name="placeholder">Placeholder text shown when the field is empty.</param>
        /// <param name="maxLength">Optional maximum text length.</param>
        /// <param name="onChanged">Callback invoked after the committed value changes.</param>
        public ModSettingsStringMultilineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged)
        {
            _onChanged = onChanged;
            _maxLength = maxLength;
            _lastCommitted = ModSettingsStringEditorShared.ClampToMaxLength(initialValue ?? string.Empty, maxLength);

            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            CustomMinimumSize = new(ModSettingsUiMetrics.StringEntryMinWidth,
                ModSettingsUiMetrics.StringEntryMultilineMinHeight);

            var edit = new TextEdit
            {
                Text = _lastCommitted,
                PlaceholderText = placeholder ?? string.Empty,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                WrapMode = TextEdit.LineWrappingMode.Boundary,
                ScrollFitContentHeight = false,
                CaretBlink = true,
            };
            ModSettingsStringEditorShared.ApplyStringTextEditTheme(edit);
            edit.TextChanged += () =>
            {
                if (_suppressCallbacks)
                    return;
                Commit(edit.Text);
            };
            edit.FocusExited += () => Commit(edit.Text);
            AddChild(edit);
            ModSettingsFocusChrome.AttachControllerSelectionReticle(edit);
            Editor = edit;
        }

        /// <summary>
        ///     Creates the multiline editor for Godot scene instantiation.
        /// </summary>
        public ModSettingsStringMultilineControl()
        {
        }

        /// <summary>
        ///     Inner <see cref="TextEdit" />; null when instantiated via parameterless constructor (e.g. Godot tooling).
        /// </summary>
        public TextEdit? Editor { get; private set; }

        /// <summary>
        ///     Updates the displayed value without recreating the control.
        /// </summary>
        /// <param name="value">The value to display.</param>
        public void SetValue(string? value)
        {
            if (Editor == null)
                return;

            var v = ModSettingsStringEditorShared.ClampToMaxLength(value ?? string.Empty, _maxLength);
            if (v == _lastCommitted && Editor.Text == v)
                return;

            _suppressCallbacks = true;
            Editor.Text = v;
            _lastCommitted = v;
            _suppressCallbacks = false;
        }

        private void Commit(string? text)
        {
            if (_suppressCallbacks || Editor == null)
                return;

            var raw = text ?? string.Empty;
            var t = ModSettingsStringEditorShared.ClampToMaxLength(raw, _maxLength);
            if (t != raw)
            {
                _suppressCallbacks = true;
                Editor.Text = t;
                _suppressCallbacks = false;
            }

            if (t == _lastCommitted)
                return;

            _lastCommitted = t;
            _onChanged?.Invoke(t);
        }
    }
}
