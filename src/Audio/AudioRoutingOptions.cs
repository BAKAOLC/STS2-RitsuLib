namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Configures optional named-channel ownership and tag-group membership for a playback handle.</para>
    ///     <para xml:lang="zh-CN">配置播放句柄可选的命名通道归属和标签组成员关系。</para>
    /// </summary>
    public sealed class AudioRoutingOptions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional case-sensitive channel name; null, empty, or whitespace
        ///         disables channel routing.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化可选且区分大小写的通道名称；为 <see langword="null" />、空或空白时禁用通道路由。</para>
        /// </summary>
        public string? Channel { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes the optional case-sensitive tag name; null, empty, or whitespace disables
        ///         tag routing.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化可选且区分大小写的标签名称；为 <see langword="null" />、空或空白时禁用标签路由。</para>
        /// </summary>
        public string? Tag { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets or initializes the collision policy used when <see cref="Channel" /> already has an owner.</para>
        ///     <para xml:lang="zh-CN">获取或初始化 <see cref="Channel" /> 已有占用者时使用的冲突策略。</para>
        /// </summary>
        public AudioChannelMode ChannelMode { get; init; } = AudioChannelMode.ReplaceExisting;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether channel and tag-group replacement cleanup may fade out previous
        ///         handles.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化清理被通道或标签组替换的旧句柄时是否允许淡出。</para>
        /// </summary>
        public bool AllowFadeOutOnReplace { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether existing handles in <see cref="Tag" /> must all release before the new handle
        ///         is attached. Incomplete cleanup causes routing to fail.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化在附加新句柄前是否必须释放 <see cref="Tag" /> 中的所有现有句柄；清理未完成会导致路由失败。
        ///     </para>
        /// </summary>
        public bool ReplaceTaggedGroup { get; init; }
    }
}
