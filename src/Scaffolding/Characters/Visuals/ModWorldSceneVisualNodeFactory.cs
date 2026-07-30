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
    ///     <para xml:lang="en">
    ///         Builds minimal merchant and rest-site character nodes in memory so mods can omit custom <c>tscn</c>
    ///         scenes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在内存中创建最精简的商人和休息处角色节点，使模组无需提供自定义 <c>tscn</c> 场景。
    ///     </para>
    /// </summary>
    public static class ModWorldSceneVisualNodeFactory
    {
        private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

        private static readonly AccessTools.FieldRef<NRestSiteCharacter, int> RestSiteCharacterIndexRef =
            AccessTools.FieldRefAccess<NRestSiteCharacter, int>("_characterIndex");

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a new <see cref="NMerchantCharacter" /> with a non-Spine sprite child when
        ///         <paramref name="character" /> defines procedural merchant visuals; otherwise, returns
        ///         <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <paramref name="character" /> 定义了程序化商人形象时，返回带有非 Spine 精灵子节点的新
        ///         <see cref="NMerchantCharacter" />；否则返回 <see langword="null" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         When the player's character defines procedural rest-site visuals, builds an
        ///         <see cref="NRestSiteCharacter" /> tree containing the hitbox, thought-bubble anchors, base-game
        ///         selection reticle, and a non-Spine <c>Visuals</c> sprite under <c>ControlRoot</c>. Returns
        ///         <see langword="null" /> when no definition is present or the required reticle resource is missing.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当玩家角色定义了程序化休息处形象时，创建 <see cref="NRestSiteCharacter" /> 节点树，其中包含
        ///         点击区域、思考气泡锚点、游戏本体的选择指示器，以及位于 <c>ControlRoot</c> 下的非 Spine
        ///         <c>Visuals</c> 精灵。没有对应定义或缺少必需的选择指示器资源时返回
        ///         <see langword="null" />。
        ///     </para>
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
                RitsuLibFramework.Logger.ErrorNoTrace(
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
