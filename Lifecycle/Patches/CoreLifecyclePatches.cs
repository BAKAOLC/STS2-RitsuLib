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
using STS2RitsuLib.Relics;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Publishes <see cref="EssentialInitializationStartingEvent" /> /
    ///         <see cref="DeferredInitializationStartingEvent" /> and matching completed events around vanilla one-time
    ///         initialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在原版一次性初始化前后发布 <see cref="EssentialInitializationStartingEvent" /> /
    ///         <see cref="DeferredInitializationStartingEvent" /> 以及对应的 completed 事件。
    ///     </para>
    /// </summary>
    internal class CoreInitializationLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "core_initialization_lifecycle";

        public static string Description =>
            "Publish framework lifecycle events around essential and deferred initialization";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential)),
                new(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteDeferred)),
            ];
        }

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
    ///     <para xml:lang="en">
    ///         Hooks <see cref="ModelDb" /> and related init to freeze registries, inject models, and publish
    ///         model lifecycle events.
    ///     </para>
    ///     <para xml:lang="zh-CN">hook <see cref="ModelDb" /> 和相关 init，以冻结注册表、注入模型并发布模型生命周期事件。</para>
    /// </summary>
    internal class ModelRegistryLifecyclePatch : IPatchMethod
    {
        private static readonly FieldInfo? ReflectionHelperModTypesField =
            typeof(ReflectionHelper).GetField("_modTypes", BindingFlags.Static | BindingFlags.NonPublic);

        public static string PatchId => "model_registry_lifecycle";
        public static string Description => "Publish lifecycle events around ModelDb initialization phases";
        public static bool IsCritical => false;

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
                    RitsuLibStartupAudit.Measure("modelDb.validateCollisions",
                        RegistrationConflictDetector.ValidateAndLogModelIdCollisions);
                    RefreshModTypeCache("ModelDb.Init");
                    RitsuLibStartupAudit.Measure("modelDb.injectDynamicModels",
                        ModContentRegistry.InjectDynamicRegisteredModels);
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ModelRegistryInitializingEvent(DateTimeOffset.UtcNow),
                        nameof(ModelRegistryInitializingEvent)
                    );
                    break;
                case ({ } declaringType, nameof(ModelIdSerializationCache.Init))
                    when declaringType == typeof(ModelIdSerializationCache):
                    RefreshModTypeCache("ModelIdSerializationCache.Init");
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

        public static void Postfix(MethodBase __originalMethod)
        {
            switch (__originalMethod.Name)
            {
                case nameof(ModelDb.Init):
                    ValidateFrozenRegistrations(nameof(ModelDb.Init));
                    RitsuLibStartupAudit.Measure("modelDb.warmResolvedCaches",
                        ModContentRegistry.WarmResolvedModelCaches);
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

        private static void RefreshModTypeCache(string phase)
        {
            if (ReflectionHelperModTypesField is null)
                return;

            if (ReflectionHelperModTypesField.GetValue(null) is Type[] cachedTypes)
                RitsuLibFramework.Logger.Warn(
                    $"[ModelDb] ReflectionHelper.ModTypes was already cached before {phase} ({cachedTypes.Length} types). " +
                    "Clearing the cache so the next scan includes all loaded mod types; another mod likely accessed " +
                    "ReflectionHelper.ModTypes before mod loading completed.");

            ReflectionHelperModTypesField.SetValue(null, null);
        }

        private static void ValidateFrozenRegistrations(string reason)
        {
            RegistrationFreezeDiagnostics.WarnRecordedFailures(reason);
            ModContentRegistry.ValidateFrozenModelReferences();
            ModEpochGatedContentRegistry.ValidateFrozenModelReferences();
            ModUnlockRegistry.ValidateFrozenModelReferences();
            OrobasAncientUpgradeRegistry.ValidateFrozenRegistrations();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Publishes <see cref="GameTreeEnteredEvent" /> and <see cref="GameReadyEvent" /> for
    ///         <see cref="NGame" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="NGame" /> 发布 <see cref="GameTreeEnteredEvent" /> 和
    ///         <see cref="GameReadyEvent" />。
    ///     </para>
    /// </summary>
    internal class GameNodeLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "game_node_lifecycle";
        public static string Description => "Publish lifecycle events when NGame enters the tree and becomes ready";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NGame), nameof(NGame._EnterTree)),
                new(typeof(NGame), nameof(NGame._Ready)),
            ];
        }

        public static void Postfix(MethodBase __originalMethod, NGame __instance)
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
    ///     <para xml:lang="en">
    ///         Publishes <see cref="RunStartedEvent" /> and <see cref="RunLoadedEvent" /> from
    ///         <see cref="RunManager" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从 <see cref="RunManager" /> 发布 <see cref="RunStartedEvent" /> 和
    ///         <see cref="RunLoadedEvent" />。
    ///     </para>
    /// </summary>
    internal class RunLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "run_lifecycle";
        public static string Description => "Publish lifecycle events for run creation and loading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), "InitializeNewRun"),
                new(typeof(RunManager), "InitializeSavedRun", [typeof(SerializableRun)]),
            ];
        }

        public static void Postfix(
            MethodBase __originalMethod,
            RunManager __instance)
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
    ///     <para xml:lang="en">
    ///         Publishes <see cref="RunEndedEvent" /> and forwards run end to <see cref="ModUnlockRegistry" />
    ///         .
    ///     </para>
    ///     <para xml:lang="zh-CN">发布 <see cref="RunEndedEvent" />，并将跑局结束转发给 <see cref="ModUnlockRegistry" />。</para>
    /// </summary>
    internal class RunEndedLifecyclePatch : IPatchMethod
    {
        private static readonly FieldInfo? RunHistoryWasUploadedField =
            typeof(RunManager).GetField("_runHistoryWasUploaded", BindingFlags.Instance | BindingFlags.NonPublic);

        public static string PatchId => "run_ended_lifecycle";
        public static string Description => "Publish lifecycle events when a run ends";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.OnEnded), [typeof(bool)]),
            ];
        }

        public static void Prefix(RunManager __instance, out bool __state)
        {
            __state = RunHistoryWasUploadedField?.GetValue(__instance) is true;
        }

        public static void Postfix(RunManager __instance, bool isVictory, SerializableRun __result, bool __state)
        {
            if (__state)
                return;

            ModUnlockRegistry.ProcessRunEnded(__instance, __result, isVictory, __instance.IsAbandoned);
            RitsuLibFramework.PublishLifecycleEvent(
                new RunEndedEvent(__result, isVictory, __instance.IsAbandoned, DateTimeOffset.UtcNow),
                nameof(RunEndedEvent)
            );
        }
    }
}
