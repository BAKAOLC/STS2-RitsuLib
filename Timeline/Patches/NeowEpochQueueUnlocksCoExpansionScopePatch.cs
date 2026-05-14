using MegaCrit.Sts2.Core.Timeline.Epochs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Timeline.Patches
{
    /// <summary>
    ///     Scopes mod timeline co-expansion to vanilla <see cref="NeowEpoch.QueueUnlocks" /> so other
    ///     <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel.QueueTimelineExpansion" /> callers (character lines, relic rows,
    ///     etc.) do not unlock or animate every <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" />.
    ///     将作用域限制为 mod timeline 共同扩展 to 原版 <see cref="NeowEpoch.QueueUnlocks" /> 使 其他
    ///     <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel.QueueTimelineExpansion" /> 调用方 (角色线, 遗物行,
    ///     etc.) 不会解锁或播放每个的动画 <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" />。
    /// </summary>
    public sealed class NeowEpochQueueUnlocksCoExpansionScopePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "neow_epoch_queue_unlocks_co_expansion_scope";

        /// <inheritdoc />
        public static string Description =>
            "Track NeowEpoch.QueueUnlocks so QueueTimelineExpansion postfix only co-unlocks mod slots in that flow";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NeowEpoch), nameof(NeowEpoch.QueueUnlocks), Type.EmptyTypes)];
        }

        /// <summary>
        ///     Increments depth before Neow&apos;s unlock queue runs.
        ///     在之前递增深度： Neow&apos;s 解锁队列运行。
        /// </summary>
        public static void Prefix()
        {
            ModTimelineNeowCoExpansion.EnterNeowQueueUnlocks();
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Always decrements depth, even when <see cref="NeowEpoch.QueueUnlocks" /> throws.
        ///     始终递减深度, 即使 <see cref="NeowEpoch.QueueUnlocks" /> 抛出异常。
        /// </summary>
        public static void Finalizer(Exception? __exception)
        {
            ModTimelineNeowCoExpansion.ExitNeowQueueUnlocks();
        }
    }
}
