using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Validation;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves.RawProgress;

namespace STS2RitsuLib.Saves.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Captures progress records that the base game would discard while their owning mod content is unavailable.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         捕获原版游戏会在对应模组内容不可用时丢弃的进度记录。
    ///     </para>
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
            if (!RawProgressCommitBridge.IsPreparingCommitProjection)
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
    ///     <para xml:lang="en">
    ///         Restores preserved records for unavailable mod content to the serializable progress data before it is saved.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         保存前，将为当前不可用的模组内容保留的记录恢复到可序列化进度数据中。
    ///     </para>
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
            if (!RawProgressCommitBridge.IsPreparingCommitProjection &&
                !RawProgressCommitBridge.IsSavingOrdinaryProgress)
                ProgressMirrorStore.SaveMirror(__result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Refreshes the progress mirror after a successful progress load.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         成功加载进度后刷新进度镜像。
    ///     </para>
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
