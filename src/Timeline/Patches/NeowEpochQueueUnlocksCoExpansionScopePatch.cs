using MegaCrit.Sts2.Core.Timeline.Epochs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Limits mod timeline co-expansion to <see cref="NeowEpoch.QueueUnlocks" />, preventing unrelated
    ///         <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel.QueueTimelineExpansion" /> calls from unlocking or
    ///         animating every <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将模组时间线的共同扩展限制在 <see cref="NeowEpoch.QueueUnlocks" /> 流程内，避免无关的
    ///         <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel.QueueTimelineExpansion" /> 调用解锁或播放所有
    ///         <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" /> 的动画。
    ///     </para>
    /// </summary>
    internal sealed class NeowEpochQueueUnlocksCoExpansionScopePatch : IPatchMethod
    {
        public static string PatchId => "neow_epoch_queue_unlocks_co_expansion_scope";

        public static string Description =>
            "Track NeowEpoch.QueueUnlocks so QueueTimelineExpansion postfix only co-unlocks mod slots in that flow";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NeowEpoch), nameof(NeowEpoch.QueueUnlocks), Type.EmptyTypes)];
        }

        public static void Prefix()
        {
            ModTimelineNeowCoExpansion.EnterNeowQueueUnlocks();
        }

        public static void Finalizer(Exception? __exception)
        {
            ModTimelineNeowCoExpansion.ExitNeowQueueUnlocks();
        }
    }
}
