using Godot;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Max-length clamping and string-field theming shared by single-line and multiline controls.
    /// </summary>
    internal static class ModSettingsStringEditorShared
    {
        internal static string ClampToMaxLength(string text, int? maxLength)
        {
            if (maxLength is not >= 1 || text.Length <= maxLength.Value)
                return text;
            return text[..maxLength.Value];
        }

        internal static void ApplyStringLineEditTheme(LineEdit edit)
        {
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(edit, ModSettingsUiResources.KreonRegular);
        }

        internal static void ApplyStringTextEditTheme(TextEdit edit)
        {
            ModSettingsUiControlTheming.ApplyEntryTextEditValueFieldTheme(edit, ModSettingsUiResources.KreonRegular);
        }
    }
}
