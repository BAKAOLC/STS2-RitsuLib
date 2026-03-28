using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Mixer snapshots (e.g. pause ducking) as Studio event instances.
    /// </summary>
    public static class FmodStudioSnapshots
    {
        /// <summary>
        ///     Creates and starts a snapshot instance. Caller must <see cref="StopAndRelease" /> when done.
        /// </summary>
        public static GodotObject? TryStart(string snapshotPath)
        {
            var instance = FmodStudioEventInstances.TryCreate(snapshotPath);
            if (instance is null)
                return null;

            return FmodStudioEventInstances.TryStart(instance) ? instance : null;
        }

        /// <summary>
        ///     Stops then releases <paramref name="snapshotInstance" />; no-op when null.
        /// </summary>
        public static void StopAndRelease(GodotObject? snapshotInstance, bool allowFadeOut = true)
        {
            if (snapshotInstance is null)
                return;

            FmodStudioEventInstances.TryStop(snapshotInstance, allowFadeOut);
            FmodStudioEventInstances.TryRelease(snapshotInstance);
        }
    }
}
