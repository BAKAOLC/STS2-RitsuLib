using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    public class BossEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "boss_epoch_compatibility";

        public static string Description =>
            "Handle boss-win epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch", [typeof(Player)])];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (!ModUnlockRegistry.TryGetBossEpochRule(character.Id, out var rule))
                return false;

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
