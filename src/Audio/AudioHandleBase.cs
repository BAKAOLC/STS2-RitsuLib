using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Base implementation for typed audio handles backed by FMOD objects available through Godot.</para>
    ///     <para xml:lang="zh-CN">由 Godot 提供的 FMOD 对象支撑的类型化音频句柄基础实现。</para>
    /// </summary>
    public abstract class AudioHandleBase : IAudioHandle
    {
        private bool _disposed;

        /// <summary>
        ///     <para xml:lang="en">Initializes a typed handle around an existing backend instance.</para>
        ///     <para xml:lang="zh-CN">围绕现有后端实例初始化类型化句柄。</para>
        /// </summary>
        /// <param name="source">
        ///     <para xml:lang="en">The logical source represented by the backend instance.</para>
        ///     <para xml:lang="zh-CN">后端实例所代表的逻辑音频源。</para>
        /// </param>
        /// <param name="scope">
        ///     <para xml:lang="en">The lifecycle scope associated with the handle.</para>
        ///     <para xml:lang="zh-CN">与句柄关联的生命周期作用域。</para>
        /// </param>
        /// <param name="rawInstance">
        ///     <para xml:lang="en">The underlying FMOD Godot object, or null for an invalid handle.</para>
        ///     <para xml:lang="zh-CN">底层 FMOD Godot 对象；为 <see langword="null" /> 时句柄无效。</para>
        /// </param>
        protected AudioHandleBase(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        {
            Source = source;
            Scope = scope;
            RawInstance = rawInstance;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the logical source represented by this handle.</para>
        ///     <para xml:lang="zh-CN">获取此句柄所代表的逻辑音频源。</para>
        /// </summary>
        public AudioSource Source { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the lifecycle scope associated with this handle.</para>
        ///     <para xml:lang="zh-CN">获取与此句柄关联的生命周期作用域。</para>
        /// </summary>
        public AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the handle is unreleased and its backend object reports a usable instance.</para>
        ///     <para xml:lang="zh-CN">获取句柄是否尚未释放，且其后端对象是否报告实例可用。</para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (IsReleased || RawInstance is null || !GodotObject.IsInstanceValid(RawInstance))
                    return false;

                if (!RawInstance.HasMethod("is_valid"))
                    return true;

                try
                {
                    return RawInstance.Call("is_valid").AsBool();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets whether backend release has completed or the backend object was already invalid.</para>
        ///     <para xml:lang="zh-CN">获取后端释放是否已完成，或后端对象是否原本已经无效。</para>
        /// </summary>
        public bool IsReleased { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the backend Godot object for advanced interop, or null after release.</para>
        ///     <para xml:lang="zh-CN">获取用于高级互操作的后端 Godot 对象；释放后为 <see langword="null" />。</para>
        /// </summary>
        public GodotObject? RawInstance { get; protected set; }

        /// <summary>
        ///     <para xml:lang="en">Starts playback through the backend's supported <c>play</c> or <c>start</c> method.</para>
        ///     <para xml:lang="zh-CN">通过后端支持的 <c>play</c> 或 <c>start</c> 方法开始播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when a supported start method completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">受支持的启动方法执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TryPlay()
        {
            if (!TryGetInstance(out var instance))
                return false;

            var method = instance.HasMethod("play") ? "play" : "start";
            return TryCall(method);
        }

        /// <summary>
        ///     <para xml:lang="en">Stops playback using the call shape supported by the backend instance.</para>
        ///     <para xml:lang="zh-CN">使用后端实例支持的调用形式停止播放。</para>
        /// </summary>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether an FMOD event instance may fade out; file-backed instances use their parameterless stop method.</para>
        ///     <para xml:lang="zh-CN">FMOD 事件实例是否允许淡出；文件型实例使用其无参数停止方法。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the supported stop method completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">受支持的停止方法执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TryStop(bool allowFadeOut = true)
        {
            if (!TryGetInstance(out var instance))
                return false;

            return instance.HasMethod("start")
                ? TryCall("stop", allowFadeOut ? 0 : 1)
                : TryCall("stop");
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to pause playback through <c>set_paused</c>.</para>
        ///     <para xml:lang="zh-CN">尝试通过 <c>set_paused</c> 暂停播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TryPause()
        {
            return TrySetPaused(true);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to resume playback through <c>set_paused</c>.</para>
        ///     <para xml:lang="zh-CN">尝试通过 <c>set_paused</c> 恢复播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TryResume()
        {
            return TrySetPaused(false);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set the backend instance's linear volume value.</para>
        ///     <para xml:lang="zh-CN">尝试设置后端实例的线性音量值。</para>
        /// </summary>
        /// <param name="volume">
        ///     <para xml:lang="en">The volume value passed to the backend.</para>
        ///     <para xml:lang="zh-CN">传递给后端的音量值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TrySetVolume(float volume)
        {
            return TryCall("set_volume", volume);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set the backend instance's pitch multiplier.</para>
        ///     <para xml:lang="zh-CN">尝试设置后端实例的音高倍率。</para>
        /// </summary>
        /// <param name="pitch">
        ///     <para xml:lang="en">The pitch value passed to the backend.</para>
        ///     <para xml:lang="zh-CN">传递给后端的音高值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TrySetPitch(float pitch)
        {
            return TryCall("set_pitch", pitch);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set a numeric FMOD event parameter by name.</para>
        ///     <para xml:lang="zh-CN">尝试按名称设置数值型 FMOD 事件参数。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">The FMOD parameter name.</para>
        ///     <para xml:lang="zh-CN">FMOD 参数名称。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The parameter value.</para>
        ///     <para xml:lang="zh-CN">参数值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TrySetParameter(string name, float value)
        {
            return TryCall("set_parameter_by_name", name, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to release backend resources and detaches registry ownership only after release succeeds.</para>
        ///     <para xml:lang="zh-CN">尝试释放后端资源，并且仅在释放成功后解除注册表所有权。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when resources are released or the backend was already invalid; <see langword="false" /> when release is unsupported or fails.</para>
        ///     <para xml:lang="zh-CN">资源已释放或后端原本已无效时为 <see langword="true" />；不支持释放或释放失败时为 <see langword="false" />。</para>
        /// </returns>
        public virtual bool TryRelease()
        {
            if (IsReleased)
                return true;

            if (RawInstance is null || !GodotObject.IsInstanceValid(RawInstance))
            {
                CompleteRelease();
                return true;
            }

            if (!RawInstance.HasMethod("release"))
                return false;

            try
            {
                RawInstance.Call("release");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle release: {ex}");
                return false;
            }

            CompleteRelease();
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to stop and release playback. A release failure keeps the handle registered and allows a later
        ///         disposal call to retry.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试停止并释放播放；释放失败时保留句柄注册关系，并允许后续释放调用重试。</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            TryStop(AllowFadeOutOnStop);
            if (!TryRelease())
            {
                _disposed = false;
                return;
            }

            AudioLifecycleRegistry.Shared.Detach(this);
            AudioChannelRegistry.Shared.Detach(this);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to set the backend's paused state.</para>
        ///     <para xml:lang="zh-CN">尝试设置后端的暂停状态。</para>
        /// </summary>
        /// <param name="paused">
        ///     <para xml:lang="en">Whether playback should be paused.</para>
        ///     <para xml:lang="zh-CN">播放是否应暂停。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        protected bool TrySetPaused(bool paused)
        {
            return TryCall("set_paused", paused);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to invoke a supported method on the backend Godot object.</para>
        ///     <para xml:lang="zh-CN">尝试调用后端 Godot 对象所支持的方法。</para>
        /// </summary>
        /// <param name="method">
        ///     <para xml:lang="en">The backend method name.</para>
        ///     <para xml:lang="zh-CN">后端方法名称。</para>
        /// </param>
        /// <param name="args">
        ///     <para xml:lang="en">The arguments passed to the backend method.</para>
        ///     <para xml:lang="zh-CN">传递给后端方法的参数。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the method exists and completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">方法存在且执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        protected bool TryCall(string method, params Variant[] args)
        {
            if (!TryGetInstance(out var instance) || !instance.HasMethod(method))
                return false;

            try
            {
                instance.Call(method, args);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[Audio] handle {method}: {ex}");
                return false;
            }
        }

        internal bool AllowFadeOutOnStop { get; set; } = true;

        private bool TryGetInstance(out GodotObject instance)
        {
            instance = RawInstance!;
            return IsValid;
        }

        private void CompleteRelease()
        {
            IsReleased = true;
            RawInstance = null;
            AudioLifecycleRegistry.Shared.Detach(this);
            AudioChannelRegistry.Shared.Detach(this);
        }
    }
}
