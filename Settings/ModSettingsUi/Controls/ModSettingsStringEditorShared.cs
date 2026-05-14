using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Max-length clamping and string-field theming shared by single-line and multiline controls.
    ///     单行和多行控件共享的最大长度截断与字符串字段主题设置。
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
            ModSettingsUiControlTheming.ApplyEntryLineEditValueFieldTheme(edit, RitsuShellTheme.Current.Font.Body);
        }

        internal static void ApplyStringTextEditTheme(TextEdit edit)
        {
            ModSettingsUiControlTheming.ApplyEntryTextEditValueFieldTheme(edit, RitsuShellTheme.Current.Font.Body);
        }
    }
}
