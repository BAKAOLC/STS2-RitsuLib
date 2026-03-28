using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Skips vanilla character-unlock epoch grants when the inferred epoch id is unusable at runtime for mod
    ///     characters.
    /// </summary>
    public class CharacterUnlockEpochRuntimeCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_unlock_epoch_runtime_compatibility";

        /// <inheritdoc />
        public static string Description =>
            "Prevent missing vanilla-style character unlock epochs from aborting runs for mod characters";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch", [typeof(Player), typeof(int)], true),
            ];
        }

        /// <summary>
        ///     Returns false to cancel the original method when the expected epoch cannot be used safely.
        /// </summary>
        public static bool Prefix(Player localPlayer, int act)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            var expectedEpochId = act switch
            {
                0 => character.Id.Entry.ToUpperInvariant() + "2_EPOCH",
                1 => character.Id.Entry.ToUpperInvariant() + "3_EPOCH",
                2 => character.Id.Entry.ToUpperInvariant() + "4_EPOCH",
                _ => null,
            };

            if (expectedEpochId == null)
                return true;

            return EpochRuntimeCompatibility.CanUseEpochId(
                expectedEpochId,
                $"vanilla character unlock epoch grant for mod character '{character.Id}' after Act {act + 1}");
        }
    }
}
