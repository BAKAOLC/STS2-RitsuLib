using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Strongly typed wrapper around a playable FMOD-backed object.
    ///     可播放 FMOD 支撑对象的强类型包装。
    /// </summary>
    public interface IAudioHandle : IDisposable
    {
        /// <summary>
        ///     Source that created this handle.
        ///     创建此句柄的来源。
        /// </summary>
        AudioSource Source { get; }

        /// <summary>
        ///     Lifecycle scope currently associated with this handle.
        ///     当前与此句柄关联的生命周期作用域。
        /// </summary>
        AudioLifecycleScope Scope { get; }

        /// <summary>
        ///     True when the underlying playback object is still available.
        ///     底层播放对象仍可用时为 true。
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        ///     True after native resources have been released.
        ///     原生资源释放后为 true。
        /// </summary>
        bool IsReleased { get; }

        /// <summary>
        ///     Raw Godot object for advanced scenarios.
        ///     高级场景使用的原始 Godot 对象。
        /// </summary>
        GodotObject? RawInstance { get; }

        /// <summary>
        ///     Starts or resumes playback when supported.
        ///     在支持时开始或恢复播放。
        /// </summary>
        bool TryPlay();

        /// <summary>
        ///     Stops playback.
        ///     停止播放。
        /// </summary>
        bool TryStop(bool allowFadeOut = true);

        /// <summary>
        ///     Pauses playback when supported.
        ///     在支持时暂停播放。
        /// </summary>
        bool TryPause();

        /// <summary>
        ///     Resumes playback when supported.
        ///     在支持时恢复播放。
        /// </summary>
        bool TryResume();

        /// <summary>
        ///     Sets per-instance volume when supported.
        ///     在支持时设置实例级音量。
        /// </summary>
        bool TrySetVolume(float volume);

        /// <summary>
        ///     Sets per-instance pitch when supported.
        ///     在支持时设置实例级音高。
        /// </summary>
        bool TrySetPitch(float pitch);

        /// <summary>
        ///     Sets a numeric FMOD parameter by name when supported.
        ///     在支持时按名称设置数值型 FMOD 参数。
        /// </summary>
        bool TrySetParameter(string name, float value);

        /// <summary>
        ///     Releases native resources owned by this handle.
        ///     释放此句柄拥有的原生资源。
        /// </summary>
        bool TryRelease();
    }
}
