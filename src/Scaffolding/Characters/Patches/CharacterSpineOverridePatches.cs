using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         After a combat creature node becomes ready, applies the mod character's Spine skeleton data from
    ///         <see cref="IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath" /> when its visuals support
    ///         skeleton replacement.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         战斗生物节点准备就绪后，如果其形象支持替换骨骼，则应用模组角色通过
    ///         <see cref="IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath" /> 指定的 Spine 骨骼数据。
    ///     </para>
    /// </summary>
    internal class CharacterCombatSpineOverridePatch : IPatchMethod
    {
        public static string PatchId => "character_combat_spine_override";

        public static string Description =>
            "Allow mod characters to replace combat Spine skeleton data while reusing existing visuals scenes";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature._Ready))];
        }

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
