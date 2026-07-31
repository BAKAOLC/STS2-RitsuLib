using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes elite-epoch handling for mod characters through <c>EliteEpochModHandling</c> when the game exposes
    ///         a dedicated check method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         游戏提供专用检查方法时，通过 <c>EliteEpochModHandling</c> 处理模组角色的精英纪元。
    ///     </para>
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
