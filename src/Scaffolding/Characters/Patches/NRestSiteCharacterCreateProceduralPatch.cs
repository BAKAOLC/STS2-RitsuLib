using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates <see cref="NRestSiteCharacter" /> nodes through the procedural rest-site definition when one
    ///         is available; otherwise, creates a compatible node from the character's rest-site scene or texture.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         有可用的程序化休息处定义时，通过该定义创建 <see cref="NRestSiteCharacter" /> 节点；否则从角色的
    ///         休息处场景或纹理创建兼容节点。
    ///     </para>
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
