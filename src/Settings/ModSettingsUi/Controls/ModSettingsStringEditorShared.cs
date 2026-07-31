using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides length clamping and value-field theming shared by single-line and multiline string
    ///         editors.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供单行与多行字符串编辑器共用的长度限制和数值字段主题设置。</para>
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
