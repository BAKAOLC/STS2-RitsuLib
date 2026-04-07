namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     One frame in a <see cref="VisualFrameSequence" /> (texture path + hold duration).
    /// </summary>
    /// <param name="TexturePath">Godot resource path to a <c>Texture2D</c>.</param>
    /// <param name="DurationSeconds">Display time before advancing; non-positive values are clamped at runtime.</param>
    public readonly record struct VisualFrame(string TexturePath, float DurationSeconds);
}
