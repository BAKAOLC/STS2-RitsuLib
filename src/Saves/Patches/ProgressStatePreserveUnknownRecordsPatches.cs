using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Validation;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Saves.Patches
{
    /// <summary>
    ///     Captures mod progress records that vanilla parsing would skip while the owning content is unavailable.
    /// </summary>
    internal sealed class ProgressStatePreserveUnknownRecordsFromSerializablePatch : IPatchMethod
    {
        public static string PatchId => "progress_state_preserve_unknown_records_from_serializable";

        public static string Description =>
            "Snapshot unavailable mod progress records before vanilla ProgressState parsing filters them";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressState), nameof(ProgressState.FromSerializable),
                    [typeof(SerializableProgress), typeof(DeserializationContext)]),
            ];
        }

        public static void Prefix(SerializableProgress save, out PreservedProgressRecords? __state)
        {
            ProgressMirrorStore.MergeMirrorInto(save);
            __state = PreservedProgressRecords.Capture(save);
        }

        public static void Postfix(ProgressState __result, DeserializationContext ctx,
            PreservedProgressRecords? __state)
        {
            PreservedProgressRecords.Attach(__result, __state);
            __state?.SuppressExpectedWarnings(ctx);
        }
    }

    /// <summary>
    ///     Restores preserved unavailable mod records into the serializable progress payload before saving.
    /// </summary>
    internal sealed class ProgressStatePreserveUnknownRecordsToSerializablePatch : IPatchMethod
    {
        public static string PatchId => "progress_state_preserve_unknown_records_to_serializable";

        public static string Description =>
            "Merge unavailable mod progress records back into SerializableProgress before saving";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressState), nameof(ProgressState.ToSerializable), Type.EmptyTypes)];
        }

        public static void Postfix(ProgressState __instance, SerializableProgress __result)
        {
            PreservedProgressRecords.MergeInto(__instance, __result);
            ProgressMirrorStore.SaveMirror(__result);
        }
    }

    /// <summary>
    ///     Refreshes the progress mirror after a real progress load completes.
    /// </summary>
    internal sealed class ProgressStatePreserveUnknownRecordsLoadProgressPatch : IPatchMethod
    {
        public static string PatchId => "progress_state_preserve_unknown_records_load_progress";
        public static string Description => "Refresh progress mirror after ProgressSaveManager.LoadProgress";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress), Type.EmptyTypes)];
        }

        public static void Postfix(ProgressSaveManager __instance, ReadSaveResult<SerializableProgress> __result)
        {
            if (__result is { Success: true, SaveData: not null })
                ProgressMirrorStore.RefreshFromProgress(__instance.Progress);
        }
    }
}
