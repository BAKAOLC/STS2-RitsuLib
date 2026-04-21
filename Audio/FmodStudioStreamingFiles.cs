using System.Collections.Concurrent;
using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Load loose audio files into the FMOD runtime (wav/ogg/mp3 per addon) from absolute filesystem paths.
    ///     Rejects <c>res://</c>, but resolves <c>user://</c> to an absolute filesystem path first. Tracks loaded paths so
    ///     you can unload deterministically.
    /// </summary>
    public static class FmodStudioStreamingFiles
    {
        private static readonly ConcurrentDictionary<string, LoadedKind> Loaded = new(StringComparer.Ordinal);

        /// <summary>
        ///     Creates a typed handle for a short loose-file sound.
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
        ///     Creates a typed handle for a streaming loose-file music instance.
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
        ///     Preloads the loose audio file at <paramref name="absolutePath" /> as a sound; succeeds immediately if already
        ///     tracked.
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
        ///     Preloads the loose audio file at <paramref name="absolutePath" /> as streaming music; succeeds immediately if
        ///     already tracked.
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
        ///     Returns a playable sound instance for the loose audio file at <paramref name="absolutePath" />, preloading as sound
        ///     when needed.
        ///     Accepts absolute filesystem paths and resolves <c>user://</c> to an absolute filesystem path.
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
        ///     Returns a streaming music instance, preloading as music when needed.
        ///     Accepts absolute filesystem paths and resolves <c>user://</c> to an absolute filesystem path.
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
        ///     Creates a sound instance from an absolute filesystem path and calls <c>play</c> with volume and pitch.
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
                RitsuLibFramework.Logger.Error($"[Audio] FMOD play file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Unloads a tracked file from FMOD and removes it from the local registry.
        /// </summary>
        public static bool TryUnloadFile(string absolutePath)
        {
            return !Loaded.TryRemove(absolutePath, out _) ||
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadFile, absolutePath);
        }

        /// <summary>
        ///     Unloads every path currently tracked by this helper.
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
                RitsuLibFramework.Logger.Error("[Audio] FMOD file playback requires a non-empty path.");
                return false;
            }

            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback does not accept res:// paths: {path}");
                return false;
            }

            resolvedPath = path.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
                ? ProjectSettings.GlobalizePath(path)
                : path;

            if (!Path.IsPathRooted(resolvedPath))
            {
                RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback requires an absolute path: {path}");
                return false;
            }

            if (File.Exists(resolvedPath)) return true;
            RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback file not found: {resolvedPath}");
            return false;
        }

        private enum LoadedKind : byte
        {
            Sound = 1,
            MusicStream = 2,
        }
    }
}
