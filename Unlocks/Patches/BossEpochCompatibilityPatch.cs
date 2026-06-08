using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Applies mod-specific boss-win epoch rules instead of vanilla fifteen-boss logic for mod characters.
    ///     对 mod 角色应用 mod 专属 Boss 胜利纪元规则，而非原版十五次 Boss 逻辑。
    /// </summary>
    internal class BossEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "boss_epoch_compatibility";

        public static string Description =>
            "Handle boss-win epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch", [typeof(Player)])];
        }

        public static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (!ModUnlockRegistry.TryGetBossEpochRule(character.Id, out var rule))
            {
                if (character is IModCharacterEpochTimelineRequirement { RequiresEpochAndTimeline: false })
                    return false;

                ModUnlockMissingRuleWarnings.WarnOnce(
                    $"boss_epoch_rule:{character.Id}",
                    $"[Unlocks] Mod character '{character.Id}' has no registered boss-win epoch rule (UnlockEpochAfterBossVictories / RegisterBossEpochRule). " +
                    "Skipping vanilla boss epoch logic for this character so the run can continue.");
                return false;
            }

            if (SaveManager.Instance.Progress.IsEpochObtained(rule.EpochId))
                return false;

            var bossIds = ModelDb.Acts
                .SelectMany(act => act.AllBossEncounters.Select(encounter => encounter.Id))
                .ToHashSet();

            var wins = CountEncounterWins(__instance, character.Id, bossIds);
            if (wins < rule.RequiredWins)
                return false;

            if (!EpochRuntimeCompatibility.CanUseEpochId(
                    rule.EpochId,
                    $"boss-win epoch rule for mod character '{character.Id}'"))
                return false;

            SaveManager.Instance.ObtainEpoch(rule.EpochId);
            NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(EpochModel.Get(rule.EpochId)));
            if (!localPlayer.DiscoveredEpochs.Contains(rule.EpochId, StringComparer.Ordinal))
                localPlayer.DiscoveredEpochs.Add(rule.EpochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained epoch '{rule.EpochId}' after {wins} boss win(s) using registered rule: {rule.Description}");

            return false;
        }

        internal static int CountEncounterWins(
            ProgressSaveManager progressSaveManager,
            ModelId characterId,
            HashSet<ModelId> encounterIds)
        {
            ArgumentNullException.ThrowIfNull(progressSaveManager);
            ArgumentNullException.ThrowIfNull(encounterIds);

            var totalWins = 0;

            foreach (var encounter in progressSaveManager.Progress.EncounterStats.Values)
            {
                if (!encounterIds.Contains(encounter.Id))
                    continue;

                foreach (var fightStat in encounter.FightStats.Where(fightStat => fightStat.Character == characterId))
                {
                    totalWins += fightStat.Wins;
                    break;
                }
            }

            return totalWins;
        }
    }
}
