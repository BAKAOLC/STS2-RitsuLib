namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines an immutable ordered frame sequence for one logical cue, such as combat, a merchant room,
    ///         or an Ancient event stage.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为一个逻辑视觉提示定义不可变的有序帧序列，例如战斗、商人房间或先古事件舞台。
    ///     </para>
    /// </summary>
    /// <param name="Frames">
    ///     <para xml:lang="en">The ordered frames. Playback requires at least one entry.</para>
    ///     <para xml:lang="zh-CN">有序帧列表；播放时至少需要一个条目。</para>
    /// </param>
    /// <param name="Loop">
    ///     <para xml:lang="en">Whether playback restarts after the last frame.</para>
    ///     <para xml:lang="zh-CN">播放到最后一帧后是否重新开始。</para>
    /// </param>
    public sealed record VisualFrameSequence(
        IReadOnlyList<VisualFrame> Frames,
        bool Loop = false)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a frame sequence with optional style metadata. The two-parameter constructor remains available
        ///         for binary compatibility with older mods.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用可选的样式元数据初始化帧序列。为保持与旧版模组的二进制兼容性，双参数构造函数仍然可用。
        ///     </para>
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
        ///     <para xml:lang="en">Gets the optional style applied to frames that do not define their own style.</para>
        ///     <para xml:lang="zh-CN">获取应用于未单独定义样式之帧的可选默认样式。</para>
        /// </summary>
        public VisualNodeStyle? DefaultStyle { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional style entries aligned by index with <see cref="Frames" />.</para>
        ///     <para xml:lang="zh-CN">获取按索引与 <see cref="Frames" /> 对齐的可选样式条目。</para>
        /// </summary>
        public IReadOnlyList<VisualNodeStyle?>? FrameStyles { get; init; }
    }
}
