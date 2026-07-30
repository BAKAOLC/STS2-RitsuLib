using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Represents caller-controllable playback backed by an FMOD object available through Godot.</para>
    ///     <para xml:lang="zh-CN">表示由 Godot 提供的 FMOD 对象支撑、可由调用方控制的播放。</para>
    /// </summary>
    public interface IAudioHandle : IDisposable
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the logical source represented by this handle.</para>
        ///     <para xml:lang="zh-CN">获取此句柄所代表的逻辑音频源。</para>
        /// </summary>
        AudioSource Source { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the built-in lifecycle scope recorded on this handle.</para>
        ///     <para xml:lang="zh-CN">获取此句柄记录的内置生命周期作用域。</para>
        /// </summary>
        AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the handle is unreleased and its backend object reports a usable instance.</para>
        ///     <para xml:lang="zh-CN">获取句柄是否尚未释放，且其后端对象是否报告实例可用。</para>
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether backend release has completed or the backend object was already invalid.</para>
        ///     <para xml:lang="zh-CN">获取后端释放是否已完成，或后端对象是否原本已经无效。</para>
        /// </summary>
        bool IsReleased { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the backend Godot object for advanced interop, or null after release.</para>
        ///     <para xml:lang="zh-CN">获取用于高级互操作的后端 Godot 对象；释放后为 <see langword="null" />。</para>
        /// </summary>
        GodotObject? RawInstance { get; }

        /// <summary>
        ///     <para xml:lang="en">Attempts to start playback through the backend's supported <c>play</c> or <c>start</c> method.</para>
        ///     <para xml:lang="zh-CN">尝试通过后端支持的 <c>play</c> 或 <c>start</c> 方法开始播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when a supported start method completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">受支持的启动方法执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool TryPlay();

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop playback using the call shape supported by the backend instance.</para>
        ///     <para xml:lang="zh-CN">尝试使用后端实例支持的调用形式停止播放。</para>
        /// </summary>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether an FMOD event instance may fade out; file-backed instances ignore this value.</para>
        ///     <para xml:lang="zh-CN">FMOD 事件实例是否可以淡出；文件型实例会忽略此值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the supported stop method completes; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">受支持的停止方法执行完成时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool TryStop(bool allowFadeOut = true);

        /// <summary>
        ///     <para xml:lang="en">Attempts to pause playback through <c>set_paused</c>.</para>
        ///     <para xml:lang="zh-CN">尝试通过 <c>set_paused</c> 暂停播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool TryPause();

        /// <summary>
        ///     <para xml:lang="en">Attempts to resume playback through <c>set_paused</c>.</para>
        ///     <para xml:lang="zh-CN">尝试通过 <c>set_paused</c> 恢复播放。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the backend supports and completes the call; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">后端支持且完成该调用时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool TryResume();

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
        bool TrySetVolume(float volume);

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
        bool TrySetPitch(float pitch);

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
        bool TrySetParameter(string name, float value);

        /// <summary>
        ///     <para xml:lang="en">Attempts to release backend resources and detach registry ownership.</para>
        ///     <para xml:lang="zh-CN">尝试释放后端资源并解除注册表归属。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when resources are released or the backend was already invalid; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">资源已释放或后端原本已无效时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool TryRelease();
    }
}
