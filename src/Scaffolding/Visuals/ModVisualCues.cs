using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides entry points for building <see cref="VisualCueSet" /> and <see cref="VisualFrameSequence" /> data used
    ///         by combat visuals, world scenes, Ancient event stages, and similar contexts.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于构建 <see cref="VisualCueSet" /> 和 <see cref="VisualFrameSequence" /> 数据的入口，
    ///         这些数据可用于战斗视觉效果、世界场景、先古事件舞台等。
    ///     </para>
    /// </summary>
    public static class ModVisualCues
    {
        /// <summary>
        ///     <para xml:lang="en">Starts a <see cref="VisualCueSet" /> builder.</para>
        ///     <para xml:lang="zh-CN">创建 <see cref="VisualCueSet" /> 构建器。</para>
        /// </summary>
        public static VisualCueSetBuilder CueSet()
        {
            return VisualCueSetBuilder.Create();
        }

        /// <summary>
        ///     <para xml:lang="en">Starts a <see cref="VisualFrameSequence" /> builder.</para>
        ///     <para xml:lang="zh-CN">创建 <see cref="VisualFrameSequence" /> 构建器。</para>
        /// </summary>
        public static VisualFrameSequenceBuilder FrameSequence()
        {
            return VisualFrameSequenceBuilder.Create();
        }
    }
}
