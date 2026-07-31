using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">Defines one texture frame and its hold duration in a <see cref="VisualFrameSequence" />.</para>
    ///     <para xml:lang="zh-CN">定义 <see cref="VisualFrameSequence" /> 中的一帧纹理及其保持时长。</para>
    /// </summary>
    /// <param name="TexturePath">
    ///     <para xml:lang="en">The Godot resource path of a <see cref="Texture2D" />.</para>
    ///     <para xml:lang="zh-CN"><see cref="Texture2D" /> 的 Godot 资源路径。</para>
    /// </param>
    /// <param name="DurationSeconds">
    ///     <para xml:lang="en">The display time before advancing; non-positive values are clamped at runtime.</para>
    ///     <para xml:lang="zh-CN">切换到下一帧之前的显示时长；运行时会限制非正值的最小值。</para>
    /// </param>
    public readonly record struct VisualFrame(string TexturePath, float DurationSeconds);
}
