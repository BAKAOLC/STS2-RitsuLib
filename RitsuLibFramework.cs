using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Content;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Data;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Lifecycle.Patches;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Unlocks.Patches;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Patches;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Shared runtime bootstrap for the framework itself and for mods that reference it.
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static class RitsuLibFramework
    {
        private static readonly Lock SyncRoot = new();
        private static ModPatcher? _frameworkPatcher;
        private static bool _profileServicesInitialized;
        private static readonly List<ILifecycleObserver> LifecycleObservers = [];
        private static readonly Dictionary<Type, IFrameworkLifecycleEvent> ReplayableLifecycleEvents = [];

        static RitsuLibFramework()
        {
            Logger = CreateLogger(Const.ModId);
        }

        public static Logger Logger { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool IsActive { get; private set; }

        public static IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState = true)
        {
            ArgumentNullException.ThrowIfNull(observer);

            IFrameworkLifecycleEvent[] lifecycleSnapshot;

            lock (SyncRoot)
            {
                LifecycleObservers.Add(observer);
                lifecycleSnapshot = replayCurrentState
                    ? ReplayableLifecycleEvents.Values.OrderBy(evt => evt.OccurredAtUtc).ToArray()
                    : [];
            }

            foreach (var evt in lifecycleSnapshot)
                SafeNotify(observer, o => o.OnEvent(evt), evt.GetType().Name);

            return new FrameworkLifecycleSubscription(() =>
            {
                lock (SyncRoot)
                {
                    LifecycleObservers.Remove(observer);
                }
            });
        }

        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (IsInitialized)
                {
                    Logger.Debug("Framework already initialized, skipping duplicate initialization.");
                    return;
                }

                Logger = CreateLogger(Const.ModId);

                Logger.Info($"Framework ID: {Const.ModId}");
                Logger.Info($"Framework Name: {Const.Name}");
                Logger.Info($"Version: {Const.Version}");
                Logger.Info("Initializing shared framework...");
                PublishLifecycleEvent(
                    new FrameworkInitializingEvent(Const.ModId, Const.Version, DateTimeOffset.UtcNow),
                    nameof(FrameworkInitializingEvent)
                );

                try
                {
                    _frameworkPatcher = CreatePatcher(Const.ModId, "framework", "framework");

                    _frameworkPatcher.RegisterPatch<CoreInitializationLifecyclePatch>();
                    _frameworkPatcher.RegisterPatch<ModelRegistryLifecyclePatch>();
                    _frameworkPatcher.RegisterPatch<GameNodeLifecyclePatch>();
                    _frameworkPatcher.RegisterPatch<RunLifecyclePatch>();
                    _frameworkPatcher.RegisterPatch<RunEndedLifecyclePatch>();

                    _frameworkPatcher.RegisterPatch<ProfilePathInitializedPatch>();
                    _frameworkPatcher.RegisterPatch<ProfileDeletePatch>();

                    _frameworkPatcher.RegisterPatch<AllCharactersPatch>();
                    _frameworkPatcher.RegisterPatch<AllPowersPatch>();
                    _frameworkPatcher.RegisterPatch<AllOrbsPatch>();
                    _frameworkPatcher.RegisterPatch<AllSharedEventsPatch>();
                    _frameworkPatcher.RegisterPatch<AllEventsPatch>();
                    _frameworkPatcher.RegisterPatch<AllSharedAncientsPatch>();
                    _frameworkPatcher.RegisterPatch<AllAncientsPatch>();
                    _frameworkPatcher.RegisterPatch<DynamicActContentPatchBootstrap>();

                    _frameworkPatcher.RegisterPatch<CharacterUnlockFilterPatch>();
                    _frameworkPatcher.RegisterPatch<SharedAncientUnlockFilterPatch>();
                    _frameworkPatcher.RegisterPatch<CardUnlockFilterPatch>();
                    _frameworkPatcher.RegisterPatch<RelicUnlockFilterPatch>();
                    _frameworkPatcher.RegisterPatch<PotionUnlockFilterPatch>();
                    _frameworkPatcher.RegisterPatch<GeneratedRoomEventUnlockFilterPatch>();

                    if (!_frameworkPatcher.PatchAll())
                    {
                        Logger.Error("Framework initialization failed: critical framework patches failed.");
                        IsActive = false;
                        return;
                    }

                    IsInitialized = true;
                    IsActive = true;

                    var frameworkInitializedEvent = new FrameworkInitializedEvent(
                        Const.ModId,
                        IsActive,
                        DateTimeOffset.UtcNow
                    );

                    PublishLifecycleEvent(frameworkInitializedEvent, nameof(FrameworkInitializedEvent));

                    Logger.Info("Shared framework initialization complete.");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Framework initialization failed: {ex.Message}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                    IsActive = false;
                }
            }
        }

        public static void EnsureProfileServicesInitialized()
        {
            lock (SyncRoot)
            {
                if (_profileServicesInitialized)
                    return;

                PublishLifecycleEvent(
                    new ProfileServicesInitializingEvent(DateTimeOffset.UtcNow),
                    nameof(ProfileServicesInitializingEvent)
                );

                ProfileManager.Instance.Initialize();
                ModDataStore.InitializeAllProfileScoped();

                _profileServicesInitialized = true;

                var profileInitializedEvent = new ProfileServicesInitializedEvent(
                    ProfileManager.Instance.CurrentProfileId,
                    DateTimeOffset.UtcNow
                );

                PublishLifecycleEvent(profileInitializedEvent, nameof(ProfileServicesInitializedEvent));

                Logger.Debug("Profile-scoped framework services initialized.");
            }
        }

        public static IDisposable BeginModDataRegistration(string modId, bool initializeProfileIfReady = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return ModDataStore.For(modId).BeginRegistrationScope(initializeProfileIfReady);
        }

        public static ModDataStore GetDataStore(string modId)
        {
            return ModDataStore.For(modId);
        }

        public static ModContentRegistry GetContentRegistry(string modId)
        {
            return ModContentRegistry.For(modId);
        }

        public static ModKeywordRegistry GetKeywordRegistry(string modId)
        {
            return ModKeywordRegistry.For(modId);
        }

        public static ModTimelineRegistry GetTimelineRegistry(string modId)
        {
            return ModTimelineRegistry.For(modId);
        }

        public static ModUnlockRegistry GetUnlockRegistry(string modId)
        {
            return ModUnlockRegistry.For(modId);
        }

        public static Logger CreateLogger(string modId, LogType logType = LogType.Generic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId, logType);
        }

        public static ModPatcher CreatePatcher(
            string ownerModId,
            string patcherName,
            string? patcherLabel = null,
            LogType logType = LogType.Generic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);
            ArgumentException.ThrowIfNullOrWhiteSpace(patcherName);

            var logger = CreateLogger(ownerModId, logType);

            return new(
                $"{ownerModId}.{patcherName}",
                logger,
                patcherLabel ?? patcherName
            );
        }

        public static I18N CreateLocalization(
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);

            return new(
                instanceName,
                fileSystemFolders?.ToArray() ?? [],
                resourceFolders?.ToArray() ?? [],
                pckFolders?.ToArray() ?? [],
                resourceAssembly ?? Assembly.GetCallingAssembly()
            );
        }

        public static I18N CreateModLocalization(
            string modId,
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);

            var folders = fileSystemFolders?.ToArray() ?? [$"user://mod-configs/{modId}/localization"];
            return CreateLocalization(instanceName, folders, resourceFolders, pckFolders, resourceAssembly);
        }

        public static bool ApplyRequiredPatcher(ModPatcher patcher, Action disableMod, string? failureMessage = null)
        {
            ArgumentNullException.ThrowIfNull(patcher);
            ArgumentNullException.ThrowIfNull(disableMod);

            var success = patcher.PatchAll();
            if (success)
                return true;

            patcher.Logger.Error(
                failureMessage ?? $"Required patcher '{patcher.PatcherName}' failed. The mod will be disabled.");
            disableMod();
            return false;
        }

        internal static ModPatcher RequireFrameworkPatcher()
        {
            return _frameworkPatcher
                   ?? throw new InvalidOperationException("Framework patcher is not available yet.");
        }

        internal static void PublishLifecycleEvent(IFrameworkLifecycleEvent evt, string phase)
        {
            lock (SyncRoot)
            {
                switch (evt)
                {
                    case ProfileDataInvalidatedEvent:
                        ReplayableLifecycleEvents.Remove(typeof(ProfileDataReadyEvent));
                        break;
                    case IReplayableFrameworkLifecycleEvent:
                        ReplayableLifecycleEvents[evt.GetType()] = evt;
                        break;
                }
            }

            NotifyObservers(o => o.OnEvent(evt), phase);
        }

        private static void NotifyObservers(Action<ILifecycleObserver> notify, string phase)
        {
            ILifecycleObserver[] snapshot;

            lock (SyncRoot)
            {
                snapshot = LifecycleObservers.ToArray();
            }

            foreach (var observer in snapshot)
                SafeNotify(observer, notify, phase);
        }

        private static void SafeNotify(ILifecycleObserver observer, Action<ILifecycleObserver> notify, string phase)
        {
            try
            {
                notify(observer);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Lifecycle] Observer callback failed in {phase}: {ex.Message}");
            }
        }
    }
}
