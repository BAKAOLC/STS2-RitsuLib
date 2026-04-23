using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Handle for active FMOD snapshots.
    /// </summary>
    public sealed class AudioSnapshotHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance)
    {
    }
}
