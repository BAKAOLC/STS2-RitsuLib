using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Delegates elite epoch handling for mod characters to <c>EliteEpochModHandling</c> when the dedicated
    ///     check method exists.
    ///     当专用检查方法存在时，将 mod 角色的精英纪元处理委托给 <c>EliteEpochModHandling</c>。
    ///     检查方法存在。
    /// </summary>
    public class EliteEpochCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "elite_epoch_compatibility";

        /// <inheritdoc />
        public static string Description =>
            "Handle elite-win epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch",
                    [typeof(Player)], true),
            ];
        }

        /// <summary>
        ///     Runs mod elite epoch logic and prevents the original method from executing for mod characters.
        ///     运行 mod 精英纪元逻辑，并阻止原始方法对 mod 角色执行。
        /// </summary>
        public static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(localPlayer);

            if (!ModContentRegistry.TryGetOwnerModId(localPlayer.Character.GetType(), out _))
                return true;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
            return false;
        }
    }
}
