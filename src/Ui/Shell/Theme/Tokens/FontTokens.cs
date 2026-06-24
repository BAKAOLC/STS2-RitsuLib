using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Theme-resolved font assets.
    ///     主题解析后的 字体资源。
    /// </summary>
    /// <param name="Body">
    ///     Regular body font.
    ///     常规正文字体。
    /// </param>
    /// <param name="BodyBold">
    ///     Emphasized body font.
    ///     强调正文字体。
    /// </param>
    /// <param name="Button">
    ///     Font used by compact and action buttons.
    ///     使用的字体： 紧凑按钮和动作按钮。
    /// </param>
    public sealed record FontTokens(Font Body, Font BodyBold, Font Button);
}
