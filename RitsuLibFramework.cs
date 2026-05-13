using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Cards.FreePlay;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.Diagnostics.CompendiumExport;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Localization;
using STS2RitsuLib.Localization.SmartFormat;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Platform;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Scaffolding.Ancients.Options;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Settings.RunSidecar;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.TopBar;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Shared runtime bootstrap for the framework itself and for mods that reference it.
    ///     框架自身以及引用该框架的 Mod 共用的运行时启动入口。
    /// </summary>
    [ModInitializer(nameof(Initialize))]
    public static partial class RitsuLibFramework
    {
        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<FrameworkPatcherArea, ModPatcher> FrameworkPatchersByArea = [];
        private static bool _frameworkInteropBootstrapRegistered;

        private static bool _profileServicesInitialized;
        private static ILifecycleObserver[] _lifecycleObservers = [];
        private static readonly ConcurrentDictionary<Type, object> LifecycleTopics = new();
        private static readonly Dictionary<Type, object> ReplayableLifecycleEvents = [];
        private static readonly HashSet<string> RegisteredScriptAssemblies = [];

        private static readonly Lock DeferredContentPackSync = new();
        private static readonly List<DeferredContentPackRegistration> DeferredContentPackRegistrations = [];
        private static bool _deferredContentPacksFlushed;

        static RitsuLibFramework()
        {
            Logger = CreateLogger(Const.ModId);
        }

        /// <summary>
        ///     Framework logger instance (typed as <c>MegaCrit.Sts2.Core.Logging.Logger</c>).
        ///     框架日志实例，类型为 <c>MegaCrit.Sts2.Core.Logging.Logger</c>。
        /// </summary>
        public static Logger Logger { get; private set; }

        /// <summary>
        ///     True after <see cref="Initialize" /> completes without a fatal patch failure.
        ///     当 <see cref="Initialize" /> 完成且没有发生致命补丁失败时为 true。
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        ///     True when the framework finished initialization and critical patches succeeded.
        ///     当框架完成初始化且关键补丁成功应用时为 true。
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        ///     True when at least one mod has registered a settings page via <see cref="RegisterModSettings" />.
        ///     当至少一个 Mod 已通过 <see cref="RegisterModSettings" /> 注册设置页时为 true。
        /// </summary>
        public static bool HasRegisteredModSettings => ModSettingsRegistry.HasPages;

        /// <summary>
        ///     Subscribes an observer to framework lifecycle events, optionally replaying the current replayable state.
        ///     订阅框架生命周期事件观察者，并可选择回放当前可回放状态。
        /// </summary>
        /// <param name="observer">
        ///     Receives lifecycle notifications via <c>OnEvent</c>.
        ///     通过 <c>OnEvent</c> 接收生命周期通知。
        /// </param>
        /// <param name="replayCurrentState">
        ///     When true, dispatches replayable events that already occurred.
        ///     为 true 时，派发已经发生过的可回放事件。
        /// </param>
        /// <returns>
        ///     Disposing unsubscribes the observer.
        ///     释放返回值会取消订阅该观察者。
        /// </returns>
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

        /// <summary>
        ///     Subscribes a typed callback for a specific <typeparamref name="TEvent" /> lifecycle event.
        ///     为指定的 <typeparamref name="TEvent" /> 生命周期事件订阅强类型回调。
        /// </summary>
        /// <typeparam name="TEvent">
        ///     Concrete lifecycle event type.
        ///     具体的生命周期事件类型。
        /// </typeparam>
        /// <param name="handler">
        ///     Invoked for each matching event.
        ///     每次匹配事件到达时调用。
        /// </param>
        /// <param name="replayCurrentState">
        ///     When true, invokes <paramref name="handler" /> with the last replayable event if
        ///     present.
        ///     为 true 时，如果存在最后一次可回放事件，则使用该事件调用 <paramref name="handler" />。
        /// </param>
        /// <returns>
        ///     Disposing unsubscribes the handler.
        ///     释放返回值会取消订阅该回调。
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
        ///     Subscribes a typed callback for a specific <typeparamref name="TEvent" />, passing the same
        ///     <see cref="IDisposable" /> subscription instance on every invocation (including synchronous replay).
        ///     为指定的 <typeparamref name="TEvent" /> 订阅强类型回调，并在每次调用时传入同一个
        ///     <see cref="IDisposable" /> 订阅实例（包括同步回放）。
        /// </summary>
        /// <typeparam name="TEvent">
        ///     Concrete lifecycle event type.
        ///     具体的生命周期事件类型。
        /// </typeparam>
        /// <param name="handler">
        ///     Invoked for each matching event. The <see cref="IDisposable" /> argument is the subscription; disposing it
        ///     unsubscribes the handler.
        ///     每次匹配事件到达时调用。<see cref="IDisposable" /> 参数就是该订阅；释放它会取消订阅该回调。
        /// </param>
        /// <param name="replayCurrentState">
        ///     When true, invokes <paramref name="handler" /> with the last replayable event if present.
        ///     为 true 时，如果存在最后一次可回放事件，则使用该事件调用 <paramref name="handler" />。
        /// </param>
        /// <returns>
        ///     Disposing unsubscribes the handler.
        ///     释放返回值会取消订阅该回调。
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
                        ? ReplayableLifecycleEvents.Values
                            .Cast<IFrameworkLifecycleEvent>()
                            .OrderBy(evt => evt.OccurredAtUtc)
                            .ToArray()
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
                    handler(evt, subscription!);
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"[Lifecycle] Observer callback failed in {LifecycleEventTypeCache<TEvent>.EventName}: {ex.Message}"
                    );
                }
            }
        }

        /// <summary>
        ///     Initializes the shared framework: settings, patch registration, and lifecycle publication.
        ///     初始化共享框架，包括设置、补丁注册和生命周期事件发布。
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

                Logger = CreateLogger(Const.ModId);

                Logger.Info($"Framework ID: {Const.ModId}");
                Logger.Info($"Framework Name: {Const.Name}");
                Logger.Info(BuildVersionLogText());
                Logger.Info("Initializing shared framework...");
                RitsuLibMobileSteamRuntime.LogSuppressedSteamFeaturesAtStartup();
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
                    var modDataInteropRegistered = ModDataRuntimeInterop.TryRegisterAll();
                    if (modDataInteropRegistered > 0)
                        Logger.Debug(
                            $"ModData runtime interop: mirror-registered {modDataInteropRegistered} provider schema(s).");

                    EnsureFrameworkInteropBootstrapRegistered();
                    RuntimeHotkeyService.Initialize();
                    RitsuToastService.Initialize();

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

        private static string BuildVersionLogText()
        {
            var compatBranchLabel = GetCompatBranchLabel();
            return string.IsNullOrWhiteSpace(compatBranchLabel)
                ? $"Version: {Const.Version}"
                : $"Version: {Const.Version} [compat branch: {compatBranchLabel}]";
        }

        private static void EnsureFrameworkInteropBootstrapRegistered()
        {
            if (_frameworkInteropBootstrapRegistered)
                return;

            _frameworkInteropBootstrapRegistered = true;
            SubscribeLifecycle<DeferredInitializationCompletedEvent>(_ => ConfirmExternalFrameworkInterop());
        }

        private static void ConfirmExternalFrameworkInterop()
        {
            ExternalFrameworkRegistry.RefreshKnownFrameworkPresence("deferred initialization completed");
            BaseLibHealthBarForecastBridge.TryRegister();
            BaseLibVisualGraftBridge.TryRegister();
            BaseLibMaxHandSizeBridge.TryInitialize();
            MaxHandSizePatchInstaller.EnsurePatched();
        }

        private static string? GetCompatBranchLabel()
        {
#if !STS2_AT_LEAST_0_104_0
            return "0.103.2";
#elif !STS2_AT_LEAST_0_105_0
            return "0.104.0";
#else
            return null;
#endif
        }

        /// <summary>
        ///     Ensures profile-bound services (<c>ProfileManager</c>, profile-scoped <c>ModDataStore</c>) are initialized once.
        ///     确保绑定到档案的服务（<c>ProfileManager</c>、档案作用域 <c>ModDataStore</c>）只初始化一次。
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
                ModRunSidecarSession.AttachLifecycleHandlers();

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
        ///     Begins a registration scope for the given mod's <c>ModDataStore</c> entries.
        ///     为指定 Mod 的 <c>ModDataStore</c> 条目开启注册作用域。
        /// </summary>
        /// <param name="modId">
        ///     Owning mod identifier.
        ///     所属 Mod 标识符。
        /// </param>
        /// <param name="initializeProfileIfReady">
        ///     When true, initializes profile services if the profile is already ready.
        ///     为 true 时，如果档案已经就绪，则初始化档案服务。
        /// </param>
        /// <returns>
        ///     Disposing ends the registration scope.
        ///     释放返回值会结束该注册作用域。
        /// </returns>
        public static IDisposable BeginModDataRegistration(string modId, bool initializeProfileIfReady = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return ModDataStore.For(modId).BeginRegistrationScope(initializeProfileIfReady);
        }

        /// <summary>
        ///     Returns the persistent data store facade for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的持久化数据存储门面。
        /// </summary>
        public static ModDataStore GetDataStore(string modId)
        {
            return ModDataStore.For(modId);
        }

        /// <summary>
        ///     Returns the content registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的内容注册表。
        /// </summary>
        public static ModContentRegistry GetContentRegistry(string modId)
        {
            return ModContentRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the keyword registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的关键词注册表。
        /// </summary>
        public static ModKeywordRegistry GetKeywordRegistry(string modId)
        {
            return ModKeywordRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the SmartFormat extension registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的 SmartFormat 扩展注册表。
        /// </summary>
        public static ModSmartFormatExtensionRegistry GetSmartFormatRegistry(string modId)
        {
            return ModSmartFormatExtensionRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the custom card-tag registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的自定义卡牌标签注册表。
        /// </summary>
        public static ModCardTagRegistry GetCardTagRegistry(string modId)
        {
            return ModCardTagRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the custom card-pile registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的自定义牌堆注册表。
        /// </summary>
        public static ModCardPileRegistry GetCardPileRegistry(string modId)
        {
            return ModCardPileRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the top-bar button registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的顶部栏按钮注册表。
        /// </summary>
        public static ModTopBarButtonRegistry GetTopBarButtonRegistry(string modId)
        {
            return ModTopBarButtonRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the timeline (epoch/story) registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的时间线（纪元/故事）注册表。
        /// </summary>
        public static ModTimelineRegistry GetTimelineRegistry(string modId)
        {
            return ModTimelineRegistry.For(modId);
        }

        /// <summary>
        ///     Returns the unlock rules registry for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 对应的解锁规则注册表。
        /// </summary>
        public static ModUnlockRegistry GetUnlockRegistry(string modId)
        {
            return ModUnlockRegistry.For(modId);
        }

        /// <summary>
        ///     Registers a non-power health bar forecast source type through the framework.
        ///     通过框架注册一个非 Power 的生命条预测来源类型。
        /// </summary>
        public static void RegisterHealthBarForecast<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            HealthBarForecastRegistry.Register<TSource>(modId, sourceId);
        }

        /// <summary>
        ///     Registers a non-power health bar visual graft source type through the framework.
        ///     通过框架注册一个非 Power 的生命条视觉嫁接来源类型。
        /// </summary>
        public static void RegisterHealthBarVisualGraft<TSource>(string modId, string? sourceId = null)
            where TSource : IHealthBarVisualGraftSource, new()
        {
            HealthBarVisualGraftRegistry.Register<TSource>(modId, sourceId);
        }

        /// <summary>
        ///     Resolves the current max-hand-size value for <paramref name="player" />.
        ///     解析 <paramref name="player" /> 当前的最大手牌数。
        /// </summary>
        public static int GetMaxHandSize(Player player)
        {
            return MaxHandSizeCalculator.Calculate(player);
        }

        /// <summary>
        ///     Registers an additional free-play detector used by framework consumers (for example material logic).
        ///     注册一个额外的免费打出检测器，供框架消费者使用（例如材质逻辑）。
        /// </summary>
        public static void RegisterFreePlayBinding(string bindingId, Func<CardPlay, bool> detector)
        {
            FreePlayBindingRegistry.Register(bindingId, detector);
        }

        /// <summary>
        ///     Registers an initial-option injection rule for <typeparamref name="TAncient" />.
        ///     为 <typeparamref name="TAncient" /> 注册一个初始选项注入规则。
        /// </summary>
        public static void RegisterAncientOption<TAncient>(string modId, ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            GetContentRegistry(modId).RegisterAncientOption<TAncient>(rule);
        }

        /// <summary>
        ///     Creates a content pack builder for <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 创建内容包构建器。
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
                registration.Apply(context);
            }

            Logger.Info($"[ContentPack] Flushed {pending.Count} deferred content pack(s).");
        }

        /// <summary>
        ///     Starts a batch PNG export of registered cards (see <see cref="CardPngExporter" />).
        ///     启动已注册卡牌的批量 PNG 导出（参见 <see cref="CardPngExporter" />）。
        /// </summary>
        /// <param name="request">
        ///     Output directory, scale, hover panel, filters, etc.
        ///     输出目录、缩放、悬停面板、过滤器等导出参数。
        /// </param>
        /// <param name="issuingPlayer">
        ///     Optional; export does not require a run or player.
        ///     可选参数；导出不要求存在当前 run 或玩家。
        /// </param>
        public static void BeginCardPngExport(CardPngExportRequest request, Player? issuingPlayer = null)
        {
            CardPngExporter.BeginExport(request, issuingPlayer, msg => Logger.Info(msg));
        }

        /// <summary>
        ///     Starts a batch PNG export of compendium-style detail panels: relic <c>inspect_relic_screen</c> popup, and
        ///     potion lab focus (scaled <c>NPotion</c> + hovers). Does not use save / unlock gating; content is the “seen
        ///     unlocked” form.
        ///     启动百科详情面板的批量 PNG 导出：遗物 <c>inspect_relic_screen</c> 弹窗，以及药水实验室焦点视图
        ///     （缩放后的 <c>NPotion</c> 加悬停提示）。不会使用存档/解锁门槛；内容按“已见且已解锁”形态导出。
        /// </summary>
        public static void BeginCompendiumDetailPngExport(CompendiumPngExportRequest request)
        {
            CompendiumDetailPngExporter.BeginExport(request, msg => Logger.Info(msg));
        }

        /// <summary>
        ///     Declares a <c>mod_data</c> JSON path that may participate in RitsuLib Steam Cloud sync when the player enables
        ///     it and the session uses Steam Cloud. Prefer ModDataStore.Register when you already use
        ///     <see cref="Data.ModDataStore" />; this call is for custom persistence that still resolves via
        ///     <see cref="Utils.Persistence.ProfileManager" />.
        ///     声明一个可参与 RitsuLib Steam 云同步的 <c>mod_data</c> JSON 路径；仅当玩家启用同步且当前会话使用
        ///     Steam 云时生效。若已经使用 <see cref="Data.ModDataStore" />，优先使用 ModDataStore.Register；
        ///     此调用面向仍通过 <see cref="Utils.Persistence.ProfileManager" /> 解析的自定义持久化。
        /// </summary>
        public static void RegisterModCloudPersistedSlot(string modId, string fileName, SaveScope scope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ModCloudSyncPathRegistry.RegisterModDataSlot(modId, fileName, scope);
        }

        /// <summary>
        ///     Registers a page in the RitsuLib mod settings submenu.
        ///     在 RitsuLib Mod 设置子菜单中注册一个页面。
        /// </summary>
        /// <remarks>
        ///     Optional layout: <see cref="ModSettingsUiPresentation.ParagraphMaxBodyHeight" />.
        ///     可选布局项：<see cref="ModSettingsUiPresentation.ParagraphMaxBodyHeight" />。
        /// </remarks>
        public static void RegisterModSettings(string modId, Action<ModSettingsPageBuilder> configure,
            string? pageId = null)
        {
            ModSettingsRegistry.Register(modId, configure, pageId);
        }

        /// <summary>
        ///     Registers a reflection-based settings provider type for attribute-driven settings pages.
        ///     注册一个基于反射的设置提供器类型，用于属性驱动的设置页。
        /// </summary>
        public static bool RegisterModSettingsReflectionProvider<TProvider>()
        {
            return RuntimeReflectionMirrorSource.RegisterProviderType<TProvider>();
        }

        /// <summary>
        ///     Registers a reflection-based settings provider type for attribute-driven settings pages.
        ///     注册一个基于反射的设置提供器类型，用于属性驱动的设置页。
        /// </summary>
        public static bool RegisterModSettingsReflectionProvider(Type providerType)
        {
            return RuntimeReflectionMirrorSource.RegisterProviderType(providerType);
        }

        /// <summary>
        ///     Registers a reflection provider and immediately attempts to mirror-register its pages.
        ///     注册一个反射提供器，并立即尝试镜像注册其页面。
        /// </summary>
        public static int RegisterModSettingsReflectionProviderAndTryRegister<TProvider>()
        {
            return RuntimeReflectionMirrorSource.RegisterProviderTypeAndTryRegister<TProvider>();
        }

        /// <summary>
        ///     Registers a reflection provider and immediately attempts to mirror-register its pages.
        ///     注册一个反射提供器，并立即尝试镜像注册其页面。
        /// </summary>
        public static int RegisterModSettingsReflectionProviderAndTryRegister(Type providerType)
        {
            return RuntimeReflectionMirrorSource.RegisterProviderTypeAndTryRegister(providerType);
        }

        /// <summary>
        ///     Sets ordering for this mod&apos;s group in the RitsuLib mod settings sidebar (lower first). Mods without a
        ///     value use <c>0</c> and sort by display name. Prefer <see cref="ModSettingsPageBuilder.WithModSidebarOrder" /> when
        ///     registering pages.
        ///     设置该 Mod 分组在 RitsuLib Mod 设置侧边栏中的排序（数值越小越靠前）。没有设置值的 Mod 使用 <c>0</c>，
        ///     并按显示名称排序。注册页面时优先使用 <see cref="ModSettingsPageBuilder.WithModSidebarOrder" />。
        /// </summary>
        public static void RegisterModSettingsSidebarOrder(string modId, int order)
        {
            ModSettingsRegistry.RegisterModSidebarOrder(modId, order);
        }

        /// <summary>
        ///     Overrides sort order for a registered page among siblings (same mod and parent page).
        ///     覆盖已注册页面在同级页面中的排序（同一 Mod 且同一父页面）。
        /// </summary>
        public static void RegisterModSettingsPageOrder(string modId, string pageId, int sortOrder)
        {
            ModSettingsRegistry.RegisterPageSortOrder(modId, pageId, sortOrder);
        }

        /// <summary>
        ///     Places <paramref name="pageId" /> after <paramref name="afterPageId" /> in the sidebar for this mod.
        ///     将该 Mod 的 <paramref name="pageId" /> 放在侧边栏中 <paramref name="afterPageId" /> 之后。
        /// </summary>
        public static bool TryRegisterModSettingsPageOrderAfter(string modId, string pageId, string afterPageId,
            int gap = 1)
        {
            return ModSettingsRegistry.TryRegisterPageSortOrderAfter(modId, pageId, afterPageId, gap);
        }

        /// <summary>
        ///     Places <paramref name="pageId" /> before <paramref name="beforePageId" /> in the sidebar for this mod.
        ///     将该 Mod 的 <paramref name="pageId" /> 放在侧边栏中 <paramref name="beforePageId" /> 之前。
        /// </summary>
        public static bool TryRegisterModSettingsPageOrderBefore(string modId, string pageId, string beforePageId,
            int gap = 1)
        {
            return ModSettingsRegistry.TryRegisterPageSortOrderBefore(modId, pageId, beforePageId, gap);
        }

        /// <summary>
        ///     Returns all registered mod settings pages (same snapshot as <see cref="ModSettingsRegistry.GetPages" />).
        ///     返回所有已注册的 Mod 设置页（与 <see cref="ModSettingsRegistry.GetPages" /> 相同的快照）。
        /// </summary>
        public static IReadOnlyList<ModSettingsPage> GetRegisteredModSettings()
        {
            return ModSettingsRegistry.GetPages();
        }

        /// <summary>
        ///     Creates a <c>MegaCrit.Sts2.Core.Logging.Logger</c> for <paramref name="modId" />.
        ///     为 <paramref name="modId" /> 创建 <c>MegaCrit.Sts2.Core.Logging.Logger</c>。
        /// </summary>
        public static Logger CreateLogger(string modId, LogType logType = LogType.Generic)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId, logType);
        }

        /// <summary>
        ///     Creates a <see cref="STS2RitsuLib.Patching.Core.ModPatcher" /> with a dedicated logger for the owning mod.
        ///     为所属 Mod 创建一个带专用日志器的 <see cref="STS2RitsuLib.Patching.Core.ModPatcher" />。
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
        ///     Creates a <see cref="STS2RitsuLib.Utils.I18N" /> instance with optional file, embedded resource, and PCK
        ///     translation roots.
        ///     创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例，可选配置文件、嵌入资源和 PCK 翻译根路径。
        /// </summary>
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

        /// <summary>
        ///     Creates a <see cref="STS2RitsuLib.Utils.I18N" /> instance for a mod, defaulting the file-system folder to
        ///     <c>user://&lt;platform&gt;/&lt;userId&gt;/mod_data/{modId}/localization</c> when none are supplied.
        ///     为某个 Mod 创建 <see cref="STS2RitsuLib.Utils.I18N" /> 实例；未提供文件系统目录时，默认使用
        ///     <c>user://&lt;platform&gt;/&lt;userId&gt;/mod_data/{modId}/localization</c>。
        /// </summary>
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

            var folders = fileSystemFolders?.ToArray() ?? [$"{ProfileManager.GetAccountBasePath(modId)}/localization"];
            return CreateLocalization(instanceName, folders, resourceFolders, pckFolders, resourceAssembly);
        }

        /// <summary>
        ///     Returns the virtual <c>LocTable</c> id for an <see cref="I18N" /> bridge table using the framework's
        ///     standard three-segment id convention: <c>MODID_I18N_STEM</c>.
        ///     返回 <see cref="I18N" /> 桥接表对应的虚拟 <c>LocTable</c> id，使用框架标准三段式约定：
        ///     <c>MODID_I18N_STEM</c>。
        /// </summary>
        public static string GetI18NLocTableId(string modId, string stem = "DEFAULT")
        {
            return I18NLocTableBridge.GetTableId(modId, stem);
        }

        /// <summary>
        ///     Registers an <see cref="I18N" /> instance as a virtual <c>LocTable</c> so the game-native
        ///     <c>LocString</c> pipeline can resolve raw templates from it.
        ///     将 <see cref="I18N" /> 实例注册为虚拟 <c>LocTable</c>，使游戏原生 <c>LocString</c> 管线可从中解析原始模板。
        /// </summary>
        public static bool RegisterI18NLocTableBridge(string modId, I18N i18N, string stem = "DEFAULT",
            bool replaceExisting = false)
        {
            return I18NLocTableBridge.TryRegister(modId, i18N, stem, replaceExisting);
        }

        /// <summary>
        ///     Unregisters a previously registered virtual <c>LocTable</c> for the given <paramref name="modId" /> and
        ///     <paramref name="stem" />.
        ///     注销之前为指定 <paramref name="modId" /> 和 <paramref name="stem" /> 注册的虚拟 <c>LocTable</c>。
        /// </summary>
        public static bool UnregisterI18NLocTableBridge(string modId, string stem = "DEFAULT")
        {
            return I18NLocTableBridge.TryUnregister(modId, stem);
        }

        /// <summary>
        ///     Registers C# scripts from <paramref name="assembly" /> with Godot (once per assembly).
        ///     将 <paramref name="assembly" /> 中的 C# 脚本注册到 Godot（每个程序集只注册一次）。
        /// </summary>
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

                var lookup = lookupMethod.CreateDelegate<Action<Assembly>>();
                lookup(assembly);
                logger?.Debug($"Registered Godot C# scripts for assembly: {assemblyName}");
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to register Godot C# scripts for assembly {assemblyName}: {ex.Message}");
                logger?.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        ///     Applies all patches on <paramref name="patcher" />; on failure logs, invokes <paramref name="disableMod" />, and
        ///     returns false.
        ///     应用 <paramref name="patcher" /> 上的所有补丁；失败时记录日志、调用 <paramref name="disableMod" />，并返回 false。
        /// </summary>
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
