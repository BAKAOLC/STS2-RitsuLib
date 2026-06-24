using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Mixer snapshots (e.g. pause ducking) as Studio event instances.
    ///     作为 Studio 事件实例的 mixer snapshot（例如暂停 ducking）。
    /// </summary>
    public static class FmodStudioSnapshots
    {
        /// <summary>
        ///     Creates, starts, and wraps a snapshot instance in a typed handle.
        ///     创建并启动 snapshot 实例，然后将其包装在类型化句柄中。
        /// </summary>
        public static AudioSnapshotHandle? TryStartHandle(string snapshotPath, AudioPlaybackOptions? options = null)
        {
            options ??= new();
            var instance = TryStart(snapshotPath);
            return instance is null
                ? null
                : new AudioSnapshotHandle(AudioSource.Snapshot(snapshotPath),
                    options.ScopeToken?.Scope ?? options.Scope, instance);
        }

        /// <summary>
        ///     Creates and starts a snapshot instance. Caller must <see cref="StopAndRelease" /> when done.
        ///     创建并启动 snapshot 实例。调用方完成后必须调用 <see cref="StopAndRelease" />。
        /// </summary>
        public static GodotObject? TryStart(string snapshotPath)
        {
            var instance = FmodStudioEventInstances.TryCreate(snapshotPath);
            if (instance is null)
                return null;

            return FmodStudioEventInstances.TryStart(instance) ? instance : null;
        }

        /// <summary>
        ///     Same as <see cref="TryStart" />, but uses a snapshot event GUID instead of a path.
        ///     与 <see cref="TryStart" /> 相同，但使用 snapshot 事件 GUID 而不是路径。
        /// </summary>
        public static GodotObject? TryStartFromGuid(string snapshotEventGuid)
        {
            var instance = FmodStudioEventInstances.TryCreateFromGuid(snapshotEventGuid);
            if (instance is null)
                return null;

            return FmodStudioEventInstances.TryStart(instance) ? instance : null;
        }

        /// <summary>
        ///     Stops then releases <paramref name="snapshotInstance" />; no-op when null.
        ///     停止并释放 <paramref name="snapshotInstance" />；为 null 时不执行操作。
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
