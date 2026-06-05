using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     After a combat creature node becomes ready, optionally swaps in mod Spine skeleton data from
    ///     <see cref="IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath" /> when visuals support it.
    ///     战斗生物节点 ready 后，如果视觉支持，则可选择从
    ///     <see cref="IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath" /> 换入 mod Spine skeleton 数据。
    /// </summary>
    public class CharacterCombatSpineOverridePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "character_combat_spine_override";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod characters to replace combat Spine skeleton data while reusing existing visuals scenes";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature._Ready))];
        }

        /// <summary>
        ///     Loads and applies the override skeleton resource to the player’s combat visuals when eligible.
        ///     符合条件时，加载覆盖 skeleton 资源并应用到玩家的战斗视觉。
        /// </summary>
        public static void Postfix(NCreature __instance)
        {
            var player = __instance.Entity?.Player;
            if (player?.Character is not { } character)
                return;

            var skeletonPath = CharacterAssetOverridePatchHelper.ResolveCombatSpineSkeletonDataPath(character);
            if (string.IsNullOrWhiteSpace(skeletonPath))
                return;

            if (!AssetPathDiagnostics.Exists(skeletonPath, character,
                    nameof(IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath)))
                return;

            var visuals = __instance.Visuals;
            if (visuals is not { HasSpineAnimation: true } ||
                !NCreatureVisualsSpineCompat.HasSpineTargetForOverride(visuals))
                return;

            try
            {
                var skeletonData = ResourceLoader.Load<Resource>(skeletonPath);
                if (skeletonData == null)
                {
                    RitsuLibFramework.Logger.Warn($"[Visuals] Failed to load combat spine data: {skeletonPath}");
                    return;
                }

                if (!NCreatureVisualsSpineCompat.TryApplyCombatSkeletonOverride(visuals, skeletonData))
                    RitsuLibFramework.Logger.Warn(
                        $"[Visuals] Could not apply combat spine override (no Body/SpineBody target): {skeletonPath}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Visuals] Failed to apply combat spine override '{skeletonPath}': {ex.Message}");
            }
        }
    }
}
