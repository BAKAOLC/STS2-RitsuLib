using MegaCrit.Sts2.Core.Saves;

namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Coordinates run-bound sidecar cache epochs and profile cleanup. Sidecar files are client-local and never
    ///     mutate vanilla <see cref="MegaCrit.Sts2.Core.Saves.SerializableRun" /> network payloads.
    /// </summary>
    public static class ModRunSidecarSession
    {
        private static int _runEpoch;
        private static readonly Lock InitLock = new();
        private static bool _handlersAttached;

        /// <summary>
        ///     Incremented whenever the active profile or run instance changes; bindings use it to drop stale caches.
        /// </summary>
        public static int RunEpoch => Volatile.Read(ref _runEpoch);

        internal static void AttachLifecycleHandlers()
        {
            lock (InitLock)
            {
                if (_handlersAttached)
                    return;

                _handlersAttached = true;
                RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => BumpRunEpoch());
                RitsuLibFramework.SubscribeLifecycle<ProfileSwitchedEvent>(_ => BumpRunEpoch());
                RitsuLibFramework.SubscribeLifecycle<ProfileDeletedEvent>(OnProfileDeleted);
            }
        }

        internal static void NotifyRunLoadedFromSave(SerializableRun save)
        {
            _ = save;
            BumpRunEpoch();
        }

        internal static void NotifyFreshRunStarted()
        {
            BumpRunEpoch();
        }

        private static void BumpRunEpoch()
        {
            Interlocked.Increment(ref _runEpoch);
        }

        private static void OnProfileDeleted(ProfileDeletedEvent evt)
        {
            BumpRunEpoch();
            ModRunSidecarStore.TryDeleteAllForProfile(evt.ProfileId);
        }
    }
}
