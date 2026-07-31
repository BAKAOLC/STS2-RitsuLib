using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Groups the font resources resolved for a shell theme.</para>
    ///     <para xml:lang="zh-CN">集中定义为 Shell 主题解析的字体资源。</para>
    /// </summary>
    /// <param name="Body">
    ///     <para xml:lang="en">The regular body font.</para>
    ///     <para xml:lang="zh-CN">常规正文字体。</para>
    /// </param>
    /// <param name="BodyBold">
    ///     <para xml:lang="en">The emphasized body font.</para>
    ///     <para xml:lang="zh-CN">强调正文字体。</para>
    /// </param>
    /// <param name="Button">
    ///     <para xml:lang="en">The font used by compact and action buttons.</para>
    ///     <para xml:lang="zh-CN">紧凑按钮及操作按钮使用的字体。</para>
    /// </param>
    public sealed record FontTokens(Font Body, Font BodyBold, Font Button);
}
