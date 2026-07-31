using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Scaffolding.Characters;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides shared elite-epoch unlock logic and detects whether the game exposes
    ///         <c>CheckFifteenElitesDefeatedEpoch</c> or performs that check inside
    ///         <see cref="ProgressSaveManager.UpdateAfterCombatWon" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供共用的精英纪元解锁逻辑，并检测游戏是提供 <c>CheckFifteenElitesDefeatedEpoch</c>，还是在
    ///         <see cref="ProgressSaveManager.UpdateAfterCombatWon" /> 内执行该检查。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Applies the game's mid-run epoch restriction without depending on the same helper method being
        ///         available in every supported build. Nonstandard game modes do not grant epochs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在不依赖所有受支持版本都提供同一辅助方法的前提下，应用游戏的局内纪元限制。非标准游戏模式不会授予纪元。
        ///     </para>
        /// </summary>
        internal static bool AreMidRunEpochsLockedFor(Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(localPlayer);
            return Sts2RunGameModeCompat.AreMidRunEpochsLockedFor(localPlayer);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a mod character's registered elite-epoch rule in place of base-game logic that rejects unknown
        ///         <see cref="CharacterModel" /> types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用模组角色已注册的精英纪元规则，取代会拒绝未知 <see cref="CharacterModel" /> 类型的游戏本体逻辑。
        ///     </para>
        /// </summary>
        internal static void TryHandleModEliteEpoch(ProgressSaveManager progressSaveManager, Player localPlayer)
        {
            ArgumentNullException.ThrowIfNull(progressSaveManager);
            ArgumentNullException.ThrowIfNull(localPlayer);

            var character = localPlayer.Character;
            if (!ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(character))
                return;

            if (!ModUnlockRegistry.TryGetEliteEpochRule(character.Id, out var rule))
            {
                if (ModCharacterTimelinePolicy.DoesNotRequireEpochAndTimeline(character))
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
            NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(EpochModel.Get(rule.EpochId)));
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
