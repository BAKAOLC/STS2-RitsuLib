using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Handle for loop-oriented playback.
    ///     面向循环播放的句柄。
    /// </summary>
    public sealed class AudioLoopHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
        /// <summary>
        ///     Restarts the loop by stopping and playing it again.
        ///     通过停止后重新播放来重启循环。
        /// </summary>
        public bool TryRestart()
        {
            return TryStop() && TryPlay();
        }
    }
}
