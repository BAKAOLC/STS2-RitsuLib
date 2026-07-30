using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Handles game versions whose elite-epoch check exists only inside
    ///         <see cref="ProgressSaveManager.UpdateAfterCombatWon" />. The postfix handles normal completion without
    ///         suppressing failures from a partially completed base-game update.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         兼容只在 <see cref="ProgressSaveManager.UpdateAfterCombatWon" /> 内执行精英纪元检查的游戏版本。后置补丁处理
    ///         正常完成的情况，且不会吞掉游戏本体更新只完成一部分时产生的异常。
    ///     </para>
    /// </summary>
    internal class EliteEpochAfterCombatFallbackPatch : IPatchMethod
    {
        public static string PatchId => "elite_epoch_after_combat_fallback";

        public static string Description =>
            "Elite epoch unlock fallback when CheckFifteenElitesDefeatedEpoch is missing (stable vs beta)";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.UpdateAfterCombatWon),
                    [typeof(Player), typeof(CombatRoom)]),
            ];
        }

        public static void Postfix(ProgressSaveManager __instance, Player localPlayer, CombatRoom room)
        {
            if (EliteEpochModHandling.HasDedicatedEliteEpochCheckMethod)
                return;

            if (room.RoomType != RoomType.Elite)
                return;

            if (!ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(localPlayer.Character))
                return;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
        }
    }
}
