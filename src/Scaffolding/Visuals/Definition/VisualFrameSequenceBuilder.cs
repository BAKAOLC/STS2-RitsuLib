namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="VisualFrameSequence" /> with per-frame durations.
    ///     带逐帧时长的 <see cref="VisualFrameSequence" /> 流式构建器。
    /// </summary>
    public sealed class VisualFrameSequenceBuilder
    {
        private readonly List<VisualNodeStyle?> _frameStyles = [];
        private readonly List<VisualFrame> _frames = [];
        private VisualNodeStyle? _defaultStyle;
        private bool _loop;

        private VisualFrameSequenceBuilder()
        {
        }

        /// <summary>
        ///     Starts a new frame sequence.
        ///     开始一个新的帧序列。
        /// </summary>
        public static VisualFrameSequenceBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Appends a frame.
        ///     追加一帧。
        /// </summary>
        public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds)
        {
            return Frame(texturePath, durationSeconds, null);
        }

        /// <summary>
        ///     Appends a frame with optional style overrides applied while this frame is visible.
        ///     追加一帧，并在该帧可见期间可选应用样式覆盖。
        /// </summary>
        public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds, VisualNodeStyle? style)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);
            if (!float.IsFinite(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds,
                    "Frame duration must be a finite value.");

            _frames.Add(new(texturePath, durationSeconds));
            _frameStyles.Add(style);
            return this;
        }

        /// <summary>
        ///     Sets a default style applied to every frame that does not define its own style.
        ///     设置默认样式，应用到所有未定义自身样式的帧。
        /// </summary>
        public VisualFrameSequenceBuilder DefaultStyle(VisualNodeStyle style)
        {
            ArgumentNullException.ThrowIfNull(style);
            _defaultStyle = style;
            return this;
        }

        /// <summary>
        ///     Sets whether the sequence should loop after the last frame (default <see langword="false" />).
        ///     设置序列是否在最后一帧后循环（默认 <see langword="false" />）。
        /// </summary>
        public VisualFrameSequenceBuilder Loop(bool loop = true)
        {
            _loop = loop;
            return this;
        }

        /// <summary>
        ///     Produces an immutable sequence (must contain at least one frame).
        ///     生成不可变序列（必须至少包含一帧）。
        /// </summary>
        public VisualFrameSequence Build()
        {
            return _frames.Count == 0
                ? throw new InvalidOperationException("Frame sequence must contain at least one frame.")
                : new(
                    [.. _frames],
                    _loop,
                    _defaultStyle,
                    _frameStyles.Any(static s => s != null) ? _frameStyles.ToArray() : null);
        }
    }
}
