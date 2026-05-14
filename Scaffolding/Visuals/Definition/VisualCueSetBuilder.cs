using System.Collections.ObjectModel;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="VisualCueSet" /> (single textures and frame sequences per cue).
    ///     <see cref="VisualCueSet" /> 的流式构建器（每个 cue 对应单张纹理和帧序列）。
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
        ///     Starts a new cue set definition.
        ///     开始一个新的 cue set 定义。
        /// </summary>
        public static VisualCueSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Binds one static texture to a cue (e.g. <c>idle</c>, <c>die</c>). Removes a frame sequence for the same
        ///     cue key if present.
        ///     将一个静态纹理绑定到 cue（例如 <c>idle</c>、<c>die</c>）。如果同一
        ///     cue 键存在帧序列，则移除它。
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath)
        {
            return Single(cueKey, texturePath, null);
        }

        /// <summary>
        ///     Binds one static texture to a cue and optionally applies style overrides whenever that cue is shown.
        ///     将一个静态贴图绑定到 cue，并在显示该 cue 时可选应用样式覆盖。
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
        ///     Binds a built frame sequence to a cue. Removes a single-texture entry for the same cue key if present.
        ///     将已构建的帧序列绑定到 cue。如果同一 cue key 已有单贴图条目，则移除该条目。
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
        ///     Binds a frame sequence configured via <paramref name="configure" />.
        ///     绑定一个通过 <paramref name="configure" /> 配置的帧序列。
        /// </summary>
        public VisualCueSetBuilder Sequence(string cueKey, Action<VisualFrameSequenceBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var inner = VisualFrameSequenceBuilder.Create();
            configure(inner);
            return Sequence(cueKey, inner.Build());
        }

        /// <summary>
        ///     Produces an immutable cue set (empty dictionaries become <see langword="null" /> fields).
        ///     生成不可变 cue set（空字典会变成 <see langword="null" /> 字段）。
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
