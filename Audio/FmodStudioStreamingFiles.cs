using System.Collections.Concurrent;
using Godot;
using STS2RitsuLib.Audio.Internal;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Load loose audio files into the FMOD runtime (wav/ogg/mp3 per addon). Tracks loaded paths so you can unload
    ///     deterministically.
    /// </summary>
    public static class FmodStudioStreamingFiles
    {
        private static readonly ConcurrentDictionary<string, LoadedKind> Loaded = new(StringComparer.Ordinal);

        /// <summary>
        ///     Preloads <paramref name="absoluteOrResPath" /> as a sound; succeeds immediately if already tracked.
        /// </summary>
        public static bool TryPreloadAsSound(string absoluteOrResPath)
        {
            if (Loaded.ContainsKey(absoluteOrResPath))
                return true;

            if (!FmodStudioGateway.TryCall(FmodStudioMethodNames.LoadFileAsSound, absoluteOrResPath))
                return false;

            Loaded[absoluteOrResPath] = LoadedKind.Sound;
            return true;
        }

        /// <summary>
        ///     Preloads <paramref name="absoluteOrResPath" /> as streaming music; succeeds immediately if already tracked.
        /// </summary>
        public static bool TryPreloadAsStreamingMusic(string absoluteOrResPath)
        {
            if (Loaded.ContainsKey(absoluteOrResPath))
                return true;

            if (!FmodStudioGateway.TryCall(FmodStudioMethodNames.LoadFileAsMusic, absoluteOrResPath))
                return false;

            Loaded[absoluteOrResPath] = LoadedKind.MusicStream;
            return true;
        }

        /// <summary>
        ///     Returns a playable sound instance, preloading as sound when needed.
        /// </summary>
        public static GodotObject? TryCreateSoundInstance(string absoluteOrResPath)
        {
            if (Loaded.ContainsKey(absoluteOrResPath))
                return !FmodStudioGateway.TryCall(out var record, FmodStudioMethodNames.CreateSoundInstance,
                    absoluteOrResPath)
                    ? null
                    : record.AsGodotObject();
            if (!TryPreloadAsSound(absoluteOrResPath))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateSoundInstance, absoluteOrResPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Returns a streaming music instance, preloading as music when needed.
        /// </summary>
        public static GodotObject? TryCreateStreamingMusicInstance(string absoluteOrResPath)
        {
            if (Loaded.ContainsKey(absoluteOrResPath))
                return !FmodStudioGateway.TryCall(out var record, FmodStudioMethodNames.CreateSoundInstance,
                    absoluteOrResPath)
                    ? null
                    : record.AsGodotObject();
            if (!TryPreloadAsStreamingMusic(absoluteOrResPath))
                return null;

            return !FmodStudioGateway.TryCall(out var v, FmodStudioMethodNames.CreateSoundInstance, absoluteOrResPath)
                ? null
                : v.AsGodotObject();
        }

        /// <summary>
        ///     Creates a sound instance and calls <c>play</c> with optional volume and pitch.
        /// </summary>
        public static bool TryPlaySoundFile(string absoluteOrResPath, float volume = 1f, float pitch = 1f)
        {
            var sound = TryCreateSoundInstance(absoluteOrResPath);
            if (sound is null)
                return false;

            try
            {
                if (Mathf.IsEqualApprox(volume, 1f))
                    sound.Call("set_volume", volume);

                if (Mathf.IsEqualApprox(pitch, 1f))
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
        public static bool TryUnloadFile(string absoluteOrResPath)
        {
            return !Loaded.TryRemove(absoluteOrResPath, out _) ||
                   FmodStudioGateway.TryCall(FmodStudioMethodNames.UnloadFile, absoluteOrResPath);
        }

        /// <summary>
        ///     Unloads every path currently tracked by this helper.
        /// </summary>
        public static void TryUnloadAllTracked()
        {
            foreach (var key in Loaded.Keys.ToArray())
                TryUnloadFile(key);
        }

        private enum LoadedKind : byte
        {
            Sound = 1,
            MusicStream = 2,
        }
    }
}
