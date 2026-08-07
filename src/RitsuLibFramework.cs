using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Cards.FreePlay;
using STS2RitsuLib.Cards.Transforms;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Combat.Healing;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.Diagnostics.CompendiumExport;
using STS2RitsuLib.Diagnostics.Logging;
using STS2RitsuLib.Graphics;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Localization;
using STS2RitsuLib.Localization.SmartFormat;
using STS2RitsuLib.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Compat;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Platform;
using STS2RitsuLib.Platform.Steam;
using STS2RitsuLib.RunData;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Search;
using STS2RitsuLib.Scaffolding.Ancients.Options;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Telemetry.Diagnostics;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.TopBar;
using STS2RitsuLib.Ui.Overlay;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Updates;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides shared runtime bootstrap and public integration entry points for RitsuLib and the mods that use it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 RitsuLib 及其使用的模组提供共享的运行时启动和公共集成入口。
    ///     </para>
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static partial class RitsuLibFramework
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<FrameworkPatcherArea, ModPatcher> FrameworkPatchersByArea = [];
        private static bool _frameworkPatchesApplied;
        private static bool _frameworkInteropBootstrapRegistered;

        private static bool _profileServicesInitialized;
        private static ILifecycleObserver[] _lifecycleObservers = [];
        private static readonly ConcurrentDictionary<Type, object> LifecycleTopics = new();
        private static readonly Dictionary<Type, object> ReplayableLifecycleEvents = [];
        private static readonly HashSet<string> RegisteredScriptAssemblies = [];
        private static readonly HashSet<string> RegisteringScriptAssemblies = [];

        private static readonly Lock DeferredContentPackSync = new();
        private static readonly List<DeferredContentPackRegistration> DeferredContentPackRegistrations = [];
        private static bool _deferredContentPacksFlushed;

        static RitsuLibFramework()
        {
            Logger = CreateLogger(Const.ModId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the framework logger.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取框架的日志记录器。
        ///     </para>
        /// </summary>
        public static Logger Logger { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether required framework patches completed successfully and initialization progressed to runtime
        ///         services.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取必需的框架补丁是否已成功完成且初始化是否已进入运行时服务阶段。
        ///     </para>
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether framework initialization finished without an exception after required patches succeeded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取必需补丁成功后，框架初始化是否在未发生异常的情况下完成。
        ///     </para>
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether at least one mod has registered a settings page through <see cref="RegisterModSettings" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取是否至少有一个模组通过 <see cref="RegisterModSettings" /> 注册了设置页。
        ///     </para>
        /// </summary>
        public static bool HasRegisteredModSettings => ModSettingsRegistry.HasPages;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes an observer to framework lifecycle events, optionally replaying the current replayable state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         订阅框架生命周期事件观察者，并可选择回放当前可回放状态。
        ///     </para>
        /// </summary>
        /// <param name="observer">
        ///     <para xml:lang="en">
        ///         Receives lifecycle notifications via <c>OnEvent</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <c>OnEvent</c> 接收生命周期通知。
        ///     </para>
        /// </param>
        /// <param name="replayCurrentState">
        ///     <para xml:lang="en">
        ///         When true, dispatches replayable events that already occurred.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，派发已经发生过的可回放事件。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Disposing unsubscribes the observer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         释放返回值会取消订阅该观察者。
        ///     </para>
        /// </returns>
        public static IDisposable SubscribeLifecycle(ILifecycleObserver observer, bool replayCurrentState = true)
        {
            ArgumentNullException.ThrowIfNull(observer);

            IFrameworkLifecycleEvent[] lifecycleSnapshot;

            lock (SyncRoot)
            {
                _lifecycleObservers = AppendItem(_lifecycleObservers, observer);
                lifecycleSnapshot = replayCurrentState
                    ?
                    [
                        .. ReplayableLifecycleEvents.Values
                            .Cast<IFrameworkLifecycleEvent>()
                            .OrderBy(evt => evt.OccurredAtUtc),
                    ]
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes a typed callback for a specific <typeparamref name="TEvent" /> lifecycle event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为特定 <typeparamref name="TEvent" /> 生命周期事件订阅类型化回调。
        ///     </para>
        /// </summary>
        /// <typeparam name="TEvent">
        ///     <para xml:lang="en">
        ///         Concrete lifecycle event type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         具体的生命周期事件类型。
        ///     </para>
        /// </typeparam>
        /// <param name="handler">
        ///     <para xml:lang="en">
        ///         Invoked for each matching event.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         每次匹配事件到达时调用。
        ///     </para>
        /// </param>
        /// <param name="replayCurrentState">
        ///     <para xml:lang="en">
        ///         When true, invokes <paramref name="handler" /> with the last replayable event if
        ///         present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，如果存在最后一个可重放事件，则用它调用
        ///         <paramref name="handler" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Disposing unsubscribes the handler.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         释放返回值会取消订阅该回调。
        ///     </para>
        /// </returns>
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes a typed callback for a specific <typeparamref name="TEvent" />, passing the same
        ///         <see cref="IDisposable" /> subscription instance on every invocation (including synchronous replay).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为特定 <typeparamref name="TEvent" /> 订阅类型化回调，并在每次调用时传入同一个
        ///         <see cref="IDisposable" /> 订阅实例（包括同步重放）。
        ///     </para>
        /// </summary>
        /// <typeparam name="TEvent">
        ///     <para xml:lang="en">
        ///         Concrete lifecycle event type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         具体的生命周期事件类型。
        ///     </para>
        /// </typeparam>
        /// <param name="handler">
        ///     <para xml:lang="en">
        ///         Invoked for each matching event. The <see cref="IDisposable" /> argument is the subscription; disposing it
        ///         unsubscribes the handler.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         对每个匹配事件调用。<see cref="IDisposable" /> 参数是订阅；释放它会取消订阅该回调。
        ///     </para>
        /// </param>
        /// <param name="replayCurrentState">
        ///     <para xml:lang="en">
        ///         When true, invokes <paramref name="handler" /> with the last replayable event if present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，如果存在最后一个可重放事件，则用它调用
        ///         <paramref name="handler" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Disposing unsubscribes the handler.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         释放返回值会取消订阅该回调。
        ///     </para>
        /// </returns>
        public static IDisposable SubscribeLifecycle<TEvent>(
            Action<TEvent, IDisposable> handler,
            bool replayCurrentState = true
        )
            where TEvent : IFrameworkLifecycleEvent
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (!LifecycleEventTypeCache<TEvent>.SupportsTypedDispatch)
            {
                var holder = new LifecycleSubscriptionHolder();
                var observer = new DelegateLifecycleObserverWithSubscription<TEvent>(handler, holder);
                IFrameworkLifecycleEvent[] lifecycleSnapshot;

                lock (SyncRoot)
                {
                    _lifecycleObservers = AppendItem(_lifecycleObservers, observer);
                    holder.Subscription = new FrameworkLifecycleSubscription(() =>
                    {
                        lock (SyncRoot)
                        {
                            _lifecycleObservers = RemoveItem(_lifecycleObservers, observer);
                        }
                    });

                    lifecycleSnapshot = replayCurrentState
                        ?
                        [
                            .. ReplayableLifecycleEvents.Values
                                .Cast<IFrameworkLifecycleEvent>()
                                .OrderBy(evt => evt.OccurredAtUtc),
                        ]
                        : [];
                }

                foreach (var evt in lifecycleSnapshot)
                    SafeNotify(observer, evt, evt.GetType().Name);

                return holder.Subscription;
            }

            object? replayEvent = null;
            var topic = GetLifecycleTopic<TEvent>();
            FrameworkLifecycleSubscription? subscription = null;

            lock (SyncRoot)
            {
                subscription = new(() =>
                {
                    lock (SyncRoot)
                    {
                        topic.Remove(Wrapped);
                    }
                });

                topic.Add(Wrapped);

                if (replayCurrentState)
                    ReplayableLifecycleEvents.TryGetValue(LifecycleEventTypeCache<TEvent>.EventType, out replayEvent);
            }

            if (replayCurrentState && replayEvent is TEvent typedReplayEvent)
                SafeNotify(Wrapped, typedReplayEvent, LifecycleEventTypeCache<TEvent>.EventName);

            return subscription;

            void Wrapped(TEvent evt)
            {
                try
                {
                    // The subscription is assigned before Wrapped can be published or invoked.
                    // ReSharper disable once AccessToModifiedClosure
                    handler(evt, subscription!);
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"[Lifecycle] Observer callback failed in {LifecycleEventTypeCache<TEvent>.EventName}: {ex.Message}"
                    );
                    DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                        ex,
                        "ritsulib_lifecycle_subscription");
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Initializes shared framework services, registers and applies required patches, and publishes lifecycle events.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         初始化共享框架服务，注册并应用必需补丁，然后发布生命周期事件。
        ///     </para>
        /// </summary>
        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (IsInitialized)
                {
                    Logger.Debug("Framework already initialized, skipping duplicate initialization.");
                    return;
                }

                LinuxHarmonyNativePreloader.EnsureLoaded(
                    message => Logger.Info(message),
                    message => Logger.Warn(message)
                );

                StartupModListLogger.Initialize();

                Logger.Info($"Framework ID: {Const.ModId}");
                Logger.Info($"Framework Name: {Const.Name}");
                Logger.Info(BuildVersionLogText());
                Logger.Info("Initializing shared framework...");

#if STS2_AT_LEAST_0_107_0 && !STS2_AT_LEAST_0_108_0
                RitsuLibStartupAudit.Measure("legacyInputMapCompat", LegacyInputMapCompat.EnsureRegistered);
#endif
                RitsuLibStartupAudit.Measure("harmonyPatchAllTypeLoadGuard",
                    () => HarmonyPatchAllTypeLoadGuard.Install(message => Logger.Warn(message)));
                RitsuLibStartupAudit.Measure("harmonyInitSetterCompat", HarmonyInitSetterCompat.Install);
                RitsuLibStartupAudit.Measure("settingsStore", RitsuLibSettingsStore.Initialize);
                RitsuLibStartupAudit.Measure("searchExpansion", RitsuSearchService.Initialize);
                RitsuLibStartupAudit.Measure("debugLogViewer",
                    () => RitsuDebugLogPipeline.Initialize(RitsuLibSettingsStore.GetDebugLogViewerOptions()));
                RitsuLibStartupAudit.Measure("modSettingsBootstrap", RitsuLibModSettingsBootstrap.Initialize);
                RitsuLibStartupAudit.Measure("telemetryBootstrap", RitsuLibTelemetryBootstrap.Initialize);
                RitsuLibStartupAudit.Measure("frameworkBuiltIns", () =>
                {
                    RitsuLibStartupAudit.Measure("mobileSteamRuntime",
                        RitsuLibMobileSteamRuntime.LogSuppressedSteamFeaturesAtStartup);
                    RitsuLibStartupAudit.Measure("imageResourceLoader",
                        RitsuLibEmbeddedPngResourceLoader.EnsureRegistered);
                    RitsuLibStartupAudit.Measure("godotNodeFactories", RitsuGodotNodeFactoryBootstrap.EnsureRegistered);
                    RitsuLibStartupAudit.Measure("modTypeDiscoveryBuiltIns",
                        ModTypeDiscoveryHub.EnsureBuiltInContributorsRegistered);
                    RitsuLibStartupAudit.Measure("secondaryResourceLocalization",
                        SecondaryResourceLocalizationBootstrap.Initialize);
                    RitsuLibStartupAudit.Measure("secondaryResourceCloneBridge",
                        SecondaryResourceCloneBridge.Initialize);
                });

                PublishLifecycleEvent(
                    new FrameworkInitializingEvent(Const.ModId, Const.Version, DateTimeOffset.UtcNow),
                    nameof(FrameworkInitializingEvent)
                );

                try
                {
                    if (!_frameworkPatchesApplied)
                    {
                        FrameworkPatchersByArea.Clear();
                        RitsuLibStartupAudit.Measure("registerPatches", () =>
                        {
                            RegisterLifecyclePatches();
                            RegisterSettingsUiPatches();
                            RegisterContentAssetPatches();
                            RegisterCharacterAssetPatches();
                            RegisterContentRegistryPatches();
                            RegisterPersistencePatches();
                            RegisterUnlockPatches();
                        });

                        if (!RitsuLibStartupAudit.Measure("patchAll", PatchAllRequired))
                        {
                            Logger.ErrorNoTrace("Framework initialization failed: critical framework patches failed.");
                            IsActive = false;
                            RitsuLibStartupAudit.LogReport("initialization (failed)");
                            return;
                        }

                        _frameworkPatchesApplied = true;
                    }

                    RitsuLibStartupAudit.Measure("runtimeServices", () =>
                    {
                        EnsureFrameworkInteropBootstrapRegistered();
                        ModCardPilePersistence.Initialize();
                        SecondaryResourcePersistence.Initialize();
                        RuntimeHotkeyService.Initialize();
                        RitsuOverlayHostService.Initialize();
                        RitsuToastService.Initialize();
                        RitsuCanvasTextureFilterService.Initialize();
                        RuntimeDetourCompatibilityScanner.Initialize();
                        SteamWorkshopUpdateCoordinator.Initialize();
                        RitsuLibUpdateCheckService.Initialize();
                    });
                    SubscribeLifecycleOnce<MainMenuReadyEvent>(_ =>
                    {
                        RitsuLibStartupAudit.LogReport("launch to main menu");
                        ModTypeDiscoveryHub.LogAutoRegistrationModIdMismatchSummary();
                        Callable.From(PrewarmModSettingsMirrorsForMainMenu).CallDeferred();
                        HarmonyPatchDumpCoordinator.TryAutoDumpOnFirstMainMenu();
                        SelfCheckBundleCoordinator.TryAutoRunOnFirstMainMenu();
                    });

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
                    Logger.ErrorNoTrace($"Framework initialization failed: {ex.Message}");
                    Logger.ErrorNoTrace($"Stack trace: {ex.StackTrace}");
                    DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                        ex,
                        "ritsulib_framework_initialize");
                    IsActive = false;
                }
            }
        }

        internal static string BuildVersionLogText()
        {
            var compatBranchLabel = GetCompatBranchLabel();
            var devBuildText = RitsuLibBuildInfo.IsDevBuild
                ? $" [dev build: {RitsuLibBuildInfo.InformationalVersion}]"
                : "";
            return $"Version: {Const.Version}{devBuildText} [compat branch: {compatBranchLabel}]";
        }

        private static void EnsureFrameworkInteropBootstrapRegistered()
        {
            if (_frameworkInteropBootstrapRegistered)
                return;

            SubscribeLifecycle<DeferredInitializationCompletedEvent>(_ => ConfirmExternalFrameworkInterop());
            _frameworkInteropBootstrapRegistered = true;
        }

        private static void ConfirmExternalFrameworkInterop()
        {
            ExternalFrameworkRegistry.RefreshKnownFrameworkPresence("deferred initialization completed");
            BaseLibHealthBarForecastBridge.TryRegister();
            BaseLibVisualGraftBridge.TryRegister();
            BaseLibMaxHandSizeBridge.TryInitialize();
            MaxHandSizePatchInstaller.EnsurePatched();
        }

        private static void PrewarmModSettingsMirrorsForMainMenu()
        {
            try
            {
                RitsuLibModSettingsBootstrap.EnsureFrameworkPagesRegistered();
                var added = ModSettingsMirrorRegistrarBootstrap.PrewarmMirroredPagesForMainMenu();
                RitsuLibModSettingsBootstrap.RefreshDynamicPages();
                Logger.Debug($"Mod settings mirror prewarm complete. Added {added} mirrored page(s).");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Mod settings mirror prewarm failed: {ex.Message}");
            }
        }

        internal static string GetCompatBranchLabel()
        {
            return RitsuLibBuildInfo.Sts2ApiCompat;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Ensures profile-bound services (<c>ProfileManager</c>, profile-scoped <c>ModDataStore</c>) are initialized
        ///         once.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确保与配置档绑定的服务（<c>ProfileManager</c>、配置档作用域的 <c>ModDataStore</c>）只初始化一次。
        ///     </para>
        /// </summary>
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

                ModDataRuntimeInterop.EnsureProfileSwitchSyncHook();
                ProfileManager.Instance.Initialize();
                ModDataStore.InitializeAllProfileScoped();
                ModDataRuntimeInterop.PushLoadedDataToAllProviders();
                _profileServicesInitialized = true;

                var profileInitializedEvent = new ProfileServicesInitializedEvent(
                    ProfileManager.Instance.CurrentProfileId,
                    DateTimeOffset.UtcNow
                );

                PublishLifecycleEvent(profileInitializedEvent, nameof(ProfileServicesInitializedEvent));

                Logger.Debug("Profile-scoped framework services initialized.");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Begins a registration scope for the specified mod's <c>ModDataStore</c> entries.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为指定模组的 <c>ModDataStore</c> 条目开启注册作用域。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">
        ///         Owning mod identifier.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         所属模组标识符。
        ///     </para>
        /// </param>
        /// <param name="initializeProfileIfReady">
        ///     <para xml:lang="en">
        ///         When true, initializes profile services if the profile is already ready.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，如果档案已经就绪，则初始化档案服务。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         Disposing ends the registration scope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         释放返回值会结束该注册作用域。
        ///     </para>
        /// </returns>
        public static IDisposable BeginModDataRegistration(string modId, bool initializeProfileIfReady = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return ModDataStore.For(modId).BeginRegistrationScope(initializeProfileIfReady);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the persistent-data store facade for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 的持久数据存储外观。
        ///     </para>
        /// </summary>
        public static ModDataStore GetDataStore(string modId)
        {
            return ModDataStore.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the run-saved data store facade for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 的本局存档数据外观。
        ///     </para>
        /// </summary>
        public static RunSavedDataStore GetRunSavedDataStore(string modId)
        {
            return RunSavedDataStore.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the content registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的内容注册表。
        ///     </para>
        /// </summary>
        public static ModContentRegistry GetContentRegistry(string modId)
        {
            return ModContentRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the secondary-resource registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的次级资源注册表。
        ///     </para>
        /// </summary>
        public static ModSecondaryResourceRegistry GetSecondaryResourceRegistry(string modId)
        {
            return ModSecondaryResourceRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the ready-time Godot node attachment registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的就绪阶段 Godot 节点挂载注册表。
        ///     </para>
        /// </summary>
        public static ModNodeAttachmentRegistry GetNodeAttachmentRegistry(string modId)
        {
            return ModNodeAttachmentRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Ensures all ready-time node attachments registered for <paramref name="parent" /> have been applied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确保已应用为 <paramref name="parent" /> 注册的所有就绪阶段节点挂载项。
        ///     </para>
        /// </summary>
        public static void EnsureReadyNodeAttachments(Node parent)
        {
            ModNodeAttachmentRegistry.EnsureReadyAttachments(parent);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the keyword registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的关键字注册表。
        ///     </para>
        /// </summary>
        public static ModKeywordRegistry GetKeywordRegistry(string modId)
        {
            return ModKeywordRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the SmartFormat extension registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的 SmartFormat 扩展注册表。
        ///     </para>
        /// </summary>
        public static ModSmartFormatExtensionRegistry GetSmartFormatRegistry(string modId)
        {
            return ModSmartFormatExtensionRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the custom card-tag registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的自定义卡牌标签注册表。
        ///     </para>
        /// </summary>
        public static ModCardTagRegistry GetCardTagRegistry(string modId)
        {
            return ModCardTagRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the custom card-pile registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的自定义卡牌牌堆注册表。
        ///     </para>
        /// </summary>
        public static ModCardPileRegistry GetCardPileRegistry(string modId)
        {
            return ModCardPileRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the generic dynamic enum value registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的通用动态枚举值注册表。
        ///     </para>
        /// </summary>
        public static ModDynamicEnumValueRegistry<TEnum> GetDynamicEnumValueRegistry<TEnum>(string modId)
            where TEnum : struct, Enum
        {
            return DynamicEnumValueRegistry<TEnum>.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped dynamic enum value and returns its deterministic value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个模组作用域的动态枚举值，并返回其确定性值。
        ///     </para>
        /// </summary>
        public static TEnum RegisterDynamicEnumValue<TEnum>(string modId, string localStem)
            where TEnum : struct, Enum
        {
            return DynamicEnumValueRegistry<TEnum>.RegisterOwned(modId, localStem).Value;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the deterministic dynamic enum value for <paramref name="id" /> without failing on hash
        ///         collisions. Unknown ids are computed but not registered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="id" /> 对应的确定性动态枚举值，且不会因哈希碰撞失败。未知 ID 只计算值，不会注册。
        ///     </para>
        /// </summary>
        public static TEnum GetDynamicEnumValueIgnoringCollisions<TEnum>(string id)
            where TEnum : struct, Enum
        {
            return DynamicEnumValueRegistry<TEnum>.GetValueIgnoringCollisions(id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped single-target <see cref="TargetType" /> and returns its deterministic enum value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个模组作用域的单体目标 <see cref="TargetType" />，并返回其确定性的枚举值。
        ///     </para>
        /// </summary>
        public static TargetType RegisterSingleTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> canTarget)
        {
            return CustomTargetType.RegisterSingleTargetType(modId, localStem, canTarget);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped player-aware single-target <see cref="TargetType" /> and returns its deterministic
        ///         enum value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个感知出牌玩家的模组作用域单体目标 <see cref="TargetType" />，并返回其确定性的枚举值。
        ///     </para>
        /// </summary>
        public static TargetType RegisterSingleTargetType(
            string modId,
            string localStem,
            Func<Creature, Player, bool> canTarget)
        {
            return CustomTargetType.RegisterSingleTargetType(modId, localStem, canTarget);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped source-aware single-target <see cref="TargetType" /> and returns its deterministic
        ///         enum value.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个感知来源上下文的模组作用域单体目标 <see cref="TargetType" />，并返回其确定性的枚举值。
        ///     </para>
        /// </summary>
        public static TargetType RegisterSingleTargetTypeWithContext(
            string modId,
            string localStem,
            Func<CustomTargetContext, bool> canTarget)
        {
            return CustomTargetType.RegisterSingleTargetTypeWithContext(modId, localStem, canTarget);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped multi-target <see cref="TargetType" /> and returns its deterministic enum value.
        ///         Cards and potions using the target type execute once with a null selected target; use the target
        ///         resolution extensions to get the affected creatures.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个模组作用域的群体目标 <see cref="TargetType" />，并返回其确定性的枚举值。
        ///         使用该目标类型的卡牌和药水会以 null 已选目标执行一次；可用目标解析扩展取得实际影响的生物。
        ///     </para>
        /// </summary>
        public static TargetType RegisterMultiTargetType(
            string modId,
            string localStem,
            Func<Creature, bool> includeTarget)
        {
            return CustomTargetType.RegisterMultiTargetType(modId, localStem, includeTarget);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod-scoped player-aware multi-target <see cref="TargetType" /> and returns its deterministic
        ///         enum value.
        ///         Cards and potions using the target type execute once with a null selected target; use the target
        ///         resolution extensions to get the affected creatures.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个感知出牌玩家的模组作用域群体目标 <see cref="TargetType" />，并返回其确定性的枚举值。
        ///         使用该目标类型的卡牌和药水会以 null 已选目标执行一次；可用目标解析扩展取得实际影响的生物。
        ///     </para>
        /// </summary>
        public static TargetType RegisterMultiTargetType(
            string modId,
            string localStem,
            Func<Creature, Player, bool> includeTarget)
        {
            return CustomTargetType.RegisterMultiTargetType(modId, localStem, includeTarget);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the top-bar button registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的顶部栏按钮注册表。
        ///     </para>
        /// </summary>
        public static ModTopBarButtonRegistry GetTopBarButtonRegistry(string modId)
        {
            return ModTopBarButtonRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the timeline (epoch/story) registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的时间线（纪元/故事）注册表。
        ///     </para>
        /// </summary>
        public static ModTimelineRegistry GetTimelineRegistry(string modId)
        {
            return ModTimelineRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the unlock rules registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的解锁规则注册表。
        ///     </para>
        /// </summary>
        public static ModUnlockRegistry GetUnlockRegistry(string modId)
        {
            return ModUnlockRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the model-clone listener registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的模型复制监听器注册表。
        ///     </para>
        /// </summary>
        public static ModelCloneRegistry GetModelCloneRegistry(string modId)
        {
            return ModelCloneRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the model-saved data store facade for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的模型保存数据存储外观。
        ///     </para>
        /// </summary>
        public static ModelSavedDataStore GetModelSavedDataStore(string modId)
        {
            return ModelSavedDataStore.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the capability set attached to <paramref name="model" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取附加到 <paramref name="model" /> 的能力集合。
        ///     </para>
        /// </summary>
        public static ModelCapabilitySet GetModelCapabilities(AbstractModel model)
        {
            return ModelCapabilities.Get(model);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a model-backed capability in this mod's content registry.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在该模组的内容注册表中注册一个基于模型的能力。
        ///     </para>
        /// </summary>
        public static void RegisterModelCapability<TCapability>(string modId)
            where TCapability : ModelCapability
        {
            GetContentRegistry(modId).RegisterModelCapability<TCapability>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a model-backed capability in this mod's content registry using
        ///         <paramref name="publicEntry" /> rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" /> 规则在该模组的内容注册表中注册一个基于模型的能力。
        ///     </para>
        /// </summary>
        public static void RegisterModelCapability<TCapability>(string modId, ModelPublicEntryOptions publicEntry)
            where TCapability : ModelCapability
        {
            GetContentRegistry(modId).RegisterModelCapability<TCapability>(publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the default capability set for matching <typeparamref name="TModel" /> instances.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置匹配的 <typeparamref name="TModel" /> 实例的默认能力集合。
        ///     </para>
        /// </summary>
        public static void ConfigureDefaultModelCapabilities<TModel>(
            string modId,
            string modifierId,
            Action<TModel, ModelCapabilityList> modifier,
            int order = 0)
            where TModel : AbstractModel
        {
            GetContentRegistry(modId).ConfigureDefaultModelCapabilities(modifierId, modifier, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the default capability set for matching <paramref name="modelType" /> instances.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         配置匹配的 <paramref name="modelType" /> 实例的默认能力集合。
        ///     </para>
        /// </summary>
        public static void ConfigureDefaultModelCapabilities(
            string modId,
            Type modelType,
            string modifierId,
            Action<AbstractModel, ModelCapabilityList> modifier,
            int order = 0)
        {
            GetContentRegistry(modId).ConfigureDefaultModelCapabilities(modelType, modifierId, modifier, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the card-transform listener registry for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="modId" /> 的卡牌转换监听器注册表。
        ///     </para>
        /// </summary>
        public static ModCardTransformRegistry GetCardTransformRegistry(string modId)
        {
            return ModCardTransformRegistry.For(modId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a non-power health bar forecast source type through the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过框架注册非能力生命条预测来源类型。
        ///     </para>
        /// </summary>
        public static void RegisterHealthBarForecast<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            HealthBarForecastRegistry.Register<TSource>(modId, sourceId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a non-power health bar visual graft source type through the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过框架注册非能力生命条视觉移植来源类型。
        ///     </para>
        /// </summary>
        public static void RegisterHealthBarVisualGraft<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarVisualGraftSource, new()
        {
            HealthBarVisualGraftRegistry.Register<TSource>(modId, sourceId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide creature healing amount listener through the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过框架注册进程级生物治疗数值监听器。
        ///     </para>
        /// </summary>
        public static void RegisterHealHookListener(IHealHookListener listener)
        {
            HealHook.RegisterGlobalListener(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide card OnPlay listener through the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过框架注册进程级卡牌 OnPlay 监听器。
        ///     </para>
        /// </summary>
        public static void RegisterCardOnPlayHookListener(ICardOnPlayHookListener listener)
        {
            CardOnPlayHook.RegisterGlobalListener(listener);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide BaseLib-compatible card type text modifier through the framework.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过框架注册进程级、与 BaseLib 兼容的卡牌类型文本修改器。
        ///     </para>
        /// </summary>
        public static void RegisterCardTypeTextModifier(ICardTypeTextModifier modifier)
        {
            CardTypeTextHook.RegisterGlobalModifier(modifier);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the current max-hand-size value for <paramref name="player" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <paramref name="player" /> 当前的最大手牌数值。
        ///     </para>
        /// </summary>
        public static int GetMaxHandSize(Player player)
        {
            return MaxHandSizeCalculator.Calculate(player);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an additional free-play detector used by framework consumers (for example material logic).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个额外的免费打出检测器，供框架消费者使用（例如材质逻辑）。
        ///     </para>
        /// </summary>
        public static void RegisterFreePlayBinding(string bindingId, Func<CardPlay, bool> detector)
        {
            FreePlayBindingRegistry.Register(bindingId, detector);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an initial-option injection rule for <typeparamref name="TAncient" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TAncient" /> 注册初始选项注入规则。
        ///     </para>
        /// </summary>
        public static void RegisterAncientOption<TAncient>(string modId, ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            GetContentRegistry(modId).RegisterAncientOption<TAncient>(rule);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TCard" /> as a candidate for the Trash Heap event's Grab option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TCard" /> 注册为垃圾堆事件“拿取”选项的候选卡牌。
        ///     </para>
        /// </summary>
        public static void RegisterTrashHeapCard<TCard>(string modId)
            where TCard : CardModel
        {
            GetContentRegistry(modId).RegisterTrashHeapCard<TCard>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TRelic" /> as a candidate for the Trash Heap event's Dive In option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TRelic" /> 注册为垃圾堆事件“深入翻找”选项的候选遗物。
        ///     </para>
        /// </summary>
        public static void RegisterTrashHeapRelic<TRelic>(string modId)
            where TRelic : RelicModel
        {
            GetContentRegistry(modId).RegisterTrashHeapRelic<TRelic>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a content pack builder for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modId" /> 创建内容包构建器。
        ///     </para>
        /// </summary>
        public static ModContentPackBuilder CreateContentPack(string modId)
        {
            return ModContentPackBuilder.For(modId);
        }

        internal static void EnqueueDeferredContentPack(string modId, Action<ModContentPackContext> apply,
            string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(apply);

            lock (DeferredContentPackSync)
            {
                if (_deferredContentPacksFlushed)
                    throw new InvalidOperationException(
                        $"Content pack registration for mod '{modId}' is already closed for this run.");

                DeferredContentPackRegistrations.Add(new(modId, apply, description));
            }
        }

        internal static void FlushDeferredContentPacks()
        {
            List<DeferredContentPackRegistration> pending;
            lock (DeferredContentPackSync)
            {
                if (_deferredContentPacksFlushed)
                    return;

                _deferredContentPacksFlushed = true;
                pending = [.. DeferredContentPackRegistrations];
                DeferredContentPackRegistrations.Clear();
            }

            if (pending.Count == 0)
                return;

            pending.Sort(static (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.ModId, y.ModId));
            foreach (var registration in pending)
            {
                var context = new ModContentPackContext(
                    registration.ModId,
                    GetContentRegistry(registration.ModId),
                    GetKeywordRegistry(registration.ModId),
                    GetTimelineRegistry(registration.ModId),
                    GetUnlockRegistry(registration.ModId),
                    GetCardTagRegistry(registration.ModId),
                    GetCardPileRegistry(registration.ModId));
                try
                {
                    registration.Apply(context);
                }
                catch (Exception ex)
                {
                    RegistrationFreezeDiagnostics.RecordFailure(
                        "ContentPack",
                        registration.ModId,
                        registration.Description ?? "deferred content pack",
                        ex);
                    Logger.ErrorNoTrace(
                        $"[ContentPack] Failed to apply deferred content pack for mod '{registration.ModId}'" +
                        $"{(string.IsNullOrWhiteSpace(registration.Description) ? "" : $" ({registration.Description})")}: {ex.Message}");
                }
            }

            Logger.Info($"[ContentPack] Flushed {pending.Count} deferred content pack(s).");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts a batch PNG export of registered cards (see <see cref="CardPngExporter" />).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         开始批量导出已注册卡牌的 PNG（见 <see cref="CardPngExporter" />）。
        ///     </para>
        /// </summary>
        /// <param name="request">
        ///     <para xml:lang="en">
        ///         Output directory, scale, hover panel, filters, etc.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         输出目录、缩放、悬停面板、过滤器等导出参数。
        ///     </para>
        /// </param>
        /// <param name="issuingPlayer">
        ///     <para xml:lang="en">
        ///         Optional; export does not require a run or player.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选参数；导出不要求存在当前一局游戏或玩家。
        ///     </para>
        /// </param>
        public static void BeginCardPngExport(CardPngExportRequest request, Player? issuingPlayer = null)
        {
            CardPngExporter.BeginExport(request, issuingPlayer, msg => Logger.Info(msg));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Starts a batch PNG export of compendium-style detail panels: relic <c>inspect_relic_screen</c> popup, and
        ///         potion lab focus (scaled <c>NPotion</c> + hovers). Does not use save / unlock gating; content is the “seen
        ///         unlocked” form.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         开始批量导出图鉴风格的详情面板 PNG：遗物 <c>inspect_relic_screen</c> 弹窗，以及
        ///         药水实验室焦点（缩放后的 <c>NPotion</c> + 悬停）。不使用存档/解锁门控；内容为“已见
        ///         已解锁”形态。
        ///     </para>
        /// </summary>
        public static void BeginCompendiumDetailPngExport(CompendiumPngExportRequest request)
        {
            CompendiumDetailPngExporter.BeginExport(request, msg => Logger.Info(msg));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Declares a <c>mod_data</c> JSON path that may participate in RitsuLib Steam Cloud sync when the player enables
        ///         it and the session uses Steam Cloud. Prefer ModDataStore.Register when you already use
        ///         <see cref="Data.ModDataStore" />; this call is for custom persistence that still resolves via
        ///         <see cref="Utils.Persistence.ProfileManager" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         声明一个 <c>mod_data</c> JSON 路径；当玩家启用 Steam Cloud 且会话使用 Steam Cloud 时，该路径可以参与 RitsuLib Steam Cloud 同步。
        ///         当你已经使用 <see cref="Data.ModDataStore" /> 时，优先使用 ModDataStore.Register；此调用用于仍通过
        ///         <see cref="Utils.Persistence.ProfileManager" /> 解析的自定义持久化。
        ///     </para>
        /// </summary>
        public static void RegisterModCloudPersistedSlot(string modId, string fileName, SaveScope scope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ModCloudSyncPathRegistry.RegisterModDataSlot(modId, fileName, scope);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a page in the RitsuLib mod settings submenu.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 RitsuLib 模组设置子菜单中注册一个页面。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Optional layout: <see cref="ModSettingsUiPresentation.ParagraphMaxBodyHeight" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选布局：<see cref="ModSettingsUiPresentation.ParagraphMaxBodyHeight" />。
        ///     </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RegisterModSettings(string modId, Action<ModSettingsPageBuilder> configure,
            string? pageId = null)
        {
            ModSettingsRegistry.RegisterWithSourceAssembly(modId, configure, pageId, Assembly.GetCallingAssembly());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reflection-based settings provider type for attribute-driven settings pages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个基于反射的设置提供器类型，用于属性驱动的设置页。
        ///     </para>
        /// </summary>
        public static bool RegisterModSettingsReflectionProvider<TProvider>()
        {
            return RuntimeReflectionMirrorSource.RegisterProviderType<TProvider>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reflection-based settings provider type for attribute-driven settings pages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个基于反射的设置提供器类型，用于属性驱动的设置页。
        ///     </para>
        /// </summary>
        public static bool RegisterModSettingsReflectionProvider(Type providerType)
        {
            return RuntimeReflectionMirrorSource.RegisterProviderType(providerType);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reflection provider and immediately attempts to mirror-register its pages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个反射提供器，并立即尝试镜像注册其页面。
        ///     </para>
        /// </summary>
        public static int RegisterModSettingsReflectionProviderAndTryRegister<TProvider>()
        {
            return RuntimeReflectionMirrorSource.RegisterProviderTypeAndTryRegister<TProvider>();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a reflection provider and immediately attempts to mirror-register its pages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个反射提供器，并立即尝试镜像注册其页面。
        ///     </para>
        /// </summary>
        public static int RegisterModSettingsReflectionProviderAndTryRegister(Type providerType)
        {
            return RuntimeReflectionMirrorSource.RegisterProviderTypeAndTryRegister(providerType);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets ordering for this mod&apos;s group in the RitsuLib mod settings sidebar (lower first). Mods without a
        ///         value use <c>0</c> and sort by display name. Prefer <see cref="ModSettingsPageBuilder.WithModSidebarOrder" />
        ///         when
        ///         registering pages.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置该模组分组在 RitsuLib 模组设置侧边栏中的排序（较小者在前）。未设置排序值的
        ///         模组使用 <c>0</c> 并按显示名排序。注册页面时优先使用
        ///         <see cref="ModSettingsPageBuilder.WithModSidebarOrder" />。
        ///     </para>
        /// </summary>
        public static void RegisterModSettingsSidebarOrder(string modId, int order)
        {
            ModSettingsRegistry.RegisterModSidebarOrder(modId, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Overrides sort order for a registered page among siblings (same mod and parent page).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         覆盖已注册页面在同级页面中的排序（同一模组且同一父页面）。
        ///     </para>
        /// </summary>
        public static void RegisterModSettingsPageOrder(string modId, string pageId, int sortOrder)
        {
            ModSettingsRegistry.RegisterPageSortOrder(modId, pageId, sortOrder);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places <paramref name="pageId" /> after <paramref name="afterPageId" /> in the sidebar for this mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="pageId" /> 放在该模组的侧边栏中 <paramref name="afterPageId" /> 之后。
        ///     </para>
        /// </summary>
        public static bool TryRegisterModSettingsPageOrderAfter(string modId, string pageId, string afterPageId,
            int gap = 1)
        {
            return ModSettingsRegistry.TryRegisterPageSortOrderAfter(modId, pageId, afterPageId, gap);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Places <paramref name="pageId" /> before <paramref name="beforePageId" /> in the sidebar for this mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="pageId" /> 放在该模组的侧边栏中 <paramref name="beforePageId" /> 之前。
        ///     </para>
        /// </summary>
        public static bool TryRegisterModSettingsPageOrderBefore(string modId, string pageId, string beforePageId,
            int gap = 1)
        {
            return ModSettingsRegistry.TryRegisterPageSortOrderBefore(modId, pageId, beforePageId, gap);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns all registered mod settings pages (same snapshot as <see cref="ModSettingsRegistry.GetPages" />).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有已注册的模组设置页（与 <see cref="ModSettingsRegistry.GetPages" /> 相同的快照）。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<ModSettingsPage> GetRegisteredModSettings()
        {
            return ModSettingsRegistry.GetPages();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Opens the Steam Workshop subscription import dialog for a text list of Workshop item ids or links.
        ///         The dialog previews the items, defaults every item to selected, and lets the player choose which ones
        ///         to subscribe to.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用创意工坊项目 ID 或链接文本列表打开 Steam 创意工坊订阅导入弹窗。该弹窗会预览项目，默认全选，
        ///         并允许玩家选择要订阅的项目。
        ///     </para>
        /// </summary>
        /// <param name="workshopItemIds">
        ///     <para xml:lang="en">
        ///         Plain item ids, Steam Workshop links, or a copied list containing them.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         普通项目 ID、Steam 创意工坊链接，或包含它们的复制列表。
        ///     </para>
        /// </param>
        /// <param name="attachParent">
        ///     <para xml:lang="en">
        ///         Optional node used to find the active scene tree. Pass the current UI node when available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用于寻找当前场景树的可选节点；有当前界面节点时传入它。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         True when a dialog was opened; false when no Workshop ids were found or no scene tree is available.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         弹窗已打开时为 <see langword="true" />；未找到创意工坊 ID 或没有可用场景树时为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool ShowSteamWorkshopImportDialog(string workshopItemIds, Node? attachParent = null)
        {
            return SteamWorkshopImportDialog.Show(workshopItemIds, attachParent);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a telemetry applicant with its own fixed adapter/endpoint and data requests.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个遥测申请方；该申请方拥有自己的固定适配器/端点和数据申请。
        ///     </para>
        /// </summary>
        public static void RegisterTelemetryApplicant(TelemetryApplicant applicant)
        {
            TelemetryRegistry.RegisterApplicant(applicant);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a shared telemetry contribution provider.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个共享遥测贡献提供器。
        ///     </para>
        /// </summary>
        public static void RegisterTelemetryContributionProvider(ITelemetryContributionProvider provider)
        {
            TelemetryRegistry.RegisterContributionProvider(provider);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the telemetry client for <paramref name="applicantId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="applicantId" /> 的遥测客户端。
        ///     </para>
        /// </summary>
        public static ITelemetryClient GetTelemetryClient(string applicantId)
        {
            return TelemetryApi.GetClient(applicantId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets consent for a telemetry applicant. Intended for settings UI integrations and explicit user actions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置遥测申请方授权。用于设置界面集成和显式用户操作。
        ///     </para>
        /// </summary>
        public static void SetTelemetryApplicantConsent(
            string applicantId,
            TelemetryConsentState state,
            IEnumerable<string>? grantedRequests = null)
        {
            TelemetryConsentStore.SetApplicantConsent(applicantId, state, grantedRequests);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sets whether one applicant may receive a shared contribution from another mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置某申请方是否可接收另一个模组的共享贡献。
        ///     </para>
        /// </summary>
        public static void SetTelemetrySharedContributionConsent(
            string applicantId,
            string contributorModId,
            string contributionId,
            bool granted)
        {
            TelemetryConsentStore.SetSharedContributionConsent(
                applicantId,
                contributorModId,
                contributionId,
                granted);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns registered telemetry applicants.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回已注册的遥测申请方。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<TelemetryApplicant> GetTelemetryApplicants()
        {
            return TelemetryRegistry.GetApplicants();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to flush queued telemetry for every registered applicant.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试发送所有已注册申请方排队的遥测。
        ///     </para>
        /// </summary>
        public static Task FlushTelemetryAsync(CancellationToken cancellationToken = default)
        {
            return TelemetryQueue.FlushAllAsync(cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <c>MegaCrit.Sts2.Core.Logging.Logger</c> for <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modId" /> 创建 <c>MegaCrit.Sts2.Core.Logging.Logger</c>。
        ///     </para>
        /// </summary>
        public static Logger CreateLogger(string modId, LogType logType = LogType.Generic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId, logType);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes an error message to Godot's standard-error output without automatically appending a stack trace.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将错误消息写入 Godot 的标准错误输出，且不会自动附加堆栈跟踪。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Include an explicit stack trace in <paramref name="text" /> if one is needed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         如需堆栈信息，请由调用方将堆栈内容放入 <paramref name="text" />。
        ///     </para>
        /// </remarks>
        public static void ErrorNoTrace(this Logger logger, string text)
        {
            ArgumentNullException.ThrowIfNull(logger);

            if (!logger.WillLog(LogLevel.Error))
                return;

            var formattedText = logger.Context != null ? $"[{logger.Context}] {text}" : text;
            GD.PrintErr($"[ERROR] {formattedText}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="STS2RitsuLib.Patching.Core.ModPatcher" /> with a dedicated logger for the owning mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用所属模组的专用日志记录器创建 <see cref="STS2RitsuLib.Patching.Core.ModPatcher" />。
        ///     </para>
        /// </summary>
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="STS2RitsuLib.Utils.I18N" /> instance with optional file, embedded resource, and PCK
        ///         translation roots.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例，可带可选的文件、嵌入资源和 PCK
        ///         翻译根。
        ///     </para>
        /// </summary>
        public static I18N CreateLocalization(
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null)
        {
            var callerAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();
            return CreateLocalizationCore(instanceName, fileSystemFolders, resourceFolders, pckFolders,
                callerAssembly,
                null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="STS2RitsuLib.Utils.I18N" /> instance with an explicit fallback language.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用显式回退语言创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例。
        ///     </para>
        /// </summary>
        public static I18N CreateLocalizationWithFallback(
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null,
            string? fallbackLanguage = null)
        {
            var callerAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();
            return CreateLocalizationCore(instanceName, fileSystemFolders, resourceFolders, pckFolders,
                callerAssembly,
                fallbackLanguage);
        }

        private static I18N CreateLocalizationCore(
            string instanceName,
            IEnumerable<string>? fileSystemFolders,
            IEnumerable<string>? resourceFolders,
            IEnumerable<string>? pckFolders,
            Assembly? resourceAssembly,
            string? fallbackLanguage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);

            return new(
                instanceName,
                fileSystemFolders?.ToArray() ?? [],
                resourceFolders?.ToArray() ?? [],
                pckFolders?.ToArray() ?? [],
                resourceAssembly,
                fallbackLanguage
            );
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an <see cref="STS2RitsuLib.Utils.I18N" /> instance for a mod. When no file-system folders are
        ///         supplied, it uses <c>user://&lt;platform&gt;/&lt;userId&gt;/mod_data/{modId}/localization</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为模组创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例。未提供文件系统目录时，默认使用
        ///         <c>user://&lt;platform&gt;/&lt;userId&gt;/mod_data/{modId}/localization</c>。
        ///     </para>
        /// </summary>
        public static I18N CreateModLocalization(
            string modId,
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null)
        {
            var callerAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();
            return CreateModLocalizationCore(modId, instanceName, fileSystemFolders, resourceFolders, pckFolders,
                callerAssembly, null);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="STS2RitsuLib.Utils.I18N" /> instance for a mod with an explicit fallback language.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用显式回退语言为模组创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例。
        ///     </para>
        /// </summary>
        public static I18N CreateModLocalizationWithFallback(
            string modId,
            string instanceName,
            IEnumerable<string>? fileSystemFolders = null,
            IEnumerable<string>? resourceFolders = null,
            IEnumerable<string>? pckFolders = null,
            Assembly? resourceAssembly = null,
            string? fallbackLanguage = null)
        {
            var callerAssembly = resourceAssembly ?? Assembly.GetCallingAssembly();
            return CreateModLocalizationCore(modId, instanceName, fileSystemFolders, resourceFolders, pckFolders,
                callerAssembly, fallbackLanguage);
        }

        private static I18N CreateModLocalizationCore(
            string modId,
            string instanceName,
            IEnumerable<string>? fileSystemFolders,
            IEnumerable<string>? resourceFolders,
            IEnumerable<string>? pckFolders,
            Assembly? resourceAssembly,
            string? fallbackLanguage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);

            var folders = fileSystemFolders?.ToArray() ?? [$"{ProfileManager.GetAccountBasePath(modId)}/localization"];
            return CreateLocalizationCore(instanceName, folders, resourceFolders, pckFolders, resourceAssembly,
                fallbackLanguage);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the virtual <c>LocTable</c> ID for an <see cref="I18N" /> bridge table, using the standard
        ///         <c>MODID_I18N_STEM</c> convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="I18N" /> 桥接表的虚拟 <c>LocTable</c> ID，采用标准的
        ///         <c>MODID_I18N_STEM</c> 命名约定。
        ///     </para>
        /// </summary>
        public static string GetI18NLocTableId(string modId, string stem = "DEFAULT")
        {
            return I18NLocTableBridge.GetTableId(modId, stem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an <see cref="I18N" /> instance as a virtual <c>LocTable</c> so the game-native
        ///         <c>LocString</c> pipeline can resolve raw templates from it.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="I18N" /> 实例注册为虚拟 <c>LocTable</c>，使游戏原生
        ///         <c>LocString</c> 管线可以从中解析原始模板。
        ///     </para>
        /// </summary>
        public static bool RegisterI18NLocTableBridge(string modId, I18N i18N, string stem = "DEFAULT",
            bool replaceExisting = false)
        {
            return I18NLocTableBridge.TryRegister(modId, i18N, stem, replaceExisting);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Unregisters a previously registered virtual <c>LocTable</c> for the given <paramref name="modId" /> and
        ///         <paramref name="stem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注销此前为给定 <paramref name="modId" /> 和
        ///         <paramref name="stem" /> 注册的虚拟 <c>LocTable</c>。
        ///     </para>
        /// </summary>
        public static bool UnregisterI18NLocTableBridge(string modId, string stem = "DEFAULT")
        {
            return I18NLocTableBridge.TryUnregister(modId, stem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers C# scripts from <paramref name="assembly" /> with Godot (once per assembly).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="assembly" /> 中的 C# 脚本注册到 Godot（每个程序集一次）。
        ///     </para>
        /// </summary>
        public static void EnsureGodotScriptsRegistered(Assembly assembly, Logger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();

            lock (SyncRoot)
            {
                if (RegisteredScriptAssemblies.Contains(assemblyName) ||
                    !RegisteringScriptAssemblies.Add(assemblyName))
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

                if (bridgeType == null || lookupMethod == null)
                {
                    logger?.Warn($"Godot script registration bridge not found for assembly {assemblyName}.");
                    return;
                }

                if (AreGodotScriptPathsAlreadyRegistered(assembly, bridgeType, logger))
                {
                    lock (SyncRoot)
                    {
                        RegisteredScriptAssemblies.Add(assemblyName);
                    }

                    logger?.Debug($"Godot C# scripts already registered for assembly: {assemblyName}");
                    return;
                }

                var lookup = lookupMethod.CreateDelegate<Action<Assembly>>();
                lookup(assembly);
                lock (SyncRoot)
                {
                    RegisteredScriptAssemblies.Add(assemblyName);
                }

                logger?.Debug($"Registered Godot C# scripts for assembly: {assemblyName}");
            }
            catch (Exception ex)
            {
                logger?.ErrorNoTrace($"Failed to register Godot C# scripts for assembly {assemblyName}: {ex.Message}");
                logger?.ErrorNoTrace($"Stack trace: {ex.StackTrace}");
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_godot_script_registration");
            }
            finally
            {
                lock (SyncRoot)
                {
                    RegisteringScriptAssemblies.Remove(assemblyName);
                }
            }
        }

        private static bool AreGodotScriptPathsAlreadyRegistered(Assembly assembly, Type bridgeType, Logger? logger)
        {
            try
            {
                var scriptPaths = EnumerateGodotScriptPaths(assembly).ToArray();
                if (scriptPaths.Length == 0)
                    return true;

                var pathTypeBiMap = bridgeType.GetField(
                    "_pathTypeBiMap",
                    BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                if (pathTypeBiMap == null)
                    return false;

                var tryGetScriptType = pathTypeBiMap.GetType().GetMethod(
                    "TryGetScriptType",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(string), typeof(Type).MakeByRefType()],
                    null);
                if (tryGetScriptType == null)
                    return false;

                var scriptTypeBiMap = bridgeType.GetField(
                    "_scriptTypeBiMap",
                    BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                var readWriteLock = scriptTypeBiMap?.GetType()
                    .GetField("ReadWriteLock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(scriptTypeBiMap) as ReaderWriterLockSlim;

                readWriteLock?.EnterReadLock();
                try
                {
                    if (scriptPaths.Select(scriptPath => (object?[])[scriptPath, null]).Any(args =>
                            tryGetScriptType.Invoke(pathTypeBiMap, args) is not true)) return false;
                }
                finally
                {
                    readWriteLock?.ExitReadLock();
                }

                return true;
            }
            catch (Exception ex)
            {
                logger?.Debug($"Could not inspect Godot script registration cache: {ex.Message}");
                return false;
            }
        }

        private static IEnumerable<string> EnumerateGodotScriptPaths(Assembly assembly)
        {
            var scriptsAttribute = assembly.GetCustomAttributes(false)
                .OfType<AssemblyHasScriptsAttribute>()
                .FirstOrDefault();
            if (scriptsAttribute == null)
                yield break;

            var candidateTypes = scriptsAttribute.RequiresLookup
                ? assembly.GetTypes().Where(type => !type.IsNested && typeof(GodotObject).IsAssignableFrom(type))
                : scriptsAttribute.ScriptTypes ?? [];

            foreach (var type in candidateTypes)
            {
                var scriptPath = type.GetCustomAttributes(false)
                    .OfType<ScriptPathAttribute>()
                    .FirstOrDefault()
                    ?.Path;
                if (!string.IsNullOrWhiteSpace(scriptPath))
                    yield return scriptPath;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies all patches on <paramref name="patcher" />; on failure logs, invokes <paramref name="disableMod" />,
        ///         and
        ///         returns false.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用 <paramref name="patcher" /> 上的所有补丁；失败时记录日志，调用 <paramref name="disableMod" />，并
        ///         返回 false。
        ///     </para>
        /// </summary>
        public static bool ApplyRequiredPatcher(ModPatcher patcher, Action disableMod, string? failureMessage = null)
        {
            ArgumentNullException.ThrowIfNull(patcher);
            ArgumentNullException.ThrowIfNull(disableMod);

            var success = patcher.PatchAll();
            if (success)
                return true;

            patcher.Logger.ErrorNoTrace(
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
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_lifecycle_handler");
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
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_lifecycle_observer");
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

        private sealed record DeferredContentPackRegistration(
            string ModId,
            Action<ModContentPackContext> Apply,
            string? Description);

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
