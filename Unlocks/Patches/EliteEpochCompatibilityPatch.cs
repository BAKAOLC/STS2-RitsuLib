using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Delegates elite epoch handling for mod characters to <c>EliteEpochModHandling</c> when the dedicated
    ///     check method exists.
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

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Runs mod elite epoch logic and prevents the original method from executing for mod characters.
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
