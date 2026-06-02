using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Validation;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Saves.Patches
{
    /// <summary>
    ///     Captures mod progress records that vanilla parsing would skip while the owning content is unavailable.
    /// </summary>
    public sealed class ProgressStatePreserveUnknownRecordsFromSerializablePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "progress_state_preserve_unknown_records_from_serializable";

        /// <inheritdoc />
        public static string Description =>
            "Snapshot unavailable mod progress records before vanilla ProgressState parsing filters them";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressState), nameof(ProgressState.FromSerializable),
                    [typeof(SerializableProgress), typeof(DeserializationContext)]),
            ];
        }

        /// <summary>
        ///     Snapshots unavailable records before vanilla validation mutates nested lists.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Prefix(SerializableProgress save, out PreservedProgressRecords? __state)
        {
            ProgressMirrorStore.MergeMirrorInto(save);
            __state = PreservedProgressRecords.Capture(save);
        }

        /// <summary>
        ///     Attaches the snapshot to the parsed progress instance.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(ProgressState __result, DeserializationContext ctx,
                PreservedProgressRecords? __state)
            // ReSharper restore InconsistentNaming
        {
            PreservedProgressRecords.Attach(__result, __state);
            __state?.SuppressExpectedWarnings(ctx);
        }
    }

    /// <summary>
    ///     Restores preserved unavailable mod records into the serializable progress payload before saving.
    /// </summary>
    public sealed class ProgressStatePreserveUnknownRecordsToSerializablePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "progress_state_preserve_unknown_records_to_serializable";

        /// <inheritdoc />
        public static string Description =>
            "Merge unavailable mod progress records back into SerializableProgress before saving";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressState), nameof(ProgressState.ToSerializable), Type.EmptyTypes)];
        }

        /// <summary>
        ///     Merges preserved records into the generated save object.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(ProgressState __instance, SerializableProgress __result)
            // ReSharper restore InconsistentNaming
        {
            PreservedProgressRecords.MergeInto(__instance, __result);
            ProgressMirrorStore.SaveMirror(__result);
        }
    }

    /// <summary>
    ///     Refreshes the progress mirror after a real progress load completes.
    /// </summary>
    public sealed class ProgressStatePreserveUnknownRecordsLoadProgressPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "progress_state_preserve_unknown_records_load_progress";

        /// <inheritdoc />
        public static string Description => "Refresh progress mirror after ProgressSaveManager.LoadProgress";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress), Type.EmptyTypes)];
        }

        /// <summary>
        ///     Writes a mirror from the parsed progress object after a successful load.
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(ProgressSaveManager __instance, ReadSaveResult<SerializableProgress> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result is { Success: true, SaveData: not null })
                ProgressMirrorStore.RefreshFromProgress(__instance.Progress);
        }
    }
}
