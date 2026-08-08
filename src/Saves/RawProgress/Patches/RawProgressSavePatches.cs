using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Saves.RawProgress.Patches
{
    internal sealed class RawProgressOrdinarySavePatch : IPatchMethod
    {
        public static string PatchId => "raw_progress_exclusive_ordinary_save";
        public static string Description => "Route ordinary progress saves through the shared exclusive window";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.SaveProgress), Type.EmptyTypes)];
        }

        public static bool Prefix(ProgressSaveManager __instance)
        {
            RawProgressCommitBridge.SaveOrdinaryProgress(__instance);
            return false;
        }
    }

    internal sealed class RawProgressLoadPatch : IPatchMethod
    {
        public static string PatchId => "raw_progress_exclusive_load";
        public static string Description => "Capture and attach the raw progress shadow inside the exclusive window";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress), Type.EmptyTypes)];
        }

        public static void Prefix(ProgressSaveManager __instance, out RawProgressCommitBridge.LoadCapture? __state)
        {
            __state = RawProgressCommitBridge.BeginProgressLoad(__instance);
        }

        public static void Postfix(
            ProgressSaveManager __instance,
            ReadSaveResult<SerializableProgress> __result,
            RawProgressCommitBridge.LoadCapture? __state)
        {
            RawProgressCommitBridge.CompleteProgressLoad(__instance, __result, __state);
        }

        public static Exception? Finalizer(Exception? __exception)
        {
            RawProgressCommitBridge.EndProgressLoad();
            return __exception;
        }
    }

    internal sealed class RawProgressProfileMutationPatch : IPatchMethod
    {
        public static string PatchId => "raw_progress_exclusive_profile_mutation";
        public static string Description => "Keep profile switches and deletion outside raw progress commits";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.InitProfileId), [typeof(int?)]),
                new(typeof(SaveManager), nameof(SaveManager.SwitchProfileId), [typeof(int)]),
                new(typeof(SaveManager), nameof(SaveManager.DeleteProfile), [typeof(int)]),
            ];
        }

        public static void Prefix()
        {
            RawProgressCommitBridge.EnterProfileMutation();
        }

        public static Exception? Finalizer(Exception? __exception)
        {
            RawProgressCommitBridge.ExitProfileMutation();
            return __exception;
        }
    }
}
