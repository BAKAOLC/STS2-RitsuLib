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
        ///     Accepts <c>res://</c> only when the path is a raw file for <see cref="FileAccess" />, absolute paths, and
        ///     <c>user://</c> (globalized).
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
        ///     Accepts <c>res://</c> only when the path is a raw file for <see cref="FileAccess" />, absolute paths, and
        ///     <c>user://</c> (globalized).
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
            if (!TryResolveSupportedPath(absolutePath, out var resolvedPath))
                return false;

            return !Loaded.TryRemove(resolvedPath, out _) ||
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadFile, resolvedPath);
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

            if (path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = ProjectSettings.GlobalizePath(path);
                if (!Path.IsPathRooted(resolvedPath))
                {
                    RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback requires an absolute path: {path}");
                    return false;
                }

                if (File.Exists(resolvedPath)) return true;
                RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback file not found: {resolvedPath}");
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

                RitsuLibFramework.Logger.Error($"[Audio] FMOD file playback file not found: {path}");
                return false;
            }

            resolvedPath = path;
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
