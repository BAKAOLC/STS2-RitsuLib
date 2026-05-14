using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

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
    public class EliteEpochAfterCombatFallbackPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "elite_epoch_after_combat_fallback";

        /// <inheritdoc />
        public static string Description =>
            "Elite epoch unlock fallback when CheckFifteenElitesDefeatedEpoch is missing (stable vs beta)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), nameof(ProgressSaveManager.UpdateAfterCombatWon),
                    [typeof(Player), typeof(CombatRoom)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     After an elite combat win, applies mod elite epoch handling when no dedicated check method exists.
        ///     精英战斗胜利后，在不存在专用检查方法时应用 mod 精英纪元处理。
        /// </summary>
        public static void Postfix(ProgressSaveManager __instance, Player localPlayer, CombatRoom room)
        {
            if (EliteEpochModHandling.HasDedicatedEliteEpochCheckMethod)
                return;

            if (room.RoomType != RoomType.Elite)
                return;

            if (!ModContentRegistry.TryGetOwnerModId(localPlayer.Character.GetType(), out _))
                return;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Swallows expected vanilla argument exceptions after attempting mod elite epoch recovery.
        ///     尝试恢复 mod 精英纪元后，吞掉预期的原版参数异常。
        /// </summary>
        public static Exception? Finalizer(
                Exception? __exception,
                ProgressSaveManager __instance,
                Player localPlayer,
                CombatRoom room)
            // ReSharper restore InconsistentNaming
        {
            if (__exception == null)
                return null;

            if (EliteEpochModHandling.HasDedicatedEliteEpochCheckMethod || room.RoomType != RoomType.Elite ||
                !ModContentRegistry.TryGetOwnerModId(localPlayer.Character.GetType(), out _) ||
                __exception is not ArgumentOutOfRangeException { ParamName: "character" })
                return __exception;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
            return null;
        }
    }
}
