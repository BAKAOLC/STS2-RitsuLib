using System.Reflection;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes epoch obtain and reveal lifecycle events from <see cref="SaveManager" />.
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, SaveManager __instance, object[] __args)
            // ReSharper restore InconsistentNaming
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(SaveManager __instance, string? __result)
            // ReSharper restore InconsistentNaming
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new UnlockIncrementedEvent(__instance, __instance.Progress.TotalUnlocks, __result,
                    DateTimeOffset.UtcNow),
                nameof(UnlockIncrementedEvent)
            );
        }
    }
}
