using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Typed handle for loose file and streaming-backed audio instances.
    ///     松散文件和流式音频实例的类型化句柄。
    /// </summary>
    public sealed class AudioFileHandle : AudioHandleBase
    {
        /// <summary>
        ///     Initializes a typed handle for a loose file or streaming audio instance.
        ///     为松散文件或流式音频实例初始化类型化句柄。
        /// </summary>
        public AudioFileHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
            : base(source, scope, rawInstance)
        {
        }
    }
}
