using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     Before the timeline queues expansion slots, align <see cref="EpochModel.AllEpochIds" /> with
    ///     <c>EpochModel._epochTypeDictionary</c>. Otherwise <see cref="MegaCrit.Sts2.Core.Saves.ProgressState" />
    ///     <c>FilterAndSortEpochs</c> may strip mod expansion ids (IsValid false) immediately after
    ///     <see cref="MegaCrit.Sts2.Core.Saves.SaveManager.UnlockSlot" />, so the live expansion UI breaks while a cold
    ///     reload still shows slots once the cache matches the dictionary.
    ///     在 timeline 将扩展槽入队前，将 <see cref="EpochModel.AllEpochIds" /> 与
    ///     <c>EpochModel._epochTypeDictionary</c> 对齐。否则 <see cref="MegaCrit.Sts2.Core.Saves.ProgressState" />
    ///     <c>FilterAndSortEpochs</c> 可能会在
    ///     <see cref="MegaCrit.Sts2.Core.Saves.SaveManager.UnlockSlot" /> 后立即剔除 mod 扩展 id（IsValid false），导致实时扩展 UI 损坏，而冷
    ///     重载在缓存与字典匹配后仍会显示槽。
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
    ///     Vanilla <see cref="NUnlockTimelineScreen.SetUnlocks" /> sorts only by <see cref="EpochSlotData.EraPosition" />,
    ///     which collides across <see cref="EpochEra" /> values — common for mod timelines. Expansion animation then
    ///     feeds <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.AddEpochSlots" /> in the wrong era
    ///     order.
    ///     原版 <see cref="NUnlockTimelineScreen.SetUnlocks" /> 只按 <see cref="EpochSlotData.EraPosition" /> 排序，
    ///     该值会在不同 <see cref="EpochEra" /> 间冲突，这在 mod timeline 中很常见。扩展动画随后会
    ///     按错误纪元顺序将数据送入 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.AddEpochSlots" />
    ///     。
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
    ///     When <c>NeowEpoch.QueueUnlocks</c> runs (scoped by <see cref="NeowEpochQueueUnlocksCoExpansionScopePatch" />),
    ///     after vanilla <see cref="EpochModel.QueueTimelineExpansion" /> unlocks the twelve base rows, also obtains
    ///     Ironclad-gated mod character root rows and runs
    ///     <see cref="MegaCrit.Sts2.Core.Saves.SaveManager.UnlockSlot" /> for already-obtained
    ///     <see cref="ModEpochTemplate" /> rows and other root mod timeline rows not in that batch, then signals the
    ///     animated <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.AddEpochSlots" /> prefix to merge the
    ///     same mod slots in-session.
    ///     当 <c>NeowEpoch.QueueUnlocks</c> 运行时（由 <see cref="NeowEpochQueueUnlocksCoExpansionScopePatch" /> 限定作用域），
    ///     在原版 <see cref="EpochModel.QueueTimelineExpansion" /> 解锁十二个基础行后，还会获得以 Ironclad 为前置的
    ///     mod 角色根行，并为该批次之外已获得的 <see cref="ModEpochTemplate" /> 行和其他 mod 时间线根节点调用
    ///     <see cref="MegaCrit.Sts2.Core.Saves.SaveManager.UnlockSlot" />，然后通知
    ///     动画版 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NTimelineScreen.AddEpochSlots" /> 前缀在当前会话中合并
    ///     相同的 mod 槽。
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
