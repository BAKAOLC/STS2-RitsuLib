namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Higher-level playback routing options such as singleton channels and tagged groups.
    /// </summary>
    public sealed class AudioRoutingOptions
    {
        /// <summary>
        ///     Optional singleton channel name. New playback can keep or replace the current channel owner.
        /// </summary>
        public string? Channel { get; init; }

        /// <summary>
        ///     Optional group tag for bulk stop or replacement patterns.
        /// </summary>
        public string? Tag { get; init; }

        /// <summary>
        ///     Channel collision behavior when <see cref="Channel" /> is already occupied.
        /// </summary>
        public AudioChannelMode ChannelMode { get; init; } = AudioChannelMode.ReplaceExisting;

        /// <summary>
        ///     Whether replacement should allow fade-out for the previous owner.
        /// </summary>
        public bool AllowFadeOutOnReplace { get; init; } = true;

        /// <summary>
        ///     When true and <see cref="Tag" /> is set, existing handles in that tag stop before the new handle is attached.
        /// </summary>
        public bool ReplaceTaggedGroup { get; init; }
    }
}
