using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Timeline.Scaffolding;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Backfills mod character root epochs from existing prerequisite character run history.
    /// </summary>
    public sealed class ModCharacterRootEpochBackfillPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "mod_character_root_epoch_backfill";

        /// <inheritdoc />
        public static string Description =>
            "Backfill mod character root unlock epochs when prerequisite character runs already exist";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), nameof(SaveManager.InitProgressData), Type.EmptyTypes),
                new(typeof(SaveManager), nameof(SaveManager.GenerateUnlockStateFromProgress), Type.EmptyTypes),
            ];
        }

        /// <summary>
        ///     Runs before unlock state generation so character select sees newly backfilled root epochs.
        /// </summary>
        public static void Prefix(MethodBase __originalMethod, SaveManager __instance)
        {
            if (__originalMethod.Name == nameof(SaveManager.GenerateUnlockStateFromProgress))
                ModCharacterRootEpochBackfill.TryBackfill(__instance);
        }

        /// <summary>
        ///     Runs after progress load so profile data is persisted once when backfill is needed.
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, SaveManager __instance)
        {
            if (__originalMethod.Name == nameof(SaveManager.InitProgressData))
                ModCharacterRootEpochBackfill.TryBackfill(__instance);
        }
    }

    internal static class ModCharacterRootEpochBackfill
    {
        internal static void TryBackfill(SaveManager saveManager)
        {
            ArgumentNullException.ThrowIfNull(saveManager);

            var obtained = new List<string>();
            foreach (var candidate in EnumerateBackfillCandidates(saveManager.Progress))
            {
                if (!EpochRuntimeCompatibility.CanUseEpochId(
                        candidate.EpochId,
                        $"mod character root epoch backfill for '{candidate.CharacterId}'"))
                    continue;

                saveManager.ObtainEpoch(candidate.EpochId);
                obtained.Add(candidate.EpochId);
            }

            if (obtained.Count == 0)
                return;

            saveManager.SaveProgressFile();
            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Backfilled mod character root epoch(s) from prerequisite run history: {string.Join(", ", obtained)}");
        }

        private static IEnumerable<BackfillCandidate> EnumerateBackfillCandidates(ProgressState progress)
        {
            foreach (var epochId in EpochModel.AllEpochIds)
            {
                EpochModel epoch;
                try
                {
                    epoch = EpochModel.Get(epochId);
                }
                catch
                {
                    continue;
                }

                if (epoch is not ModEpochTemplate)
                    continue;
                if (progress.IsEpochObtained(epoch.Id))
                    continue;
                if (!TryGetCharacterUnlockType(epoch.GetType(), out var characterType))
                    continue;

                CharacterModel character;
                try
                {
                    character = ModelDb.GetById<CharacterModel>(ModelDb.GetId(characterType));
                }
                catch
                {
                    continue;
                }

                if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                    continue;
                if (character is IModCharacterEpochTimelineRequirement { RequiresEpochAndTimeline: false })
                    continue;
                if (character is not IModCharacterUnlockPrerequisite { UnlocksAfterRunAsType: { } prerequisiteType })
                    continue;

                ModelId prerequisiteId;
                try
                {
                    prerequisiteId = ModelDb.GetId(prerequisiteType);
                }
                catch
                {
                    continue;
                }

                var stats = progress.GetStatsForCharacter(prerequisiteId);
                if (stats is not { TotalWins: > 0 } && stats is not { TotalLosses: > 0 })
                    continue;

                yield return new(epoch.Id, character.Id);
            }
        }

        private static bool TryGetCharacterUnlockType(Type epochType, out Type characterType)
        {
            for (var type = epochType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(CharacterUnlockEpochTemplate<>))
                    continue;

                characterType = type.GetGenericArguments()[0];
                return true;
            }

            characterType = typeof(CharacterModel);
            return false;
        }

        private readonly record struct BackfillCandidate(string EpochId, ModelId CharacterId);
    }
}
