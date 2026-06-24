using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes epoch obtain and reveal lifecycle events from <see cref="SaveManager" />.
    ///     从 <see cref="SaveManager" /> 发布 epoch 取得和揭示生命周期事件。
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
    ///     Publishes a lifecycle event when <see cref="SaveManager.IncrementUnlock" /> completes and returns a key.
    ///     当 <see cref="SaveManager.IncrementUnlock" /> 完成并返回键时发布生命周期事件。
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
                new(typeof(SaveManager), nameof(SaveManager.IncrementUnlock), Type.EmptyTypes),
            ];
        }

        public static void Postfix(SaveManager __instance, string? __result)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new UnlockIncrementedEvent(__instance, __instance.Progress.TotalUnlocks, __result,
                    DateTimeOffset.UtcNow),
                nameof(UnlockIncrementedEvent)
            );
        }
    }
}
