using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     Vanilla <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.InitScreen" /> only passes epochs
    ///     that already exist in <see cref="MegaCrit.Sts2.Core.Saves.ProgressState.Epochs" /> into
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.AddEpochSlots" />.
    ///     Mod story lines are not inserted into the save until gameplay or expansion runs
    ///     <see cref="MegaCrit.Sts2.Core.Saves.ProgressState.UnlockSlot" />, so mod columns would stay missing while the
    ///     underlying unlock flow stays correct. Cold open (<c>isAnimated: false</c>) merges only after vanilla Neow&apos;s
    ///     primary expansion has started (see <see cref="ModTimelineNeowCoExpansion.HasVanillaNeowTimelineExpansionStarted" />
    ///     ).
    ///     Animated expansion merges only when <see cref="NeowEpoch.QueueUnlocks" /> just queued that batch (pending flag from
    ///     <see cref="QueueTimelineExpansionUnlockModSlotsAfterNeowPatch" />).
    ///     原版 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.InitScreen" /> 只会把纪元
    ///     中已经存在于 <see cref="MegaCrit.Sts2.Core.Saves.ProgressState.Epochs" /> 的内容传给
    ///     在游戏流程或扩展运行
    ///     <see cref="MegaCrit.Sts2.Core.Saves.ProgressState.UnlockSlot" /> 之前，mod 故事线不会插入存档，因此 mod 列会保持缺失，而
    ///     底层解锁流程仍然正确。冷打开（<c>isAnimated: false</c>）只会在原版 Neow 的
    ///     主扩展开始后合并（见 <see cref="ModTimelineNeowCoExpansion.HasVanillaNeowTimelineExpansionStarted" />
    ///     主扩展开始后（见 <c>ModTimelineNeowCoExpansion.HasVanillaNeowTimelineExpansionStarted</c>
    ///     ）。
    ///     动画扩展只会在 <see cref="NeowEpoch.QueueUnlocks" /> 刚刚将该批次入队时合并（pending 标志来自
    ///     <see cref="QueueTimelineExpansionUnlockModSlotsAfterNeowPatch" />）。
    /// </summary>
    public sealed class NTimelineScreenAddEpochSlotsMergeModTemplatesPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "n_timeline_screen_add_epoch_slots_merge_mod_templates";

        /// <inheritdoc />
        public static string Description =>
            "Merge ModEpochTemplate slots only after Neow primary expansion (cold) or same-session Neow QueueUnlocks (animated)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen),
                    nameof(NTimelineScreen.AddEpochSlots),
                    [typeof(List<EpochSlotData>), typeof(bool)]),
            ];
        }

        /// <summary>
        ///     Cold: merge after Neow primary expansion has touched save. Animated: merge only with pending from Neow
        ///     <see cref="NeowEpoch.QueueUnlocks" /> plus matching slot batch.
        ///     冷打开：在 Neow 主扩展触及存档后合并。动画：仅在来自 Neow
        ///     <see cref="NeowEpoch.QueueUnlocks" /> 的 pending 状态加上匹配槽批次时合并。
        /// </summary>
        public static void Prefix(List<EpochSlotData> slotsToAdd, bool isAnimated)
        {
            var progress = SaveManager.Instance?.Progress;

            if (!isAnimated)
            {
                if (!ModTimelineNeowCoExpansion.HasVanillaNeowTimelineExpansionStarted(progress))
                    return;

                ModTimelineNeowCoExpansion.MergeModEpochTemplateSlotsInto(slotsToAdd, progress);
                return;
            }

            if (slotsToAdd.Count == 1 && slotsToAdd[0].Model.Id == EpochModel.GetId<NeowEpoch>())
                return;

            if (!ModTimelineNeowCoExpansion.IsNeowPrimaryTimelineExpansionSlots(slotsToAdd))
                return;

            if (!ModTimelineNeowCoExpansion.TryConsumePendingNeowAnimatedSlotMerge())
                return;

            ModTimelineNeowCoExpansion.MergeModEpochTemplateSlotsInto(slotsToAdd, progress);
        }
    }
}
