namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Immutable per-cue visuals: one static texture and/or a <see cref="VisualFrameSequence" /> per cue name. Used for
    ///     combat, game-over, merchant / rest-site shells, ancient foreground layers, and similar.
    ///     不可变的逐 cue 视觉：每个 cue 名称对应一个静态纹理和/或一个 <see cref="VisualFrameSequence" />。用于
    ///     战斗、游戏结束、商人 / 休息处外壳、远古事件前景图层等。
    /// </summary>
    /// <param name="TexturePathByCue">
    ///     One texture per cue key.
    ///     每个 cue key 对应一个贴图。
    /// </param>
    /// <param name="FrameSequenceByCue">
    ///     Overrides <paramref name="TexturePathByCue" /> for the same cue key when present.
    ///     存在相同 cue 键时，覆盖 <paramref name="TexturePathByCue" />。
    /// </param>
    public sealed record VisualCueSet(
        IReadOnlyDictionary<string, string>? TexturePathByCue = null,
        IReadOnlyDictionary<string, VisualFrameSequence>? FrameSequenceByCue = null)
    {
        /// <summary>
        ///     Constructor with optional style metadata. The two-parameter constructor remains the binary-compatible
        ///     baseline for older mods.
        ///     带可选样式元数据的构造器；双参数构造器仍保留为旧 mod 的二进制兼容基线。
        /// </summary>
        public VisualCueSet(
            IReadOnlyDictionary<string, string>? TexturePathByCue,
            IReadOnlyDictionary<string, VisualFrameSequence>? FrameSequenceByCue,
            IReadOnlyDictionary<string, VisualNodeStyle>? TextureStyleByCue)
            : this(TexturePathByCue, FrameSequenceByCue)
        {
            this.TextureStyleByCue = TextureStyleByCue;
        }

        /// <summary>
        ///     Optional per-cue style applied when a static texture cue is shown.
        ///     显示静态贴图 cue 时应用的可选逐 cue 样式。
        /// </summary>
        public IReadOnlyDictionary<string, VisualNodeStyle>? TextureStyleByCue { get; init; }
    }
}
