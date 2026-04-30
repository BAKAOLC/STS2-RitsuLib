using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Theme-resolved font assets.
    /// </summary>
    /// <param name="Body">Regular body font.</param>
    /// <param name="BodyBold">Emphasized body font.</param>
    /// <param name="Button">Font used by compact and action buttons.</param>
    public sealed record FontTokens(Font Body, Font BodyBold, Font Button);
}
