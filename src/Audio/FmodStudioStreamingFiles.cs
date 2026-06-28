using System.Collections.Concurrent;
using Godot;
using STS2RitsuLib.Audio.Internal;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Load loose audio files into the FMOD runtime (wav/ogg/mp3 per addon). For <c>res://</c>, only paths that are
    ///     still visible as raw files to <see cref="FileAccess" /> are accepted (e.g. Import dock &quot;Keep File (No Import)
    ///     &quot;).
    ///     Resolves <c>user://</c> to an absolute filesystem path. Tracks loaded paths so you can unload deterministically.
    ///     将松散音频文件加载到 FMOD runtime（按 addon 支持 wav/ogg/mp3）。对于 <c>res://</c>，只接受
    ///     对 <see cref="FileAccess" /> 仍可见的原始文件路径（例如 Import dock 的 &quot;Keep File (No Import)
    ///     &quot;）。
    ///     将 <c>user://</c> 解析为绝对文件系统路径。跟踪已加载路径，以便确定性卸载。
    /// </summary>
    public static class FmodStudioStreamingFiles
    {
        private static readonly ConcurrentDictionary<string, LoadedKind> Loaded = new(StringComparer.Ordinal);

        /// <summary>
        ///     Creates a typed handle for a short loose-file sound.
        ///     为短音效松散文件创建类型化句柄。
        /// </summary>
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
        ///     Creates a typed handle for a packed/imported Godot audio resource by materializing it to a private cache file.
        ///     将 Godot 打包/导入音频资源 materialize 到私有缓存文件后，为短音效创建类型化句柄。
        /// </summary>
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
        ///     Creates a typed handle for a streaming loose-file music instance.
        ///     为流式松散文件音乐实例创建类型化句柄。
        /// </summary>
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
        ///     Creates a typed music handle for a packed/imported Godot audio resource by materializing it to a private cache
        ///     file.
        ///     将 Godot 打包/导入音频资源 materialize 到私有缓存文件后，为流式音乐创建类型化句柄。
        /// </summary>
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
        ///     Preloads the loose audio file at <paramref name="absolutePath" /> as a sound; succeeds immediately if already
        ///     tracked.
        ///     将 <paramref name="absolutePath" /> 处的松散音频文件预加载为 sound；如果已
        ///     跟踪则立即成功。
        /// </summary>
        public static bool TryPreloadAsSound(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            if (Loaded.ContainsKey(resolvedPath))
                return true;

            if (!FmodStudioGateway.TryCall(FmodStudioMethodNames.LoadFileAsSound, resolvedPath))
                return false;

            Loaded[resolvedPath] = LoadedKind.Sound;
            return true;
        }

        /// <summary>
        ///     Materializes a packed/imported Godot audio resource and preloads it as a sound.
        ///     将 Godot 打包/导入音频资源 materialize 后预加载为 sound。
        /// </summary>
        public static bool TryPreloadResourceAsSound(string resourcePath)
        {
            return FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) &&
                   TryPreloadAsSound(absolutePath);
        }

        /// <summary>
        ///     Preloads the loose audio file at <paramref name="absolutePath" /> as streaming music; succeeds immediately if
        ///     already tracked.
        ///     将 <paramref name="absolutePath" /> 处的松散音频文件预加载为流式音乐；如果
        ///     已跟踪则立即成功。
        /// </summary>
        public static bool TryPreloadAsStreamingMusic(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            if (Loaded.ContainsKey(resolvedPath))
                return true;

            if (!FmodStudioGateway.TryCall(FmodStudioMethodNames.LoadFileAsMusic, resolvedPath))
                return false;

            Loaded[resolvedPath] = LoadedKind.MusicStream;
            return true;
        }

        /// <summary>
        ///     Materializes a packed/imported Godot audio resource and preloads it as streaming music.
        ///     将 Godot 打包/导入音频资源 materialize 后预加载为流式音乐。
        /// </summary>
        public static bool TryPreloadResourceAsStreamingMusic(string resourcePath)
        {
            return FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) &&
                   TryPreloadAsStreamingMusic(absolutePath);
        }

        /// <summary>
        ///     Returns a playable sound instance for the loose audio file at <paramref name="absolutePath" />, preloading as sound
        ///     when needed.
        ///     Accepts <c>res://</c> only when the path is a raw file for <see cref="FileAccess" />, absolute paths, and
        ///     <c>user://</c> (globalized).
        ///     返回 <paramref name="absolutePath" /> 处松散音频文件的可播放 sound 实例，必要时预加载为 sound。
        ///     仅当 <c>res://</c> 路径是 <see cref="FileAccess" /> 的原始文件时才接受，同时接受绝对路径和
        ///     <c>user://</c>（globalized）。
        /// </summary>
        public static GodotObject? TryCreateSoundInstance(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return null;

            if (Loaded.ContainsKey(resolvedPath))
                return !FmodStudioGateway.TryCall(out var record, FmodStudioMethodNames.CreateSoundInstance,
                    resolvedPath)
                    ? null
                    : record.AsGodotObject();
            if (!TryPreloadAsSound(resolvedPath))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateSoundInstance, resolvedPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Returns a playable sound instance for a packed/imported Godot audio resource.
        ///     返回 Godot 打包/导入音频资源的可播放 sound 实例。
        /// </summary>
        public static GodotObject? TryCreateResourceSoundInstance(string resourcePath)
        {
            return !FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath)
                ? null
                : TryCreateSoundInstance(absolutePath);
        }

        /// <summary>
        ///     Returns a streaming music instance, preloading as music when needed.
        ///     Accepts <c>res://</c> only when the path is a raw file for <see cref="FileAccess" />, absolute paths, and
        ///     <c>user://</c> (globalized).
        ///     返回流式音乐实例，必要时预加载为 music。
        ///     仅当 <c>res://</c> 路径是 <see cref="FileAccess" /> 的原始文件时才接受，同时接受绝对路径和
        ///     <c>user://</c>（globalized）。
        /// </summary>
        public static GodotObject? TryCreateStreamingMusicInstance(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return null;

            if (Loaded.ContainsKey(resolvedPath))
                return !FmodStudioGateway.TryCall(out var record, FmodStudioMethodNames.CreateSoundInstance,
                    resolvedPath)
                    ? null
                    : record.AsGodotObject();
            if (!TryPreloadAsStreamingMusic(resolvedPath))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateSoundInstance, resolvedPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Returns a streaming music instance for a packed/imported Godot audio resource.
        ///     返回 Godot 打包/导入音频资源的 streaming music 实例。
        /// </summary>
        public static GodotObject? TryCreateResourceStreamingMusicInstance(string resourcePath)
        {
            return !FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath)
                ? null
                : TryCreateStreamingMusicInstance(absolutePath);
        }

        /// <summary>
        ///     Creates a sound instance from an absolute filesystem path and calls <c>play</c> with volume and pitch.
        ///     从绝对文件系统路径创建 sound 实例，并用 volume 和 pitch 调用 <c>play</c>。
        /// </summary>
        public static bool TryPlaySoundFile(string absolutePath, float volume = 1f, float pitch = 1f)
        {
            var sound = TryCreateSoundInstance(absolutePath);
            if (sound is null)
                return false;

            try
            {
                sound.Call("set_volume", volume);
                sound.Call("set_pitch", pitch);
                sound.Call("play");
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD play file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Materializes a packed/imported Godot audio resource, creates a sound instance, and plays it.
        ///     将 Godot 打包/导入音频资源 materialize，创建 sound 实例并播放。
        /// </summary>
        public static bool TryPlayResourceSound(string resourcePath, float volume = 1f, float pitch = 1f)
        {
            var sound = TryCreateResourceSoundInstance(resourcePath);
            if (sound is null)
                return false;

            try
            {
                sound.Call("set_volume", volume);
                sound.Call("set_pitch", pitch);
                sound.Call("play");
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] FMOD play resource file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Unloads a tracked file from FMOD and removes it from the local registry.
        ///     从 FMOD 卸载已跟踪文件，并将其从本地注册表移除。
        /// </summary>
        public static bool TryUnloadFile(string absolutePath)
        {
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            return !Loaded.TryRemove(resolvedPath, out _) ||
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadFile, resolvedPath);
        }

        /// <summary>
        ///     Materializes a packed/imported resource path and unloads the cached file from FMOD if it is tracked.
        ///     将打包/导入资源路径 materialize，并在已跟踪时从 FMOD 卸载缓存文件。
        /// </summary>
        public static bool TryUnloadResourceFile(string resourcePath)
        {
            return !FmodPackedAudioResourceCache.TryMaterialize(resourcePath, out var absolutePath) ||
                   TryUnloadFile(absolutePath);
        }

        /// <summary>
        ///     Unloads every path currently tracked by this helper.
        ///     卸载此 helper 当前跟踪的每个路径。
        /// </summary>
        public static void TryUnloadAllTracked()
        {
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

        private enum LoadedKind : byte
        {
            Sound = 1,
            MusicStream = 2,
        }
    }
}
