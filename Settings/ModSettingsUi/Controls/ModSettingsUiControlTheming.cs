using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Central place for repeated Godot theme overrides on LineEdit, TextEdit, buttons, and popup menus.
    /// </summary>
    public static class ModSettingsUiControlTheming
    {
        /// <summary>
        ///     Applies the shared surface-button chrome to all standard button states.
        /// </summary>
        /// <param name="control">The button to style.</param>
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
        /// </summary>
        /// <param name="picker">The color picker button to style.</param>
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
        /// </summary>
        /// <param name="edit">The line edit to style.</param>
        /// <param name="font">The font to use for the value text.</param>
        /// <param name="fontSize">The font size to apply.</param>
        public static void ApplyEntryLineEditValueFieldTheme(LineEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextBody);
            var normal = ModSettingsUiFactory.CreateEntryFieldFrameStyle(false);
            var emphasis = ModSettingsUiFactory.CreateEntryFieldFrameStyle(true);
            edit.AddThemeStyleboxOverride("normal", normal);
            edit.AddThemeStyleboxOverride("hover", emphasis);
            edit.AddThemeStyleboxOverride("focus", emphasis);
            edit.AddThemeStyleboxOverride("read_only", normal);
        }

        /// <summary>
        ///     Applies the standard value-field theme to a multi-line text entry.
        /// </summary>
        /// <param name="edit">The text edit to style.</param>
        /// <param name="font">The font to use for the value text.</param>
        /// <param name="fontSize">The font size to apply.</param>
        public static void ApplyEntryTextEditValueFieldTheme(TextEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextBody);
            var normal = ModSettingsUiFactory.CreateEntryFieldFrameStyle(false);
            var emphasis = ModSettingsUiFactory.CreateEntryFieldFrameStyle(true);
            edit.AddThemeStyleboxOverride("normal", normal);
            edit.AddThemeStyleboxOverride("hover", emphasis);
            edit.AddThemeStyleboxOverride("focus", emphasis);
            edit.AddThemeStyleboxOverride("read_only", normal);
        }

        /// <summary>
        ///     Applies the standard popup-menu list styling used by settings pickers.
        /// </summary>
        /// <param name="popup">The popup menu to style.</param>
        /// <param name="fontSize">The font size to apply to menu rows.</param>
        public static void ApplyPopupMenuListTheme(PopupMenu popup, int fontSize)
        {
            popup.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            popup.AddThemeFontSizeOverride("font_size", fontSize);
            popup.AddThemeConstantOverride("v_separation", 12);
            popup.AddThemeConstantOverride("h_separation", 10);
        }

        /// <summary>
        ///     Creates a segmented row container for compact mode-selection buttons.
        /// </summary>
        /// <param name="buttons">The buttons to place in the row.</param>
        /// <returns>A horizontal container with standard spacing for segmented controls.</returns>
        public static HBoxContainer CreateSegmentedButtonRow(params Button[] buttons)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            foreach (var button in buttons)
                row.AddChild(button);
            return row;
        }

        /// <summary>
        ///     Creates a segmented toggle button using standard settings sizing.
        /// </summary>
        /// <param name="text">The button label.</param>
        /// <param name="pressed">Whether the button starts pressed.</param>
        /// <param name="group">Optional exclusive toggle group.</param>
        /// <returns>A configured segmented toggle button.</returns>
        public static Button CreateSegmentedToggleButton(string text, bool pressed, ButtonGroup? group = null)
        {
            return new()
            {
                Text = text,
                ToggleMode = true,
                ButtonGroup = group,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.EntryValueMinHeight),
            };
        }

        /// <summary>
        ///     Creates a button-style settings toggle that matches the standard on/off visual language.
        /// </summary>
        /// <param name="text">The button label.</param>
        /// <param name="pressed">Whether the toggle starts enabled.</param>
        /// <returns>A configured toggle button with standard interactive styling.</returns>
        public static Button CreateSettingsToggleButton(string text, bool pressed)
        {
            var button = new Button
            {
                Text = text,
                ToggleMode = true,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.EntryValueMinHeight),
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
        /// </summary>
        /// <param name="text">The button label.</param>
        /// <param name="pressed">Whether the toggle starts enabled.</param>
        /// <returns>A compact toggle button with standard interactive styling.</returns>
        public static Button CreateCompactSettingsToggleButton(string text, bool pressed)
        {
            var button = new Button
            {
                Text = text,
                ToggleMode = true,
                ButtonPressed = pressed,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new(110f, ModSettingsUiMetrics.EntryValueMinHeight),
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
        /// </summary>
        /// <param name="initialValue">Whether the toggle starts enabled.</param>
        /// <param name="onChanged">Callback invoked after the value changes.</param>
        /// <returns>A compact toggle control sized for dense editor layouts.</returns>
        public static ModSettingsToggleControl CreateCompactStateToggle(bool initialValue, Action<bool> onChanged)
        {
            return new(initialValue, onChanged)
            {
                CustomMinimumSize = new(0f, ModSettingsUiMetrics.EntryValueMinHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
        }

        /// <summary>
        ///     Creates a labeled compact editor field for dense multi-column layouts.
        /// </summary>
        /// <param name="labelText">The descriptive label shown above the editor.</param>
        /// <param name="editor">The editor control to place below the label.</param>
        /// <returns>A vertically stacked label-and-editor field.</returns>
        public static Control CreateCompactEditorField(string labelText, Control editor)
        {
            var wrapper = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            wrapper.AddThemeConstantOverride("separation", 6);
            wrapper.AddChild(ModSettingsUiFactory.CreateInlineDescription(labelText));
            wrapper.AddChild(editor);
            return wrapper;
        }

        /// <summary>
        ///     Creates a compact multi-column row for dense settings editors.
        /// </summary>
        /// <param name="columns">The number of columns to use.</param>
        /// <param name="controls">The fields to place in the row.</param>
        /// <returns>A compact grid container for grouped editors.</returns>
        public static Control CreateCompactEditorRow(int columns, params Control[] controls)
        {
            var grid = new GridContainer
            {
                Columns = columns,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 8);
            foreach (var control in controls)
                grid.AddChild(control);
            return grid;
        }

        /// <summary>
        ///     Creates a labeled compact toggle field for dense multi-column editor rows.
        /// </summary>
        /// <param name="labelText">The descriptive label shown above the toggle.</param>
        /// <param name="toggle">The toggle control to place below the label.</param>
        /// <returns>A vertically stacked label-and-toggle field.</returns>
        public static Control CreateCompactToggleField(string labelText, Control toggle)
        {
            return CreateCompactEditorField(labelText, toggle);
        }

        /// <summary>
        ///     Creates a compact multi-column row for labeled toggle fields.
        /// </summary>
        /// <param name="controls">The fields to place in the row.</param>
        /// <returns>A three-column grid sized for dense settings editors.</returns>
        public static Control CreateCompactToggleRow(params Control[] controls)
        {
            return CreateCompactEditorRow(3, controls);
        }

        /// <summary>
        ///     Creates a styled single-line text entry with an initial value.
        /// </summary>
        /// <param name="text">The initial text value.</param>
        /// <param name="placeholder">Placeholder text to display when the field is empty.</param>
        /// <param name="width">The minimum width to reserve for the field.</param>
        /// <param name="height">The minimum height to reserve for the field.</param>
        /// <param name="fontSize">The font size to apply.</param>
        /// <returns>The configured line edit instance.</returns>
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
        /// </summary>
        /// <param name="button">The button to style.</param>
        /// <param name="on">Whether the toggle is enabled.</param>
        /// <param name="hovered">Whether the button should use its emphasized hover/focus state.</param>
        public static void ApplySettingsToggleButtonStyle(Button button, bool on, bool hovered)
        {
            button.AddThemeFontOverride("font", ModSettingsUiResources.KreonBold);
            button.AddThemeFontSizeOverride("font_size", 18);
            button.AddThemeColorOverride("font_color", ModSettingsUiPalette.LabelPrimary);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeColorOverride("font_pressed_color", Colors.White);
            button.AddThemeColorOverride("font_focus_color", Colors.White);
            button.AddThemeStyleboxOverride("normal", CreateSettingsToggleButtonStyle(on, hovered));
            button.AddThemeStyleboxOverride("hover", CreateSettingsToggleButtonStyle(on, true));
            button.AddThemeStyleboxOverride("pressed", CreateSettingsToggleButtonStyle(true, true));
            button.AddThemeStyleboxOverride("focus", CreateSettingsToggleButtonStyle(on, true));
        }

        /// <summary>
        ///     Creates the stylebox used by button-style settings toggles.
        /// </summary>
        /// <param name="on">Whether the toggle is enabled.</param>
        /// <param name="hovered">Whether the button should use its emphasized hover/focus state.</param>
        /// <returns>A stylebox representing the requested visual state.</returns>
        public static StyleBoxFlat CreateSettingsToggleButtonStyle(bool on, bool hovered)
        {
            var borderColor = on ? new Color(0.52f, 0.87f, 0.69f, 0.95f) : new Color(0.34f, 0.46f, 0.58f, 0.45f);
            var borderWidth = hovered ? 3 : 2;
            return new()
            {
                BgColor = on
                    ? new(0.18f, 0.42f, 0.31f, 0.98f)
                    : hovered
                        ? new Color(0.18f, 0.22f, 0.28f, 0.98f)
                        : new Color(0.12f, 0.15f, 0.19f, 0.98f),
                BorderColor = borderColor,
                BorderWidthLeft = borderWidth,
                BorderWidthTop = borderWidth,
                BorderWidthRight = borderWidth,
                BorderWidthBottom = borderWidth,
                CornerRadiusTopLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusTopRight = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomLeft = ModSettingsUiMetrics.CornerRadius,
                CornerRadiusBottomRight = ModSettingsUiMetrics.CornerRadius,
                ShadowColor = hovered
                    ? new(borderColor.R, borderColor.G, borderColor.B, 0.42f)
                    : new Color(0f, 0f, 0f, 0.12f),
                ShadowSize = hovered ? 7 : 2,
                ContentMarginLeft = 14,
                ContentMarginTop = 8,
                ContentMarginRight = 14,
                ContentMarginBottom = 8,
            };
        }

        /// <summary>
        ///     Applies the standard framed input styling used by single-line text fields.
        /// </summary>
        /// <param name="placeholder">Placeholder text to display when the field is empty.</param>
        /// <param name="width">The minimum width to reserve for the field.</param>
        /// <param name="height">The minimum height to reserve for the field.</param>
        /// <param name="fontSize">The font size to apply.</param>
        /// <returns>The configured line edit instance.</returns>
        public static LineEdit CreateStyledLineEdit(string placeholder, float width = 220f, float height = 44f,
            int fontSize = 17)
        {
            var edit = new LineEdit
            {
                PlaceholderText = placeholder,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(width, height),
            };
            ApplyEntryLineEditValueFieldTheme(edit, ModSettingsUiResources.KreonRegular, fontSize);
            return edit;
        }
    }
}
