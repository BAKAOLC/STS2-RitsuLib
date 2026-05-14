using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes <see cref="EssentialInitializationStartingEvent" /> / <see cref="DeferredInitializationStartingEvent" />
    ///     and matching completed events around vanilla one-time initialization.
    ///     在原版一次性初始化前后发布 <see cref="EssentialInitializationStartingEvent" /> /
    ///     <see cref="DeferredInitializationStartingEvent" />
    ///     以及对应的 completed 事件。
    /// </summary>
    public class CoreInitializationLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "core_initialization_lifecycle";

        /// <inheritdoc />
        public static string Description =>
            "Publish framework lifecycle events around essential and deferred initialization";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential)),
                new(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Emits “starting” lifecycle events before essential or deferred initialization runs.
        ///     在必要或延迟初始化运行前发出“starting”生命周期事件。
        /// </summary>
        public static void Prefix(MethodBase __originalMethod)
        {
            switch (__originalMethod.Name)
            {
                case nameof(OneTimeInitialization.ExecuteEssential):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EssentialInitializationStartingEvent(DateTimeOffset.UtcNow),
                        nameof(EssentialInitializationStartingEvent)
                    );
                    break;
                case nameof(OneTimeInitialization.ExecuteDeferred):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new DeferredInitializationStartingEvent(DateTimeOffset.UtcNow),
                        nameof(DeferredInitializationStartingEvent)
                    );
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Emits “completed” lifecycle events after essential or deferred initialization runs.
        ///     在必要或延迟初始化运行后发出“completed”生命周期事件。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod)
        {
            switch (__originalMethod.Name)
            {
                case nameof(OneTimeInitialization.ExecuteEssential):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EssentialInitializationCompletedEvent(DateTimeOffset.UtcNow),
                        nameof(EssentialInitializationCompletedEvent)
                    );
                    break;
                case nameof(OneTimeInitialization.ExecuteDeferred):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new DeferredInitializationCompletedEvent(DateTimeOffset.UtcNow),
                        nameof(DeferredInitializationCompletedEvent)
                    );
                    break;
            }
        }
    }

    /// <summary>
    ///     Hooks <see cref="ModelDb" /> and related init to freeze registries, inject models, and publish model lifecycle
    ///     events.
    ///     hook <see cref="ModelDb" /> 和相关 init，以冻结注册表、注入模型并发布模型生命周期
    ///     事件。
    /// </summary>
    public class ModelRegistryLifecyclePatch : IPatchMethod
    {
        private static readonly FieldInfo? ReflectionHelperModTypesField =
            typeof(ReflectionHelper).GetField("_modTypes", BindingFlags.Static | BindingFlags.NonPublic);

        /// <inheritdoc />
        public static string PatchId => "model_registry_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish lifecycle events around ModelDb initialization phases";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ModelDb), nameof(ModelDb.Init)),
                new(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init)),
                new(typeof(ModelDb), nameof(ModelDb.InitIds)),
                new(typeof(ModelDb), nameof(ModelDb.Preload)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Runs registry freezes, validation, and “starting” model lifecycle events before targeted init methods.
        ///     在目标初始化方法前运行注册表冻结、验证和“starting”模型生命周期事件。
        /// </summary>
        public static void Prefix(MethodBase __originalMethod)
        {
            switch (__originalMethod.DeclaringType, __originalMethod.Name)
            {
                case ({ } declaringType, nameof(ModelDb.Init)) when declaringType == typeof(ModelDb):
                    ModContentRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModKeywordRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModCardTagRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModCardPileRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModTimelineRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModEpochGatedContentRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    ModUnlockRegistry.FreezeRegistrations(nameof(ModelDb.Init));
                    RegistrationConflictDetector.ValidateAndLogModelIdCollisions();
                    RefreshModTypeCache();
                    ModContentRegistry.InjectDynamicRegisteredModels();
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelRegistryInitializingEvent(DateTimeOffset.UtcNow),
                        nameof(ModelRegistryInitializingEvent)
                    );
                    break;
                case ({ } declaringType, nameof(ModelIdSerializationCache.Init))
                    when declaringType == typeof(ModelIdSerializationCache):
                    RefreshModTypeCache();
                    break;
                case ({ } declaringType, nameof(ModelDb.InitIds)) when declaringType == typeof(ModelDb):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelIdsInitializingEvent(DateTimeOffset.UtcNow),
                        nameof(ModelIdsInitializingEvent)
                    );
                    break;
                case ({ } declaringType, nameof(ModelDb.Preload)) when declaringType == typeof(ModelDb):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelPreloadingStartingEvent(DateTimeOffset.UtcNow),
                        nameof(ModelPreloadingStartingEvent)
                    );
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Publishes “completed” model lifecycle events after <see cref="ModelDb" /> init phases.
        ///     在 <see cref="ModelDb" /> 初始化阶段后发布“completed”模型生命周期事件。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod)
        {
            switch (__originalMethod.Name)
            {
                case nameof(ModelDb.Init):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelRegistryInitializedEvent(ModelDb.AllAbstractModelSubtypes.Length,
                            DateTimeOffset.UtcNow),
                        nameof(ModelRegistryInitializedEvent)
                    );
                    break;
                case nameof(ModelDb.InitIds):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelIdsInitializedEvent(DateTimeOffset.UtcNow),
                        nameof(ModelIdsInitializedEvent)
                    );
                    break;
                case nameof(ModelDb.Preload):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelPreloadingCompletedEvent(DateTimeOffset.UtcNow),
                        nameof(ModelPreloadingCompletedEvent)
                    );
                    break;
            }
        }

        private static void RefreshModTypeCache()
        {
            ReflectionHelperModTypesField?.SetValue(null, null);
        }
    }

    /// <summary>
    ///     Publishes <see cref="GameTreeEnteredEvent" /> and <see cref="GameReadyEvent" /> for <see cref="NGame" />.
    ///     为 <see cref="NGame" /> 发布 <see cref="GameTreeEnteredEvent" /> 和 <see cref="GameReadyEvent" />。
    /// </summary>
    public class GameNodeLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "game_node_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish lifecycle events when NGame enters the tree and becomes ready";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NGame), nameof(NGame._EnterTree)),
                new(typeof(NGame), nameof(NGame._Ready)),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches game tree / ready lifecycle events based on which <see cref="NGame" /> method was patched.
        ///     根据被补丁的 <see cref="NGame" /> 方法分发游戏树/就绪生命周期事件。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, NGame __instance)
            // ReSharper restore InconsistentNaming
        {
            switch (__originalMethod.Name)
            {
                case nameof(NGame._EnterTree):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new GameTreeEnteredEvent(__instance, DateTimeOffset.UtcNow),
                        nameof(GameTreeEnteredEvent)
                    );
                    break;
                case nameof(NGame._Ready):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new GameReadyEvent(__instance, DateTimeOffset.UtcNow),
                        nameof(GameReadyEvent)
                    );
                    break;
            }
        }
    }

    /// <summary>
    ///     Publishes <see cref="RunStartedEvent" /> and <see cref="RunLoadedEvent" /> from <see cref="RunManager" />.
    ///     从 <see cref="RunManager" /> 发布 <see cref="RunStartedEvent" /> 和 <see cref="RunLoadedEvent" />。
    /// </summary>
    public class RunLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "run_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish lifecycle events for run creation and loading";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), "InitializeNewRun"),
                new(typeof(RunManager), "InitializeSavedRun", [typeof(SerializableRun)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Emits run started/loaded events after new or saved run initialization.
        ///     在新跑局或存档跑局初始化后发出跑局开始/加载事件。
        /// </summary>
        public static void Postfix(
                MethodBase __originalMethod,
                RunManager __instance)
            // ReSharper restore InconsistentNaming
        {
            var state = __instance.State;
            var isMultiplayer = __instance.NetService != null && __instance.NetService.Type != NetGameType.Singleplayer;
            var isDaily = __instance.DailyTime.HasValue;

            switch (__originalMethod.Name)
            {
                case "InitializeNewRun" when state != null:
                    RitsuLibFramework.PublishLifecycleEvent(
                        new RunStartedEvent(state, isMultiplayer, isDaily, DateTimeOffset.UtcNow),
                        nameof(RunStartedEvent)
                    );
                    break;
                case "InitializeSavedRun" when state != null:
                    RitsuLibFramework.PublishLifecycleEvent(
                        new RunLoadedEvent(state, isMultiplayer, isDaily, DateTimeOffset.UtcNow),
                        nameof(RunLoadedEvent)
                    );
                    break;
            }
        }
    }

    /// <summary>
    ///     Publishes <see cref="RunEndedEvent" /> and forwards run end to <see cref="ModUnlockRegistry" />.
    ///     发布 <see cref="RunEndedEvent" />，并将跑局结束转发给 <see cref="ModUnlockRegistry" />。
    /// </summary>
    public class RunEndedLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "run_ended_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish lifecycle events when a run ends";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.OnEnded), [typeof(bool)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Runs unlock bookkeeping and publishes <see cref="RunEndedEvent" /> when a run terminates.
        ///     当跑局终止时运行解锁记账并发布 <see cref="RunEndedEvent" />。
        /// </summary>
        public static void Postfix(RunManager __instance, bool isVictory, SerializableRun __result)
            // ReSharper restore InconsistentNaming
        {
            ModUnlockRegistry.ProcessRunEnded(__instance, __result, isVictory, __instance.IsAbandoned);
            RitsuLibFramework.PublishLifecycleEvent(
                new RunEndedEvent(__result, isVictory, __instance.IsAbandoned, DateTimeOffset.UtcNow),
                nameof(RunEndedEvent)
            );
        }
    }
}
