using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Applies optional card-pool deck-view style overrides after the vanilla deck screen initializes.
    ///     在原版牌组查看界面初始化后应用可选的卡池样式覆盖。
    /// </summary>
    internal sealed class CardPoolDeckViewStylePatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NDeckViewScreen, Player> PlayerRef =
            AccessTools.FieldRefAccess<NDeckViewScreen, Player>("_player");

        public static string PatchId => "content_asset_override_card_pool_deck_view_style";
        public static string Description => "Allow card pools to style the vanilla deck-view screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDeckViewScreen), nameof(NDeckViewScreen._Ready))];
        }

        public static void Postfix(NDeckViewScreen __instance)
        {
            var player = PlayerRef(__instance);
            var character = player.Character;
            var cardPool = character.CardPool;
            if (!CardPoolDeckViewStyleRegistry.TryGetStyle(cardPool, out var style))
                return;

            var context = new CardPoolDeckViewStyleContext(player, character, cardPool, __instance);
            ApplyToolbarBackground(__instance, cardPool, style, context);
            ApplySortButtonHue(__instance, cardPool, style, context);
            ApplyUpgradeLabel(__instance, style);
        }

        private static void ApplyToolbarBackground(
            NDeckViewScreen screen,
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            var background = screen.GetNodeOrNull<Control>("%SortingBg");
            if (background == null)
                return;

            if (!string.IsNullOrWhiteSpace(style.ToolbarBackgroundTexturePath))
                ApplyToolbarBackgroundTexture(background, cardPool, style.ToolbarBackgroundTexturePath);

            var material = ResolveToolbarBackgroundMaterial(cardPool, style, context);
            if (material != null)
                background.Material = material;
        }

        private static void ApplyToolbarBackgroundTexture(Control background, CardPoolModel cardPool, string path)
        {
            if (background is not TextureRect textureRect)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Deck-view toolbar background for '{cardPool.GetType().Name}' is not a TextureRect; texture override skipped.");
                return;
            }

            try
            {
                textureRect.Texture = PreloadManager.Cache.GetAsset<Texture2D>(path);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Could not load deck-view toolbar background texture '{path}' for '{cardPool.GetType().Name}': {ex.Message}");
            }
        }

        private static void ApplySortButtonHue(
            NDeckViewScreen screen,
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            var material = ResolveSortButtonHueMaterial(cardPool, style, context);
            if (material == null)
                return;

            foreach (var uniqueName in new[]
                     {
                         "%ObtainedSorter",
                         "%CardTypeSorter",
                         "%CostSorter",
                         "%AlphabeticalSorter",
                     })
                screen.GetNodeOrNull<NCardViewSortButton>(uniqueName)?.SetHue(material);
        }

        private static void ApplyUpgradeLabel(NDeckViewScreen screen, CardPoolDeckViewStyle style)
        {
            var label = screen.GetNodeOrNull<MegaLabel>("%ViewUpgradesLabel");
            if (label == null)
                return;

            if (style.UpgradePreviewLabelColor is { } labelColor)
                label.AddThemeColorOverride("font_color", labelColor);
            if (style.UpgradePreviewLabelOutlineColor is { } outlineColor)
                label.AddThemeColorOverride("font_outline_color", outlineColor);
        }

        private static Material? ResolveToolbarBackgroundMaterial(
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            if (style.ToolbarBackgroundMaterialProvider == null)
                return style.ToolbarBackgroundMaterial;

            try
            {
                return style.ToolbarBackgroundMaterialProvider(context) ?? style.ToolbarBackgroundMaterial;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Deck-view toolbar material provider failed for '{cardPool.GetType().Name}': {ex.Message}");
                return style.ToolbarBackgroundMaterial;
            }
        }

        private static ShaderMaterial? ResolveSortButtonHueMaterial(
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            if (style.SortButtonHueMaterialProvider == null)
                return style.SortButtonHueMaterial;

            try
            {
                return style.SortButtonHueMaterialProvider(context) ?? style.SortButtonHueMaterial;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Deck-view sort-button hue provider failed for '{cardPool.GetType().Name}': {ex.Message}");
                return style.SortButtonHueMaterial;
            }
        }
    }
}
