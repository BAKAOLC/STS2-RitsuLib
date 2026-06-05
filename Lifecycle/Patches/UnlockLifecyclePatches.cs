using System.Reflection;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes epoch obtain and reveal lifecycle events from <see cref="SaveManager" />.
    ///     从 <see cref="SaveManager" /> 发布 epoch 取得和揭示生命周期事件。
    /// </summary>
    public class EpochLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "epoch_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish epoch obtain and reveal lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.ObtainEpoch), [typeof(string)]),
                new(typeof(SaveManager), nameof(SaveManager.RevealEpoch), [typeof(string), typeof(bool)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: publishes <see cref="EpochObtainedEvent" /> or <see cref="EpochRevealedEvent" /> after the
        ///     matching method runs.
        ///     <see cref="EpochRevealedEvent" />。
        ///     Harmony postfix：匹配的方法运行后发布 <see cref="EpochObtainedEvent" /> 或 <see cref="EpochRevealedEvent" />。
        ///     <see cref="EpochRevealedEvent" />。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, SaveManager __instance, object[] __args)
        {
            switch (__originalMethod.Name)
            {
                case nameof(SaveManager.ObtainEpoch):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EpochObtainedEvent(__instance, (string)__args[0], DateTimeOffset.UtcNow),
                        nameof(EpochObtainedEvent)
                    );
                    break;
                case nameof(SaveManager.RevealEpoch):
                    RitsuLibFramework.PublishLifecycleEvent(
                        new EpochRevealedEvent(__instance, (string)__args[0], (bool)__args[1], DateTimeOffset.UtcNow),
                        nameof(EpochRevealedEvent)
                    );
                    break;
            }
        }
    }

    /// <summary>
    ///     Publishes a lifecycle event when <see cref="SaveManager.IncrementUnlock" /> completes and returns a key.
    ///     当 <see cref="SaveManager.IncrementUnlock" /> 完成并返回键时发布生命周期事件。
    /// </summary>
    public class UnlockIncrementLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "unlock_increment_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish agnostic unlock increment lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.IncrementUnlock), Type.EmptyTypes),
            ];
        }

        /// <summary>
        ///     Harmony postfix: publishes <see cref="UnlockIncrementedEvent" /> with total unlocks and optional result key.
        ///     Harmony postfix：发布包含总解锁数和可选结果键的 <see cref="UnlockIncrementedEvent" />。
        /// </summary>
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
