using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Typed handle for FMOD Studio event instances.
    ///     FMOD Studio 事件实例的类型化句柄。
    /// </summary>
    public sealed class AudioEventHandle : AudioHandleBase
    {
        /// <summary>
        ///     Initializes a typed handle for an FMOD Studio event instance.
        ///     为 FMOD Studio 事件实例初始化类型化句柄。
        /// </summary>
        public AudioEventHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
            : base(source, scope, rawInstance)
        {
        }
    }
}
