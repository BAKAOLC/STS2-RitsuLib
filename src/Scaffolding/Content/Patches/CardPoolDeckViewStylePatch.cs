using System.Reflection;
using System.Reflection.Emit;
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
        private const string SafeHueFallbackMaterialPath = "res://materials/cards/frames/card_frame_colorless_mat.tres";

        private static readonly AccessTools.FieldRef<NDeckViewScreen, Player> PlayerRef =
            AccessTools.FieldRefAccess<NDeckViewScreen, Player>("_player");

        private static readonly MethodInfo ResolveVanillaDeckViewHueMaterialMethod = AccessTools.DeclaredMethod(
            typeof(CardPoolDeckViewStylePatch),
            nameof(ResolveVanillaDeckViewHueMaterial));

        public static string PatchId => "content_asset_override_card_pool_deck_view_style";
        public static string Description => "Allow card pools to style the vanilla deck-view screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDeckViewScreen), nameof(NDeckViewScreen._Ready))];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Castclass && instruction.operand is Type type &&
                    type == typeof(ShaderMaterial))
                {
                    var loadScreen = new CodeInstruction(OpCodes.Ldarg_0);
                    loadScreen.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return loadScreen;
                    yield return new(OpCodes.Call, ResolveVanillaDeckViewHueMaterialMethod);
                    replaced++;
                    continue;
                }

                yield return instruction;
            }

            if (replaced != 1)
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Expected to rewrite one NDeckViewScreen ShaderMaterial cast, but rewrote {replaced}.");
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
            ApplySortButtons(__instance, cardPool, style, context);
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

        private static void ApplySortButtons(
            NDeckViewScreen screen,
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            foreach (var uniqueName in new[]
                     {
                         "%ObtainedSorter",
                         "%CardTypeSorter",
                         "%CostSorter",
                         "%AlphabeticalSorter",
                     })
            {
                if (screen.GetNodeOrNull<NCardViewSortButton>(uniqueName) is not { } button)
                    continue;

                ApplySortButtonBackground(button, cardPool, style, context);
                ApplySortButtonHue(button, cardPool, style, context);
            }
        }

        private static void ApplySortButtonBackground(
            NCardViewSortButton button,
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            if (string.IsNullOrWhiteSpace(style.SortButtonBackgroundTexturePath) &&
                style.SortButtonBackgroundMaterial == null &&
                style.SortButtonBackgroundMaterialProvider == null)
                return;

            var background = button.GetNodeOrNull<TextureRect>("%ButtonImage");
            if (background == null)
                return;

            if (!string.IsNullOrWhiteSpace(style.SortButtonBackgroundTexturePath))
                try
                {
                    background.Texture =
                        PreloadManager.Cache.GetAsset<Texture2D>(style.SortButtonBackgroundTexturePath);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] Could not load deck-view sort button background texture '{style.SortButtonBackgroundTexturePath}' for '{cardPool.GetType().Name}': {ex.Message}");
                }

            var material = ResolveSortButtonBackgroundMaterial(cardPool, style, context);
            if (material != null)
                background.Material = material;
        }

        private static void ApplySortButtonHue(
            NCardViewSortButton button,
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            if (style.DisableSortButtonHue == true)
                return;

            var material = ResolveSortButtonHueMaterial(cardPool, style, context);
            if (material != null)
                button.SetHue(material);
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

        private static Material? ResolveSortButtonBackgroundMaterial(
            CardPoolModel cardPool,
            CardPoolDeckViewStyle style,
            CardPoolDeckViewStyleContext context)
        {
            if (style.SortButtonBackgroundMaterialProvider == null)
                return style.SortButtonBackgroundMaterial;

            try
            {
                return style.SortButtonBackgroundMaterialProvider(context) ?? style.SortButtonBackgroundMaterial;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Deck-view sort button background material provider failed for '{cardPool.GetType().Name}': {ex.Message}");
                return style.SortButtonBackgroundMaterial;
            }
        }

        private static ShaderMaterial ResolveVanillaDeckViewHueMaterial(Material material, NDeckViewScreen screen)
        {
            if (material is ShaderMaterial shaderMaterial)
                return shaderMaterial;

            var player = PlayerRef(screen);
            var pool = player.Character.CardPool;
            try
            {
                if (PreloadManager.Cache.GetMaterial(SafeHueFallbackMaterialPath) is ShaderMaterial fallback)
                    return fallback;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Could not load safe deck-view hue fallback for '{pool.GetType().Name}': {ex.Message}");
            }

            RitsuLibFramework.Logger.Warn(
                $"[Assets] Deck-view frame material for '{pool.GetType().Name}' is {material.GetType().Name}, not ShaderMaterial. Using an empty ShaderMaterial fallback.");
            return new();
        }
    }
}
