using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

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
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.stringEntry.layout.singleLine.minSize",
                new(RitsuShellTheme.Current.Metric.StringEntry.MinWidth,
                    RitsuShellTheme.Current.Metric.Entry.ValueMinHeight));

            var edit = new LineEdit
            {
                Text = _lastCommitted,
                PlaceholderText = placeholder ?? string.Empty,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.stringEntry.layout.singleLine.editorMinSize",
                    new(0f, RitsuShellTheme.Current.Metric.Slider.ValueFieldHeight)),
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

            var validationCorners = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.stringValidation.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Validation);

            _validationNeutralStyle ??= new()
            {
                BgColor = RitsuShellTheme.Current.Component.StringValidation.Neutral.Bg,
                BorderColor = RitsuShellTheme.Current.Component.StringValidation.Neutral.Border,
                BorderWidthBottom = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.neutral.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Thin).Bottom,
                BorderWidthTop = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.neutral.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Thin).Top,
                BorderWidthLeft = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.neutral.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Thin).Left,
                BorderWidthRight = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.neutral.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Thin).Right,
                CornerRadiusTopLeft = validationCorners.TopLeft,
                CornerRadiusTopRight = validationCorners.TopRight,
                CornerRadiusBottomLeft = validationCorners.BottomLeft,
                CornerRadiusBottomRight = validationCorners.BottomRight,
            };
            _validationInvalidStyle ??= new()
            {
                BgColor = RitsuShellTheme.Current.Component.StringValidation.Invalid.Bg,
                BorderColor = RitsuShellTheme.Current.Component.StringValidation.Invalid.Border,
                BorderWidthBottom = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.invalid.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Normal).Bottom,
                BorderWidthTop = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.invalid.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Normal).Top,
                BorderWidthLeft = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.invalid.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Normal).Left,
                BorderWidthRight = RitsuShellThemeLayoutResolver.ResolveEdges(
                    "components.stringValidation.layout.invalid.borderWidth",
                    RitsuShellTheme.Current.Metric.BorderWidth.Normal).Right,
                CornerRadiusTopLeft = validationCorners.TopLeft,
                CornerRadiusTopRight = validationCorners.TopRight,
                CornerRadiusBottomLeft = validationCorners.BottomLeft,
                CornerRadiusBottomRight = validationCorners.BottomRight,
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
            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.stringEntry.layout.multiline.minSize",
                new(RitsuShellTheme.Current.Metric.StringEntry.MinWidth,
                    RitsuShellTheme.Current.Metric.StringEntry.MultilineMinHeight));

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
