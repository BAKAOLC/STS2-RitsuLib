namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Immutable ordered frames for one logical cue (combat, merchant room, ancient stage, …).
    ///     一个逻辑 cue 的不可变有序帧（战斗、商人房间、远古事件舞台等）。
    /// </summary>
    /// <param name="Frames">
    ///     At least one entry for playback.
    ///     至少包含一个用于播放的条目。
    /// </param>
    /// <param name="Loop">
    ///     Whether to restart after the last frame.
    ///     到达最后一帧后是否重新开始。
    /// </param>
    public sealed record VisualFrameSequence(
        IReadOnlyList<VisualFrame> Frames,
        bool Loop = false)
    {
        /// <summary>
        ///     Constructor with optional style metadata. The two-parameter constructor remains the binary-compatible
        ///     baseline for older mods.
        ///     带可选样式元数据的构造器；双参数构造器仍保留为旧 mod 的二进制兼容基线。
        /// </summary>
        public VisualFrameSequence(
            IReadOnlyList<VisualFrame> Frames,
            bool Loop,
            VisualNodeStyle? DefaultStyle,
            IReadOnlyList<VisualNodeStyle?>? FrameStyles)
            : this(Frames, Loop)
        {
            this.DefaultStyle = DefaultStyle;
            this.FrameStyles = FrameStyles;
        }

        /// <summary>
        ///     Optional style applied to every frame unless the frame has its own style.
        ///     应用于每一帧的可选样式；如果帧自身定义了样式，则使用帧样式。
        /// </summary>
        public VisualNodeStyle? DefaultStyle { get; init; }

        /// <summary>
        ///     Optional style entries aligned by index with <see cref="Frames" />.
        ///     与 <see cref="Frames" /> 按索引对齐的可选样式条目。
        /// </summary>
        public IReadOnlyList<VisualNodeStyle?>? FrameStyles { get; init; }
    }
}
