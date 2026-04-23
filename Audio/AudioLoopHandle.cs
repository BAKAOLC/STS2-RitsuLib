using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Handle for loop-oriented playback.
    /// </summary>
    public sealed class AudioLoopHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
        /// <summary>
        ///     Restarts the loop by stopping and playing it again.
        /// </summary>
        public bool TryRestart()
        {
            return TryStop() && TryPlay();
        }
    }
}
