using System.Globalization;
using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A color editor with a swatch picker and an editable serialized value field.</para>
    ///     <para xml:lang="zh-CN">带色样选择器和可编辑序列化值字段的颜色编辑器。</para>
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
        ///     <para xml:lang="en">Creates a color editor with configurable alpha and HDR intensity editing.</para>
        ///     <para xml:lang="zh-CN">创建可配置 Alpha 和 HDR 强度编辑的颜色编辑器。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">
        ///         The initial hex, Godot HTML, or BaseLib-compatible component string; null or empty leaves the
        ///         value unset.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始十六进制、Godot HTML 或 BaseLib 兼容分量字符串；为 <see langword="null" /> 或空时保持未设置状态。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked after the user commits a color or clears the value.</para>
        ///     <para xml:lang="zh-CN">用户提交颜色或清除值后调用的回调。</para>
        /// </param>
        /// <param name="editAlpha">
        ///     <para xml:lang="en">Whether the picker allows editing the alpha channel.</para>
        ///     <para xml:lang="zh-CN">颜色选择器是否允许编辑 Alpha 通道。</para>
        /// </param>
        /// <param name="editIntensity">
        ///     <para xml:lang="en">Whether the picker allows HDR intensity values outside the standard color range.</para>
        ///     <para xml:lang="zh-CN">颜色选择器是否允许使用超出标准颜色范围的 HDR 强度值。</para>
        /// </param>
        public ModSettingsColorControl(
            string? initialValue,
            Action<string?> onChanged,
            bool editAlpha = true,
            bool editIntensity = false)
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
        ///     <para xml:lang="en">Initializes an unconfigured color editor for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的颜色编辑器。</para>
        /// </summary>
        public ModSettingsColorControl()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the serialized value shown by the editor, or an empty string while unset.</para>
        ///     <para xml:lang="zh-CN">获取编辑器显示的序列化值；未设置时为空字符串。</para>
        /// </summary>
        public string ValueText => _hexEdit?.Text ?? _lastCommitted;

        void IModSettingsTransientPopupOwner.ForceCloseTransientUi()
        {
            if (_pickerButton?.GetPopup() is { Visible: true } popup)
                popup.Hide();
            _pickerChangedWhileOpen = false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Serializes a standard-range color as <c>#RRGGBBAA</c>, or an HDR color as a BaseLib-compatible
        ///         invariant component list.
        ///     </para>
        ///     <para xml:lang="zh-CN">将标准范围颜色序列化为 <c>#RRGGBBAA</c>，将 HDR 颜色序列化为 BaseLib 兼容的固定区域性分量列表。</para>
        /// </summary>
        /// <param name="color">
        ///     <para xml:lang="en">The finite color to serialize.</para>
        ///     <para xml:lang="zh-CN">要序列化的有限颜色值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The settings-storage representation.</para>
        ///     <para xml:lang="zh-CN">用于设置存储的字符串表示。</para>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">Thrown when any color component is not finite.</para>
        ///     <para xml:lang="zh-CN">任一颜色分量不是有限值时抛出。</para>
        /// </exception>
        public static string FormatStoredColorString(Color color)
        {
            return FormatColorValue(color);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to parse hex, Godot HTML, or a BaseLib-compatible invariant four-component color string.</para>
        ///     <para xml:lang="zh-CN">尝试解析十六进制、Godot HTML 或 BaseLib 兼容的固定区域性四分量颜色字符串。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The serialized color text.</para>
        ///     <para xml:lang="zh-CN">序列化的颜色文本。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">Receives the parsed finite color on success.</para>
        ///     <para xml:lang="zh-CN">成功时接收解析得到的有限颜色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the text contains a supported finite color; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">文本包含受支持的有限颜色时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryDeserializeColorForSettings(string? text, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            if (TryParseHexColorString(trimmed, out color))
                return true;

            if (!Color.HtmlIsValid(trimmed))
                return TryParseBracketRgbaColor(trimmed, out color);

            color = Color.FromHtml(trimmed);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Connects text and picker events when the control becomes ready.</para>
        ///     <para xml:lang="zh-CN">控件就绪时连接文本字段和颜色选择器事件。</para>
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
            }

            if (_pickerButton == null) return;
            _pickerButton.PopupClosed += OnPickerPopupClosed;
            _pickerButton.ColorChanged += OnPickerColorChanged;
            if (_pickerButton.GetPopup() is { } popup)
                popup.AddThemeStyleboxOverride("panel", RitsuShellChromeStyles.CreateInsetSurfaceStyle());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Parses and displays a value without invoking the change callback; invalid text restores the
        ///         current presentation.
        ///     </para>
        ///     <para xml:lang="zh-CN">解析并显示一个值而不调用变更回调；文本无效时恢复当前显示。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The supported serialized color, or null or empty to display the unset state.</para>
        ///     <para xml:lang="zh-CN">受支持的序列化颜色；为 <see langword="null" /> 或空时显示未设置状态。</para>
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
            try
            {
                _hexEdit?.Set("text", string.Empty);
                _pickerButton?.Set("color", _unsetPreviewColor);
            }
            finally
            {
                _suppressCallbacks = false;
            }

            _lastCommitted = string.Empty;

            if (notify)
                InvokeOnChanged(null);
        }

        private void ApplyColor(Color color, bool notify)
        {
            if (_suppressCallbacks)
                return;

            var formatted = FormatColorValue(color);
            _suppressCallbacks = true;
            try
            {
                _pickerButton?.Set("color", color);
                _hexEdit?.Set("text", formatted);
            }
            finally
            {
                _suppressCallbacks = false;
            }

            _lastCommitted = formatted;
            _unsetPreviewColor = color;

            if (notify)
                InvokeOnChanged(formatted);
        }

        private void RestoreCurrentPresentation()
        {
            ApplyFromHex(_lastCommitted, false);
        }

        private void OnPickerColorChanged(Color color)
        {
            _pickerChangedWhileOpen = true;
            ApplyColor(color, false);
            InvokeOnChanged(FormatColorValue(color));
        }

        private void InvokeOnChanged(string? value)
        {
            _onChanged?.Invoke(value);
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
            if (!float.IsFinite(r) || !float.IsFinite(g) || !float.IsFinite(b) || !float.IsFinite(a))
                return false;
            color = new(r, g, b, a);
            return true;
        }

        private static string FormatColorValue(Color color)
        {
            if (!float.IsFinite(color.R) || !float.IsFinite(color.G) ||
                !float.IsFinite(color.B) || !float.IsFinite(color.A))
                throw new ArgumentOutOfRangeException(nameof(color), color,
                    "Color components must be finite.");

            if (color.R is < 0f or > 1f || color.G is < 0f or > 1f ||
                color.B is < 0f or > 1f || color.A is < 0f or > 1f)
                return string.Create(CultureInfo.InvariantCulture,
                    $"[{color.R:R}, {color.G:R}, {color.B:R}, {color.A:R}]");

            return
                $"#{Mathf.RoundToInt(color.R * 255f):X2}{Mathf.RoundToInt(color.G * 255f):X2}{Mathf.RoundToInt(color.B * 255f):X2}{Mathf.RoundToInt(color.A * 255f):X2}";
        }
    }
}
