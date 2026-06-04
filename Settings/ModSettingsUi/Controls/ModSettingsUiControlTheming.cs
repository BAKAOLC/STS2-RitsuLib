using Godot;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Central place for repeated Godot theme overrides on LineEdit, TextEdit, buttons, and popup menus.
    ///     集中处理 LineEdit、TextEdit、按钮和弹出菜单上重复的 Godot 主题覆盖。
    /// </summary>
    public static class ModSettingsUiControlTheming
    {
        /// <summary>
        ///     Applies the shared surface-button chrome to all standard button states.
        ///     将共享表面按钮外观应用到所有标准按钮状态。
        /// </summary>
        /// <param name="control">
        ///     The button to style.
        ///     要设置样式的按钮。
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
        ///     Applies the shared frame chrome used by color picker swatch buttons.
        ///     应用颜色选择器色块按钮使用的共享边框外观。
        /// </summary>
        /// <param name="picker">
        ///     The color picker button to style.
        ///     要设置样式的颜色选择器按钮。
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
        ///     Applies the standard value-field theme to a single-line text entry.
        ///     将标准值字段主题应用到单行文本条目。
        /// </summary>
        /// <param name="edit">
        ///     The line edit to style.
        ///     要设置样式的 line edit。
        /// </param>
        /// <param name="font">
        ///     The font to use for the value text.
        ///     值文本使用的字体。
        /// </param>
        /// <param name="fontSize">
        ///     The font size to apply.
        ///     要应用的字体大小。
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
        ///     Applies the standard value-field theme to a multi-line text entry.
        ///     将标准值字段主题应用到多行文本条目。
        /// </summary>
        /// <param name="edit">
        ///     The text edit to style.
        ///     要设置样式的 text edit。
        /// </param>
        /// <param name="font">
        ///     The font to use for the value text.
        ///     值文本使用的字体。
        /// </param>
        /// <param name="fontSize">
        ///     The font size to apply.
        ///     要应用的字体大小。
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
        ///     Applies the standard popup-menu list styling used by settings pickers.
        ///     应用设置选择器使用的标准弹出菜单列表样式。
        /// </summary>
        /// <param name="popup">
        ///     The popup menu to style.
        ///     要设置样式的弹出菜单。
        /// </param>
        /// <param name="fontSize">
        ///     The font size to apply to menu rows.
        ///     要应用到菜单行的字体大小。
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
        ///     Creates a segmented row container for compact mode-selection buttons.
        ///     创建用于紧凑模式选择按钮的分段行容器。
        /// </summary>
        /// <param name="buttons">
        ///     The buttons to place in the row.
        ///     要放入该行的按钮。
        /// </param>
        /// <returns>
        ///     A horizontal container with standard spacing for segmented controls.
        ///     用于分段控件、带标准间距的水平容器。
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
        ///     Creates a segmented toggle button using standard settings sizing.
        ///     使用标准设置尺寸创建分段切换按钮。
        /// </summary>
        /// <param name="text">
        ///     The button label.
        ///     按钮标签。
        /// </param>
        /// <param name="pressed">
        ///     Whether the button starts pressed.
        ///     按钮初始是否按下。
        /// </param>
        /// <param name="group">
        ///     Optional exclusive toggle group.
        ///     可选 exclusive toggle group.。
        /// </param>
        /// <returns>
        ///     A configured segmented toggle button.
        ///     已配置的分段切换按钮。
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
        ///     Creates a button-style settings toggle that matches the standard on/off visual language.
        ///     创建按钮样式的设置切换控件，匹配标准 on/off 视觉语言。
        /// </summary>
        /// <param name="text">
        ///     The button label.
        ///     按钮标签。
        /// </param>
        /// <param name="pressed">
        ///     Whether the toggle starts enabled.
        ///     切换控件初始是否启用。
        /// </param>
        /// <returns>
        ///     A configured toggle button with standard interactive styling.
        ///     带标准交互样式的已配置切换按钮。
        /// </returns>
        public static Button CreateSettingsToggleButton(string text, bool pressed)
        {
            var button = new Button
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
            button.Toggled += on => ApplySettingsToggleButtonStyle(button, on, false);
            button.MouseEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.MouseExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            button.FocusEntered += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, true);
            button.FocusExited += () => ApplySettingsToggleButtonStyle(button, button.ButtonPressed, false);
            return button;
        }

        /// <summary>
        ///     Creates a compact button-style settings toggle for list headers and other dense layouts.
        ///     创建用于列表标题和其他密集布局的紧凑按钮样式设置切换控件。
        /// </summary>
        /// <param name="text">
        ///     The button label.
        ///     按钮标签。
        /// </param>
        /// <param name="pressed">
        ///     Whether the toggle starts enabled.
        ///     切换控件初始是否启用。
        /// </param>
        /// <returns>
        ///     A compact toggle button with standard interactive styling.
        ///     带标准交互样式的紧凑切换按钮。
        /// </returns>
        public static Button CreateCompactSettingsToggleButton(string text, bool pressed)
        {
            var button = new Button
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
        ///     Creates a compact On/Off toggle using the standard settings toggle control chrome.
        ///     使用标准设置切换控件外观创建紧凑 On/Off 切换。
        /// </summary>
        /// <param name="initialValue">
        ///     Whether the toggle starts enabled.
        ///     切换控件初始是否启用。
        /// </param>
        /// <param name="onChanged">
        ///     Callback invoked after the value changes.
        ///     值变化后调用的回调。
        /// </param>
        /// <returns>
        ///     A compact toggle control sized for dense editor layouts.
        ///     为密集编辑器布局定尺寸的紧凑切换控件。
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
        ///     Creates a labeled compact editor field for dense multi-column layouts.
        ///     为密集多列布局创建带标签的紧凑编辑器字段。
        /// </summary>
        /// <param name="labelText">
        ///     The descriptive label shown above the editor.
        ///     显示在编辑器上方的描述性标签。
        /// </param>
        /// <param name="editor">
        ///     The editor control to place below the label.
        ///     放在标签下方的编辑器控件。
        /// </param>
        /// <returns>
        ///     A vertically stacked label-and-editor field.
        ///     垂直堆叠的标签和编辑器字段。
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
        ///     Creates a compact multi-column row for dense settings editors.
        ///     为密集设置编辑器创建紧凑多列行。
        /// </summary>
        /// <param name="columns">
        ///     The number of columns to use.
        ///     指定 number of columns to use。
        /// </param>
        /// <param name="controls">
        ///     The fields to place in the row.
        ///     要放入该行的字段。
        /// </param>
        /// <returns>
        ///     A compact grid container for grouped editors.
        ///     用于分组编辑器的紧凑网格容器。
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
        ///     Creates a labeled compact toggle field for dense multi-column editor rows.
        ///     为密集多列编辑器行创建带标签的紧凑切换字段。
        /// </summary>
        /// <param name="labelText">
        ///     The descriptive label shown above the toggle.
        ///     显示在切换控件上方的描述性标签。
        /// </param>
        /// <param name="toggle">
        ///     The toggle control to place below the label.
        ///     放在标签下方的切换控件。
        /// </param>
        /// <returns>
        ///     A vertically stacked label-and-toggle field.
        ///     垂直堆叠的标签和切换字段。
        /// </returns>
        public static Control CreateCompactToggleField(string labelText, Control toggle)
        {
            return CreateCompactEditorField(labelText, toggle);
        }

        /// <summary>
        ///     Creates a compact multi-column row for labeled toggle fields.
        ///     为带标签的切换字段创建紧凑多列行。
        /// </summary>
        /// <param name="controls">
        ///     The fields to place in the row.
        ///     要放入该行的字段。
        /// </param>
        /// <returns>
        ///     A three-column grid sized for dense settings editors.
        ///     为密集设置编辑器定尺寸的三列网格。
        /// </returns>
        public static Control CreateCompactToggleRow(params Control[] controls)
        {
            return CreateCompactEditorRow(3, controls);
        }

        /// <summary>
        ///     Creates a styled single-line text entry with an initial value.
        ///     创建带初始值的样式化单行文本条目。
        /// </summary>
        /// <param name="text">
        ///     The initial text value.
        ///     初始文本值。
        /// </param>
        /// <param name="placeholder">
        ///     Placeholder text to display when the field is empty.
        ///     字段为空时要显示的占位文本。
        /// </param>
        /// <param name="width">
        ///     The minimum width to reserve for the field.
        ///     为字段保留的最小宽度。
        /// </param>
        /// <param name="height">
        ///     The minimum height to reserve for the field.
        ///     为字段保留的最小高度。
        /// </param>
        /// <param name="fontSize">
        ///     The font size to apply.
        ///     要应用的字体大小。
        /// </param>
        /// <returns>
        ///     The configured line edit instance.
        ///     指定 configured line edit instance。
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
        ///     Applies the shared button-style toggle chrome for the current state.
        ///     为当前状态应用共享的按钮样式切换外观。
        /// </summary>
        /// <param name="button">
        ///     The button to style.
        ///     要设置样式的按钮。
        /// </param>
        /// <param name="on">
        ///     Whether the toggle is enabled.
        ///     切换控件是否启用。
        /// </param>
        /// <param name="hovered">
        ///     Whether the button should use its emphasized hover/focus state.
        ///     按钮是否应使用强调的悬停/焦点状态。
        /// </param>
        public static void ApplySettingsToggleButtonStyle(Button button, bool on, bool hovered)
        {
            button.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            button.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Button);
            button.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            button.AddThemeColorOverride("font_hover_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeColorOverride("font_pressed_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeColorOverride("font_focus_color", RitsuShellTheme.Current.Text.HoverHighlight);
            button.AddThemeStyleboxOverride("normal", CreateSettingsToggleButtonStyle(on, hovered));
            button.AddThemeStyleboxOverride("hover", CreateSettingsToggleButtonStyle(on, true));
            button.AddThemeStyleboxOverride("pressed", CreateSettingsToggleButtonStyle(true, true));
            button.AddThemeStyleboxOverride("focus", CreateSettingsToggleButtonStyle(on, true));
        }

        /// <summary>
        ///     Creates the stylebox used by button-style settings toggles.
        ///     创建按钮样式设置切换控件使用的 stylebox。
        /// </summary>
        /// <param name="on">
        ///     Whether the toggle is enabled.
        ///     切换控件是否启用。
        /// </param>
        /// <param name="hovered">
        ///     Whether the button should use its emphasized hover/focus state.
        ///     按钮是否应使用强调的悬停/焦点状态。
        /// </param>
        /// <returns>
        ///     A stylebox representing the requested visual state.
        ///     表示所请求视觉状态的 stylebox。
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
        ///     Applies the standard framed input styling used by single-line text fields.
        ///     应用单行文本字段使用的标准带框输入样式。
        /// </summary>
        /// <param name="placeholder">
        ///     Placeholder text to display when the field is empty.
        ///     字段为空时要显示的占位文本。
        /// </param>
        /// <param name="width">
        ///     The minimum width to reserve for the field.
        ///     为字段保留的最小宽度。
        /// </param>
        /// <param name="height">
        ///     The minimum height to reserve for the field.
        ///     为字段保留的最小高度。
        /// </param>
        /// <param name="fontSize">
        ///     The font size to apply.
        ///     要应用的字体大小。
        /// </param>
        /// <returns>
        ///     The configured line edit instance.
        ///     指定 configured line edit instance。
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
        ///     Applies themed scrollbar track and grabber colors to a settings
        ///     <see cref="ScrollContainer" />, sets vertical bar width from
        ///     <c>components.scrollbar.layout.size</c>, and applies
        ///     <c>components.scrollbar.layout.scrollbarVSeparation</c> when supported.
        ///     将主题化滚动条轨道和滑块颜色应用到设置 <see cref="ScrollContainer" />，从 <c>components.scrollbar.layout.size</c> 设置垂直滚动条宽度，并在支持时应用
        ///     <c>components.scrollbar.layout.scrollbarVSeparation</c>。
        /// </summary>
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
        ///     Applies the same scrollbar chrome as <see cref="ApplySettingsScrollContainerTheme" />, using
        ///     <c>components.dropdown.layout.scroll.barWidth</c> (fallback: global scrollbar width) and
        ///     <c>components.dropdown.layout.scroll.scrollbarVSeparation</c> (fallback: global separation).
        ///     应用与 <see cref="ApplySettingsScrollContainerTheme" /> 相同的滚动条 chrome，使用
        ///     <c>components.dropdown.layout.scroll.barWidth</c>（回退：全局滚动条宽度）和
        ///     <c>components.dropdown.layout.scroll.scrollbarVSeparation</c>（回退：全局间距）。
        /// </summary>
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
