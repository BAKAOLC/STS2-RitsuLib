using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Shared elite-epoch unlock logic and build detection. Beta builds expose
    ///     <c>CheckFifteenElitesDefeatedEpoch</c>; older/stable builds may only run the check inline from
    ///     <see cref="ProgressSaveManager.UpdateAfterCombatWon" />.
    /// </summary>
    internal static class EliteEpochModHandling
    {
        internal static readonly bool HasDedicatedEliteEpochCheckMethod =
            typeof(ProgressSaveManager).GetMethod(
                "CheckFifteenElitesDefeatedEpoch",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                [typeof(Player)],
                null) != null;

        /// <summary>
        ///     Mirrors <see cref="ProgressSaveManager" /> mid-run epoch gating (same as
        ///     <c>TryObtainEpochMidRun</c> / <c>AreAchievementsAndEpochsLocked</c>: non-standard modes do not
        ///     grant epochs) without depending on the extension method existing on every game build.
        /// </summary>
        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
            return Sts2RunGameModeCompat.AreMidRunEpochsLockedFor(localPlayer);
        }

        /// <summary>
        ///     Mod-character elite epoch path: suppress vanilla (which throws on unknown <see cref="CharacterModel" />
        ///     types) and apply registered rules when applicable.
        /// </summary>
        internal static void TryHandleModEliteEpoch(ProgressSaveManager progressSaveManager, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(progressSaveManager);
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return;

            if (!ModUnlockRegistry.TryGetEliteEpochRule(character.Id, out var rule))
            {
                if (character is IModCharacterEpochTimelineRequirement { RequiresEpochAndTimeline: false })
                    return;

                ModUnlockMissingRuleWarnings.WarnOnce(
                    $"elite_epoch_rule:{character.Id}",
                    $"[Unlocks] Mod character '{character.Id}' has no registered elite-win epoch rule (UnlockEpochAfterEliteVictories / RegisterEliteEpochRule). " +
                    "Skipping vanilla elite epoch logic for this character so the run can continue.");
                return;
            }

            if (AreMidRunEpochsLockedFor(localPlayer))
                return;

            if (SaveManager.Instance.Progress.IsEpochObtained(rule.EpochId))
                return;

            var eliteWins = CountEliteWinsForCharacter(progressSaveManager, character.Id);
            if (eliteWins < rule.RequiredEliteWins)
                return;

            if (!EpochRuntimeCompatibility.CanUseEpochId(
                    rule.EpochId,
                    $"elite-win epoch rule for mod character '{character.Id}'"))
                return;

            SaveManager.Instance.ObtainEpoch(rule.EpochId);
            if (!localPlayer.DiscoveredEpochs.Contains(rule.EpochId, StringComparer.Ordinal))
                localPlayer.DiscoveredEpochs.Add(rule.EpochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained epoch '{rule.EpochId}' after {eliteWins} elite win(s) using registered rule: {rule.Description}");
        }

        internal static int CountEliteWinsForCharacter(ProgressSaveManager progressSaveManager, ModelId characterId)
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
