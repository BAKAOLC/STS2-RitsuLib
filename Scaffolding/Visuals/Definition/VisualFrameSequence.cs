namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Immutable ordered frames for one logical cue (combat, merchant room, ancient stage, …).
    /// </summary>
    /// <param name="Frames">At least one entry for playback.</param>
    /// <param name="Loop">Whether to restart after the last frame.</param>
    public sealed record VisualFrameSequence(
        IReadOnlyList<VisualFrame> Frames,
        bool Loop = false);
}
