using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     When <c>CheckFifteenElitesDefeatedEpoch</c> is absent, elite logic may live only inside
    ///     <c>MegaCrit.Sts2.Core.Saves.Managers.ProgressSaveManager.UpdateAfterCombatWon</c>. Postfix covers the
    ///     non-throwing case; Finalizer recovers from vanilla <c>ArgumentOutOfRangeException</c> for unknown mod
    ///     characters.
    ///     当 <c>CheckFifteenElitesDefeatedEpoch</c> 不存在时，精英逻辑可能只存在于
    ///     <c>MegaCrit.Sts2.Core.Saves.Managers.ProgressSaveManager.UpdateAfterCombatWon</c> 内。Postfix 覆盖
    ///     未抛出异常的情况；Finalizer 则从未知 mod
    ///     角色触发的原版 <c>ArgumentOutOfRangeException</c> 中恢复。
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

        public static Exception? Finalizer(
            Exception? __exception,
            ProgressSaveManager __instance,
            Player localPlayer,
            CombatRoom room)
        {
            if (__exception == null)
                return null;

            if (EliteEpochModHandling.HasDedicatedEliteEpochCheckMethod || room.RoomType != RoomType.Elite ||
                !ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(localPlayer.Character) ||
                __exception is not ArgumentOutOfRangeException { ParamName: "character" })
                return __exception;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
            return null;
        }
    }
}
