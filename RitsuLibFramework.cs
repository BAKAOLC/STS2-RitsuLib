using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Cards.Costs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Shared runtime bootstrap for the framework itself and for mods that reference it.
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static partial class RitsuLibFramework
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<FrameworkPatcherArea, ModPatcher> FrameworkPatchersByArea = [];

        private static bool _profileServicesInitialized;
        private static ILifecycleObserver[] _lifecycleObservers = [];
        private static readonly ConcurrentDictionary<Type, object> LifecycleTopics = new();
        private static readonly Dictionary<Type, object> ReplayableLifecycleEvents = [];
        private static readonly HashSet<string> RegisteredScriptAssemblies = [];

        static RitsuLibFramework()
        {
            Logger = CreateLogger(Const.ModId);
        }

        public static Logger Logger { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool IsActive { get; private set; }

        public static bool HasRegisteredModSettings => ModSettingsRegistry.HasPages;

        public static IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState = true)
        {
            ArgumentNullException.ThrowIfNull(observer);

            IFrameworkLifecycleEvent[] lifecycleSnapshot;

            lock (SyncRoot)
            {
                _lifecycleObservers = AppendItem(_lifecycleObservers, observer);
                lifecycleSnapshot = replayCurrentState
                    ? ReplayableLifecycleEvents.Values
                        .Cast<IFrameworkLifecycleEvent>()
                        .OrderBy(evt => evt.OccurredAtUtc)
                        .ToArray()
                    : [];
            }

            foreach (var evt in lifecycleSnapshot)
                SafeNotify(observer, evt, evt.GetType().Name);

            return new FrameworkLifecycleSubscription(() =>
            {
                lock (SyncRoot)
                {
                    _lifecycleObservers = RemoveItem(_lifecycleObservers, observer);
                }
            });
        }

        public static IDisposable SubscribeLifecycle<TEvent>(Action<TEvent> handler, bool replayCurrentState = true)
            where TEvent : IFrameworkLifecycleEvent
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!LifecycleEventTypeCache<TEvent>.SupportsTypedDispatch)
                return SubscribeLifecycle(new DelegateLifecycleObserver<TEvent>(handler), replayCurrentState);

            object? replayEvent = null;
            var topic = GetLifecycleTopic<TEvent>();

            lock (SyncRoot)
            {
                topic.Add(handler);

                if (replayCurrentState)
                    ReplayableLifecycleEvents.TryGetValue(LifecycleEventTypeCache<TEvent>.EventType, out replayEvent);
            }

            if (replayEvent is TEvent typedReplayEvent)
                SafeNotify(handler, typedReplayEvent, LifecycleEventTypeCache<TEvent>.EventName);

            return new FrameworkLifecycleSubscription(() =>
            {
                lock (SyncRoot)
                {
                    topic.Remove(handler);
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
                ModTypeDiscoveryHub.EnsureBuiltInContributorsRegistered();
                RitsuLibSettingsStore.Initialize();
                RitsuLibModSettingsBootstrap.Initialize();
                PublishLifecycleEvent(
                    new FrameworkInitializingEvent(Const.ModId, Const.Version, DateTimeOffset.UtcNow),
                    nameof(FrameworkInitializingEvent)
                );

                try
                {
                    FrameworkPatchersByArea.Clear();
                    RegisterLifecyclePatches();
                    RegisterSettingsUiPatches();
                    RegisterContentAssetPatches();
                    RegisterCharacterAssetPatches();
                    RegisterContentRegistryPatches();
                    RegisterPersistencePatches();
                    RegisterUnlockPatches();

                    if (!PatchAllRequired())
                    {
                        Logger.Error("Framework initialization failed: critical framework patches failed.");
                        IsActive = false;
                        return;
                    }

                    IsInitialized = true;
                    IsActive = true;

                    CustomCardPlayCostLedger.EnsureSubscribed();

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

        public static ModContentPackBuilder CreateContentPack(string modId)
        {
            return ModContentPackBuilder.For(modId);
        }

        public static void RegisterModSettings(string modId, Action<ModSettingsPageBuilder> configure,
            string? pageId = null)
        {
            ModSettingsRegistry.Register(modId, configure, pageId);
        }

        public static IReadOnlyList<ModSettingsPage> GetRegisteredModSettings()
        {
            return ModSettingsRegistry.GetPages();
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

        public static void EnsureGodotScriptsRegistered(Assembly assembly, Logger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();

            lock (SyncRoot)
            {
                if (!RegisteredScriptAssemblies.Add(assemblyName))
                    return;
            }

            try
            {
                var bridgeType = typeof(GodotObject).Assembly.GetType("Godot.Bridge.ScriptManagerBridge");
                var lookupMethod = bridgeType?.GetMethod(
                    "LookupScriptsInAssembly",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(Assembly)],
                    null);

                if (lookupMethod == null)
                {
                    logger?.Warn($"Godot script registration bridge not found for assembly {assemblyName}.");
                    return;
                }

                lookupMethod.Invoke(null, [assembly]);
                logger?.Debug($"Registered Godot C# scripts for assembly: {assemblyName}");
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to register Godot C# scripts for assembly {assemblyName}: {ex.Message}");
                logger?.Error($"Stack trace: {ex.StackTrace}");
            }
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

        internal static void PublishLifecycleEvent<TEvent>(TEvent evt, string phase)
            where TEvent : IFrameworkLifecycleEvent
        {
            var typedHandlers = Array.Empty<Action<TEvent>>();
            ILifecycleObserver[] observers;

            lock (SyncRoot)
            {
                if (LifecycleEventTypeCache<TEvent>.InvalidatesProfileDataReady)
                    ReplayableLifecycleEvents.Remove(typeof(ProfileDataReadyEvent));

                if (LifecycleEventTypeCache<TEvent>.IsReplayable)
                    ReplayableLifecycleEvents[LifecycleEventTypeCache<TEvent>.EventType] = evt;

                observers = _lifecycleObservers;
            }

            if (LifecycleEventTypeCache<TEvent>.SupportsTypedDispatch)
                typedHandlers = GetLifecycleTopic<TEvent>().ReadSnapshot();

            foreach (var handler in typedHandlers)
                SafeNotify(handler, evt, phase);

            foreach (var observer in observers)
                SafeNotify(observer, evt, phase);
        }

        private static T[] AppendItem<T>(T[] source, T item)
        {
            var result = new T[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[^1] = item;
            return result;
        }

        private static T[] RemoveItem<T>(T[] source, T item)
        {
            var index = Array.IndexOf(source, item);
            if (index < 0)
                return source;

            if (source.Length == 1)
                return [];

            var result = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, result, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, result, index, source.Length - index - 1);

            return result;
        }

        private static void SafeNotify<TEvent>(Action<TEvent> handler, TEvent evt, string phase)
            where TEvent : IFrameworkLifecycleEvent
        {
            try
            {
                handler(evt);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Lifecycle] Observer callback failed in {phase}: {ex.Message}");
            }
        }

        private static void SafeNotify<TEvent>(ILifecycleObserver observer, TEvent evt, string phase)
            where TEvent : IFrameworkLifecycleEvent
        {
            try
            {
                observer.OnEvent(evt);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Lifecycle] Observer callback failed in {phase}: {ex.Message}");
            }
        }

        private static LifecycleTopic<TEvent> GetLifecycleTopic<TEvent>()
            where TEvent : IFrameworkLifecycleEvent
        {
            return (LifecycleTopic<TEvent>)LifecycleTopics.GetOrAdd(
                LifecycleEventTypeCache<TEvent>.EventType,
                static _ => new LifecycleTopic<TEvent>()
            );
        }

        private static class LifecycleEventTypeCache<TEvent>
            where TEvent : IFrameworkLifecycleEvent
        {
            // ReSharper disable StaticMemberInGenericType
            public static readonly Type EventType = typeof(TEvent);
            public static readonly string EventName = EventType.Name;
            public static readonly bool SupportsTypedDispatch = EventType.IsValueType || EventType.IsSealed;

            public static readonly bool IsReplayable =
                typeof(IReplayableFrameworkLifecycleEvent).IsAssignableFrom(EventType);

            public static readonly bool InvalidatesProfileDataReady = EventType == typeof(ProfileDataInvalidatedEvent);
            // ReSharper restore StaticMemberInGenericType
        }

        private sealed class LifecycleTopic<TEvent>
            where TEvent : IFrameworkLifecycleEvent
        {
            private Action<TEvent>[] _handlers = [];

            public Action<TEvent>[] ReadSnapshot()
            {
                return Volatile.Read(ref _handlers);
            }

            public void Add(Action<TEvent> handler)
            {
                while (true)
                {
                    var snapshot = Volatile.Read(ref _handlers);
                    var updated = AppendItem(snapshot, handler);

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _handlers, updated, snapshot), snapshot))
                        return;
                }
            }

            public void Remove(Action<TEvent> handler)
            {
                while (true)
                {
                    var snapshot = Volatile.Read(ref _handlers);
                    var updated = RemoveItem(snapshot, handler);

                    if (ReferenceEquals(updated, snapshot))
                        return;

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _handlers, updated, snapshot), snapshot))
                        return;
                }
            }
        }
    }
}
