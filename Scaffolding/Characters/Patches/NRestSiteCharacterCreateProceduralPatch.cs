using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     When <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" /><c>.RestSite</c> is set, builds the
    ///     rest-site character node in memory instead of loading <c>RestSiteAnimPath</c>.
    /// </summary>
    public class NRestSiteCharacterCreateProceduralPatch : IPatchMethod
    {
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
        ///     Supplies a procedural instance when applicable; otherwise runs vanilla <c>Create</c>.
        /// </summary>
        public static bool Prefix(Player player, int characterIndex, ref NRestSiteCharacter __result)
        {
            var created = ModWorldSceneVisualNodeFactory.TryCreateRestSiteCharacter(player, characterIndex);
            if (created == null)
                return true;

            __result = created;
            return false;
        }
    }
}
