namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Immutable per-cue visuals: one static texture and/or a <see cref="VisualFrameSequence" /> per cue name. Used for
    ///     combat, game-over, merchant / rest-site shells, ancient foreground layers, and similar.
    /// </summary>
    /// <param name="TexturePathByCue">One texture per cue key.</param>
    /// <param name="FrameSequenceByCue">Overrides <paramref name="TexturePathByCue" /> for the same cue key when present.</param>
    public sealed record VisualCueSet(
        IReadOnlyDictionary<string, string>? TexturePathByCue = null,
        IReadOnlyDictionary<string, VisualFrameSequence>? FrameSequenceByCue = null);
}
