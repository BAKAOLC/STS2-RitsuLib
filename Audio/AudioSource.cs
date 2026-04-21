namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Abstract audio source consumed by the high-level playback API.
    /// </summary>
    public abstract record AudioSource
    {
        /// <summary>
        ///     Creates a Studio event source from an event path.
        /// </summary>
        public static StudioEventSource Event(string path)
        {
            return new(path);
        }

        /// <summary>
        ///     Creates a Studio event source from a wrapped event path.
        /// </summary>
        public static StudioEventSource Event(FmodEventPath path)
        {
            return new(path);
        }

        /// <summary>
        ///     Creates a Studio GUID source.
        /// </summary>
        public static StudioGuidSource Guid(string guid)
        {
            return new(guid);
        }

        /// <summary>
        ///     Creates a loose-file sound source.
        /// </summary>
        public static SoundFileSource File(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     Creates a loose-file streaming music source.
        /// </summary>
        public static StreamingMusicSource StreamingMusic(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     Creates a snapshot source.
        /// </summary>
        public static SnapshotSource Snapshot(string path)
        {
            return new(path);
        }
    }

    /// <summary>
    ///     Studio event-path source.
    /// </summary>
    public sealed record StudioEventSource(FmodEventPath Path) : AudioSource;

    /// <summary>
    ///     Studio event GUID source.
    /// </summary>
    public sealed record StudioGuidSource(string Value) : AudioSource;

    /// <summary>
    ///     Loose-file sound source.
    /// </summary>
    public sealed record SoundFileSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     Loose-file streaming music source.
    /// </summary>
    public sealed record StreamingMusicSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     Snapshot source.
    /// </summary>
    public sealed record SnapshotSource(string Path) : AudioSource;
}
