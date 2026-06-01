using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Base implementation for typed audio handles backed by a Godot object.
    ///     由 Godot 对象支撑的类型化音频句柄基础实现。
    /// </summary>
    public abstract class AudioHandleBase : IAudioHandle
    {
        private bool _disposed;

        /// <summary>
        ///     Initializes a typed audio handle around an existing Godot-backed audio instance.
        ///     围绕现有 Godot 支撑的音频实例初始化类型化音频句柄。
        /// </summary>
        protected AudioHandleBase(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        {
            Source = source;
            Scope = scope;
            RawInstance = rawInstance;
        }

        /// <summary>
        ///     Source that created this handle.
        ///     创建此句柄的来源。
        /// </summary>
        public AudioSource Source { get; }

        /// <summary>
        ///     Scope associated with this handle.
        ///     与此句柄关联的作用域。
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True when the underlying instance is still available.
        ///     底层实例仍可用时为 true。
        /// </summary>
        public bool IsValid => !IsReleased && RawInstance is not null;

        /// <summary>
        ///     True after native resources have been released.
        ///     原生资源释放后为 true。
        /// </summary>
        public bool IsReleased { get; private set; }

        /// <summary>
        ///     Raw Godot object for advanced scenarios.
        ///     高级场景使用的原始 Godot 对象。
        /// </summary>
        public GodotObject? RawInstance { get; protected set; }

        /// <summary>
        ///     Starts playback.
        ///     开始播放。
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
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle play: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Stops playback.
        ///     停止播放。
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
                    RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle stop: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Pauses playback when supported.
        ///     在支持时暂停播放。
        /// </summary>
        public virtual bool TryPause()
        {
            return TrySetPaused(true);
        }

        /// <summary>
        ///     Resumes playback when supported.
        ///     在支持时恢复播放。
        /// </summary>
        public virtual bool TryResume()
        {
            return TrySetPaused(false);
        }

        /// <summary>
        ///     Sets per-instance volume when supported.
        ///     在支持时设置实例级音量。
        /// </summary>
        public virtual bool TrySetVolume(float volume)
        {
            return TryCall("set_volume", volume);
        }

        /// <summary>
        ///     Sets per-instance pitch when supported.
        ///     在支持时设置实例级音高。
        /// </summary>
        public virtual bool TrySetPitch(float pitch)
        {
            return TryCall("set_pitch", pitch);
        }

        /// <summary>
        ///     Sets a numeric FMOD parameter by name when supported.
        ///     在支持时按名称设置数值型 FMOD 参数。
        /// </summary>
        public virtual bool TrySetParameter(string name, float value)
        {
            return TryCall("set_parameter_by_name", name, value);
        }

        /// <summary>
        ///     Releases native resources.
        ///     释放原生资源。
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
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle release: {ex.Message}");
                return false;
            }

            IsReleased = true;
            RawInstance = null;
            return true;
        }

        /// <summary>
        ///     Stops playback, releases resources, and detaches registry ownership.
        ///     停止播放、释放资源并解除注册表所有权。
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
        ///     在支持时设置暂停状态。
        /// </summary>
        protected bool TrySetPaused(bool paused)
        {
            return TryCall("set_paused", paused);
        }

        /// <summary>
        ///     Calls a raw method on the underlying Godot object.
        ///     在底层 Godot 对象上调用原始方法。
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
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle {method}: {ex.Message}");
                return false;
            }
        }
    }
}
