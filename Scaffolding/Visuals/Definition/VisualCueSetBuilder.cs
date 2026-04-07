using System.Collections.ObjectModel;

namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="VisualCueSet" /> (single textures and frame sequences per cue).
    /// </summary>
    public sealed class VisualCueSetBuilder
    {
        private readonly Dictionary<string, VisualFrameSequence> _sequences =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _textures =
            new(StringComparer.OrdinalIgnoreCase);

        private VisualCueSetBuilder()
        {
        }

        /// <summary>
        ///     Starts a new cue set definition.
        /// </summary>
        public static VisualCueSetBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Binds one static texture to a cue (e.g. <c>idle</c>, <c>die</c>). Removes a frame sequence for the same
        ///     cue key if present.
        /// </summary>
        public VisualCueSetBuilder Single(string cueKey, string texturePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cueKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);

            _textures[cueKey] = texturePath;
            _sequences.Remove(cueKey);
            return this;
        }

        /// <summary>
        ///     Binds a built frame sequence to a cue. Removes a single-texture entry for the same cue key if present.
        /// </summary>
        public VisualCueSetBuilder Sequence(string cueKey, VisualFrameSequence sequence)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cueKey);
            ArgumentNullException.ThrowIfNull(sequence);

            _sequences[cueKey] = sequence;
            _textures.Remove(cueKey);
            return this;
        }

        /// <summary>
        ///     Binds a frame sequence configured via <paramref name="configure" />.
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
                    : null);
        }
    }
}
