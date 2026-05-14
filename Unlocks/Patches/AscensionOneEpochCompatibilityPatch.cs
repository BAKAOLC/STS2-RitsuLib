using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using SerializableRun = MegaCrit.Sts2.Core.Saves.SerializableRun;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Replaces vanilla ascension-one epoch checks for mod-owned characters with registry-driven epoch grants.
    ///     将 mod 拥有角色的原版进阶一纪元检查替换为由注册表驱动的纪元授予。
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
        ///     在适当时获得已注册的进阶一纪元；已处理时跳过原版逻辑。
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
                if (character is IModCharacterEpochTimelineRequirement { RequiresEpochAndTimeline: false })
                    return false;

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
    ///     将 mod 角色的原版跑局后角色解锁纪元检查替换为由注册表驱动的授予。
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
        ///     在适当时获得已注册的跑局后角色解锁纪元；已处理时跳过原版逻辑。
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
                if (character is IModCharacterEpochTimelineRequirement { RequiresEpochAndTimeline: false })
                    return false;

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
    ///     覆盖已注册显示纪元依赖的角色进阶显示查询。
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
        ///     注册自定义进阶显示纪元时，根据存档状态设置结果。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ModelId characterId, ref bool __result)
        {
            var character = ModelDb.GetById<CharacterModel>(characterId);
            if (!ModUnlockRegistry.TryGetAscensionRevealEpoch(characterId, out var epochId))
            {
                if (character is not IModCharacterEpochTimelineRequirement
                    {
                        RequiresEpochAndTimeline: false,
                    }) return true;
                __result = true;
                return false;
            }

            if (ModUnlockRegistry.IsEpochRequirementIgnoredForModelType(character.GetType()))
            {
                __result = true;
                return false;
            }

            __result = SaveManager.Instance.IsEpochRevealed(epochId);
            return false;
        }
    }
}
