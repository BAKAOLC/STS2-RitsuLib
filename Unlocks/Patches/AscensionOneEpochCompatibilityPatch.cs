using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using SerializableRun = MegaCrit.Sts2.Core.Saves.SerializableRun;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Replaces vanilla ascension-one epoch checks for mod-owned characters with registry-driven epoch grants.
    /// </summary>
    public class AscensionOneEpochCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ascension_one_epoch_compatibility";

        /// <inheritdoc />
        public static string Description =>
            "Handle ascension-one epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "CheckAscensionOneCompleted",
                    [typeof(SerializablePlayer), typeof(SerializableRun)],
                    true),
            ];
        }

        /// <summary>
        ///     Obtains the registered ascension-one epoch when appropriate; skips vanilla when handled.
        /// </summary>
        public static bool Prefix(SerializablePlayer serializablePlayer, SerializableRun serializableRun)
        {
            ArgumentNullException.ThrowIfNull(serializablePlayer);
            ArgumentNullException.ThrowIfNull(serializableRun);

            if (serializableRun.Ascension != 1)
                return true;

            ArgumentNullException.ThrowIfNull(serializablePlayer.CharacterId);
            var character = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId);
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (!Sts2RunGameModeCompat.IsStandardSerializableRunForEpochUnlocks(serializableRun))
                return true;

            if (!ModUnlockRegistry.TryGetAscensionOneEpoch(character.Id, out var epochId))
            {
                ModUnlockMissingRuleWarnings.WarnOnce(
                    $"ascension_one_epoch:{character.Id}",
                    $"[Unlocks] Mod character '{character.Id}' has no registered ascension-one win epoch (UnlockEpochAfterAscensionOneWin / RegisterAscensionOneEpoch). " +
                    "Leaving vanilla post-run check in place (no-op for this character).");
                return true;
            }

            if (SaveManager.Instance.Progress.IsEpochObtained(epochId))
                return false;

            if (!EpochRuntimeCompatibility.CanUseEpochId(
                    epochId,
                    $"ascension-one epoch rule for mod character '{character.Id}'"))
                return false;

            SaveManager.Instance.ObtainEpoch(epochId);
            if (!serializablePlayer.DiscoveredEpochs.Contains(epochId, StringComparer.Ordinal))
                serializablePlayer.DiscoveredEpochs.Add(epochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained epoch '{epochId}' after ascension-1 win for mod character '{character.Id}'.");

            return false;
        }
    }

    /// <summary>
    ///     Replaces vanilla post-run character-unlock epoch checks for mod characters with registry-driven grants.
    /// </summary>
    public class PostRunCharacterUnlockEpochCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "postrun_character_unlock_epoch_compatibility";

        /// <inheritdoc />
        public static string Description =>
            "Handle post-run character unlock epochs for mod characters via registered RitsuLib unlock rules";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "PostRunUnlockCharacterEpochCheck",
                    [typeof(SerializablePlayer), typeof(SerializableRun)],
                    true),
            ];
        }

        /// <summary>
        ///     Obtains the registered post-run character-unlock epoch when appropriate; skips vanilla when handled.
        /// </summary>
        public static bool Prefix(SerializablePlayer serializablePlayer, SerializableRun serializableRun)
        {
            ArgumentNullException.ThrowIfNull(serializablePlayer);
            ArgumentNullException.ThrowIfNull(serializableRun);

            ArgumentNullException.ThrowIfNull(serializablePlayer.CharacterId);
            var character = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId);
            if (!ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (!Sts2RunGameModeCompat.IsStandardSerializableRunForEpochUnlocks(serializableRun))
                return true;

            if (!ModUnlockRegistry.TryGetPostRunCharacterUnlockEpoch(character.Id, out var epochId))
            {
                ModUnlockMissingRuleWarnings.WarnOnce(
                    $"postrun_char_unlock_epoch:{character.Id}",
                    $"[Unlocks] Mod character '{character.Id}' has no registered post-run character-unlock epoch (UnlockCharacterAfterRunAs / RegisterPostRunCharacterUnlockEpoch). " +
                    "Leaving vanilla post-run check in place (no-op for this character).");
                return true;
            }

            if (SaveManager.Instance.Progress.IsEpochObtained(epochId))
                return false;

            if (!EpochRuntimeCompatibility.CanUseEpochId(
                    epochId,
                    $"post-run character unlock epoch rule for mod character '{character.Id}'"))
                return false;

            SaveManager.Instance.ObtainEpoch(epochId);
            if (!serializablePlayer.DiscoveredEpochs.Contains(epochId, StringComparer.Ordinal))
                serializablePlayer.DiscoveredEpochs.Add(epochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained post-run character unlock epoch '{epochId}' for mod character '{character.Id}'.");

            return false;
        }
    }

    /// <summary>
    ///     Overrides ascension reveal queries for characters with a registered reveal epoch dependency.
    /// </summary>
    public class AscensionEpochRevealCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ascension_epoch_reveal_compatibility";

        /// <inheritdoc />
        public static string Description =>
            "Handle ascension reveal checks for mod characters via registered RitsuLib unlock rules";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "IsAscensionEpochRevealed", [typeof(ModelId)])];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Sets the result from save state when a custom ascension reveal epoch is registered.
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ModelId characterId, ref bool __result)
        {
            if (!ModUnlockRegistry.TryGetAscensionRevealEpoch(characterId, out var epochId))
                return true;

            __result = SaveManager.Instance.IsEpochRevealed(epochId);
            return false;
        }
    }
}
