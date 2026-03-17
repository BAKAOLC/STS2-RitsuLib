using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static class ContentAssetOverridePatchHelper
    {
        // ReSharper disable once InconsistentNaming
        internal static bool TryUseStringOverride<TOverrides>(
            object instance,
            ref string __result,
            Func<TOverrides, string?> selector)
            where TOverrides : class
        {
            if (instance is not TOverrides overrides)
                return true;

            var value = selector(overrides);
            if (string.IsNullOrWhiteSpace(value))
                return true;

            __result = value;
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseTextureOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, out var path))
                return true;

            __result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseCompressedTextureOverride<TOverrides>(
            object instance,
            ref CompressedTexture2D __result,
            Func<TOverrides, string?> selector)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, out var path))
                return true;

            __result = ResourceLoader.Load<CompressedTexture2D>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseMaterialOverride<TOverrides>(
            object instance,
            ref Material __result,
            Func<TOverrides, string?> selector)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, out var path))
                return true;

            __result = ResourceLoader.Load<Material>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUsePortraitPathList(IModCardAssetOverrides overrides, ref IEnumerable<string> __result)
        {
            var paths = new[] { overrides.CustomPortraitPath, overrides.CustomBetaPortraitPath }
                .Where(path => !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
                .Cast<string>()
                .ToArray();

            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseExistenceOverride(string? path, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            __result = ResourceLoader.Exists(path);
            return false;
        }

        private static bool TryGetPath<TOverrides>(object instance, Func<TOverrides, string?> selector, out string path)
            where TOverrides : class
        {
            path = string.Empty;

            if (instance is not TOverrides overrides)
                return false;

            var candidate = selector(overrides);
            if (string.IsNullOrWhiteSpace(candidate) || !ResourceLoader.Exists(candidate))
                return false;

            path = candidate;
            return true;
        }
    }

    public interface IModCardAssetOverrides
    {
        CardAssetProfile AssetProfile { get; }
        string? CustomPortraitPath { get; }
        string? CustomBetaPortraitPath { get; }
        string? CustomFramePath { get; }
        string? CustomPortraitBorderPath { get; }
        string? CustomEnergyIconPath { get; }
        string? CustomFrameMaterialPath { get; }
        string? CustomOverlayScenePath { get; }
        string? CustomBannerTexturePath { get; }
        string? CustomBannerMaterialPath { get; }
    }

    public interface IModRelicAssetOverrides
    {
        RelicAssetProfile AssetProfile { get; }
        string? CustomIconPath { get; }
        string? CustomIconOutlinePath { get; }
        string? CustomBigIconPath { get; }
    }

    public interface IModPowerAssetOverrides
    {
        PowerAssetProfile AssetProfile { get; }
        string? CustomIconPath { get; }
        string? CustomBigIconPath { get; }
    }

    public interface IModOrbAssetOverrides
    {
        OrbAssetProfile AssetProfile { get; }
        string? CustomIconPath { get; }
        string? CustomVisualsScenePath { get; }
    }

    public interface IModActAssetOverrides
    {
        ActAssetProfile AssetProfile => ActAssetProfile.Empty;
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;
        string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;
        string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;
        string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;
        string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;
        string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;
    }

    public class CardPortraitPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_path";
        public static string Description => "Allow mod cards to override CardModel portrait paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_PortraitPath"),
                new(typeof(CardModel), "get_BetaPortraitPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_PortraitPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomPortraitPath),
                "get_BetaPortraitPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomBetaPortraitPath),
                _ => true,
            };
        }
    }

    public class CardPortraitAvailabilityPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_availability";
        public static string Description => "Allow mod cards to override CardModel portrait availability checks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_HasPortrait"),
                new(typeof(CardModel), "get_HasBetaPortrait"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            return __originalMethod.Name switch
            {
                "get_HasPortrait" => ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                    overrides.CustomPortraitPath, ref __result),
                "get_HasBetaPortrait" => ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                    overrides.CustomBetaPortraitPath, ref __result),
                _ => true,
            };
        }
    }

    public class CardTextureOverridePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_texture";

        public static string Description =>
            "Allow mod cards to override card frame, portrait border, and energy icon textures";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_Frame"),
                new(typeof(CardModel), "get_PortraitBorder"),
                new(typeof(CardModel), "get_EnergyIcon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Frame" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(__instance,
                    ref __result, o => o.CustomFramePath),
                "get_PortraitBorder" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomPortraitBorderPath),
                "get_EnergyIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomEnergyIconPath),
                _ => true,
            };
        }
    }

    public class CardFrameMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_frame_material";
        public static string Description => "Allow mod cards to override card frame materials";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_FrameMaterial"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomFrameMaterialPath);
        }
    }

    public class CardAllPortraitPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_all_portrait_paths";
        public static string Description => "Allow mod cards to advertise custom portrait assets for preloading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_AllPortraitPaths"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            return __instance is not IModCardAssetOverrides overrides ||
                   ContentAssetOverridePatchHelper.TryUsePortraitPathList(overrides, ref __result);
        }
    }

    public class CardOverlayPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_overlay_path";
        public static string Description => "Allow mod cards to override overlay scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_OverlayPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomOverlayScenePath);
        }
    }

    public class CardOverlayAvailabilityPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_overlay_availability";
        public static string Description => "Allow mod cards to advertise overlay availability from custom scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_HasBuiltInOverlay"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(overrides.CustomOverlayScenePath,
                ref __result);
        }
    }

    public class CardOverlayCreatePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_create_overlay";
        public static string Description => "Allow mod cards to instantiate overlays from custom scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.CreateOverlay)),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            var path = overrides.CustomOverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
                return true;

            __result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }
    }

    public class RelicIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_icon_path";
        public static string Description => "Allow mod relics to override icon path assets";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "get_IconPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(RelicModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModRelicAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath);
        }
    }

    public class RelicTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_texture";
        public static string Description => "Allow mod relics to override icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "get_Icon"),
                new(typeof(RelicModel), "get_IconOutline"),
                new(typeof(RelicModel), "get_BigIcon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, RelicModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(__instance,
                    ref __result, o => o.CustomIconPath),
                "get_IconOutline" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(
                    __instance, ref __result, o => o.CustomIconOutlinePath),
                "get_BigIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(
                    __instance, ref __result, o => o.CustomBigIconPath),
                _ => true,
            };
        }
    }

    public class PowerIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_icon_path";
        public static string Description => "Allow mod powers to override icon path assets";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "get_IconPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(PowerModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath);
        }
    }

    public class PowerTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_texture";
        public static string Description => "Allow mod powers to override icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "get_Icon"),
                new(typeof(PowerModel), "get_BigIcon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, PowerModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(__instance,
                    ref __result, o => o.CustomIconPath),
                "get_BigIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(
                    __instance, ref __result, o => o.CustomBigIconPath),
                _ => true,
            };
        }
    }

    public class OrbIconPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_icon";
        public static string Description => "Allow mod orbs to override icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_Icon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(OrbModel __instance, ref CompressedTexture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseCompressedTextureOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath);
        }
    }

    public class OrbSpritePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_sprite_path";
        public static string Description => "Allow mod orbs to override visuals scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_SpritePath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(OrbModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomVisualsScenePath);
        }
    }

    public class OrbAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_asset_paths";
        public static string Description => "Allow mod orbs to advertise custom asset paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_AssetPaths"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(OrbModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModOrbAssetOverrides overrides)
                return true;

            var paths = new[] { overrides.CustomIconPath, overrides.CustomVisualsScenePath }
                .Where(path => !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
                .Cast<string>()
                .ToArray();
            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }
    }

    public class PotionImagePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_image_path";
        public static string Description => "Allow mod potions to override image paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "get_ImagePath"),
                new(typeof(PotionModel), "get_OutlinePath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_ImagePath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomImagePath),
                "get_OutlinePath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomOutlinePath),
                _ => true,
            };
        }
    }

    public class PotionTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_texture";
        public static string Description => "Allow mod potions to override image textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "get_Image"),
                new(typeof(PotionModel), "get_Outline"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Image" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomImagePath),
                "get_Outline" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomOutlinePath),
                _ => true,
            };
        }
    }

    public class CardBannerTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_banner_texture";
        public static string Description => "Allow mod cards to override BannerTexture";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_BannerTexture")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerTexturePath);
        }
    }

    public class CardBannerMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_banner_material";
        public static string Description => "Allow mod cards to override BannerMaterial";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_BannerMaterial")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerMaterialPath);
        }
    }

    public class ActBackgroundScenePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_background_scene_path";
        public static string Description => "Allow mod acts to override background scene path";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "get_BackgroundScenePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath);
        }
    }

    public class ActRestSiteBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_rest_site_background_path";
        public static string Description => "Allow mod acts to override rest site background path";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "get_RestSiteBackgroundPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomRestSiteBackgroundPath);
        }
    }

    public class ActMapBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_map_background_path";
        public static string Description => "Allow mod acts to override map background paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), "get_MapTopBgPath"),
                new(typeof(ActModel), "get_MapMidBgPath"),
                new(typeof(ActModel), "get_MapBotBgPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(MethodBase __originalMethod, ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_MapTopBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapTopBgPath),
                "get_MapMidBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapMidBgPath),
                "get_MapBotBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapBotBgPath),
                _ => true,
            };
        }
    }

    public interface IModAfflictionAssetOverrides
    {
        AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;
        string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }

    public class AfflictionOverlayPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_overlay_path";
        public static string Description => "Allow mod afflictions to override OverlayPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "get_OverlayPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(AfflictionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                __instance, ref __result, o => o.CustomOverlayScenePath);
        }
    }

    public class AfflictionHasOverlayPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_has_overlay";
        public static string Description => "Allow mod afflictions to advertise overlay availability";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "get_HasOverlay")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(AfflictionModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            var path = string.Empty;
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                       __instance,
                       ref path,
                       o => o.CustomOverlayScenePath) ||
                   ContentAssetOverridePatchHelper.TryUseExistenceOverride(path, ref __result);
        }
    }

    public class AfflictionCreateOverlayPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_create_overlay";
        public static string Description => "Allow mod afflictions to instantiate overlays from custom scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), nameof(AfflictionModel.CreateOverlay))];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(AfflictionModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
        {
            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath))
                return true;

            if (!ResourceLoader.Exists(path))
                return true;

            __result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }
    }

    public interface IModEnchantmentAssetOverrides
    {
        EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;
        string? CustomIconPath => AssetProfile.IconPath;
    }

    public class EnchantmentIntendedIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_enchantment_intended_icon_path";
        public static string Description => "Allow mod enchantments to override IntendedIconPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EnchantmentModel), "get_IntendedIconPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(EnchantmentModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEnchantmentAssetOverrides>(
                __instance, ref __result, o => o.CustomIconPath);
        }
    }

    public class PowerResolvedBigIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_resolved_big_icon_path";
        public static string Description => "Allow mod powers to override ResolvedBigIconPath for preloading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), "get_ResolvedBigIconPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(PowerModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                __instance, ref __result, o => o.CustomBigIconPath);
        }
    }
}
