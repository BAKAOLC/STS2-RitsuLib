using Godot;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">Creates and controls FMOD Studio mixer snapshots represented by event instances, such as pause-menu ducking.</para>
    ///     <para xml:lang="zh-CN">创建并控制以事件实例表示的 FMOD Studio 混音器快照，例如暂停菜单的音量压低效果。</para>
    /// </summary>
    public static class FmodStudioSnapshots
    {
        /// <summary>
        ///     <para xml:lang="en">Attempts to create and start a path-based snapshot, then wraps it in a typed handle.</para>
        ///     <para xml:lang="zh-CN">尝试按路径创建并启动快照，然后将其包装为类型化句柄。</para>
        /// </summary>
        /// <param name="snapshotPath">
        ///     <para xml:lang="en">The FMOD Studio snapshot path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 快照路径。</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional playback metadata. Only its manual-token scope or lifecycle scope is copied to the new handle.</para>
        ///     <para xml:lang="zh-CN">可选的播放元数据。新句柄只会复制其中手动令牌的作用域或生命周期作用域。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The started snapshot handle, or null when creation or start fails.</para>
        ///     <para xml:lang="zh-CN">已启动的快照句柄；创建或启动失败时为 <see langword="null" />。</para>
        /// </returns>
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
        ///     <para xml:lang="en">Attempts to create and start a snapshot instance by path.</para>
        ///     <para xml:lang="zh-CN">尝试按路径创建并启动快照实例。</para>
        /// </summary>
        /// <param name="snapshotPath">
        ///     <para xml:lang="en">The FMOD Studio snapshot path.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 快照路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The started instance, or null when creation or start fails. The caller owns a successful instance and must pass it to <see cref="StopAndRelease" />.</para>
        ///     <para xml:lang="zh-CN">已启动的实例；创建或启动失败时为 <see langword="null" />。调用方拥有成功返回的实例，并且必须将其传给 <see cref="StopAndRelease" />。</para>
        /// </returns>
        public static GodotObject? TryStart(string snapshotPath)
        {
            var instance = FmodStudioEventInstances.TryCreate(snapshotPath);
            if (instance is null)
                return null;

            if (FmodStudioEventInstances.TryStart(instance))
                return instance;

            FmodStudioEventInstances.TryRelease(instance);
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to create and start a snapshot instance by event GUID.</para>
        ///     <para xml:lang="zh-CN">尝试按事件 GUID 创建并启动快照实例。</para>
        /// </summary>
        /// <param name="snapshotEventGuid">
        ///     <para xml:lang="en">The FMOD Studio snapshot event GUID.</para>
        ///     <para xml:lang="zh-CN">FMOD Studio 快照事件 GUID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The started instance, or null when normalization, creation, or start fails. The caller owns a successful instance and must pass it to <see cref="StopAndRelease" />.</para>
        ///     <para xml:lang="zh-CN">已启动的实例；规范化、创建或启动失败时为 <see langword="null" />。调用方拥有成功返回的实例，并且必须将其传给 <see cref="StopAndRelease" />。</para>
        /// </returns>
        public static GodotObject? TryStartFromGuid(string snapshotEventGuid)
        {
            var instance = FmodStudioEventInstances.TryCreateFromGuid(snapshotEventGuid);
            if (instance is null)
                return null;

            if (FmodStudioEventInstances.TryStart(instance))
                return instance;

            FmodStudioEventInstances.TryRelease(instance);
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to stop and then release a snapshot instance, retaining it when stop fails so the caller can retry.</para>
        ///     <para xml:lang="zh-CN">尝试停止并随后释放快照实例；停止失败时保留实例，以便调用方重试。</para>
        /// </summary>
        /// <param name="snapshotInstance">
        ///     <para xml:lang="en">The snapshot instance; null or invalid objects are ignored.</para>
        ///     <para xml:lang="zh-CN">快照实例；为 <see langword="null" /> 或无效时会被忽略。</para>
        /// </param>
        /// <param name="allowFadeOut">
        ///     <para xml:lang="en">Whether FMOD may apply the snapshot's fade-out when stopping.</para>
        ///     <para xml:lang="zh-CN">停止时是否允许 FMOD 应用快照的淡出效果。</para>
        /// </param>
        public static void StopAndRelease(GodotObject? snapshotInstance, bool allowFadeOut = true)
        {
            if (snapshotInstance is null || !GodotObject.IsInstanceValid(snapshotInstance))
                return;

            if (!FmodStudioEventInstances.TryStop(snapshotInstance, allowFadeOut))
            {
                RitsuLibFramework.Logger.Warn(
                    "[Audio] FMOD snapshot stop failed; release was skipped so the caller can retry.");
                return;
            }

            FmodStudioEventInstances.TryRelease(snapshotInstance);
        }
    }
}
