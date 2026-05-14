using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Coordinates run sidecar cache epochs and disk cleanup aligned with vanilla run and save lifecycle. Sidecar
    ///     files are client-local and never mutate vanilla <see cref="SerializableRun" /> network payloads.
    ///     协调跑局 sidecar 缓存纪元和磁盘清理，使其与原版跑局和存档生命周期对齐。sidecar 文件仅存在于客户端本地，绝不会修改原版 <see cref="SerializableRun" /> 网络载荷。
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="RunStartedEvent" /> / <see cref="RunLoadedEvent" />: bump <see cref="RunEpoch" />
    ///                 (same moments as <see cref="STS2RitsuLib.Lifecycle.Patches.RunLifecyclePatch" />).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="RunEndedEvent" />: delete the ended run’s sidecar folder, then bump epoch (covers
    ///                 <c>ShouldSave == false</c> where vanilla may keep the run save file).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="SaveManager.DeleteCurrentRun" /> / <see cref="SaveManager.DeleteCurrentMultiplayerRun" />:
    ///                 sidecar folder is removed in a Harmony prefix while the save file still exists (main-menu
    ///                 abandon, normal run end after save deletion, etc.); see
    ///                 <see cref="STS2RitsuLib.Settings.RunSidecar.Patches.ModRunSidecarSaveDeletionPatches" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="ProfileSwitchedEvent" />: bump epoch. <see cref="ProfileDeletedEvent" />: delete the
    ///                 entire <c>run_sidecar</c> subtree for that profile.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="RunStartedEvent" /> / <see cref="RunLoadedEvent" />：提升 <see cref="RunEpoch" />
    ///                 （与 <see cref="STS2RitsuLib.Lifecycle.Patches.RunLifecyclePatch" /> 相同的时机）。
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="RunEndedEvent" />：删除已结束跑局的 sidecar 文件夹，然后提升纪元（覆盖
    ///                 原版可能保留跑局存档文件的 <c>ShouldSave == false</c> 情况）。
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="SaveManager.DeleteCurrentRun" /> / <see cref="SaveManager.DeleteCurrentMultiplayerRun" />：
    ///                 在存档文件仍存在时，于 Harmony prefix 中移除 sidecar 文件夹（主菜单
    ///                 放弃、存档删除后的正常跑局结束等）；见
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="ProfileSwitchedEvent" />：提升纪元。<see cref="ProfileDeletedEvent" />：删除该档案的
    ///                 整个 <c>run_sidecar</c> 子树。
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static class ModRunSidecarSession
    {
        private static int _runEpoch;
        private static readonly Lock InitLock = new();
        private static bool _handlersAttached;

        /// <summary>
        ///     Incremented whenever the active profile or run instance changes; bindings use it to drop stale caches.
        ///     每当活动档案或跑局实例变化时递增；binding 用它丢弃过期缓存。
        /// </summary>
        public static int RunEpoch => Volatile.Read(ref _runEpoch);

        internal static void AttachLifecycleHandlers()
        {
            lock (InitLock)
            {
                if (_handlersAttached)
                    return;

                _handlersAttached = true;
                RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => BumpRunEpoch());
                RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => BumpRunEpoch());
                RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(OnRunEnded);
                RitsuLibFramework.SubscribeLifecycle<ProfileSwitchedEvent>(_ => BumpRunEpoch());
                RitsuLibFramework.SubscribeLifecycle<ProfileDeletedEvent>(OnProfileDeleted);
            }
        }

        private static void BumpRunEpoch()
        {
            Interlocked.Increment(ref _runEpoch);
        }

        private static void OnRunEnded(RunEndedEvent evt)
        {
            try
            {
                var netId = RunManager.Instance?.NetService?.NetId ?? 0UL;
                var fingerprint = ModRunSidecarFingerprint.FromSerializableRun(evt.Run, netId);
                ModRunSidecarStore.TryDeleteRunDirectoryForFingerprint(fingerprint);
            }
            catch
            {
                // best-effort: run teardown must not throw
            }

            BumpRunEpoch();
        }

        private static void OnProfileDeleted(ProfileDeletedEvent evt)
        {
            BumpRunEpoch();
            ModRunSidecarStore.TryDeleteAllForProfile(evt.ProfileId);
        }
    }
}
