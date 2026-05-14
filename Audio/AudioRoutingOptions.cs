namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Higher-level playback routing options such as singleton channels and tagged groups.
    ///     高级播放路由选项，例如单例通道和带标签的组。
    /// </summary>
    public sealed class AudioRoutingOptions
    {
        /// <summary>
        ///     Optional singleton channel name. New playback can keep or replace the current channel owner.
        ///     可选单例通道名称。新播放可以保留或替换当前通道所有者。
        /// </summary>
        public string? Channel { get; init; }

        /// <summary>
        ///     Optional group tag for bulk stop or replacement patterns.
        ///     用于批量停止或替换模式的可选组标签。
        /// </summary>
        public string? Tag { get; init; }

        /// <summary>
        ///     Channel collision behavior when <see cref="Channel" /> is already occupied.
        ///     <see cref="Channel" /> 已被占用时的通道冲突行为。
        /// </summary>
        public AudioChannelMode ChannelMode { get; init; } = AudioChannelMode.ReplaceExisting;

        /// <summary>
        ///     Whether replacement should allow fade-out for the previous owner.
        ///     替换是否应允许上一所有者淡出。
        /// </summary>
        public bool AllowFadeOutOnReplace { get; init; } = true;

        /// <summary>
        ///     When true and <see cref="Tag" /> is set, existing handles in that tag stop before the new handle is attached.
        ///     为 true 且设置了 <see cref="Tag" /> 时，新句柄附加前会先停止该标签中的现有句柄。
        /// </summary>
        public bool ReplaceTaggedGroup { get; init; }
    }
}
