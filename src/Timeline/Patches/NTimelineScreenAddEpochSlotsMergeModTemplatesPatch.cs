using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Merges eligible mod epoch slots into <c>NTimelineScreen.AddEpochSlots</c>. On a nonanimated screen open,
    ///         merging begins only after Neow's primary expansion has started. During an animated expansion, merging
    ///         occurs only for the batch queued by <see cref="NeowEpoch.QueueUnlocks" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将符合条件的模组纪元槽位合并到 <c>NTimelineScreen.AddEpochSlots</c>。非动画方式打开界面时，仅在涅奥的
    ///         主扩展开始后合并；播放扩展动画时，则只合并由 <see cref="NeowEpoch.QueueUnlocks" /> 入队的批次。
    ///     </para>
    /// </summary>
    internal sealed class NTimelineScreenAddEpochSlotsMergeModTemplatesPatch : IPatchMethod
    {
        public static string PatchId => "n_timeline_screen_add_epoch_slots_merge_mod_templates";

        public static string Description =>
            "Merge obtained, Neow-unlocked root, or parent-visible ModEpochTemplate slots after Neow expansion";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen),
                    nameof(NTimelineScreen.AddEpochSlots),
                    [typeof(List<EpochSlotData>), typeof(bool)]),
            ];
        }

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
