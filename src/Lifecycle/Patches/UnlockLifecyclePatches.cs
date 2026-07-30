using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
#if STS2_AT_LEAST_0_110_0
using MegaCrit.Sts2.Core.Timeline;
#endif

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Publishes Epoch acquisition and reveal lifecycle events from <see cref="SaveManager" />.</para>
    ///     <para xml:lang="zh-CN">从 <see cref="SaveManager" /> 发布纪元获得与显示生命周期事件。</para>
    /// </summary>
    internal sealed class EpochObtainedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "epoch_lifecycle_obtain_epoch";
        public static string Description => "Publish epoch obtained lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.ObtainEpoch), [typeof(string)])];
        }

        public static void Postfix(SaveManager __instance, string __0)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new EpochObtainedEvent(__instance, __0, DateTimeOffset.UtcNow),
                nameof(EpochObtainedEvent));
        }
    }

    internal sealed class EpochRevealedLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "epoch_lifecycle_reveal_epoch";
        public static string Description => "Publish epoch revealed lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(SaveManager), nameof(SaveManager.RevealEpoch), [typeof(string), typeof(bool)])];
        }

        public static void Postfix(SaveManager __instance, string __0, bool __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new EpochRevealedEvent(__instance, __0, __1, DateTimeOffset.UtcNow),
                nameof(EpochRevealedEvent));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Publishes a lifecycle event after the legacy unlock counter advances or, on game API 0.110.0 and later,
    ///         after the save manager attempts to grant the next score-based epoch.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在旧版解锁计数推进后发布生命周期事件；从游戏 API 0.110.0 起，则在存档管理器尝试授予
    ///         下一个基于分数的纪元后发布。
    ///     </para>
    /// </summary>
    internal class UnlockIncrementLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "unlock_increment_lifecycle";
        public static string Description => "Publish agnostic unlock increment lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_110_0
                new(typeof(SaveManager), nameof(SaveManager.GrantNextUnlock), Type.EmptyTypes),
#else
                new(typeof(SaveManager), nameof(SaveManager.IncrementUnlock), Type.EmptyTypes),
#endif
            ];
        }

#if STS2_AT_LEAST_0_110_0
        public static void Postfix(SaveManager __instance, EpochModel? __result)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new UnlockIncrementedEvent(__instance, __instance.Progress.TotalUnlocks, __result?.Id,
                    DateTimeOffset.UtcNow),
                nameof(UnlockIncrementedEvent)
            );
        }
#else
        public static void Postfix(SaveManager __instance, string? __result)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new UnlockIncrementedEvent(__instance, __instance.Progress.TotalUnlocks, __result,
                    DateTimeOffset.UtcNow),
                nameof(UnlockIncrementedEvent)
            );
        }
#endif
    }
}
