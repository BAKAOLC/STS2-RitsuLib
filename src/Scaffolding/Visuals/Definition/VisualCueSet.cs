namespace STS2RitsuLib.Scaffolding.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines immutable visuals for named cues, with one static texture, one <see cref="VisualFrameSequence" />,
    ///         or both for each cue. Cue sets support combat, game-over screens, merchant and rest-site characters,
    ///         Ancient foreground layers, and similar contexts.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义按名称索引的不可变视觉提示；每个提示可包含一张静态纹理、一个
    ///         <see cref="VisualFrameSequence" />，或同时包含两者。视觉提示集可用于战斗、游戏结束界面、
    ///         商人和休息处角色、先古事件前景图层等场景。
    ///     </para>
    /// </summary>
    /// <param name="TexturePathByCue">
    ///     <para xml:lang="en">The texture path associated with each cue key.</para>
    ///     <para xml:lang="zh-CN">与每个视觉提示键关联的纹理路径。</para>
    /// </param>
    /// <param name="FrameSequenceByCue">
    ///     <para xml:lang="en">
    ///         The frame sequence associated with each cue key. A sequence takes precedence over
    ///         <paramref name="TexturePathByCue" /> for the same key.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         与每个视觉提示键关联的帧序列。同一键同时存在时，帧序列优先于
    ///         <paramref name="TexturePathByCue" />。
    ///     </para>
    /// </param>
    public sealed record VisualCueSet(
        IReadOnlyDictionary<string, string>? TexturePathByCue = null,
        IReadOnlyDictionary<string, VisualFrameSequence>? FrameSequenceByCue = null)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes a cue set with optional style metadata. The two-parameter constructor remains available for
        ///         binary compatibility with older mods.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用可选的样式元数据初始化视觉提示集。为保持与旧版模组的二进制兼容性，双参数构造函数仍然可用。
        ///     </para>
        /// </summary>
        public VisualCueSet(
            IReadOnlyDictionary<string, string>? TexturePathByCue,
            IReadOnlyDictionary<string, VisualFrameSequence>? FrameSequenceByCue,
            IReadOnlyDictionary<string, VisualNodeStyle>? TextureStyleByCue)
            : this(TexturePathByCue, FrameSequenceByCue)
        {
            this.TextureStyleByCue = TextureStyleByCue;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional per-cue style applied when a static texture is shown.</para>
        ///     <para xml:lang="zh-CN">获取显示静态纹理时按视觉提示应用的可选样式。</para>
        /// </summary>
        public IReadOnlyDictionary<string, VisualNodeStyle>? TextureStyleByCue { get; init; }
    }
}
