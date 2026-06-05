using System.Reflection;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes profile initialization, switching, progress save, and profile deletion lifecycle events around
    ///     <see cref="SaveManager" /> APIs.
    ///     围绕 <see cref="SaveManager" /> API 发布档案初始化、切换、进度保存和档案删除生命周期事件。
    /// </summary>
    public class SaveManagerLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "save_manager_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish profile and progress save lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.InitProfileId), [typeof(int?)]),
                new(typeof(SaveManager), nameof(SaveManager.SwitchProfileId), [typeof(int)]),
                new(typeof(SaveManager), nameof(SaveManager.SaveProgressFile), Type.EmptyTypes),
                new(typeof(SaveManager), nameof(SaveManager.DeleteProfile), [typeof(int)]),
            ];
        }

        /// <summary>
        ///     Harmony prefix: publishes events before profile switch, progress save, or profile delete run.
        ///     Harmony prefix：在档案切换、进度保存或档案删除运行前发布事件。
        /// </summary>
        public static void Prefix(MethodBase __originalMethod, SaveManager __instance, object[] __args)
        {
            switch (__originalMethod.Name)
            {
                case nameof(SaveManager.SwitchProfileId):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileSwitchingEvent(
                            __instance,
                            TryGetCurrentProfileId(__instance),
                            (int)__args[0],
                            DateTimeOffset.UtcNow
                        ),
                        nameof(ProfileSwitchingEvent)
                    );
                    break;
                case nameof(SaveManager.SaveProgressFile):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProgressSavingEvent(__instance, TryGetCurrentProfileId(__instance), DateTimeOffset.UtcNow),
                        nameof(ProgressSavingEvent)
                    );
                    break;
                case nameof(SaveManager.DeleteProfile):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileDeletingEvent(__instance, (int)__args[0], DateTimeOffset.UtcNow),
                        nameof(ProfileDeletingEvent)
                    );
                    break;
            }
        }

        /// <summary>
        ///     Harmony postfix: publishes events after profile id init/switch, progress save, or profile delete complete.
        ///     Harmony postfix：在档案 ID 初始化/切换、进度保存或档案删除完成后发布事件。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, SaveManager __instance, object[] __args)
        {
            switch (__originalMethod.Name)
            {
                case nameof(SaveManager.InitProfileId):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileIdInitializedEvent(__instance, __instance.CurrentProfileId, DateTimeOffset.UtcNow),
                        nameof(ProfileIdInitializedEvent)
                    );
                    break;
                case nameof(SaveManager.SwitchProfileId):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileSwitchedEvent(
                            __instance,
                            __args[0] is int nextProfileId
                                ? TryGetCurrentProfileId(__instance) == nextProfileId ? null : nextProfileId
                                : null,
                            __instance.CurrentProfileId,
                            DateTimeOffset.UtcNow
                        ),
                        nameof(ProfileSwitchedEvent)
                    );
                    break;
                case nameof(SaveManager.SaveProgressFile):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProgressSavedEvent(__instance, TryGetCurrentProfileId(__instance), DateTimeOffset.UtcNow),
                        nameof(ProgressSavedEvent)
                    );
                    break;
                case nameof(SaveManager.DeleteProfile):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new ProfileDeletedEvent(__instance, (int)__args[0], DateTimeOffset.UtcNow),
                        nameof(ProfileDeletedEvent)
                    );
                    break;
            }
        }

        private static int? TryGetCurrentProfileId(SaveManager saveManager)
        {
            try
            {
                return saveManager.CurrentProfileId;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Publishes lifecycle events when a run is saved through <see cref="SaveManager.SaveRun" />.
    ///     当通过 <see cref="SaveManager.SaveRun" /> 保存跑局时发布生命周期事件。
    /// </summary>
    public class RunSavingLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "run_saving_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish run save lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.SaveRun), [typeof(AbstractRoom), typeof(bool)]),
            ];
        }

        /// <summary>
        ///     Harmony prefix: publishes <see cref="RunSavingEvent" /> before the async save begins.
        ///     Harmony prefix：在异步保存开始前发布 <see cref="RunSavingEvent" />。
        /// </summary>
        public static void Prefix(SaveManager __instance, AbstractRoom? preFinishedRoom, bool saveProgress)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new RunSavingEvent(__instance, preFinishedRoom, saveProgress, DateTimeOffset.UtcNow),
                nameof(RunSavingEvent)
            );
        }

        /// <summary>
        ///     Harmony postfix: chains onto the save task and publishes <see cref="RunSavedEvent" /> when it completes.
        ///     Harmony postfix：链接到保存任务，并在其完成时发布 <see cref="RunSavedEvent" />。
        /// </summary>
        public static void Postfix(SaveManager __instance, AbstractRoom? preFinishedRoom, bool saveProgress,
            ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RunSavedEvent(__instance, preFinishedRoom, saveProgress, DateTimeOffset.UtcNow),
                    nameof(RunSavedEvent)
                ));
        }
    }
}
