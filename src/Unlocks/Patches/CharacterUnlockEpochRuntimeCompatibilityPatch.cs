using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Skips vanilla character-unlock epoch grants when the inferred epoch id is unusable at runtime for mod
    ///     characters.
    ///     当推断出的纪元 id 对 mod
    ///     角色在运行时不可安全使用时，跳过原版角色解锁纪元授予。
    /// </summary>
    internal class CharacterUnlockEpochRuntimeCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "character_unlock_epoch_runtime_compatibility";

        public static string Description =>
            "Prevent missing vanilla-style character unlock epochs from aborting runs for mod characters";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch", [typeof(Player), typeof(int)], true),
            ];
        }

        public static bool Prefix(Player localPlayer, int act)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(character))
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

            if (ModCharacterTimelinePolicy.DoesNotRequireEpochAndTimeline(character) &&
                !EpochModel.IsValid(expectedEpochId))
                return false;

            return EpochRuntimeCompatibility.CanUseEpochId(
                expectedEpochId,
                $"vanilla character unlock epoch grant for mod character '{character.Id}' after Act {act + 1}");
        }
    }
}
