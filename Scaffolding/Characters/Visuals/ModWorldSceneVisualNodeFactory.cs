using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace STS2RitsuLib.Scaffolding.Characters.Visuals
{
    /// <summary>
    ///     Builds minimal in-memory merchant / rest-site character nodes so mods can omit custom <c>tscn</c> scenes.
    /// </summary>
    public static class ModWorldSceneVisualNodeFactory
    {
        private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

        private static readonly AccessTools.FieldRef<NRestSiteCharacter, int> RestSiteCharacterIndexRef =
            AccessTools.FieldRefAccess<NRestSiteCharacter, int>("_characterIndex");

        /// <summary>
        ///     When <paramref name="character" /> defines <see cref="IModCharacterAssetOverrides.WorldProceduralVisuals" />
        ///     merchant data, returns a new <see cref="NMerchantCharacter" /> with a non-Spine sprite child; otherwise
        ///     <see langword="null" />.
        /// </summary>
        public static NMerchantCharacter? TryInstantiateMerchantCharacter(CharacterModel character)
        {
            if (character is not IModCharacterAssetOverrides { WorldProceduralVisuals.Merchant: not null })
                return null;

            var root = new NMerchantCharacter();
            root.Name = "RitsuProceduralMerchant";

            var sprite = new Sprite2D();
            sprite.Name = "Visuals";
            root.AddChild(sprite);
            sprite.Owner = root;

            return root;
        }

        /// <summary>
        ///     When the player character defines rest-site procedural visuals, builds an <see cref="NRestSiteCharacter" />
        ///     tree compatible with vanilla scripts (hitbox, thought anchors, selection reticle from base game assets).
        ///     The <see cref="Sprite2D" /> named <c>Visuals</c> is parented under <c>ControlRoot</c> so vanilla
        ///     <c>FlipX</c> matches BaseLib <c>NRestSiteCharacterFactory</c> rest-site layouts.
        /// </summary>
        public static NRestSiteCharacter? TryCreateRestSiteCharacter(Player player, int characterIndex)
        {
            if (player.Character is not IModCharacterAssetOverrides { WorldProceduralVisuals.RestSite: not null })
                return null;

            var root = new NRestSiteCharacter();
            root.Name = "RitsuProceduralRestSiteCharacter";
            root.Player = player;
            RestSiteCharacterIndexRef(root) = characterIndex;

            var controlRoot = new Control { Name = "ControlRoot" };
            root.AddChild(controlRoot);
            controlRoot.Owner = root;

            var hitbox = new Control { Name = "Hitbox" };
            hitbox.UniqueNameInOwner = true;
            hitbox.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            hitbox.OffsetLeft = -155f;
            hitbox.OffsetTop = -351f;
            hitbox.OffsetRight = 266f;
            hitbox.OffsetBottom = 332f;
            controlRoot.AddChild(hitbox);
            hitbox.Owner = root;

            if (!ResourceLoader.Exists(SelectionReticleScenePath))
            {
                RitsuLibFramework.Logger.Error(
                    $"[WorldVisuals] Missing selection reticle scene '{SelectionReticleScenePath}'; cannot build rest-site shell.");
                root.QueueFree();
                return null;
            }

            var reticle =
                PreloadManager.Cache.GetScene(SelectionReticleScenePath)
                    .Instantiate<NSelectionReticle>();
            reticle.Name = "SelectionReticle";
            reticle.UniqueNameInOwner = true;
            controlRoot.AddChild(reticle);
            reticle.Owner = root;

            var thoughtLeft = new Control { Name = "ThoughtBubbleLeft" };
            thoughtLeft.UniqueNameInOwner = true;
            thoughtLeft.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            thoughtLeft.OffsetLeft = -73.6836f;
            thoughtLeft.OffsetTop = -324.997f;
            controlRoot.AddChild(thoughtLeft);
            thoughtLeft.Owner = root;

            var thoughtRight = new Control { Name = "ThoughtBubbleRight" };
            thoughtRight.UniqueNameInOwner = true;
            thoughtRight.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            thoughtRight.OffsetLeft = 209.209f;
            thoughtRight.OffsetTop = -317.103f;
            controlRoot.AddChild(thoughtRight);
            thoughtRight.Owner = root;

            var sprite = new Sprite2D { Name = "Visuals" };
            controlRoot.AddChild(sprite);
            sprite.Owner = root;

            return root;
        }
    }
}
