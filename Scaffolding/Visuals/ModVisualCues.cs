using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals
{
    /// <summary>
    ///     Entry points for building <see cref="VisualCueSet" /> and <see cref="VisualFrameSequence" /> data used across
    ///     combat, world shells, ancient stages, etc.
    ///     用于构建 <see cref="VisualCueSet" /> 和 <see cref="VisualFrameSequence" /> 数据的入口点，这些数据用于
    ///     战斗、世界外壳、远古事件舞台等。
    /// </summary>
    public static class ModVisualCues
    {
        /// <summary>
        ///     Begins a <see cref="VisualCueSet" /> builder.
        ///     开始创建 <see cref="VisualCueSet" /> 构建器。
        /// </summary>
        public static VisualCueSetBuilder CueSet()
        {
            return VisualCueSetBuilder.Create();
        }

        /// <summary>
        ///     Begins a <see cref="VisualFrameSequence" /> builder.
        ///     开始创建 <see cref="VisualFrameSequence" /> 构建器。
        /// </summary>
        public static VisualFrameSequenceBuilder FrameSequence()
        {
            return VisualFrameSequenceBuilder.Create();
        }
    }
}
