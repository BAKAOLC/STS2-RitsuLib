using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Per-owner character visuals for relic/potion/card assets; applied before model-level
    ///     <see cref="IModRelicAssetOverrides" />, <see cref="IModPotionAssetOverrides" />, and
    ///     <see cref="IModCardAssetOverrides" /> patches.
    /// </summary>
    internal static class ModCharacterOwnedVisualOverrideHelper
    {
        internal static bool TryRelicIconPath(RelicModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryRelicIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicIconOutlinePath(RelicModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconOutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconOutlinePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryRelicIconOutlineTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconOutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconOutlinePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicBigIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.BigIconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.BigIconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryPotionImagePath(PotionModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.ImagePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.ImagePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryPotionOutlinePath(PotionModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.OutlinePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryPotionImageTexture(PotionModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.ImagePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.ImagePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryPotionOutlineTexture(PotionModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.OutlinePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardPortraitPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardBetaPortraitPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BetaPortraitPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BetaPortraitPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardFrameTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.FramePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.FramePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardPortraitBorderTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitBorderPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitBorderPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardEnergyIconTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.EnergyIconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.EnergyIconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardFrameMaterial(CardModel instance, ref Material result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.FrameMaterialPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.FrameMaterialPath)))
                return true;

            result = ResourceLoader.Load<Material>(path);
            return false;
        }

        internal static bool TryCardOverlayPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardOverlayExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath));
            return false;
        }

        internal static bool TryCardCreateOverlay(CardModel instance, ref Control result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath)))
                return true;

            result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }

        internal static bool TryCardBannerTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BannerTexturePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BannerTexturePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardBannerMaterial(CardModel instance, ref Material result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BannerMaterialPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BannerMaterialPath)))
                return true;

            result = ResourceLoader.Load<Material>(path);
            return false;
        }

        internal static bool TryCardPortraitExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitPath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitPath));
            return false;
        }

        internal static bool TryCardBetaPortraitExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BetaPortraitPath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BetaPortraitPath));
            return false;
        }

        internal static string[] GetExistingCardPortraitPaths(CardModel instance)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return [];

            return AssetPathDiagnostics.CollectExistingPaths(
                instance,
                (profile.PortraitPath, nameof(CardAssetProfile.PortraitPath)),
                (profile.BetaPortraitPath, nameof(CardAssetProfile.BetaPortraitPath)));
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(RelicModel instance)
        {
            if (instance.IsCanonical)
                return null;

            return instance.Owner?.Character as IModCharacterAssetOverrides;
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(PotionModel instance)
        {
            if (instance.IsCanonical)
                return null;

            return instance.Owner?.Character as IModCharacterAssetOverrides;
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(CardModel instance)
        {
            if (instance.IsCanonical)
                return null;

            return instance.Owner?.Character as IModCharacterAssetOverrides;
        }
    }
}
