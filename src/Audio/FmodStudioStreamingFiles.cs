using System.Collections.Concurrent;
using Godot;
using STS2RitsuLib.Audio.Internal;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Loads loose audio files through the FMOD add-on, creates paused sound or streaming-music
    ///         instances, and tracks successfully loaded paths for deterministic unload.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过 FMOD 插件加载松散音频文件，创建初始暂停的音效或流式音乐实例，并跟踪成功加载的路径以便确定性卸载。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Accepted inputs are existing absolute paths, globalized <c>user://</c> paths, and <c>res://</c>
    ///         files visible to <see cref="FileAccess" />. Packed or imported Godot audio resources must use a
    ///         resource-specific method, which materializes WAV, Ogg Vorbis, or MP3 data into a private cache.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可接受的输入包括现有绝对路径、全局化后的 <c>user://</c> 路径，以及 <see cref="FileAccess" /> 可见的 <c>res://</c>
    ///         文件。打包或导入的 Godot 音频资源必须使用资源专用方法，将 WAV、Ogg Vorbis 或 MP3 数据写入私有缓存。
    ///     </para>
    /// </remarks>
    public static class FmodStudioStreamingFiles
    {
        private static readonly ConcurrentDictionary<string, LoadedKind> Loaded = new(StringComparer.Ordinal);
        private static readonly Lock LoadedGate = new();
        private static readonly StringName SetVolume = new("set_volume");
        private static readonly StringName SetPitch = new("set_pitch");
        private static readonly StringName Play = new("play");

        /// <summary>
        ///     <para xml:lang="en">Attempts to create a typed, initially paused sound handle from a supported loose-file path.</para>
        ///     <para xml:lang="zh-CN">尝试根据受支持的松散文件路径创建类型化且初始暂停的音效句柄。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The typed handle, or null when resolution, loading, or instance creation fails.</para>
        ///     <para xml:lang="zh-CN">类型化句柄；路径解析、加载或实例创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static AudioFileHandle? TryCreateSoundHandle(string absolutePath, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = TryCreateSoundInstance(absolutePath);
            return instance is null
                ? null
                : new AudioFileHandle(AudioSource.File(absolutePath), options.ScopeToken?.Scope ?? options.Scope,
                    instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a packed or imported Godot audio resource and create a typed, initially
        ///         paused sound handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将打包或导入的 Godot 音频资源写入缓存，并创建类型化且初始暂停的音效句柄。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The typed handle, or null when materialization, loading, or instance creation fails.</para>
        ///     <para xml:lang="zh-CN">类型化句柄；缓存写入、加载或实例创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static AudioFileHandle? TryCreateResourceSoundHandle(string resourcePath,
            AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = TryCreateResourceSoundInstance(resourcePath);
            return instance is null
                ? null
                : new AudioFileHandle(AudioSource.ResourceFile(resourcePath),
                    options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to create a typed, initially paused streaming-music handle from a supported loose-file
        ///         path.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试根据受支持的松散文件路径创建类型化且初始暂停的流式音乐句柄。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The typed music handle, or null when resolution, loading, or instance creation fails.</para>
        ///     <para xml:lang="zh-CN">类型化音乐句柄；路径解析、加载或实例创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static AudioMusicHandle? TryCreateStreamingMusicHandle(string absolutePath,
            AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = TryCreateStreamingMusicInstance(absolutePath);
            return instance is null
                ? null
                : new AudioMusicHandle(AudioSource.StreamingMusic(absolutePath),
                    options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a packed or imported Godot audio resource and create a typed, initially
        ///         paused streaming-music handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将打包或导入的 Godot 音频资源写入缓存，并创建类型化且初始暂停的流式音乐句柄。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">
        ///         Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new
        ///         handle.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The typed music handle, or null when materialization, loading, or instance creation fails.</para>
        ///     <para xml:lang="zh-CN">类型化音乐句柄；缓存写入、加载或实例创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static AudioMusicHandle? TryCreateResourceStreamingMusicHandle(string resourcePath,
            AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = TryCreateResourceStreamingMusicInstance(resourcePath);
            return instance is null
                ? null
                : new AudioMusicHandle(AudioSource.StreamingResourceMusic(resourcePath),
                    options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to load a supported loose file as a fully buffered sound and records its resolved
        ///         path.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将受支持的松散文件加载为完整缓冲的音效，并记录解析后的路径。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the same resolved path is already tracked as a sound or loading
        ///         returns a valid file object; otherwise <see langword="false" />. A path tracked as streaming music is rejected.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         同一解析路径已作为音效跟踪，或加载返回有效文件对象时为 <see langword="true" />；否则为 <see langword="false" />
        ///         。已作为流式音乐跟踪的路径会被拒绝。
        ///     </para>
        /// </returns>
        public static bool TryPreloadAsSound(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            lock (LoadedGate)
            {
                if (Loaded.TryGetValue(resolvedPath, out var loadedKind))
                    return CheckLoadedKind(resolvedPath, loadedKind, LoadedKind.Sound);

                if (!TryLoadFile(FmodStudioMethodNames.LoadFileAsSound, resolvedPath))
                    return false;

                Loaded[resolvedPath] = LoadedKind.Sound;
                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a Godot audio resource and preload its cached file as a fully buffered
        ///         sound.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，并将缓存文件预加载为完整缓冲的音效。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when materialization and sound preloading succeed; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">缓存写入和音效预加载均成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryPreloadResourceAsSound(string resourcePath)
        {
            return FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) &&
                   TryPreloadAsSound(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to load a supported loose file as streaming, looping music and records its resolved
        ///         path.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将受支持的松散文件加载为流式循环音乐，并记录解析后的路径。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the same resolved path is already tracked as streaming music or
        ///         loading returns a valid file object; otherwise <see langword="false" />. A path tracked as a sound is rejected.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         同一解析路径已作为流式音乐跟踪，或加载返回有效文件对象时为 <see langword="true" />；否则为 <see langword="false" />
        ///         。已作为音效跟踪的路径会被拒绝。
        ///     </para>
        /// </returns>
        public static bool TryPreloadAsStreamingMusic(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            lock (LoadedGate)
            {
                if (Loaded.TryGetValue(resolvedPath, out var loadedKind))
                    return CheckLoadedKind(resolvedPath, loadedKind, LoadedKind.MusicStream);

                if (!TryLoadFile(FmodStudioMethodNames.LoadFileAsMusic, resolvedPath))
                    return false;

                Loaded[resolvedPath] = LoadedKind.MusicStream;
                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a Godot audio resource and preload its cached file as streaming,
        ///         looping music.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，并将缓存文件预加载为流式循环音乐。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when materialization and streaming-music preloading succeed; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">缓存写入和流式音乐预加载均成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryPreloadResourceAsStreamingMusic(string resourcePath)
        {
            return FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) &&
                   TryPreloadAsStreamingMusic(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to preload a supported loose file as a sound and create an initially paused playback
        ///         instance.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将受支持的松散文件预加载为音效，并创建初始暂停的播放实例。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A valid paused sound instance, or null when resolution, preloading, or creation fails.</para>
        ///     <para xml:lang="zh-CN">有效的暂停音效实例；路径解析、预加载或创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryCreateSoundInstance(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return null;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!TryPreloadAsSound(resolvedPath))
                return null;

            return TryCreateLoadedInstance(resolvedPath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a Godot audio resource and create an initially paused sound instance
        ///         from its cached file.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，并根据缓存文件创建初始暂停的音效实例。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A valid paused sound instance, or null when materialization, preloading, or creation fails.</para>
        ///     <para xml:lang="zh-CN">有效的暂停音效实例；缓存写入、预加载或创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryCreateResourceSoundInstance(string resourcePath)
        {
            return !FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath)
                ? null
                : TryCreateSoundInstance(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to preload a supported loose file as streaming music and create an initially paused
        ///         playback instance.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将受支持的松散文件预加载为流式音乐，并创建初始暂停的播放实例。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A valid paused streaming-music instance, or null when resolution, preloading, or creation
        ///         fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">有效的暂停流式音乐实例；路径解析、预加载或创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryCreateStreamingMusicInstance(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return null;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!TryPreloadAsStreamingMusic(resolvedPath))
                return null;

            return TryCreateLoadedInstance(resolvedPath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a Godot audio resource and create an initially paused streaming-music
        ///         instance from its cached file.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，并根据缓存文件创建初始暂停的流式音乐实例。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A valid paused streaming-music instance, or null when materialization, preloading, or creation
        ///         fails.
        ///     </para>
        ///     <para xml:lang="zh-CN">有效的暂停流式音乐实例；缓存写入、预加载或创建失败时为 <see langword="null" />。</para>
        /// </returns>
        public static GodotObject? TryCreateResourceStreamingMusicInstance(string resourcePath)
        {
            return !FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath)
                ? null
                : TryCreateStreamingMusicInstance(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to create a loose-file sound instance, apply volume and pitch, and start playback.</para>
        ///     <para xml:lang="zh-CN">尝试创建松散文件音效实例，应用音量和音高并开始播放。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear volume passed to the sound instance.</para>
        ///     <para xml:lang="zh-CN">传递给音效实例的未钳制线性音量。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The unclamped pitch multiplier passed to the sound instance.</para>
        ///     <para xml:lang="zh-CN">传递给音效实例的未钳制音高倍率。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when creation, setup, and start complete; otherwise
        ///         <see langword="false" />. An instance that fails before start is stopped through its release method.
        ///     </para>
        ///     <para xml:lang="zh-CN">创建、设置和启动均完成时为 <see langword="true" />；否则为 <see langword="false" />。启动前失败的实例会通过其释放方法停止。</para>
        /// </returns>
        public static bool TryPlaySoundFile(string absolutePath, float volume = 1f, float pitch = 1f)
        {
            var sound = TryCreateSoundInstance(absolutePath);
            return TryConfigureAndPlay(sound, volume, pitch, "file");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to materialize a Godot audio resource, create a sound instance, apply volume and
        ///         pitch, and start playback.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，创建音效实例，应用音量和音高并开始播放。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <param name="volume">
        ///     <para xml:lang="en">The unclamped linear volume passed to the sound instance.</para>
        ///     <para xml:lang="zh-CN">传递给音效实例的未钳制线性音量。</para>
        /// </param>
        /// <param name="pitch">
        ///     <para xml:lang="en">The unclamped pitch multiplier passed to the sound instance.</para>
        ///     <para xml:lang="zh-CN">传递给音效实例的未钳制音高倍率。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when materialization, creation, setup, and start complete; otherwise
        ///         <see langword="false" />. An instance that fails before start is stopped through its release method.
        ///     </para>
        ///     <para xml:lang="zh-CN">缓存写入、创建、设置和启动均完成时为 <see langword="true" />；否则为 <see langword="false" />。启动前失败的实例会通过其释放方法停止。</para>
        /// </returns>
        public static bool TryPlayResourceSound(string resourcePath, float volume = 1f, float pitch = 1f)
        {
            var sound = TryCreateResourceSoundInstance(resourcePath);
            return TryConfigureAndPlay(sound, volume, pitch, "resource file");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to unload a resolved path from FMOD and removes its local tracking entry only after
        ///         invocation succeeds.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试从 FMOD 卸载解析后的路径，并且只在调用成功后移除本地跟踪条目。</para>
        /// </summary>
        /// <param name="absolutePath">
        ///     <para xml:lang="en">An absolute, <c>user://</c>, or raw <c>res://</c> audio path.</para>
        ///     <para xml:lang="zh-CN">绝对路径、<c>user://</c> 路径或原始 <c>res://</c> 音频路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the path is not tracked or unload invocation succeeds; otherwise
        ///         <see langword="false" />. Failed unloads remain tracked for retry.
        ///     </para>
        ///     <para xml:lang="zh-CN">路径未被跟踪或卸载调用成功时为 <see langword="true" />；否则为 <see langword="false" />。卸载失败的路径会继续保留以供重试。</para>
        /// </returns>
        public static bool TryUnloadFile(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            lock (LoadedGate)
            {
                if (!Loaded.ContainsKey(resolvedPath))
                    return true;

                if (!FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadFile, resolvedPath))
                    return false;

                Loaded.TryRemove(resolvedPath, out _);
                return true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to materialize a Godot audio resource and unload its cached file from FMOD.</para>
        ///     <para xml:lang="zh-CN">尝试将 Godot 音频资源写入缓存，并从 FMOD 卸载对应缓存文件。</para>
        /// </summary>
        /// <param name="resourcePath">
        ///     <para xml:lang="en">The <c>res://</c> or <c>user://</c> audio resource path.</para>
        ///     <para xml:lang="zh-CN"><c>res://</c> 或 <c>user://</c> 音频资源路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when materialization succeeds and the cached file is untracked or
        ///         unloads successfully; otherwise <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">缓存写入成功，且缓存文件未被跟踪或成功卸载时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryUnloadResourceFile(string resourcePath)
        {
            return FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) &&
                   TryUnloadFile(absolutePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to unload a snapshot of every path currently tracked by this helper.</para>
        ///     <para xml:lang="zh-CN">尝试卸载此辅助类当前跟踪的所有路径快照。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">Failed unloads and paths added concurrently after the snapshot remain tracked.</para>
        ///     <para xml:lang="zh-CN">卸载失败的路径，以及生成快照后并发加入的路径，仍会继续保留在跟踪表中。</para>
        /// </remarks>
        public static void TryUnloadAllTracked()
        {
            // The snapshot is intentionally taken outside LoadedGate so concurrently added paths remain tracked.
            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var key in Loaded.Keys.ToArray())
                TryUnloadFile(key);
        }

        private static bool TryResolveSupportedPath(string path, out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                RitsuLibFramework.Logger.ErrorNoTrace("[Audio] FMOD file playback requires a non-empty path.");
                return false;
            }

            if (path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = ProjectSettings.GlobalizePath(path);
                if (!Path.IsPathRooted(resolvedPath))
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[Audio] FMOD file playback requires an absolute path: {path}");
                    return false;
                }

                if (File.Exists(resolvedPath)) return true;
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD file playback file not found: {resolvedPath}");
                return false;
            }

            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            {
                if (FileAccess.FileExists(path))
                {
                    resolvedPath = path;
                    return true;
                }

                if (ResourceLoader.Exists(path))
                {
                    RitsuLibFramework.Logger.Warn(
                        "[Audio] FMOD file playback: path resolves only as imported/packed resource, not as a raw file for FileAccess. " +
                        "Avoid default import for assets you stream through FMOD: use the Import dock \"Keep File (No Import)\" " +
                        "(or ship a loose file / FMOD Studio bank). Path: " + path);
                    return false;
                }

                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD file playback file not found: {path}");
                return false;
            }

            resolvedPath = path;
            if (!Path.IsPathRooted(resolvedPath))
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD file playback requires an absolute path: {path}");
                return false;
            }

            if (File.Exists(resolvedPath)) return true;
            RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD file playback file not found: {resolvedPath}");
            return false;
        }

        private static bool CheckLoadedKind(string path, LoadedKind actual, LoadedKind requested)
        {
            if (actual == requested)
                return true;

            RitsuLibFramework.Logger.Warn(
                $"[Audio] FMOD file is already loaded as {actual} and cannot also be loaded as {requested}: {path}");
            return false;
        }

        private static GodotObject? TryCreateLoadedInstance(string resolvedPath)
        {
            if (!FmodStudioGateway.TryCall(out var value, FmodStudioMethodNames.CreateSoundInstance, resolvedPath))
                return null;

            if (value.VariantType != Variant.Type.Object)
                return null;

            var instance = value.AsGodotObject();
            return instance is not null && GodotObject.IsInstanceValid(instance) ? instance : null;
        }

        private static bool TryLoadFile(StringName method, string resolvedPath)
        {
            if (!FmodStudioGateway.TryCall(out var value, method, resolvedPath) ||
                value.VariantType != Variant.Type.Object)
                return false;

            var file = value.AsGodotObject();
            return file is not null && GodotObject.IsInstanceValid(file);
        }

        private static bool TryConfigureAndPlay(GodotObject? sound, float volume, float pitch, string sourceKind)
        {
            if (sound is null || !GodotObject.IsInstanceValid(sound))
                return false;

            var started = false;
            try
            {
                if (!sound.HasMethod(SetVolume) || !sound.HasMethod(SetPitch) || !sound.HasMethod(Play))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Audio] FMOD play {sourceKind}: sound instance is missing a required method.");
                    return false;
                }

                sound.Call(SetVolume, volume);
                sound.Call(SetPitch, pitch);
                sound.Call(Play);
                started = true;
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD play {sourceKind}: {ex}");
                return false;
            }
            finally
            {
                if (!started)
                    FmodStudioEventInstances.TryRelease(sound);
            }
        }

        private enum LoadedKind : byte
        {
            Sound = 1,
            MusicStream = 2,
        }
    }
}
