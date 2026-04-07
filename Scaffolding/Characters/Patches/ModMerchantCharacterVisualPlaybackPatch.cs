using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Merchant character scenes without Spine use <see cref="ModCreatureVisualPlayback" /> for
    ///     <see cref="NMerchantCharacter.PlayAnimation" /> (textures, AnimationPlayer, AnimatedSprite2D).
    /// </summary>
    public class ModMerchantCharacterVisualPlaybackPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "mod_merchant_character_visual_playback";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Play non-Spine merchant character animations via ModCreatureVisualPlayback";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Returns <see langword="false" /> when playback handled the request (skip vanilla Spine path).
        /// </summary>
        public static bool Prefix(NMerchantCharacter __instance, string anim, bool loop)
        {
            var children = __instance.GetChildren();
            if (children.Count == 0)
                return true;

            if (children[0].GetType().Name.Equals(MegaSprite.spineClassName))
                return true;

            ModCreatureVisualPlayback.TryResolveMerchantCharacterModel(NMerchantRoom.Instance, __instance,
                out var character);

            var worldCues = TryGetMerchantWorldCueSet(character);
            return !ModCreatureVisualPlayback.TryPlayOnVisualRoot(__instance, character, anim, loop, worldCues);
        }

        private static VisualCueSet? TryGetMerchantWorldCueSet(CharacterModel? character)
        {
            return character is not IModCharacterAssetOverrides
            {
                WorldProceduralVisuals.Merchant.CueSet: { } cueSet,
            }
                ? null
                : cueSet;
        }
    }
}
