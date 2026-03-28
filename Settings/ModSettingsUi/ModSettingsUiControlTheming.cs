using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Central place for repeated Godot theme overrides on LineEdit, TextEdit, buttons, and popup menus.
    /// </summary>
    internal static class ModSettingsUiControlTheming
    {
        internal static void ApplyUniformSurfaceButtonStates(BaseButton control)
        {
            var box = ModSettingsUiFactory.CreateSurfaceStyle();
            control.AddThemeStyleboxOverride("normal", box);
            control.AddThemeStyleboxOverride("hover", box);
            control.AddThemeStyleboxOverride("pressed", box);
            control.AddThemeStyleboxOverride("focus", box);
        }

        internal static void ApplyColorPickerSwatchButtonChrome(ColorPickerButton picker)
        {
            var box = ModSettingsUiFactory.CreateColorPickerSwatchFrameStyle();
            picker.AddThemeStyleboxOverride("normal", box);
            picker.AddThemeStyleboxOverride("hover", box);
            picker.AddThemeStyleboxOverride("pressed", box);
            picker.AddThemeStyleboxOverride("focus", box);
        }

        internal static void ApplyEntryLineEditValueFieldTheme(LineEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextBody);
            edit.AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateSurfaceStyle());
            edit.AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateSurfaceStyle());
        }

        internal static void ApplyEntryTextEditValueFieldTheme(TextEdit edit, Font font, int fontSize = 17)
        {
            edit.AddThemeFontOverride("font", font);
            edit.AddThemeFontSizeOverride("font_size", fontSize);
            edit.AddThemeColorOverride("font_color", ModSettingsUiPalette.RichTextBody);
            edit.AddThemeStyleboxOverride("normal", ModSettingsUiFactory.CreateSurfaceStyle());
            edit.AddThemeStyleboxOverride("focus", ModSettingsUiFactory.CreateSurfaceStyle());
        }

        /// <summary>
        ///     Popup list rows: Kreon Regular, spacing tuned for touch / gamepad.
        /// </summary>
        internal static void ApplyPopupMenuListTheme(PopupMenu popup, int fontSize)
        {
            popup.AddThemeFontOverride("font", ModSettingsUiResources.KreonRegular);
            popup.AddThemeFontSizeOverride("font_size", fontSize);
            popup.AddThemeConstantOverride("v_separation", 12);
            popup.AddThemeConstantOverride("h_separation", 10);
        }
    }
}
