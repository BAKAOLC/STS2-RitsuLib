using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Timeline;
using SerializableRun = MegaCrit.Sts2.Core.Saves.SerializableRun;

namespace STS2RitsuLib.Unlocks.Patches
{
    /// <summary>
    ///     Replaces vanilla ascension-one epoch checks for mod-owned characters with registry-driven epoch grants.
    ///     将 mod 拥有角色的原版进阶一纪元检查替换为由注册表驱动的纪元授予。
    /// </summary>
    internal class AscensionOneEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "ascension_one_epoch_compatibility";

        public static string Description =>
            "Handle ascension-one epoch unlock checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "CheckAscensionOneCompleted",
                    [typeof(SerializablePlayer), typeof(SerializableRun)],
                    true),
            ];
        }

        public static bool Prefix(SerializablePlayer serializablePlayer, SerializableRun serializableRun)
        {
            ArgumentNullException.ThrowIfNull(serializablePlayer);
            ArgumentNullException.ThrowIfNull(serializableRun);

            if (serializableRun.Ascension != 1)
                return true;

            ArgumentNullException.ThrowIfNull(serializablePlayer.CharacterId);
            var character = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId);
            if (!ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(character))
                return true;

            if (!Sts2RunGameModeCompat.IsStandardSerializableRunForEpochUnlocks(serializableRun))
                return true;

            if (!ModUnlockRegistry.TryGetAscensionOneEpoch(character.Id, out var epochId))
            {
                if (ModCharacterTimelinePolicy.DoesNotRequireEpochAndTimeline(character))
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
            NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(EpochModel.Get(epochId)));
            if (!serializablePlayer.DiscoveredEpochs.Contains(epochId, StringComparer.Ordinal))
                serializablePlayer.DiscoveredEpochs.Add(epochId);

            RitsuLibFramework.Logger.Info(
                $"[Unlocks] Obtained epoch '{epochId}' after ascension-1 win for mod character '{character.Id}'.");

            return false;
        }
    }

    /// <summary>
    ///     Extends vanilla post-run character-unlock epoch checks with registered and template-derived mod grants.
    ///     用已注册和模板推导出的 mod 授予扩展原版跑局后角色解锁纪元检查。
    /// </summary>
    internal class PostRunCharacterUnlockEpochCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "postrun_character_unlock_epoch_compatibility";

        public static string Description =>
            "Handle registered or template-derived post-run character unlock epochs without blocking vanilla chains";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ProgressSaveManager), "PostRunUnlockCharacterEpochCheck",
                    [typeof(SerializablePlayer), typeof(SerializableRun)],
                    true),
            ];
        }

        public static bool Prefix(SerializablePlayer serializablePlayer, SerializableRun serializableRun)
        {
            ArgumentNullException.ThrowIfNull(serializablePlayer);
            ArgumentNullException.ThrowIfNull(serializableRun);

            ArgumentNullException.ThrowIfNull(serializablePlayer.CharacterId);
            var character = ModelDb.GetById<CharacterModel>(serializablePlayer.CharacterId);
            var isModCharacter = ModCharacterTimelinePolicy.IsOwnedOrUsesTimelinePolicy(character);

            if (!Sts2RunGameModeCompat.IsStandardSerializableRunForEpochUnlocks(serializableRun))
                return true;

            var epochIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var epochId in ModUnlockRegistry.GetPostRunCharacterUnlockEpochs(character.Id))
                epochIds.Add(epochId);
            foreach (var epochId in ModTimelineNeowCoExpansion.GetModCharacterRootEpochIdsUnlockedAfterRunAs(
                         character.Id))
                epochIds.Add(epochId);

            if (epochIds.Count == 0)
            {
                if (!isModCharacter)
                    return true;

                if (ModCharacterTimelinePolicy.DoesNotRequireEpochAndTimeline(character))
                    return false;

                ModUnlockMissingRuleWarnings.WarnOnce(
                    $"postrun_char_unlock_epoch:{character.Id}",
                    $"[Unlocks] Mod character '{character.Id}' has no registered post-run character-unlock epoch (UnlocksAfterRunAsType / UnlockCharacterAfterRunAs / RegisterPostRunCharacterUnlockEpoch). " +
                    "Leaving vanilla post-run check in place (no-op for this character).");
                return true;
            }

            foreach (var epochId in epochIds.Where(epochId => !SaveManager.Instance.Progress.IsEpochObtained(epochId))
                         .Where(epochId => EpochRuntimeCompatibility.CanUseEpochId(
                             epochId,
                             $"post-run character unlock epoch rule after run as '{character.Id}'")))
            {
                SaveManager.Instance.ObtainEpoch(epochId);
                NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(EpochModel.Get(epochId)));
                if (!serializablePlayer.DiscoveredEpochs.Contains(epochId, StringComparer.Ordinal))
                    serializablePlayer.DiscoveredEpochs.Add(epochId);

                RitsuLibFramework.Logger.Info(
                    $"[Unlocks] Obtained post-run character unlock epoch '{epochId}' after run as '{character.Id}'.");
            }

            return !isModCharacter;
        }
    }

    /// <summary>
    ///     Overrides ascension reveal queries for characters with a registered reveal epoch dependency.
    ///     覆盖已注册显示纪元依赖的角色进阶显示查询。
    /// </summary>
    internal class AscensionEpochRevealCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "ascension_epoch_reveal_compatibility";

        public static string Description =>
            "Handle ascension reveal checks for mod characters via registered RitsuLib unlock rules";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(StartRunLobby), "IsAscensionEpochRevealed", [typeof(ModelId)])];
        }

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
                if (!ModCharacterTimelinePolicy.DoesNotRequireEpochAndTimeline(character))
                    return true;
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
