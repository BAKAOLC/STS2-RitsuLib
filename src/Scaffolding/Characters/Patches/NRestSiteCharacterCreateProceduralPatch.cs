using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     When <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> is set, builds the
    ///     rest-site character node in memory instead of loading <c>RestSiteAnimPath</c>.
    ///     当 <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> 已设置时，在内存中构建休息点角色节点，而不是加载
    ///     <c>RestSiteAnimPath</c>。
    /// </summary>
    internal class NRestSiteCharacterCreateProceduralPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NRestSiteCharacter, int> RestSiteCharacterIndexRef =
            AccessTools.FieldRefAccess<NRestSiteCharacter, int>("_characterIndex");

        public static string PatchId => "n_rest_site_character_create_procedural";

        public static string Description =>
            "Build procedural NRestSiteCharacter when WorldProceduralVisuals.RestSite is defined";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))];
        }

        public static bool Prefix(Player player, int characterIndex, ref NRestSiteCharacter __result)
        {
            var procedural = ModWorldSceneVisualNodeFactory.TryCreateRestSiteCharacter(player, characterIndex);
            if (procedural != null)
            {
                __result = procedural;
                return false;
            }

            __result = CharacterWorldScenePathFactoryHelper.CreateFromSceneOrTexture<NRestSiteCharacter>(
                player.Character,
                player.Character.RestSiteAnimPath,
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath),
                PackedScene.GenEditState.Disabled);
            __result.Player = player;
            RestSiteCharacterIndexRef(__result) = characterIndex;
            return false;
        }
    }
}
