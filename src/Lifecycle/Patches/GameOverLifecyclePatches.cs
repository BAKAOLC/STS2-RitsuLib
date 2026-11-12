using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
#if STS2_AT_LEAST_0_113_0
using MegaCrit.Sts2.Core.Entities.Save;

#else
using MegaCrit.Sts2.Core.Saves;
#endif

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Publishes a lifecycle event when <see cref="NGameOverScreen" /> is created from run state and
    ///         save data.
    ///     </para>
    ///     <para xml:lang="zh-CN">当使用局内状态和存档数据创建 <see cref="NGameOverScreen" /> 时发布生命周期事件。</para>
    /// </summary>
    internal class GameOverScreenLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "game_over_screen_lifecycle";
        public static string Description => "Publish lifecycle events when the game over screen is created";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NGameOverScreen), nameof(NGameOverScreen.Create),
#if STS2_AT_LEAST_0_113_0
                    [typeof(RunState), typeof(PostRunProgressUpdateResult)]),
#else
                    [typeof(RunState), typeof(SerializableRun)]),
#endif
            ];
        }

#if STS2_AT_LEAST_0_113_0
        public static void Postfix(RunState runState, PostRunProgressUpdateResult __1, NGameOverScreen? __result)
#else
        public static void Postfix(RunState runState, SerializableRun serializableRun, NGameOverScreen? __result)
#endif
        {
            if (__result == null)
                return;

#if STS2_AT_LEAST_0_113_0
            var serializableRun = __1.run;
            if (serializableRun == null)
                return;
#endif
            RitsuLibFramework.PublishLifecycleEvent(
                new GameOverScreenCreatedEvent(runState, serializableRun, __result, DateTimeOffset.UtcNow),
                nameof(GameOverScreenCreatedEvent)
            );
        }
    }
}
