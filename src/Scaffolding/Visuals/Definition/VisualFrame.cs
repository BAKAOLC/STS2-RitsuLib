namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     One frame in a <see cref="VisualFrameSequence" /> (texture path + hold duration).
    ///     One frame in a <see cref="VisualFrameSequence" /> (纹理 路径 + hold duration)。
    /// </summary>
    /// <param name="TexturePath">
    ///     Godot resource path to a <c>Texture2D</c>.
    ///     Godot 资源 路径 to a <c>Texture2D</c>。
    /// </param>
    /// <param name="DurationSeconds">
    ///     Display time before advancing; non-positive values are clamped at runtime.
    ///     切换到下一帧前的显示时长；非正值会在运行时被钳制。
    /// </param>
    public readonly record struct VisualFrame(string TexturePath, float DurationSeconds);
}
