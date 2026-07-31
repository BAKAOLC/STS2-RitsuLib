using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Synchronizes <see cref="EpochModel.AllEpochIds" /> with the live epoch-type dictionary before timeline
    ///         expansion queues slots. This prevents progression filtering from discarding newly registered mod epoch
    ///         IDs immediately after their slots are unlocked.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在时间线扩展将槽位入队前，使 <see cref="EpochModel.AllEpochIds" /> 与实时纪元类型字典同步，防止进度筛选在
    ///         新注册的模组纪元槽位刚解锁后便丢弃其 ID。
    ///     </para>
    /// </summary>
    internal class QueueTimelineExpansionSyncEpochIdListPatch : IPatchMethod
    {
        public static string PatchId => "queue_timeline_expansion_sync_epoch_id_list";

        public static string Description =>
            "Sync EpochModel.AllEpochIds with the epoch type dictionary before QueueTimelineExpansion runs UnlockSlot";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EpochModel), nameof(EpochModel.QueueTimelineExpansion), [typeof(EpochModel[])]),
            ];
        }

        public static void Prefix(EpochModel[] epochs)
        {
            ArgumentNullException.ThrowIfNull(epochs);
            ModTimelineRegistry.EnsureAllEpochIdsSyncedWithDictionary();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Sorts timeline expansion slots by <see cref="EpochSlotData.Era" /> and then
    ///         <see cref="EpochSlotData.EraPosition" />, avoiding collisions between equal positions in different eras.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         先按 <see cref="EpochSlotData.Era" />、再按 <see cref="EpochSlotData.EraPosition" /> 对时间线扩展槽位排序，
    ///         避免不同时代中相同位置发生排序冲突。
    ///     </para>
    /// </summary>
    internal class NUnlockTimelineScreenExpansionSlotSortPatch : IPatchMethod
    {
        public static string PatchId => "n_unlock_timeline_screen_expansion_slot_sort";

        public static string Description =>
            "Sort timeline expansion slots by Era then EraPosition for mod-compatible column ordering";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NUnlockTimelineScreen), nameof(NUnlockTimelineScreen.SetUnlocks),
                    [typeof(List<EpochSlotData>)]),
            ];
        }

        public static void Postfix(NUnlockTimelineScreen __instance, List<EpochSlotData> eras)
        {
            ArgumentNullException.ThrowIfNull(eras);
            var field = AccessTools.Field(typeof(NUnlockTimelineScreen), "_erasToUnlock");
            if (field == null)
                return;

            var ordered = eras.OrderBy(a => a.Era).ThenBy(a => a.EraPosition).ToList();
            field.SetValue(__instance, ordered);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Extends Neow's primary timeline expansion with eligible mod character roots and other mod epoch slots,
    ///         then signals the animated timeline screen to merge those slots in the current session.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将符合条件的模组角色根节点和其他模组纪元槽位加入涅奥的主时间线扩展，并通知动画时间线界面在当前会话中
    ///         合并这些槽位。
    ///     </para>
    /// </summary>
    internal sealed class QueueTimelineExpansionUnlockModSlotsAfterNeowPatch : IPatchMethod
    {
        public static string PatchId => "queue_timeline_expansion_unlock_mod_slots_after_neow";

        public static string Description =>
            "After Neow primary timeline expansion, obtain Ironclad-gated mod character roots and unlock merged mod slots";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EpochModel), nameof(EpochModel.QueueTimelineExpansion), [typeof(EpochModel[])]),
            ];
        }

        public static void Postfix(EpochModel[] epochs)
        {
            ArgumentNullException.ThrowIfNull(epochs);
            ModTimelineNeowCoExpansion.OnQueueTimelineExpansionPostfix(epochs);
        }
    }
}
