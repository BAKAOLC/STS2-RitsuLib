using System.Collections.ObjectModel;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a fluent builder for <see cref="VisualCueSet" /> instances containing static textures or frame
    ///         sequences for named cues.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 <see cref="VisualCueSet" /> 的流式构建器，可为具名视觉提示配置静态纹理或帧序列。
    ///     </para>
    /// </summary>
    public sealed class VisualCueSetBuilder
    {
        private readonly Dictionary<string, VisualFrameSequence> _sequences =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _textures =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, VisualNodeStyle> _textureStyles =
            new(StringComparer.OrdinalIgnoreCase);

        private VisualCueSetBuilder()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Starts a new cue-set definition.</para>
        ///     <para xml:lang="zh-CN">开始定义新的视觉提示集。</para>
        /// </summary>
        public static VisualCueSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds one static texture to a cue such as <c>idle</c> or <c>die</c>, replacing any frame sequence
        ///         registered for the same key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将一张静态纹理绑定到 <c>idle</c> 或 <c>die</c> 等视觉提示，并替换同一键下已有的帧序列。
        ///     </para>
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath)
        {
            return Single(cueKey, texturePath, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds one static texture to a cue and optionally applies style overrides while that cue is shown.
        ///     </para>
        ///     <para xml:lang="zh-CN">将一张静态纹理绑定到视觉提示，并可在显示该提示时应用样式覆盖。</para>
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath, VisualNodeStyle? style)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cueKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);

            _textures[cueKey] = texturePath;
            if (style != null)
                _textureStyles[cueKey] = style;
            else
                _textureStyles.Remove(cueKey);
            _sequences.Remove(cueKey);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds one texture to a non-looping timed cue. The cue completes after its effective
        ///         <paramref name="durationSeconds" />, allowing state machines to advance.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将一张纹理绑定到非循环的定时视觉提示。提示会在有效的 <paramref name="durationSeconds" />
        ///         结束后完成，使状态机能够推进。
        ///     </para>
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath, float durationSeconds)
        {
            return Single(cueKey, texturePath, durationSeconds, null);
        }

        /// <summary>
        ///     <para xml:lang="en">Binds one texture to a non-looping timed cue with optional style overrides.</para>
        ///     <para xml:lang="zh-CN">将一张纹理绑定到非循环的定时视觉提示，并可应用样式覆盖。</para>
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath, float durationSeconds,
            VisualNodeStyle? style)
        {
            return Sequence(cueKey, VisualFrameSequenceBuilder.Create()
                .Frame(texturePath, durationSeconds, style)
                .Build());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds a completed frame sequence to a cue, replacing any static texture registered for the same key.
        ///     </para>
        ///     <para xml:lang="zh-CN">将已构建的帧序列绑定到视觉提示，并替换同一键下已有的静态纹理。</para>
        /// </summary>
        public VisualCueSetBuilder Sequence(string cueKey, VisualFrameSequence sequence)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cueKey);
            ArgumentNullException.ThrowIfNull(sequence);

            _sequences[cueKey] = sequence;
            _textures.Remove(cueKey);
            _textureStyles.Remove(cueKey);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Binds a frame sequence configured through <paramref name="configure" />.</para>
        ///     <para xml:lang="zh-CN">绑定通过 <paramref name="configure" /> 配置的帧序列。</para>
        /// </summary>
        public VisualCueSetBuilder Sequence(string cueKey, Action<VisualFrameSequenceBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var inner = VisualFrameSequenceBuilder.Create();
            configure(inner);
            return Sequence(cueKey, inner.Build());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds an immutable cue set, using <see langword="null" /> for empty dictionaries.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建不可变的视觉提示集；空字典对应的字段会设为 <see langword="null" />。
        ///     </para>
        /// </summary>
        public VisualCueSet Build()
        {
            return new(
                _textures.Count > 0
                    ? new ReadOnlyDictionary<string, string>(
                        new Dictionary<string, string>(_textures, StringComparer.OrdinalIgnoreCase))
                    : null,
                _sequences.Count > 0
                    ? new ReadOnlyDictionary<string, VisualFrameSequence>(
                        new Dictionary<string, VisualFrameSequence>(_sequences, StringComparer.OrdinalIgnoreCase))
                    : null,
                _textureStyles.Count > 0
                    ? new ReadOnlyDictionary<string, VisualNodeStyle>(
                        new Dictionary<string, VisualNodeStyle>(_textureStyles, StringComparer.OrdinalIgnoreCase))
                    : null);
        }
    }
}
