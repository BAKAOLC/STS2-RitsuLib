using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Delegates elite epoch handling for mod characters to <c>EliteEpochModHandling</c> when the dedicated
    ///     check method exists.
    ///     当专用检查方法存在时，将 mod 角色的精英纪元处理委托给 <c>EliteEpochModHandling</c>。
    ///     检查方法存在。
    /// </summary>
    internal class EliteEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "elite_epoch_compatibility";

        public static string Description =>
            "Handle elite-win epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch",
                    [typeof(Player)], true),
            ];
        }

        public static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(localPlayer);

            if (!ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(localPlayer.Character))
                return true;

            EliteEpochModHandling.TryHandleModEliteEpoch(__instance, localPlayer);
            return false;
        }
    }
}
