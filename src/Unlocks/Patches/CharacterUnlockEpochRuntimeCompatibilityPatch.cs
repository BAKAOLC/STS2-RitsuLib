using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Skips a base-game-style character-unlock grant when its inferred epoch ID is unavailable for a mod
    ///         character.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         模组角色按游戏本体规则推导出的纪元 ID 不可用时，跳过该角色解锁纪元的授予。
    ///     </para>
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
