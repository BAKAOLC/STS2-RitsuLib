using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Strongly typed wrapper around a playable FMOD-backed object.
    /// </summary>
    public interface IAudioHandle : IDisposable
    {
        /// <summary>
        ///     Source that created this handle.
        /// </summary>
        AudioSource Source { get; }

        /// <summary>
        ///     Lifecycle scope currently associated with this handle.
        /// </summary>
        AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True when the underlying playback object is still available.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        ///     True after native resources have been released.
        /// </summary>
        bool IsReleased { get; }

        /// <summary>
        ///     Raw Godot object for advanced scenarios.
        /// </summary>
        GodotObject? RawInstance { get; }

        /// <summary>
        ///     Starts or resumes playback when supported.
        /// </summary>
        bool TryPlay();

        /// <summary>
        ///     Stops playback.
        /// </summary>
        bool TryStop(bool allowFadeOut = true);

        /// <summary>
        ///     Pauses playback when supported.
        /// </summary>
        bool TryPause();

        /// <summary>
        ///     Resumes playback when supported.
        /// </summary>
        bool TryResume();

        /// <summary>
        ///     Sets per-instance volume when supported.
        /// </summary>
        bool TrySetVolume(float volume);

        /// <summary>
        ///     Sets per-instance pitch when supported.
        /// </summary>
        bool TrySetPitch(float pitch);

        /// <summary>
        ///     Sets a numeric FMOD parameter by name when supported.
        /// </summary>
        bool TrySetParameter(string name, float value);

        /// <summary>
        ///     Releases native resources owned by this handle.
        /// </summary>
        bool TryRelease();
    }
}
