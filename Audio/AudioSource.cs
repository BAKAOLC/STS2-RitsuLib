namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Abstract audio source consumed by the high-level playback API.
    ///     高级播放 API 使用的抽象音频源。
    /// </summary>
    public abstract record AudioSource
    {
        /// <summary>
        ///     Creates a Studio event source from an event path.
        ///     从事件路径创建 Studio 事件源。
        /// </summary>
        public static StudioEventSource Event(string path)
        {
            return new(path);
        }

        /// <summary>
        ///     Creates a Studio event source from a wrapped event path.
        ///     从包装的事件路径创建 Studio 事件源。
        /// </summary>
        public static StudioEventSource Event(FmodEventPath path)
        {
            return new(path);
        }

        /// <summary>
        ///     Creates a Studio GUID source.
        ///     创建 Studio GUID 源。
        /// </summary>
        public static StudioGuidSource Guid(string guid)
        {
            return new(guid);
        }

        /// <summary>
        ///     Creates a loose-file sound source.
        ///     创建松散文件声音源。
        /// </summary>
        public static SoundFileSource File(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     Creates a loose-file streaming music source.
        ///     创建松散文件流式音乐源。
        /// </summary>
        public static StreamingMusicSource StreamingMusic(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     Creates a snapshot source.
        ///     创建 snapshot 源。
        /// </summary>
        public static SnapshotSource Snapshot(string path)
        {
            return new(path);
        }
    }

    /// <summary>
    ///     Studio event-path source.
    ///     Studio 事件路径源。
    /// </summary>
    public sealed record StudioEventSource(FmodEventPath Path) : AudioSource;

    /// <summary>
    ///     Studio event GUID source.
    ///     Studio 事件 GUID 源。
    /// </summary>
    public sealed record StudioGuidSource(string Value) : AudioSource;

    /// <summary>
    ///     Loose-file sound source.
    ///     松散文件声音源。
    /// </summary>
    public sealed record SoundFileSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     Loose-file streaming music source.
    ///     松散文件流式音乐源。
    /// </summary>
    public sealed record StreamingMusicSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     Snapshot source.
    ///     Snapshot 源。
    /// </summary>
    public sealed record SnapshotSource(string Path) : AudioSource;
}
