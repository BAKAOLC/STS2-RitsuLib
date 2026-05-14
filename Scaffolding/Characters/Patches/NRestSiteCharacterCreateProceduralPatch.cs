using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     When <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> is set, builds the
    ///     rest-site character node in memory instead of loading <c>RestSiteAnimPath</c>.
    ///     当 <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> 已设置时，在内存中构建休息点角色节点，而不是加载
    ///     <c>RestSiteAnimPath</c>。
    /// </summary>
    public class NRestSiteCharacterCreateProceduralPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NRestSiteCharacter, int> RestSiteCharacterIndexRef =
            AccessTools.FieldRefAccess<NRestSiteCharacter, int>("_characterIndex");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "n_rest_site_character_create_procedural";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Build procedural NRestSiteCharacter when WorldProceduralVisuals.RestSite is defined";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Supplies a procedural instance when applicable; otherwise builds from
        ///     <see cref="MegaCrit.Sts2.Core.Models.CharacterModel.RestSiteAnimPath" /> via
        ///     <see cref="RitsuGodotNodeFactories" /> so baselib-style Godot scenes convert like combat visuals.
        ///     适用时提供程序化实例；否则通过 <see cref="RitsuGodotNodeFactories" /> 从
        ///     <see cref="MegaCrit.Sts2.Core.Models.CharacterModel.RestSiteAnimPath" /> 构建，让 baselib 风格 Godot 场景像战斗视觉一样转换。
        /// </summary>
        public static bool Prefix(Player player, int characterIndex, ref NRestSiteCharacter __result)
        {
            var procedural = ModWorldSceneVisualNodeFactory.TryCreateRestSiteCharacter(player, characterIndex);
            if (procedural != null)
            {
                __result = procedural;
                return false;
            }

            var scene = PreloadManager.Cache.GetScene(player.Character.RestSiteAnimPath);
            __result = RitsuGodotNodeFactories.CreateFromScene<NRestSiteCharacter>(scene,
                PackedScene.GenEditState.Disabled);
            __result.Player = player;
            RestSiteCharacterIndexRef(__result) = characterIndex;
            return false;
        }
    }
}
