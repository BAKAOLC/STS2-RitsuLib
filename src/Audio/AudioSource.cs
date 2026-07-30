namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Base record for the source descriptors dispatched by the high-level playback API.</para>
    ///     <para xml:lang="zh-CN">高级播放 API 所分派音频源描述符的基础记录类型。</para>
    /// </summary>
    public abstract record AudioSource
    {
        /// <summary>
        ///     <para xml:lang="en">Wraps a raw FMOD Studio event or snapshot path as an event source.</para>
        ///     <para xml:lang="zh-CN">将原始 FMOD Studio 事件或快照路径包装为事件源。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The raw FMOD Studio path.</para>
        ///     <para xml:lang="zh-CN">原始 FMOD Studio 路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The event source descriptor.</para>
        ///     <para xml:lang="zh-CN">事件源描述符。</para>
        /// </returns>
        public static StudioEventSource Event(string path)
        {
            return new(path);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an event source from an existing FMOD Studio path value.</para>
        ///     <para xml:lang="zh-CN">根据现有 FMOD Studio 路径值创建事件源。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The wrapped FMOD Studio path.</para>
        ///     <para xml:lang="zh-CN">已包装的 FMOD Studio 路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The event source descriptor.</para>
        ///     <para xml:lang="zh-CN">事件源描述符。</para>
        /// </returns>
        public static StudioEventSource Event(FmodEventPath path)
        {
            return new(path);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps an FMOD Studio event GUID string as a source.</para>
        ///     <para xml:lang="zh-CN">将 FMOD Studio 事件 GUID 字符串包装为音频源。</para>
        /// </summary>
        /// <param name="guid">
        ///     <para xml:lang="en">The GUID string to normalize when playback is requested.</para>
        ///     <para xml:lang="zh-CN">请求播放时要规范化的 GUID 字符串。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The GUID source descriptor.</para>
        ///     <para xml:lang="zh-CN">GUID 音频源描述符。</para>
        /// </returns>
        public static StudioGuidSource Guid(string guid)
        {
            return new(guid);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a loose audio-file path as a non-streaming sound source.</para>
        ///     <para xml:lang="zh-CN">将松散音频文件路径包装为非流式声音源。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">The absolute, <c>user://</c>, or raw-file <c>res://</c> path resolved during playback.</para>
        ///     <para xml:lang="zh-CN">播放时解析的绝对路径、<c>user://</c> 路径或原始文件 <c>res://</c> 路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The loose-file sound source descriptor.</para>
        ///     <para xml:lang="zh-CN">松散文件声音源描述符。</para>
        /// </returns>
        public static SoundFileSource File(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a packed or imported Godot audio resource as a non-streaming sound source.</para>
        ///     <para xml:lang="zh-CN">将打包或导入的 Godot 音频资源包装为非流式声音源。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The Godot resource path materialized during playback.</para>
        ///     <para xml:lang="zh-CN">播放时物化的 Godot 资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resource sound source descriptor.</para>
        ///     <para xml:lang="zh-CN">资源声音源描述符。</para>
        /// </returns>
        public static ResourceSoundFileSource ResourceFile(string resourcePath)
        {
            return new(resourcePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a loose audio-file path as a streaming music source.</para>
        ///     <para xml:lang="zh-CN">将松散音频文件路径包装为流式音乐源。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">The absolute, <c>user://</c>, or raw-file <c>res://</c> path resolved during playback.</para>
        ///     <para xml:lang="zh-CN">播放时解析的绝对路径、<c>user://</c> 路径或原始文件 <c>res://</c> 路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The loose-file streaming source descriptor.</para>
        ///     <para xml:lang="zh-CN">松散文件流式音频源描述符。</para>
        /// </returns>
        public static StreamingMusicSource StreamingMusic(string absolutePath)
        {
            return new(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps a packed or imported Godot audio resource as a streaming music source.</para>
        ///     <para xml:lang="zh-CN">将打包或导入的 Godot 音频资源包装为流式音乐源。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The Godot resource path materialized during playback.</para>
        ///     <para xml:lang="zh-CN">播放时物化的 Godot 资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resource streaming source descriptor.</para>
        ///     <para xml:lang="zh-CN">资源流式音频源描述符。</para>
        /// </returns>
        public static StreamingResourceMusicSource StreamingResourceMusic(string resourcePath)
        {
            return new(resourcePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Wraps an FMOD Studio snapshot path as a snapshot source.</para>
        ///     <para xml:lang="zh-CN">将 FMOD Studio 快照路径包装为快照源。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The FMOD Studio snapshot path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 快照路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The snapshot source descriptor.</para>
        ///     <para xml:lang="zh-CN">快照源描述符。</para>
        /// </returns>
        public static SnapshotSource Snapshot(string path)
        {
            return new(path);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">An FMOD Studio event or snapshot source addressed by path.</para>
    ///     <para xml:lang="zh-CN">通过路径寻址的 FMOD Studio 事件或快照源。</para>
    /// </summary>
    /// <param name="Path">
    ///     <para xml:lang="en">The wrapped FMOD Studio path.</para>
    ///     <para xml:lang="zh-CN">已包装的 FMOD Studio 路径。</para>
    /// </param>
    public sealed record StudioEventSource(FmodEventPath Path) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">An FMOD Studio event source addressed by GUID string.</para>
    ///     <para xml:lang="zh-CN">通过 GUID 字符串寻址的 FMOD Studio 事件源。</para>
    /// </summary>
    /// <param name="Value">
    ///     <para xml:lang="en">The GUID string normalized when playback is requested.</para>
    ///     <para xml:lang="zh-CN">请求播放时规范化的 GUID 字符串。</para>
    /// </param>
    public sealed record StudioGuidSource(string Value) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">A non-streaming sound source backed by a loose audio file.</para>
    ///     <para xml:lang="zh-CN">由松散音频文件支撑的非流式声音源。</para>
    /// </summary>
    /// <param name="AbsolutePath">
    ///     <para xml:lang="en">The filesystem or raw Godot file path resolved during playback.</para>
    ///     <para xml:lang="zh-CN">播放时解析的文件系统路径或 Godot 原始文件路径。</para>
    /// </param>
    public sealed record SoundFileSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">A non-streaming sound source backed by a packed or imported Godot audio resource.</para>
    ///     <para xml:lang="zh-CN">由打包或导入的 Godot 音频资源支撑的非流式声音源。</para>
    /// </summary>
    /// <param name="ResourcePath">
    ///     <para xml:lang="en">The Godot resource path materialized during playback.</para>
    ///     <para xml:lang="zh-CN">播放时物化的 Godot 资源路径。</para>
    /// </param>
    public sealed record ResourceSoundFileSource(string ResourcePath) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">A streaming music source backed by a loose audio file.</para>
    ///     <para xml:lang="zh-CN">由松散音频文件支撑的流式音乐源。</para>
    /// </summary>
    /// <param name="AbsolutePath">
    ///     <para xml:lang="en">The filesystem or raw Godot file path resolved during playback.</para>
    ///     <para xml:lang="zh-CN">播放时解析的文件系统路径或 Godot 原始文件路径。</para>
    /// </param>
    public sealed record StreamingMusicSource(string AbsolutePath) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">A streaming music source backed by a packed or imported Godot audio resource.</para>
    ///     <para xml:lang="zh-CN">由打包或导入的 Godot 音频资源支撑的流式音乐源。</para>
    /// </summary>
    /// <param name="ResourcePath">
    ///     <para xml:lang="en">The Godot resource path materialized during playback.</para>
    ///     <para xml:lang="zh-CN">播放时物化的 Godot 资源路径。</para>
    /// </param>
    public sealed record StreamingResourceMusicSource(string ResourcePath) : AudioSource;

    /// <summary>
    ///     <para xml:lang="en">An FMOD Studio snapshot source addressed by path.</para>
    ///     <para xml:lang="zh-CN">通过路径寻址的 FMOD Studio 快照源。</para>
    /// </summary>
    /// <param name="Path">
    ///     <para xml:lang="en">The FMOD Studio snapshot path.</para>
    ///     <para xml:lang="zh-CN">FMOD Studio 快照路径。</para>
    /// </param>
    public sealed record SnapshotSource(string Path) : AudioSource;
}
