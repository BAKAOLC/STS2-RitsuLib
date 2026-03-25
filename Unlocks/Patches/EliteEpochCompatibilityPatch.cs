using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Unlocks.Patches
{
    public class EliteEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "elite_epoch_compatibility";

        public static string Description =>
            "Handle elite-win epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch", [typeof(Player)])];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(ProgressSaveManager __instance, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (!ModUnlockRegistry.TryGetEliteEpochRule(character.Id, out var rule))
            {
                RitsuLibFramework.Logger.Debug(
                    $"[Unlocks] Skipping vanilla elite epoch check for mod character '{character.Id}' with no registered elite rule.");
                return false;
            }

            /*
            if (localPlayer.RunState.GameMode.AreAchievementsAndEpochsLocked())
                return false;
            */ // for next version

            if (SaveManager.Instance.Progress.IsEpochObtained(rule.EpochId))
                return false;

            var eliteWins = CountEliteWinsForCharacter(__instance, character.Id);
            if (eliteWins < rule.RequiredEliteWins)
                return false;

            if (!EpochRuntimeCompatibility.CanUseEpochId(
                    rule.EpochId,
                    $"elite-win epoch rule for mod character '{character.Id}'"))
                return false;

            SaveManager.Instance.ObtainEpoch(rule.EpochId);
            if (!localPlayer.DiscoveredEpochs.Contains(rule.EpochId, StringComparer.Ordinal))
                localPlayer.DiscoveredEpochs.Add(rule.EpochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained epoch '{rule.EpochId}' after {eliteWins} elite win(s) using registered rule: {rule.Description}");

            return false;
        }

        private static int CountEliteWinsForCharacter(ProgressSaveManager progressSaveManager, ModelId characterId)
        {
            var eliteEncounterMethod = typeof(ProgressSaveManager)
                                           .GetMethod("GetEliteEncounters",
                                               BindingFlags.NonPublic | BindingFlags.Static)
                                       ?? throw new MissingMethodException(typeof(ProgressSaveManager).FullName,
                                           "GetEliteEncounters");

            var eliteEncounters = (HashSet<ModelId>)eliteEncounterMethod.Invoke(null, null)!;
            var progress = progressSaveManager.Progress;
            var totalWins = 0;

            foreach (var encounter in progress.EncounterStats.Values)
            {
                if (!eliteEncounters.Contains(encounter.Id))
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
