using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Handle for active FMOD snapshots.
    ///     活动 FMOD snapshot 的句柄。
    /// </summary>
    public sealed class AudioSnapshotHandle(AudioSource source, AudioLifecycleScope scope, GodotObject? rawInstance)
        : AudioHandleBase(source, scope, rawInstance);
}
