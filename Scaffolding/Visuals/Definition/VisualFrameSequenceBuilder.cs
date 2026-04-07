namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     Fluent builder for <see cref="VisualFrameSequence" /> with per-frame durations.
    /// </summary>
    public sealed class VisualFrameSequenceBuilder
    {
        private readonly List<VisualFrame> _frames = [];
        private bool _loop;

        private VisualFrameSequenceBuilder()
        {
        }

        /// <summary>
        ///     Starts a new frame sequence.
        /// </summary>
        public static VisualFrameSequenceBuilder Create()
        {
            return new();
        }

        /// <summary>
        ///     Appends a frame.
        /// </summary>
        public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);
            if (!float.IsFinite(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds,
                    "Frame duration must be a finite value.");

            _frames.Add(new(texturePath, durationSeconds));
            return this;
        }

        /// <summary>
        ///     Sets whether the sequence should loop after the last frame (default <see langword="false" />).
        /// </summary>
        public VisualFrameSequenceBuilder Loop(bool loop = true)
        {
            _loop = loop;
            return this;
        }

        /// <summary>
        ///     Produces an immutable sequence (must contain at least one frame).
        /// </summary>
        public VisualFrameSequence Build()
        {
            return _frames.Count == 0
                ? throw new InvalidOperationException("Frame sequence must contain at least one frame.")
                : new(_frames.ToArray(), _loop);
        }
    }
}
