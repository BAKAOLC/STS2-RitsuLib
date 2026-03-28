using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes a lifecycle event when <see cref="NGameOverScreen" /> is created from run state and save data.
    /// </summary>
    public class GameOverScreenLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "game_over_screen_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish lifecycle events when the game over screen is created";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NGameOverScreen), nameof(NGameOverScreen.Create),
                    [typeof(RunState), typeof(SerializableRun)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: when creation succeeds, publishes <see cref="GameOverScreenCreatedEvent" />.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Postfix(RunState runState, SerializableRun serializableRun, NGameOverScreen? __result)
        {
            if (__result == null)
                return;

            RitsuLibFramework.PublishLifecycleEvent(
                new GameOverScreenCreatedEvent(runState, serializableRun, __result, DateTimeOffset.UtcNow),
                nameof(GameOverScreenCreatedEvent)
            );
        }
    }
}
