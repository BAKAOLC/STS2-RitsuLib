using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Base implementation for typed audio handles backed by a Godot object.
    /// </summary>
    public abstract class AudioHandleBase : IAudioHandle
    {
        private bool _disposed;

        /// <summary>
        ///     Initializes a typed audio handle around an existing Godot-backed audio instance.
        /// </summary>
        protected AudioHandleBase(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        {
            Source = source;
            Scope = scope;
            RawInstance = rawInstance;
        }

        /// <summary>
        ///     Source that created this handle.
        /// </summary>
        public AudioSource Source { get; }

        /// <summary>
        ///     Scope associated with this handle.
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True when the underlying instance is still available.
        /// </summary>
        public bool IsValid => !IsReleased && RawInstance is not null;

        /// <summary>
        ///     True after native resources have been released.
        /// </summary>
        public bool IsReleased { get; private set; }

        /// <summary>
        ///     Raw Godot object for advanced scenarios.
        /// </summary>
        public GodotObject? RawInstance { get; protected set; }

        /// <summary>
        ///     Starts playback.
        /// </summary>
        public virtual bool TryPlay()
        {
            if (RawInstance is null)
                return false;

            try
            {
                RawInstance.Call("play");
                return true;
            }
            catch
            {
                try
                {
                    RawInstance.Call("start");
                    return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] handle play: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Stops playback.
        /// </summary>
        public virtual bool TryStop(bool allowFadeOut = true)
        {
            if (RawInstance is null)
                return false;

            try
            {
                RawInstance.Call("stop", allowFadeOut ? 0 : 1);
                return true;
            }
            catch
            {
                try
                {
                    RawInstance.Call("stop");
                    return true;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Error($"[Audio] handle stop: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Pauses playback when supported.
        /// </summary>
        public virtual bool TryPause()
        {
            return TrySetPaused(true);
        }

        /// <summary>
        ///     Resumes playback when supported.
        /// </summary>
        public virtual bool TryResume()
        {
            return TrySetPaused(false);
        }

        /// <summary>
        ///     Sets per-instance volume when supported.
        /// </summary>
        public virtual bool TrySetVolume(float volume)
        {
            return TryCall("set_volume", volume);
        }

        /// <summary>
        ///     Sets per-instance pitch when supported.
        /// </summary>
        public virtual bool TrySetPitch(float pitch)
        {
            return TryCall("set_pitch", pitch);
        }

        /// <summary>
        ///     Sets a numeric FMOD parameter by name when supported.
        /// </summary>
        public virtual bool TrySetParameter(string name, float value)
        {
            return TryCall("set_parameter_by_name", name, value);
        }

        /// <summary>
        ///     Releases native resources.
        /// </summary>
        public virtual bool TryRelease()
        {
            if (IsReleased)
                return true;

            if (RawInstance is null)
            {
                IsReleased = true;
                return true;
            }

            try
            {
                RawInstance.Call("release");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] handle release: {ex.Message}");
                return false;
            }

            IsReleased = true;
            RawInstance = null;
            return true;
        }

        /// <summary>
        ///     Stops playback, releases resources, and detaches registry ownership.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            TryStop();
            TryRelease();
            AudioLifecycleRegistry.Shared.Detach(this);
            AudioChannelRegistry.Shared.Detach(this);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Sets paused state when supported.
        /// </summary>
        protected bool TrySetPaused(bool paused)
        {
            return TryCall("set_paused", paused);
        }

        /// <summary>
        ///     Calls a raw method on the underlying Godot object.
        /// </summary>
        protected bool TryCall(string method, params Variant[] args)
        {
            if (RawInstance is null)
                return false;

            try
            {
                RawInstance.Call(method, args);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Error($"[Audio] handle {method}: {ex.Message}");
                return false;
            }
        }
    }
}
