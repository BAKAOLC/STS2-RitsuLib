using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Provides shared Godot theme overrides and compact editor-control factories for settings UI.</para>
    ///     <para xml:lang="zh-CN">提供设置界面共用的 Godot 主题覆盖和紧凑编辑控件工厂。</para>
    /// </summary>
    public static class ModSettingsUiControlTheming
    {
        private const string AdaptiveButtonTextEnabledMeta = "ritsulib_adaptive_button_text";
        private const string AdaptiveButtonTextMaximumMeta = "ritsulib_adaptive_button_text_maximum";
        private const string AdaptiveButtonTextMinimumMeta = "ritsulib_adaptive_button_text_minimum";
        private const string DisabledOpacityTokenPath = "semantic.state.disabled.opacity";
        private const float DisabledOpacityFallback = 0.78f;

        internal static void EnableAdaptiveButtonText(Button button, int minimumFontSize, int maximumFontSize)
        {
            ArgumentNullException.ThrowIfNull(button);
            if (minimumFontSize < 1 || maximumFontSize < minimumFontSize)
                throw new ArgumentOutOfRangeException(nameof(minimumFontSize));
            var alreadyEnabled = button.HasMeta(AdaptiveButtonTextEnabledMeta);
            button.SetMeta(AdaptiveButtonTextEnabledMeta, true);
            button.SetMeta(AdaptiveButtonTextMinimumMeta, minimumFontSize);
            button.SetMeta(AdaptiveButtonTextMaximumMeta, maximumFontSize);
            button.ClipText = true;
            if (alreadyEnabled)
            {
                RefreshAdaptiveButtonText(button);
                return;
            }

            button.Resized += () => RefreshAdaptiveButtonText(button);
            button.Ready += () => RefreshAdaptiveButtonText(button);
            RefreshAdaptiveButtonText(button);
        }

        internal static void RefreshAdaptiveButtonText(Button button)
        {
            if (!GodotObject.IsInstanceValid(button) || !button.HasMeta(AdaptiveButtonTextEnabledMeta))
                return;
            var minimumFontSize = button.GetMeta(AdaptiveButtonTextMinimumMeta).AsInt32();
            var maximumFontSize = button.GetMeta(AdaptiveButtonTextMaximumMeta).AsInt32();
            var font = button.GetThemeFont("font");
            var available = button.Size;
            if (button.GetThemeStylebox("normal") is StyleBoxFlat style)
            {
                available.X -= style.ContentMarginLeft + style.ContentMarginRight;
                available.Y -= style.ContentMarginTop + style.ContentMarginBottom;
            }

            if (button.Icon != null)
                available.X -= button.Icon.GetWidth() + button.GetThemeConstant("h_separation");
            if (available.X <= 1f || available.Y <= 1f)
            {
                button.AddThemeFontSizeOverride("font_size", maximumFontSize);
                return;
            }

            var fontSize = maximumFontSize;
            while (fontSize > minimumFontSize &&
                   (font.GetStringSize(button.Text, HorizontalAlignment.Left, -1f, fontSize).X > available.X ||
                    font.GetHeight(fontSize) > available.Y))
                fontSize--;
            if (button.GetThemeFontSize("font_size") != fontSize)
                button.AddThemeFontSizeOverride("font_size", fontSize);
        }

        internal static Color ResolveDisabledForeground(Color foreground)
        {
            var opacity = RitsuShellTheme.Current.TryGetNumber(DisabledOpacityTokenPath, out var resolved) &&
                          resolved is > 0.05 and <= 1.0
                ? (float)resolved
                : DisabledOpacityFallback;
            return new(foreground.R, foreground.G, foreground.B, foreground.A * opacity);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies one shared surface style to the button's normal, hover, pressed, and focus states.</para>
        ///     <para xml:lang="zh-CN">将同一个共用表面样式应用到按钮的常态、悬停、按下和焦点状态。</para>
        /// </summary>
        /// <param name="control">
        ///     <para xml:lang="en">The button to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的按钮。</para>
        /// </param>
        public static void ApplyUniformSurfaceButtonStates(BaseButton control)
        {
            var box = ModSettingsUiFactory.CreateSurfaceStyle();
            control.AddThemeStyleboxOverride("normal", box);
            control.AddThemeStyleboxOverride("hover", box);
            control.AddThemeStyleboxOverride("pressed", box);
            control.AddThemeStyleboxOverride("focus", box);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies one shared swatch-frame style to every interactive state of a color-picker button.</para>
        ///     <para xml:lang="zh-CN">将同一个共用色样边框样式应用到颜色选择按钮的所有交互状态。</para>
        /// </summary>
        /// <param name="picker">
        ///     <para xml:lang="en">The color-picker button to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的颜色选择按钮。</para>
        /// </param>
        public static void ApplyColorPickerSwatchButtonChrome(ColorPickerButton picker)
        {
            var box = ModSettingsUiFactory.CreateColorPickerSwatchFrameStyle();
            picker.AddThemeStyleboxOverride("normal", box);
            picker.AddThemeStyleboxOverride("hover", box);
            picker.AddThemeStyleboxOverride("pressed", box);
            picker.AddThemeStyleboxOverride("focus", box);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the standard font, colors, and framed value-field states to a <see cref="LineEdit" />.</para>
        ///     <para xml:lang="zh-CN">将标准字体、颜色和带框数值字段状态应用到 <see cref="LineEdit" />。</para>
        /// </summary>
        /// <param name="edit">
        ///     <para xml:lang="en">The single-line editor to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的单行编辑器。</para>
        /// </param>
        /// <param name="font">
        ///     <para xml:lang="en">The font used for the value text.</para>
        ///     <para xml:lang="zh-CN">数值文本使用的字体。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The value-text font size.</para>
        ///     <para xml:lang="zh-CN">数值文本的字号。</para>
        /// </param>
        public static void ApplyEntryLineEditValueFieldTheme(LineEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichBody);
            var normal = ModSettingsUiFactory.CreateEntryFieldFrameStyle(false);
            var emphasis = ModSettingsUiFactory.CreateEntryFieldFrameStyle(true);
            edit.AddThemeStyleboxOverride("normal", normal);
            edit.AddThemeStyleboxOverride("hover", emphasis);
            edit.AddThemeStyleboxOverride("focus", emphasis);
            edit.AddThemeStyleboxOverride("read_only", normal);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the standard font, colors, and framed value-field states to a <see cref="TextEdit" />.</para>
        ///     <para xml:lang="zh-CN">将标准字体、颜色和带框数值字段状态应用到 <see cref="TextEdit" />。</para>
        /// </summary>
        /// <param name="edit">
        ///     <para xml:lang="en">The multiline editor to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的多行编辑器。</para>
        /// </param>
        /// <param name="font">
        ///     <para xml:lang="en">The font used for the value text.</para>
        ///     <para xml:lang="zh-CN">数值文本使用的字体。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The value-text font size.</para>
        ///     <para xml:lang="zh-CN">数值文本的字号。</para>
        /// </param>
        public static void ApplyEntryTextEditValueFieldTheme(TextEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.RichBody);
            var normal = ModSettingsUiFactory.CreateEntryFieldFrameStyle(false);
            var emphasis = ModSettingsUiFactory.CreateEntryFieldFrameStyle(true);
            edit.AddThemeStyleboxOverride("normal", normal);
            edit.AddThemeStyleboxOverride("hover", emphasis);
            edit.AddThemeStyleboxOverride("focus", emphasis);
            edit.AddThemeStyleboxOverride("read_only", normal);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies settings fonts, colors, spacing, panel, and hover styles to a popup menu.</para>
        ///     <para xml:lang="zh-CN">将设置界面的字体、颜色、间距、面板和悬停样式应用到弹出菜单。</para>
        /// </summary>
        /// <param name="popup">
        ///     <para xml:lang="en">The popup menu to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的弹出菜单。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The font size used by menu rows.</para>
        ///     <para xml:lang="zh-CN">菜单行使用的字号。</para>
        /// </param>
        public static void ApplyPopupMenuListTheme(PopupMenu popup, int fontSize)
        {
            popup.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            popup.AddThemeFontSizeOverride("font_size", fontSize);
            popup.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.DropdownRow);
            popup.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            popup.AddThemeColorOverride("font_disabled_color", RitsuShellTheme.Current.Text.LabelSecondary);
            popup.AddThemeConstantOverride("v_separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.popup.vSeparation", 12));
            popup.AddThemeConstantOverride("h_separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.dropdown.layout.popup.hSeparation", 10));
            popup.AddThemeStyleboxOverride("panel", ModSettingsUiFactory.CreateListShellStyle());
            popup.AddThemeStyleboxOverride("hover", ModSettingsMiniButton.CreateStyle(true));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a horizontal row containing the supplied segmented buttons with themed spacing.</para>
        ///     <para xml:lang="zh-CN">创建包含所给分段按钮并采用主题间距的水平行。</para>
        /// </summary>
        /// <param name="buttons">
        ///     <para xml:lang="en">The buttons to add in order.</para>
        ///     <para xml:lang="zh-CN">按顺序添加的按钮。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The populated horizontal container.</para>
        ///     <para xml:lang="zh-CN">填充后的水平容器。</para>
        /// </returns>
        public static HBoxContainer CreateSegmentedButtonRow(params Button[] buttons)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.segmented.layout.rowSeparation", 8));
            foreach (var button in buttons)
                row.AddChild(button);
            return row;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an expanding toggle button with the standard segmented-control minimum size.</para>
        ///     <para xml:lang="zh-CN">创建采用标准分段控件最小尺寸、可水平扩展的切换按钮。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="pressed">
        ///     <para xml:lang="en">Whether the button is initially pressed.</para>
        ///     <para xml:lang="zh-CN">按钮初始是否处于按下状态。</para>
        /// </param>
        /// <param name="group">
        ///     <para xml:lang="en">An optional button group that controls exclusivity.</para>
        ///     <para xml:lang="zh-CN">用于控制互斥关系的可选按钮组。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured toggle button.</para>
        ///     <para xml:lang="zh-CN">配置完成的切换按钮。</para>
        /// </returns>
        public static Button CreateSegmentedToggleButton(string text, bool pressed, ButtonGroup? group = null)
        {
            return new()
            {
                Text = text,
                ToggleMode = true,
                ButtonGroup = group,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.toggle.layout.segmented.minSize",
                    new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an expanding settings toggle whose style follows its on, hover, and focus state.</para>
        ///     <para xml:lang="zh-CN">创建可水平扩展的设置切换按钮，其样式会随开启、悬停和焦点状态更新。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="pressed">
        ///     <para xml:lang="en">Whether the toggle is initially on.</para>
        ///     <para xml:lang="zh-CN">切换按钮初始是否开启。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured settings toggle.</para>
        ///     <para xml:lang="zh-CN">配置完成的设置切换按钮。</para>
        /// </returns>
        public static Button CreateSettingsToggleButton(string text, bool pressed)
        {
            var button = new ModSettingsGamepadCompatibleButton
            {
                Text = text,
                ToggleMode = true,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.toggle.layout.settings.minSize",
                    new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
            };
            ApplySettingsToggleButtonStyle(button, pressed, false);
            EnableAdaptiveButtonText(
                button,
                11,
                RitsuShellTheme.Current.Metric.FontSize.Button);
            button.Toggled += on => ApplySettingsToggleButtonStyle(button, on, false);
            button.MouseEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.MouseExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            button.FocusEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.FocusExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            return button;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a start-aligned settings toggle sized for list headers and other dense layouts.</para>
        ///     <para xml:lang="zh-CN">创建适用于列表标题等紧凑布局、靠起始侧对齐的设置切换按钮。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="pressed">
        ///     <para xml:lang="en">Whether the toggle is initially on.</para>
        ///     <para xml:lang="zh-CN">切换按钮初始是否开启。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured compact settings toggle.</para>
        ///     <para xml:lang="zh-CN">配置完成的紧凑设置切换按钮。</para>
        /// </returns>
        public static Button CreateCompactSettingsToggleButton(string text, bool pressed)
        {
            var button = new ModSettingsGamepadCompatibleButton
            {
                Text = text,
                ToggleMode = true,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.toggle.layout.compact.minSize",
                    new(110f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
            };
            ApplySettingsToggleButtonStyle(button, pressed, false);
            button.Toggled += on => ApplySettingsToggleButtonStyle(button, on, false);
            button.MouseEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.MouseExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            button.FocusEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.FocusExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            return button;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an expanding <see cref="ModSettingsToggleControl" /> sized for a compact editor field.</para>
        ///     <para xml:lang="zh-CN">创建适合紧凑编辑字段、可水平扩展的 <see cref="ModSettingsToggleControl" />。</para>
        /// </summary>
        /// <param name="initialValue">
        ///     <para xml:lang="en">The initial toggle value.</para>
        ///     <para xml:lang="zh-CN">切换控件的初始值。</para>
        /// </param>
        /// <param name="onChanged">
        ///     <para xml:lang="en">The callback invoked when the control changes the value.</para>
        ///     <para xml:lang="zh-CN">控件更改值时调用的回调。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured toggle control.</para>
        ///     <para xml:lang="zh-CN">配置完成的切换控件。</para>
        /// </returns>
        public static ModSettingsToggleControl CreateCompactStateToggle(bool initialValue, Action<bool> onChanged)
        {
            return new(initialValue, onChanged)
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.toggle.layout.compactState.minSize",
                    new(0f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Stacks a description label above an editor for use in compact layouts.</para>
        ///     <para xml:lang="zh-CN">将说明标签置于编辑器上方，组成适用于紧凑布局的字段。</para>
        /// </summary>
        /// <param name="labelText">
        ///     <para xml:lang="en">The label shown above the editor.</para>
        ///     <para xml:lang="zh-CN">显示在编辑器上方的标签。</para>
        /// </param>
        /// <param name="editor">
        ///     <para xml:lang="en">The editor placed below the label.</para>
        ///     <para xml:lang="zh-CN">放置在标签下方的编辑控件。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The vertical field container.</para>
        ///     <para xml:lang="zh-CN">组成字段的垂直容器。</para>
        /// </returns>
        public static Control CreateCompactEditorField(string labelText, Control editor)
        {
            var wrapper = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            wrapper.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.editor.layout.fieldSeparation", 6));
            wrapper.AddChild(ModSettingsUiFactory.CreateInlineDescription(labelText));
            wrapper.AddChild(editor);
            return wrapper;
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a themed grid containing the supplied editor controls.</para>
        ///     <para xml:lang="zh-CN">创建包含所给编辑控件并采用主题间距的网格。</para>
        /// </summary>
        /// <param name="columns">
        ///     <para xml:lang="en">The number of grid columns.</para>
        ///     <para xml:lang="zh-CN">网格的列数。</para>
        /// </param>
        /// <param name="controls">
        ///     <para xml:lang="en">The controls to add in order.</para>
        ///     <para xml:lang="zh-CN">按顺序添加的控件。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The populated grid container.</para>
        ///     <para xml:lang="zh-CN">填充后的网格容器。</para>
        /// </returns>
        public static Control CreateCompactEditorRow(int columns, params Control[] controls)
        {
            var grid = new GridContainer
            {
                Columns = columns,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.editor.layout.gridHSeparation", 8));
            grid.AddThemeConstantOverride("v_separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.editor.layout.gridVSeparation", 8));
            foreach (var control in controls)
                grid.AddChild(control);
            return grid;
        }

        /// <summary>
        ///     <para xml:lang="en">Stacks a description label above a toggle for use in compact layouts.</para>
        ///     <para xml:lang="zh-CN">将说明标签置于切换控件上方，组成适用于紧凑布局的字段。</para>
        /// </summary>
        /// <param name="labelText">
        ///     <para xml:lang="en">The label shown above the toggle.</para>
        ///     <para xml:lang="zh-CN">显示在切换控件上方的标签。</para>
        /// </param>
        /// <param name="toggle">
        ///     <para xml:lang="en">The toggle placed below the label.</para>
        ///     <para xml:lang="zh-CN">放置在标签下方的切换控件。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The vertical field container.</para>
        ///     <para xml:lang="zh-CN">组成字段的垂直容器。</para>
        /// </returns>
        public static Control CreateCompactToggleField(string labelText, Control toggle)
        {
            return CreateCompactEditorField(labelText, toggle);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a three-column themed grid containing the supplied toggle fields.</para>
        ///     <para xml:lang="zh-CN">创建包含所给切换字段并采用主题间距的三列网格。</para>
        /// </summary>
        /// <param name="controls">
        ///     <para xml:lang="en">The fields to add in order.</para>
        ///     <para xml:lang="zh-CN">按顺序添加的字段。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The populated three-column grid.</para>
        ///     <para xml:lang="zh-CN">填充后的三列网格。</para>
        /// </returns>
        public static Control CreateCompactToggleRow(params Control[] controls)
        {
            return CreateCompactEditorRow(3, controls);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a themed single-line editor and assigns its initial text.</para>
        ///     <para xml:lang="zh-CN">创建采用主题样式的单行编辑器并设置其初始文本。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The initial text.</para>
        ///     <para xml:lang="zh-CN">初始文本。</para>
        /// </param>
        /// <param name="placeholder">
        ///     <para xml:lang="en">The placeholder shown while the field is empty.</para>
        ///     <para xml:lang="zh-CN">字段为空时显示的占位文本。</para>
        /// </param>
        /// <param name="width">
        ///     <para xml:lang="en">The fallback minimum width in pixels.</para>
        ///     <para xml:lang="zh-CN">以像素为单位的回退最小宽度。</para>
        /// </param>
        /// <param name="height">
        ///     <para xml:lang="en">The fallback minimum height in pixels.</para>
        ///     <para xml:lang="zh-CN">以像素为单位的回退最小高度。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The editor font size.</para>
        ///     <para xml:lang="zh-CN">编辑器的字号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured editor.</para>
        ///     <para xml:lang="zh-CN">配置完成的编辑器。</para>
        /// </returns>
        public static LineEdit CreateStyledLineEdit(string text, string placeholder, float width = 220f,
            float height = 44f,
            int fontSize = 17)
        {
            var edit = CreateStyledLineEdit(placeholder, width, height, fontSize);
            edit.Text = text;
            return edit;
        }

        /// <summary>
        ///     <para xml:lang="en">Applies settings-toggle fonts, colors, and state styles for the supplied visual state.</para>
        ///     <para xml:lang="zh-CN">根据所给视觉状态应用设置切换按钮的字体、颜色和状态样式。</para>
        /// </summary>
        /// <param name="button">
        ///     <para xml:lang="en">The toggle button to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的切换按钮。</para>
        /// </param>
        /// <param name="on">
        ///     <para xml:lang="en">Whether the toggle is on.</para>
        ///     <para xml:lang="zh-CN">切换按钮是否开启。</para>
        /// </param>
        /// <param name="hovered">
        ///     <para xml:lang="en">Whether to use the emphasized hover or focus appearance.</para>
        ///     <para xml:lang="zh-CN">是否使用强调的悬停或焦点外观。</para>
        /// </param>
        public static void ApplySettingsToggleButtonStyle(Button button, bool on, bool hovered)
        {
            button.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            button.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            button.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            button.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeColorOverride("font_disabled_color",
                ResolveDisabledForeground(RitsuShellTheme.Current.Text.LabelSecondary));
            button.AddThemeStyleboxOverride("normal", CreateSettingsToggleButtonStyle(on, hovered));
            button.AddThemeStyleboxOverride("hover", CreateSettingsToggleButtonStyle(on, true));
            button.AddThemeStyleboxOverride("pressed", CreateSettingsToggleButtonStyle(true, true));
            button.AddThemeStyleboxOverride("focus", CreateSettingsToggleButtonStyle(on, true));
            button.AddThemeStyleboxOverride("disabled", ModSettingsToggleControl.CreateDisabledStyle());
            RefreshAdaptiveButtonText(button);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the cached flat style for a settings toggle's on and emphasis state.</para>
        ///     <para xml:lang="zh-CN">获取设置切换按钮对应开启状态与强调状态的缓存扁平样式。</para>
        /// </summary>
        /// <param name="on">
        ///     <para xml:lang="en">Whether the toggle is on.</para>
        ///     <para xml:lang="zh-CN">切换按钮是否开启。</para>
        /// </param>
        /// <param name="hovered">
        ///     <para xml:lang="en">Whether to use the emphasized hover or focus appearance.</para>
        ///     <para xml:lang="zh-CN">是否使用强调的悬停或焦点外观。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The shared style instance for the requested state.</para>
        ///     <para xml:lang="zh-CN">所请求状态对应的共享样式实例。</para>
        /// </returns>
        public static StyleBoxFlat CreateSettingsToggleButtonStyle(bool on, bool hovered)
        {
            var key = (on, hovered) switch
            {
                (true, true) => "toggle.on.hover",
                (true, false) => "toggle.on",
                (false, true) => "toggle.off.hover",
                _ => "toggle.off",
            };
            return RitsuShellStyleCache.GetOrBuild(key, () => BuildSettingsToggleButtonStyle(on, hovered));
        }

        private static StyleBoxFlat BuildSettingsToggleButtonStyle(bool on, bool hovered)
        {
            var borderColor =
                on
                    ? RitsuShellTheme.Current.Component.Toggle.On.Border
                    : RitsuShellTheme.Current.Component.Toggle.Off.Border;
            var normalBorder = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidth", 2);
            var hoverBorder =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.borderWidthHover", 3);
            var border = hovered ? hoverBorder : normalBorder;
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii("components.toggle.layout.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var shadowSize = hovered
                ? RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSizeHover", 7)
                : RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.shadowSize", 2);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.toggle.layout.padding", 14);
            padding = new(
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.left", padding.Left),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.top", 8),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.right", padding.Right),
                RitsuShellThemeLayoutResolver.ResolveInt("components.toggle.layout.padding.bottom", 8));
            return new()
            {
                BgColor = on
                    ? RitsuShellTheme.Current.Component.Toggle.On.Bg
                    : hovered
                        ? RitsuShellTheme.Current.Component.Toggle.OffHover.Bg
                        : RitsuShellTheme.Current.Component.Toggle.Off.Bg,
                BorderColor = borderColor,
                BorderWidthLeft = border.Left,
                BorderWidthTop = border.Top,
                BorderWidthRight = border.Right,
                BorderWidthBottom = border.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                ShadowColor = hovered
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : RitsuShellTheme.Current.Component.Toggle.Shadow,
                ShadowSize = shadowSize,
                ContentMarginLeft = padding.Left,
                ContentMarginTop = padding.Top,
                ContentMarginRight = padding.Right,
                ContentMarginBottom = padding.Bottom,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an empty themed single-line editor with a placeholder and theme-resolved minimum size.</para>
        ///     <para xml:lang="zh-CN">创建带占位文本、采用主题解析最小尺寸的空单行编辑器。</para>
        /// </summary>
        /// <param name="placeholder">
        ///     <para xml:lang="en">The placeholder shown while the field is empty.</para>
        ///     <para xml:lang="zh-CN">字段为空时显示的占位文本。</para>
        /// </param>
        /// <param name="width">
        ///     <para xml:lang="en">The fallback minimum width in pixels.</para>
        ///     <para xml:lang="zh-CN">以像素为单位的回退最小宽度。</para>
        /// </param>
        /// <param name="height">
        ///     <para xml:lang="en">The fallback minimum height in pixels.</para>
        ///     <para xml:lang="zh-CN">以像素为单位的回退最小高度。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The editor font size.</para>
        ///     <para xml:lang="zh-CN">编辑器的字号。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured editor.</para>
        ///     <para xml:lang="zh-CN">配置完成的编辑器。</para>
        /// </returns>
        public static LineEdit CreateStyledLineEdit(string placeholder, float width = 220f, float height = 44f,
            int fontSize = 17)
        {
            var edit = new LineEdit
            {
                PlaceholderText = placeholder,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.entryField.layout.styledLineEdit.minSize",
                    new(width, height)),
            };
            ApplyEntryLineEditValueFieldTheme(edit, RitsuShellTheme.Current.Font.Body, fontSize);
            return edit;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies the settings scrollbar track, grabber states, width, separation, and optional minimum
        ///         grabber length to a scroll container.
        ///     </para>
        ///     <para xml:lang="zh-CN">将设置界面的滚动条轨道、滑块状态、宽度、间距和可选滑块最小长度应用到滚动容器。</para>
        /// </summary>
        /// <param name="container">
        ///     <para xml:lang="en">The scroll container to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的滚动容器。</para>
        /// </param>
        public static void ApplySettingsScrollContainerTheme(ScrollContainer container)
        {
            ApplySettingsScrollContainerThemeCore(
                container,
                "components.scrollbar.layout.size",
                8,
                "components.scrollbar.layout.scrollbarVSeparation",
                0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Calculates the right content gutter from theme defaults and, when available, the scroll
        ///         container's actual bar width and separation.
        ///     </para>
        ///     <para xml:lang="zh-CN">根据主题默认值以及可用时滚动容器的实际滚动条宽度与间距，计算内容右侧留白。</para>
        /// </summary>
        /// <param name="container">
        ///     <para xml:lang="en">The themed scroll container, or <see langword="null" /> to use theme defaults only.</para>
        ///     <para xml:lang="zh-CN">已应用主题的滚动容器；传入 <see langword="null" /> 时仅使用主题默认值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The required nonnegative right gutter in pixels.</para>
        ///     <para xml:lang="zh-CN">以像素为单位、所需的非负右侧留白。</para>
        /// </returns>
        public static int ResolveSettingsScrollContentRightGutter(ScrollContainer? container)
        {
            const string gutterToken = "components.scrollbar.layout.contentRightGutter";
            const string sizeToken = "components.scrollbar.layout.size";
            const string separationToken = "components.scrollbar.layout.scrollbarVSeparation";

            var themedSize = RitsuShellThemeLayoutResolver.ResolveInt(sizeToken, 8);
            var themedSeparation = RitsuShellThemeLayoutResolver.ResolveInt(separationToken, 0);
            var themedGutter = RitsuShellThemeLayoutResolver.ResolveInt(gutterToken,
                themedSize + themedSeparation);
            var nonBarGutter = Mathf.Max(0, themedGutter - themedSize - themedSeparation);

            if (container == null || !GodotObject.IsInstanceValid(container))
                return themedGutter;

            var actualSeparation = Mathf.Max(0, container.GetThemeConstant("scrollbar_v_separation"));
            var actualSize = (float)themedSize;
            var vScrollBar = container.GetVScrollBar();
            if (GodotObject.IsInstanceValid(vScrollBar))
                actualSize = Mathf.Max(actualSize, Mathf.Max(vScrollBar.CustomMinimumSize.X, vScrollBar.Size.X));

            return Mathf.CeilToInt(Mathf.Max(0f, actualSize + actualSeparation + nonBarGutter));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies the settings scrollbar theme using dropdown-specific width and separation tokens with
        ///         global scrollbar fallbacks.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用下拉列表专用的宽度与间距主题项应用设置滚动条主题，并以全局滚动条配置作为回退。</para>
        /// </summary>
        /// <param name="container">
        ///     <para xml:lang="en">The dropdown-list scroll container to style.</para>
        ///     <para xml:lang="zh-CN">要设置样式的下拉列表滚动容器。</para>
        /// </param>
        public static void ApplySettingsScrollContainerThemeForDropdownList(ScrollContainer container)
        {
            var globalBar = RitsuShellThemeLayoutResolver.ResolveInt("components.scrollbar.layout.size", 8);
            var globalSep =
                RitsuShellThemeLayoutResolver.ResolveInt("components.scrollbar.layout.scrollbarVSeparation", 0);
            ApplySettingsScrollContainerThemeCore(
                container,
                "components.dropdown.layout.scroll.barWidth",
                globalBar,
                "components.dropdown.layout.scroll.scrollbarVSeparation",
                globalSep);
        }

        private static void ApplySettingsScrollContainerThemeCore(ScrollContainer container,
            string scrollBarWidthToken, int scrollBarWidthIfMissing, string scrollbarVSeparationToken,
            int scrollbarVSeparationIfMissing)
        {
            if (!GodotObject.IsInstanceValid(container))
                return;

            var vScrollBar = container.GetVScrollBar();
            if (!GodotObject.IsInstanceValid(vScrollBar))
                return;

            vScrollBar.AddThemeStyleboxOverride("scroll", CreateSettingsScrollTrackStyle());
            vScrollBar.AddThemeStyleboxOverride("grabber",
                CreateSettingsScrollGrabberStyle("components.scrollbar.grabber"));
            vScrollBar.AddThemeStyleboxOverride("grabber_highlight",
                CreateSettingsScrollGrabberStyle("components.scrollbar.grabberHover"));
            vScrollBar.AddThemeStyleboxOverride("grabber_pressed",
                CreateSettingsScrollGrabberStyle("components.scrollbar.grabberPressed"));

            var scrollSize = RitsuShellThemeLayoutResolver.ResolveInt(scrollBarWidthToken, scrollBarWidthIfMissing);
            vScrollBar.CustomMinimumSize = new(scrollSize, vScrollBar.CustomMinimumSize.Y);

            var sep = RitsuShellThemeLayoutResolver.ResolveInt(scrollbarVSeparationToken,
                scrollbarVSeparationIfMissing);
            container.AddThemeConstantOverride("scrollbar_v_separation", sep);

            if (!TryResolveThemeConstantInt("components.scrollbar.layout.grabber.minLength", out var minGrabberLen) ||
                minGrabberLen <= 0) return;
            // Some Godot versions use different constant names; set both when present.
            vScrollBar.AddThemeConstantOverride("grabber_min_size", minGrabberLen);
            vScrollBar.AddThemeConstantOverride("minimum_grabber_size", minGrabberLen);
        }

        private static bool TryResolveThemeConstantInt(string path, out int value)
        {
            if (!RitsuShellTheme.Current.TryGetNumber(path, out var n))
            {
                value = 0;
                return false;
            }

            value = (int)Math.Round(n);
            return true;
        }

        private static Color ResolveSettingsScrollThemeColor(string path, string fallbackPath, Color fallback)
        {
            return RitsuShellTheme.Current.TryGetColor(path, out var color)
                ? color
                : RitsuShellTheme.Current.TryGetColor(fallbackPath, out color)
                    ? color
                    : fallback;
        }

        private static StyleBoxFlat CreateSettingsScrollTrackStyle()
        {
            return RitsuShellStyleCache.GetOrBuild("settings.scrollbar.track", BuildSettingsScrollTrackStyle);
        }

        private static StyleBoxFlat BuildSettingsScrollTrackStyle()
        {
            var bg = ResolveSettingsScrollThemeColor("components.scrollbar.track.bg",
                "semantic.color.surface.inset.bg", RitsuShellTheme.Current.Surface.Inset.Bg);
            var border = ResolveSettingsScrollThemeColor("components.scrollbar.track.border",
                "semantic.color.surface.inset.border", RitsuShellTheme.Current.Surface.Inset.Border);
            var borderWidth =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.scrollbar.layout.track.borderWidth", 1);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.scrollbar.layout.track.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            var padding = RitsuShellThemeLayoutResolver.ResolveEdges("components.scrollbar.layout.track.padding", 0);
            return new()
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthLeft = borderWidth.Left,
                BorderWidthTop = borderWidth.Top,
                BorderWidthRight = borderWidth.Right,
                BorderWidthBottom = borderWidth.Bottom,
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

        private static StyleBoxFlat CreateSettingsScrollGrabberStyle(string basePath)
        {
            return RitsuShellStyleCache.GetOrBuild("settings.scrollbar.grabber." + basePath,
                () => BuildSettingsScrollGrabberStyle(basePath));
        }

        private static StyleBoxFlat BuildSettingsScrollGrabberStyle(string basePath)
        {
            var bg = ResolveSettingsScrollThemeColor(basePath + ".bg", "components.chromeMenu.default.bg",
                RitsuShellTheme.Current.Component.ChromeMenu.Default.Bg);
            var border = ResolveSettingsScrollThemeColor(basePath + ".border", "components.chromeMenu.default.border",
                RitsuShellTheme.Current.Component.ChromeMenu.Default.Border);
            var borderWidth =
                RitsuShellThemeLayoutResolver.ResolveEdges("components.scrollbar.layout.grabber.borderWidth", 1);
            var cornerRadii = RitsuShellThemeLayoutResolver.ResolveCornerRadii(
                "components.scrollbar.layout.grabber.cornerRadius",
                RitsuShellTheme.Current.Metric.Radius.Default);
            return new()
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthLeft = borderWidth.Left,
                BorderWidthTop = borderWidth.Top,
                BorderWidthRight = borderWidth.Right,
                BorderWidthBottom = borderWidth.Bottom,
                CornerRadiusTopLeft = cornerRadii.TopLeft,
                CornerRadiusTopRight = cornerRadii.TopRight,
                CornerRadiusBottomRight = cornerRadii.BottomRight,
                CornerRadiusBottomLeft = cornerRadii.BottomLeft,
            };
        }
    }
}
