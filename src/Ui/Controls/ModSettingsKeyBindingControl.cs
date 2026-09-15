using Godot;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         A single-binding editor that captures keyboard combinations and, when enabled, Godot input
    ///         actions.
    ///     </para>
    ///     <para xml:lang="zh-CN">捕获键盘组合，并可按配置捕获 Godot 输入动作的单绑定编辑器。</para>
    /// </summary>
    public sealed partial class ModSettingsKeyBindingControl : VBoxContainer, IModSettingsDirectionalInputClaimant
    {
        private readonly bool _allowActionBindings;
        private readonly bool _allowModifierCombos;
        private readonly bool _allowModifierOnly;
        private readonly bool _distinguishModifierSides;
        private readonly Action<string>? _onChanged;
        private readonly HashSet<string> _pendingModifierBindings = [];
        private Button? _captureButton;
        private bool _capturing;
        private string _currentValue = string.Empty;
        private Label? _hintLabel;

        /// <summary>
        ///     <para xml:lang="en">Creates a keyboard-binding editor with the requested modifier-capture rules.</para>
        ///     <para xml:lang="zh-CN">创建采用指定修饰键捕获规则的键盘绑定编辑器。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">The binding text shown initially; this overload does not normalize it.</para>
        ///     <para xml:lang="zh-CN">初始显示的绑定文本；此重载不会将其规范化。</para>
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
        ///     <para xml:lang="en">The callback invoked after the user captures or clears the binding.</para>
        ///     <para xml:lang="zh-CN">用户捕获或清除绑定后调用的回调。</para>
        /// </param>
        public ModSettingsKeyBindingControl(string initialValue, bool allowModifierCombos, bool allowModifierOnly,
            bool distinguishModifierSides, Action<string> onChanged)
            : this(initialValue, allowModifierCombos, allowModifierOnly, distinguishModifierSides, onChanged, false)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a single-binding editor with optional Godot input-action capture.</para>
        ///     <para xml:lang="zh-CN">创建可选择启用 Godot 输入动作捕获的单绑定编辑器。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">The binding text shown initially; this constructor does not normalize it.</para>
        ///     <para xml:lang="zh-CN">初始显示的绑定文本；此构造函数不会将其规范化。</para>
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
        ///     <para xml:lang="en">The callback invoked after the user captures or clears the binding.</para>
        ///     <para xml:lang="zh-CN">用户捕获或清除绑定后调用的回调。</para>
        /// </param>
        /// <param name="allowActionBindings">
        ///     <para xml:lang="en">Whether pressed Godot input actions can be captured as <c>action:&lt;name&gt;</c> tokens.</para>
        ///     <para xml:lang="zh-CN">是否可将按下的 Godot 输入动作捕获为 <c>action:&lt;名称&gt;</c> 标记。</para>
        /// </param>
        public ModSettingsKeyBindingControl(string initialValue, bool allowModifierCombos, bool allowModifierOnly,
            bool distinguishModifierSides, Action<string> onChanged, bool allowActionBindings)
        {
            _allowModifierCombos = allowModifierCombos;
            _allowModifierOnly = allowModifierOnly;
            _allowActionBindings = allowActionBindings;
            _distinguishModifierSides = distinguishModifierSides;
            _onChanged = onChanged;
            _currentValue = initialValue;

            CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.keybinding.layout.block.minSize",
                new(RitsuShellTheme.Current.Metric.Keybinding.BlockWidth, 80f));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.blockSeparation", 8));

            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.keybinding.layout.rowSeparation", 6));
            AddChild(row);

            var captureButton = new Button
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.captureButton.minSize",
                    new(RitsuShellTheme.Current.Metric.Keybinding.CaptureMinWidth,
                        RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                ClipText = true,
            };
            captureButton.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            captureButton.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.ValueLabel);
            captureButton.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            captureButton.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            captureButton.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            captureButton.AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            captureButton.AddThemeColorOverride("font_disabled_color",
                ModSettingsUiControlTheming.ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelSecondary));
            ModSettingsUiControlTheming.ApplyUniformSurfaceButtonStates(captureButton);
            row.AddChild(captureButton);
            _captureButton = captureButton;

            row.AddChild(new ModSettingsMiniButton(RitsuModuleLocalization.Get("button.clear", "Clear"),
                () => ApplyBinding(string.Empty, true))
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.keybinding.layout.clearButton.minSize",
                    new(64f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });

            var hint = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = allowActionBindings
                    ? RitsuModuleLocalization.Get("keybinding.hint.input",
                        "Click to record. Supports key combinations and controller actions.")
                    : allowModifierCombos
                        ? RitsuModuleLocalization.Get("keybinding.hint.combo",
                            "Click to record. Supports key combinations.")
                        : RitsuModuleLocalization.Get("keybinding.hint.single", "Click to record a single key."),
            };
            hint.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            hint.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.Keybinding.HintFontSize);
            hint.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            AddChild(hint);
            _hintLabel = hint;

            RefreshText();
            SetProcessUnhandledKeyInput(true);
            SetProcessUnhandledInput(allowActionBindings);
        }

        /// <summary>
        ///     <para xml:lang="en">Initializes an unconfigured editor for Godot scene deserialization.</para>
        ///     <para xml:lang="zh-CN">为 Godot 场景反序列化初始化尚未配置的编辑器。</para>
        /// </summary>
        public ModSettingsKeyBindingControl()
        {
        }

        bool IModSettingsDirectionalInputClaimant.ClaimsDirectionalInput => _capturing;

        /// <inheritdoc />
        public override void _Ready()
        {
            if (_captureButton != null)
                _captureButton.Pressed += BeginCapture;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces the displayed binding without normalizing it, entering capture mode, or invoking the
        ///         change callback.
        ///     </para>
        ///     <para xml:lang="zh-CN">替换显示的绑定，但不将其规范化、不进入捕获模式，也不调用变更回调。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The binding text to display.</para>
        ///     <para xml:lang="zh-CN">要显示的绑定文本。</para>
        /// </param>
        public void SetValue(string value)
        {
            _currentValue = value;
            if (!_capturing)
                RefreshText();
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
                        _capturing = false;
                        _pendingModifierBindings.Clear();
                        RefreshText();
                        return;
                    case Key.Backspace or Key.Delete:
                        ApplyBinding(string.Empty, true);
                        _capturing = false;
                        _pendingModifierBindings.Clear();
                        return;
                }

                if (IsModifierKey(keyEvent.Keycode))
                {
                    if (!_allowModifierCombos && !_allowModifierOnly)
                        return;

                    _pendingModifierBindings.Add(GetRecordedKeyName(keyEvent, _distinguishModifierSides));
                    RefreshText();
                    return;
                }

                var binding = BuildBindingFromPendingModifiers(keyEvent, _allowModifierCombos,
                    _distinguishModifierSides, _pendingModifierBindings);
                if (string.IsNullOrWhiteSpace(binding))
                    return;

                ApplyBinding(binding, true);
                _capturing = false;
                _pendingModifierBindings.Clear();
                return;
            }

            if (_pendingModifierBindings.Count == 0 || !IsModifierKey(keyEvent.Keycode))
                return;

            if (_allowModifierOnly)
                ApplyBinding(string.Join('+', OrderedModifierTokens(_pendingModifierBindings)), true);

            _capturing = false;
            _pendingModifierBindings.Clear();
            RefreshText();
        }

        /// <inheritdoc />
        public override void _UnhandledInput(InputEvent @event)
        {
            if (!_allowActionBindings || !_capturing ||
                @event is not InputEventAction { Pressed: true } actionEvent || actionEvent.IsEcho())
                return;

            var binding = RuntimeHotkeyParser.ActionBinding(actionEvent.Action.ToString());
            if (string.IsNullOrWhiteSpace(binding))
                return;

            GetViewport().SetInputAsHandled();
            ApplyBinding(binding, true);
            _capturing = false;
            _pendingModifierBindings.Clear();
        }

        private void BeginCapture()
        {
            _capturing = true;
            _pendingModifierBindings.Clear();
            RefreshText();
            _captureButton?.GrabFocus();
        }

        private void ApplyBinding(string value, bool notify)
        {
            _currentValue = value;
            RefreshText();
            if (notify)
                InvokeOnChanged(value);
        }

        private void InvokeOnChanged(string value)
        {
            _onChanged?.Invoke(value);
        }

        private void RefreshText()
        {
            var pendingBindingText = _pendingModifierBindings.Count == 0
                ? string.Empty
                : string.Join('+', OrderedModifierTokens(_pendingModifierBindings));

            var captureText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? RitsuModuleLocalization.Get("keybinding.capturing", "Press combination...")
                    : pendingBindingText + "+..."
                : string.IsNullOrWhiteSpace(_currentValue)
                    ? RitsuModuleLocalization.Get("keybinding.unbound", "Unbound")
                    : _currentValue;
            if (_captureButton != null)
                _captureButton.Text = captureText;

            var hintText = _capturing
                ? string.IsNullOrWhiteSpace(pendingBindingText)
                    ? RitsuModuleLocalization.Get("keybinding.hint.capturing",
                        _allowActionBindings
                            ? "Press a key combination or controller action. Esc cancels, Backspace/Delete clears."
                            : "Press a key combination. Esc cancels, Backspace/Delete clears.")
                    : RitsuModuleLocalization.Get("keybinding.hint.capturingPending",
                        "Modifier keys recorded. Press another key to complete, or release to keep a modifier-only binding.")
                : _allowActionBindings
                    ? RitsuModuleLocalization.Get("keybinding.hint.input",
                        "Click to record. Supports key combinations and controller actions.")
                    : _allowModifierCombos
                        ? _allowModifierOnly
                            ? RitsuModuleLocalization.Get("keybinding.hint.combo",
                                "Click to record. Supports key combinations.")
                            : RitsuModuleLocalization.Get("keybinding.hint.comboNonModifier",
                                "Click to record. Supports key combinations and requires a non-modifier key.")
                        : RitsuModuleLocalization.Get("keybinding.hint.single", "Click to record a single key.");
            if (_hintLabel != null)
                _hintLabel.Text = hintText;
        }

        internal static string BuildBindingFromPendingModifiers(InputEventKey keyEvent, bool allowModifierCombos,
            bool distinguishModifierSides, IEnumerable<string> pendingModifiers)
        {
            var parts = allowModifierCombos
                ? OrderedModifierTokens(pendingModifiers).ToList()
                : [];
            parts.Add(GetRecordedKeyName(keyEvent, distinguishModifierSides));
            return string.Join('+', parts);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Converts a captured key event to a binding token, using <see cref="InputEventKey.Location" /> to
        ///         qualify left- and right-side modifier keys when requested.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将捕获的按键事件转换为绑定标记，并在需要时根据 <see cref="InputEventKey.Location" /> 标明修饰键的左、右侧。
        ///     </para>
        /// </summary>
        /// <param name="keyEvent">
        ///     <para xml:lang="en">The captured keyboard event.</para>
        ///     <para xml:lang="zh-CN">捕获的键盘事件。</para>
        /// </param>
        /// <param name="distinguishModifierSides">
        ///     <para xml:lang="en">Whether modifier locations should be preserved in the returned token.</para>
        ///     <para xml:lang="zh-CN">返回标记中是否应保留修饰键侧别。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A key token accepted by the runtime hotkey parser.</para>
        ///     <para xml:lang="zh-CN">运行时热键解析器可接受的按键标记。</para>
        /// </returns>
        internal static string GetRecordedKeyName(InputEventKey keyEvent, bool distinguishModifierSides)
        {
            if (distinguishModifierSides)
            {
                var modifierKind = RuntimeHotkeyParser.GetModifierKindForKeyEvent(keyEvent);
                if (modifierKind != ModifierKind.None)
                    switch (keyEvent.Location)
                    {
                        case KeyLocation.Left:
                            return $"Left{modifierKind}";
                        case KeyLocation.Right:
                            return $"Right{modifierKind}";
                    }
            }

            var code = distinguishModifierSides ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            if (code == Key.None)
                code = keyEvent.Keycode;
            return code.ToString();
        }

        internal static IEnumerable<string> OrderedModifierTokens(IEnumerable<string> tokens)
        {
            return tokens.OrderBy(GetModifierSortOrder).ThenBy(static t => t, StringComparer.OrdinalIgnoreCase);
        }

        private static int GetModifierSortOrder(string token)
        {
            var normalized = token.ToLowerInvariant();
            if (normalized.Contains("ctrl") || normalized.Contains("control"))
                return 0;
            if (normalized.Contains("alt"))
                return 1;
            if (normalized.Contains("shift"))
                return 2;
            if (normalized.Contains("meta") || normalized.Contains("cmd") || normalized.Contains("command"))
                return 3;
            return 100;
        }

        internal static bool IsModifierKey(Key key)
        {
            var name = key.ToString().ToLowerInvariant();
            return name.Contains("shift") || name.Contains("ctrl") || name.Contains("control") ||
                   name.Contains("alt") || name.Contains("meta") || name.Contains("cmd") ||
                   name.Contains("command");
        }
    }
}
