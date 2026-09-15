using Godot;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Ui.Layout;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">A native settings editor for adding, replacing, and removing multiple keyboard bindings.</para>
    ///     <para xml:lang="zh-CN">用于添加、替换和移除多个键盘绑定的原生设置编辑器。</para>
    /// </summary>
    public sealed partial class ModSettingsMultiKeyBindingControl : VBoxContainer, IModSettingsDirectionalInputClaimant
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
        ///     <para xml:lang="en">Creates a multi-binding editor with the requested modifier-capture rules.</para>
        ///     <para xml:lang="zh-CN">创建采用指定修饰键捕获规则的多绑定编辑器。</para>
        /// </summary>
        /// <param name="initialValues">
        ///     <para xml:lang="en">The initial bindings; invalid values and later duplicates are discarded after normalization.</para>
        ///     <para xml:lang="zh-CN">初始绑定；规范化后无效值和后续重复项会被丢弃。</para>
        /// </param>
        /// <param name="allowModifierCombos">
        ///     <para xml:lang="en">Whether pending modifier keys are included when a non-modifier key is captured.</para>
        ///     <para xml:lang="zh-CN">捕获非修饰键时是否包含此前按下的修饰键。</para>
        /// </param>
        /// <param name="allowModifierOnly">
        ///     <para xml:lang="en">Whether releasing a captured modifier may commit a modifier-only binding.</para>
        ///     <para xml:lang="zh-CN">释放已捕获的修饰键时是否可以提交仅含修饰键的绑定。</para>
        /// </param>
        /// <param name="distinguishModifierSides">
        ///     <para xml:lang="en">Whether captured modifiers are qualified as left- or right-side tokens.</para>
        ///     <para xml:lang="zh-CN">是否将捕获的修饰键记录为带左侧或右侧限定的标记。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked with a new normalized list after the user changes the bindings.</para>
        ///     <para xml:lang="zh-CN">用户更改绑定后，以新的规范化列表调用的回调。</para>
        /// </param>
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
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.blockSeparation", 8));

            var bindingsList = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            bindingsList.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.listSeparation", 6));
            AddChild(bindingsList);
            _bindingsList = bindingsList;

            var hint = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.Keybinding.HintFontSize);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshPresentation();
            SetProcessUnhandledKeyInput(true);
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured editor for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的编辑器。</para>
        /// </summary>
        public ModSettingsMultiKeyBindingControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _capturing;

        /// <inheritdoc />
        public override void _Ready()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes and replaces the displayed bindings without entering capture mode or invoking the
        ///         change callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">规范化并替换显示的绑定，但不进入捕获模式，也不调用变更回调。</para>
        /// </summary>
        /// <param name="values">
        ///     <para xml:lang="en">The replacement bindings; invalid values and later duplicates are discarded.</para>
        ///     <para xml:lang="zh-CN">用于替换的绑定；无效值和后续重复项会被丢弃。</para>
        /// </param>
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
                InvokeOnChanged([.. _values]);
        }

        private void InvokeOnChanged(List<string> values)
        {
            _onChanged?.Invoke(values);
        }

        private void RefreshPresentation()
        {
            var pendingBindingText = _pendingModifierBindings.Count == 0
                ? string.Empty
                : string.Join('+', ModSettingsKeyBindingControl.OrderedModifierTokens(_pendingModifierBindings));

            var hintText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? RitsuModuleLocalization.Get("ritsulib.keybindingMulti.capturing",
                        _capturingIndex >= 0
                            ? "Press a new key combination for the selected binding. Esc cancels, Backspace/Delete removes it."
                            : "Press a key combination to add. Esc cancels, Backspace/Delete clears all.")
                    : RitsuModuleLocalization.Get("keybinding.hint.capturingPending",
                        "Modifier keys recorded. Press another key to complete, or release to keep a modifier-only binding.")
                : _allowModifierCombos
                    ? _allowModifierOnly
                        ? RitsuModuleLocalization.Get("ritsulib.keybindingMulti.hint",
                            "Add one or more bindings. Use Rebind on a row to replace just that binding.")
                        : RitsuModuleLocalization.Get("keybinding.hint.comboNonModifier",
                            "Click to record. Supports key combinations and requires a non-modifier key.")
                    : RitsuModuleLocalization.Get("keybinding.hint.single", "Click to record a single key.");
            if (_hintLabel != null)
                _hintLabel.Text = hintText;

            RebuildBindingsList();
        }

        private void RebuildBindingsList()
        {
            if (_bindingsList == null)
                return;

            foreach (var child in _bindingsList.GetChildren())
            {
                _bindingsList.RemoveChild(child);
                child.QueueFree();
            }

            if (_values.Count == 0)
            {
                var empty = new Label
                {
                    Text = RitsuModuleLocalization.Get("ritsulib.keybindingMulti.empty", "No bindings recorded."),
                    MouseFilter = MouseFilterEnum.Ignore,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                empty.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
                empty.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.PillCount);
                empty.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
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
                        ? RitsuModuleLocalization.Get("keybinding.capturing", "Press combination...")
                        : rowPendingBindingText + "+..."
                    : string.IsNullOrWhiteSpace(binding)
                        ? RitsuModuleLocalization.Get("keybinding.unbound", "Unbound")
                        : binding;

                _bindingsList.AddChild(CreateBindingRow(
                    rowText,
                    () => BeginCapture(bindingIndex),
                    RitsuModuleLocalization.Get("button.remove", "Remove"),
                    () => RemoveBindingAt(bindingIndex)));
            }

            var addRowText = _capturing && _capturingNewBinding
                ? string.IsNullOrWhiteSpace(rowPendingBindingText)
                    ? RitsuModuleLocalization.Get("keybinding.capturing", "Press combination...")
                    : rowPendingBindingText + "+..."
                : RitsuModuleLocalization.Get("ritsulib.keybindingMulti.add", "Add binding");

            _bindingsList.AddChild(CreateBindingRow(
                addRowText,
                BeginAddCapture,
                RitsuModuleLocalization.Get("button.clear", "Clear all"),
                () => ApplyBindings([], true)));

            _bindingsList.UpdateMinimumSize();
            _bindingsList.QueueSort();
            UpdateMinimumSize();
            QueueSort();
            RitsuVerticalStack.RequestAncestorLayouts(this);
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
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.multi.rowSeparation", 6));

            var primaryButton = new Button
            {
                Text = primaryText,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.multi.captureButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Keybinding.CaptureMinWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            primaryButton.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            primaryButton.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.MiniButton);
            primaryButton.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(primaryButton);
            primaryButton.Pressed += primaryAction;
            row.AddChild(primaryButton);

            row.AddChild(new ModSettingsMiniButton(secondaryText, secondaryAction)
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.multi.secondaryButton.minSize",
                    new(86f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
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
                if (!RuntimeHotkeyParser.TryNormalizeBinding(value, out var normalizedBinding))
                    continue;
                if (seen.Add(normalizedBinding))
                    normalized.Add(normalizedBinding);
            }

            return normalized;
        }
    }
}
