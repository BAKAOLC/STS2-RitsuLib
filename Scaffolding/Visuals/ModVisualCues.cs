using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals
{
    /// <summary>
    ///     Entry points for building <see cref="VisualCueSet" /> and <see cref="VisualFrameSequence" /> data used across
    ///     combat, world shells, ancient stages, etc.
    /// </summary>
    public static class ModVisualCues
    {
        /// <summary>
        ///     Begins a <see cref="VisualCueSet" /> builder.
        /// </summary>
        public static VisualCueSetBuilder CueSet()
        {
            return VisualCueSetBuilder.Create();
        }

        /// <summary>
        ///     Begins a <see cref="VisualFrameSequence" /> builder.
        /// </summary>
        public static VisualFrameSequenceBuilder FrameSequence()
        {
            return VisualFrameSequenceBuilder.Create();
        }
    }
}
