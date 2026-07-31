namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a fluent builder for <see cref="VisualFrameSequence" /> instances with per-frame durations.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供可配置逐帧时长的 <see cref="VisualFrameSequence" /> 流式构建器。</para>
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
        ///     <para xml:lang="en">Starts a new frame-sequence definition.</para>
        ///     <para xml:lang="zh-CN">开始定义新的帧序列。</para>
        /// </summary>
        public static VisualFrameSequenceBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a frame.</para>
        ///     <para xml:lang="zh-CN">追加一帧。</para>
        /// </summary>
        public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds)
        {
            return Frame(texturePath, durationSeconds, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a frame with optional style overrides applied while it is visible.</para>
        ///     <para xml:lang="zh-CN">追加一帧，并可在该帧可见期间应用样式覆盖。</para>
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
        ///     <para xml:lang="en">Sets the default style applied to frames that do not define their own style.</para>
        ///     <para xml:lang="zh-CN">设置应用于未单独定义样式之帧的默认样式。</para>
        /// </summary>
        public VisualFrameSequenceBuilder DefaultStyle(VisualNodeStyle style)
        {
            ArgumentNullException.ThrowIfNull(style);
            _defaultStyle = style;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets whether the sequence loops after its last frame. The default is <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置序列在最后一帧之后是否循环；默认值为 <see langword="false" />。
        ///     </para>
        /// </summary>
        public VisualFrameSequenceBuilder Loop(bool loop = true)
        {
            _loop = loop;
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Builds an immutable sequence. At least one frame is required.</para>
        ///     <para xml:lang="zh-CN">构建不可变序列；序列必须至少包含一帧。</para>
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
