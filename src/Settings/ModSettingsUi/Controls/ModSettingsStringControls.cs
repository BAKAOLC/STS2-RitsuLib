using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Single-line string editor backed by a <see cref="LineEdit" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         由 <see cref="LineEdit" /> 实现的单行字符串编辑器。
    ///     </para>
    /// </summary>
    public sealed partial class ModSettingsStringLineControl : HBoxContainer
    {
        private readonly Func<string, bool>? _commitValidation;
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private readonly Func<string, bool>? _validationVisual;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;
        private StyleBoxFlat? _validationInvalidStyle;
        private StyleBoxFlat? _validationNeutralStyle;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a single-line string editor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建单行字符串编辑器。
        ///     </para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">
        ///         Initial text. <see langword="null" /> is treated as an empty string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始文本；<see langword="null" /> 按空字符串处理。
        ///     </para>
        /// </param>
        /// <param name="placeholder">
        ///     <para xml:lang="en">
        ///         Placeholder shown while the field is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         字段为空时显示的占位文本。
        ///     </para>
        /// </param>
        /// <param name="maxLength">
        ///     <para xml:lang="en">
        ///         Optional maximum text length. Values below one disable the limit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的最大文本长度；小于一时不限制长度。
        ///     </para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">
        ///         Callback invoked with each newly committed value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次提交新值时调用的回调。
        ///     </para>
        /// </param>
        public ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged)
            : this(initialValue, placeholder, maxLength, onChanged, null)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a single-line string editor with optional validation styling. Validation affects appearance
        ///         only and does not block commits.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建带可选验证样式的单行字符串编辑器。验证仅影响外观，不会阻止提交。
        ///     </para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">
        ///         Initial text. <see langword="null" /> is treated as an empty string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始文本；<see langword="null" /> 按空字符串处理。
        ///     </para>
        /// </param>
        /// <param name="placeholder">
        ///     <para xml:lang="en">
        ///         Placeholder shown while the field is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         字段为空时显示的占位文本。
        ///     </para>
        /// </param>
        /// <param name="maxLength">
        ///     <para xml:lang="en">
        ///         Optional maximum text length. Values below one disable the limit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的最大文本长度；小于一时不限制长度。
        ///     </para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">
        ///         Callback invoked with each newly committed value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次提交新值时调用的回调。
        ///     </para>
        /// </param>
        /// <param name="validationVisual">
        ///     <para xml:lang="en">
        ///         Optional predicate that selects normal or error styling for the current text. Returning
        ///         <see langword="false" /> or throwing selects the error style.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选谓词，用于按当前文本选择正常或错误样式。返回 <see langword="false" /> 或抛出异常时使用错误样式。
        ///     </para>
        /// </param>
        public ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged, Func<string, bool>? validationVisual)
            : this(initialValue, placeholder, maxLength, onChanged, validationVisual, null)
        {
        }

        internal ModSettingsStringLineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged, Func<string, bool>? validationVisual, Func<string, bool>? commitValidation)
        {
            ArgumentNullException.ThrowIfNull(onChanged);

            _onChanged = onChanged;
            _maxLength = maxLength;
            _commitValidation = commitValidation;
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
                Commit(text, true);
                edit.ReleaseFocusIfInsideTree();
            };
            edit.FocusExited += () => Commit(edit.Text, true);
            AddChild(edit);
            Editor = edit;
            ApplyValidationChrome(_lastCommitted);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an uninitialized control for Godot scene instantiation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建供 Godot 场景实例化使用的未初始化控件。
        ///     </para>
        /// </summary>
        public ModSettingsStringLineControl()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the inner <see cref="LineEdit" />, or <see langword="null" /> after parameterless construction.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取内部 <see cref="LineEdit" />；通过无参构造函数创建后为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public LineEdit? Editor { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the displayed and committed value without invoking the change callback or recreating the
        ///         control.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         更新显示值和已提交值，不调用变更回调，也不重新创建控件。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">
        ///         Value to display. <see langword="null" /> is treated as an empty string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要显示的值；<see langword="null" /> 按空字符串处理。
        ///     </para>
        /// </param>
        public void SetValue(string? value)
        {
            if (Editor == null)
                return;

            var v = ModSettingsStringEditorShared.ClampToMaxLength(value ?? string.Empty, _maxLength);
            if (v == _lastCommitted && Editor.Text == v)
                return;

            _suppressCallbacks = true;
            try
            {
                Editor.Text = v;
                _lastCommitted = v;
            }
            finally
            {
                _suppressCallbacks = false;
            }
            ApplyValidationChrome(v);
        }

        private void OnLineEditTextChanged(string newText)
        {
            if (_suppressCallbacks)
                return;
            Commit(newText, false);
        }

        private void Commit(string? text, bool revertInvalid)
        {
            if (_suppressCallbacks)
                return;

            var t = ModSettingsStringEditorShared.ClampToMaxLength(text ?? string.Empty, _maxLength);
            if (!CanCommit(t))
            {
                ApplyValidationChrome(t);
                if (revertInvalid && Editor != null)
                    Editor.Text = _lastCommitted;
                return;
            }

            if (t == _lastCommitted)
            {
                ApplyValidationChrome(Editor?.Text ?? t);
                return;
            }

            _onChanged?.Invoke(t);
            _lastCommitted = t;
            ApplyValidationChrome(t);
        }

        private bool CanCommit(string text)
        {
            if (_commitValidation == null)
                return true;

            try
            {
                return _commitValidation(text);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] A string commit validator failed: {ex}");
                return false;
            }
        }

        private void ApplyValidationChrome(string text)
        {
            var validator = _validationVisual ?? _commitValidation;
            if (validator == null || Editor == null)
                return;

            bool ok;
            try
            {
                ok = validator(text);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Settings] A string validation visual failed: {ex}");
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
    ///     <para xml:lang="en">
    ///         Multiline string editor backed by a <see cref="TextEdit" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         由 <see cref="TextEdit" /> 实现的多行字符串编辑器。
    ///     </para>
    /// </summary>
    public sealed partial class ModSettingsStringMultilineControl : HBoxContainer
    {
        private readonly int? _maxLength;
        private readonly Action<string>? _onChanged;
        private string _lastCommitted = string.Empty;
        private bool _suppressCallbacks;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a multiline string editor.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建多行字符串编辑器。
        ///     </para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">
        ///         Initial text. <see langword="null" /> is treated as an empty string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始文本；<see langword="null" /> 按空字符串处理。
        ///     </para>
        /// </param>
        /// <param name="placeholder">
        ///     <para xml:lang="en">
        ///         Placeholder shown while the field is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         字段为空时显示的占位文本。
        ///     </para>
        /// </param>
        /// <param name="maxLength">
        ///     <para xml:lang="en">
        ///         Optional maximum text length. Values below one disable the limit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的最大文本长度；小于一时不限制长度。
        ///     </para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">
        ///         Callback invoked with each newly committed value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次提交新值时调用的回调。
        ///     </para>
        /// </param>
        public ModSettingsStringMultilineControl(string? initialValue, string? placeholder, int? maxLength,
            Action<string> onChanged)
        {
            ArgumentNullException.ThrowIfNull(onChanged);

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
            Editor = edit;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an uninitialized control for Godot scene instantiation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建供 Godot 场景实例化使用的未初始化控件。
        ///     </para>
        /// </summary>
        public ModSettingsStringMultilineControl()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the inner <see cref="TextEdit" />, or <see langword="null" /> after parameterless construction.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取内部 <see cref="TextEdit" />；通过无参构造函数创建后为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public TextEdit? Editor { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the displayed and committed value without invoking the change callback or recreating the
        ///         control.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         更新显示值和已提交值，不调用变更回调，也不重新创建控件。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">
        ///         Value to display. <see langword="null" /> is treated as an empty string.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要显示的值；<see langword="null" /> 按空字符串处理。
        ///     </para>
        /// </param>
        public void SetValue(string? value)
        {
            if (Editor == null)
                return;

            var v = ModSettingsStringEditorShared.ClampToMaxLength(value ?? string.Empty, _maxLength);
            if (v == _lastCommitted && Editor.Text == v)
                return;

            _suppressCallbacks = true;
            try
            {
                Editor.Text = v;
                _lastCommitted = v;
            }
            finally
            {
                _suppressCallbacks = false;
            }
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
                try
                {
                    Editor.Text = t;
                }
                finally
                {
                    _suppressCallbacks = false;
                }
            }

            if (t == _lastCommitted)
                return;

            _onChanged?.Invoke(t);
            _lastCommitted = t;
        }
    }
}
