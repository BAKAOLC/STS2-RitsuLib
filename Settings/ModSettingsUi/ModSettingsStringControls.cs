using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Single-line string entry backed by a <see cref="LineEdit" />.
    /// </summary>
    internal sealed partial class ModSettingsStringLineControl : HBoxContainer
    {
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;

        public ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged)
        {
            _onChanged = onChanged;
            _maxLength = maxLength;
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
        }

        public ModSettingsStringLineControl()
        {
        }

        /// <summary>
        ///     Inner <see cref="LineEdit" />; null when instantiated via parameterless constructor (e.g. Godot tooling).
        /// </summary>
        public LineEdit? Editor { get; private set; }

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
                return;

            _lastCommitted = t;
            _onChanged?.Invoke(t);
        }
    }

    /// <summary>
    ///     Multiline string entry backed by a <see cref="TextEdit" />.
    /// </summary>
    internal sealed partial class ModSettingsStringMultilineControl : HBoxContainer
    {
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;

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

        public ModSettingsStringMultilineControl()
        {
        }

        /// <summary>
        ///     Inner <see cref="TextEdit" />; null when instantiated via parameterless constructor (e.g. Godot tooling).
        /// </summary>
        public TextEdit? Editor { get; private set; }

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
