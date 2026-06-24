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
    internal static class SaveLifecycleProfileId
    {
        internal static int? TryGetCurrentProfileId(SaveManager saveManager)
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

    internal sealed class ProfileIdInitializedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "save_manager_lifecycle_init_profile_id";
        public static string Description => "Publish profile id initialized lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.InitProfileId), [typeof(int?)])];
        }

        public static void Postfix(SaveManager __instance)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileIdInitializedEvent(__instance, __instance.CurrentProfileId, DateTimeOffset.UtcNow),
                nameof(ProfileIdInitializedEvent));
        }
    }

    internal sealed class ProfileSwitchLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "save_manager_lifecycle_switch_profile_id";
        public static string Description => "Publish profile switching lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.SwitchProfileId), [typeof(int)])];
        }

        public static void Prefix(SaveManager __instance, int __0)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileSwitchingEvent(
                    __instance,
                    SaveLifecycleProfileId.TryGetCurrentProfileId(__instance),
                    __0,
                    DateTimeOffset.UtcNow),
                nameof(ProfileSwitchingEvent));
        }

        public static void Postfix(SaveManager __instance, int __0)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileSwitchedEvent(
                    __instance,
                    SaveLifecycleProfileId.TryGetCurrentProfileId(__instance) == __0 ? null : __0,
                    __instance.CurrentProfileId,
                    DateTimeOffset.UtcNow),
                nameof(ProfileSwitchedEvent));
        }
    }

    internal sealed class ProgressSaveLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "save_manager_lifecycle_save_progress_file";
        public static string Description => "Publish progress save lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.SaveProgressFile), Type.EmptyTypes)];
        }

        public static void Prefix(SaveManager __instance)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProgressSavingEvent(
                    __instance,
                    SaveLifecycleProfileId.TryGetCurrentProfileId(__instance),
                    DateTimeOffset.UtcNow),
                nameof(ProgressSavingEvent));
        }

        public static void Postfix(SaveManager __instance)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProgressSavedEvent(
                    __instance,
                    SaveLifecycleProfileId.TryGetCurrentProfileId(__instance),
                    DateTimeOffset.UtcNow),
                nameof(ProgressSavedEvent));
        }
    }

    internal sealed class ProfileDeleteLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "save_manager_lifecycle_delete_profile";
        public static string Description => "Publish profile delete lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.DeleteProfile), [typeof(int)])];
        }

        public static void Prefix(SaveManager __instance, int __0)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileDeletingEvent(__instance, __0, DateTimeOffset.UtcNow),
                nameof(ProfileDeletingEvent));
        }

        public static void Postfix(SaveManager __instance, int __0)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new ProfileDeletedEvent(__instance, __0, DateTimeOffset.UtcNow),
                nameof(ProfileDeletedEvent));
        }
    }

    /// <summary>
    ///     Publishes lifecycle events when a run is saved through <see cref="SaveManager.SaveRun" />.
    ///     当通过 <see cref="SaveManager.SaveRun" /> 保存跑局时发布生命周期事件。
    /// </summary>
    internal class RunSavingLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "run_saving_lifecycle";
        public static string Description => "Publish run save lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.SaveRun), [typeof(AbstractRoom), typeof(bool)]),
            ];
        }

        public static void Prefix(SaveManager __instance, AbstractRoom? preFinishedRoom, bool saveProgress)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new RunSavingEvent(__instance, preFinishedRoom, saveProgress, DateTimeOffset.UtcNow),
                nameof(RunSavingEvent)
            );
        }

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
