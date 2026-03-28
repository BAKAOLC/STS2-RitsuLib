using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Bundle of optional resource paths for mod card portraits, frames, energy icon, overlay scene, and banner.
    /// </summary>
    /// <param name="PortraitPath">Main card portrait image path.</param>
    /// <param name="BetaPortraitPath">Alternate “beta” portrait path, if any.</param>
    /// <param name="FramePath">Card frame texture path.</param>
    /// <param name="PortraitBorderPath">Portrait border / frame accent texture.</param>
    /// <param name="EnergyIconPath">Small energy icon texture for this card.</param>
    /// <param name="FrameMaterialPath">Material resource path for the card frame.</param>
    /// <param name="OverlayScenePath">Packed scene path for built-in card overlay UI.</param>
    /// <param name="BannerTexturePath">Texture used on run-summary or banner UI.</param>
    /// <param name="BannerMaterialPath">Material path for banner rendering.</param>
    public sealed record CardAssetProfile(
        string? PortraitPath = null,
        string? BetaPortraitPath = null,
        string? FramePath = null,
        string? PortraitBorderPath = null,
        string? EnergyIconPath = null,
        string? FrameMaterialPath = null,
        string? OverlayScenePath = null,
        string? BannerTexturePath = null,
        string? BannerMaterialPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static CardAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional relic icon paths (atlas entries and large shop/detail image).
    /// </summary>
    /// <param name="IconPath">Primary relic icon texture path.</param>
    /// <param name="IconOutlinePath">Outline / silhouette icon path.</param>
    /// <param name="BigIconPath">Large relic art path.</param>
    public sealed record RelicAssetProfile(
        string? IconPath = null,
        string? IconOutlinePath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static RelicAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional power icon paths (atlas and large illustration).
    /// </summary>
    /// <param name="IconPath">Power icon texture path.</param>
    /// <param name="BigIconPath">Large power art path.</param>
    public sealed record PowerAssetProfile(
        string? IconPath = null,
        string? BigIconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static PowerAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional orb HUD icon and combat visuals scene paths.
    /// </summary>
    /// <param name="IconPath">Orb icon texture path.</param>
    /// <param name="VisualsScenePath">Scene path for orb combat presentation.</param>
    public sealed record OrbAssetProfile(
        string? IconPath = null,
        string? VisualsScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static OrbAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional potion bottle image and outline atlas paths.
    /// </summary>
    /// <param name="ImagePath">Main potion image texture path.</param>
    /// <param name="OutlinePath">Outline texture path.</param>
    public sealed record PotionAssetProfile(
        string? ImagePath = null,
        string? OutlinePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static PotionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional affliction card overlay scene path.
    /// </summary>
    /// <param name="OverlayScenePath">Packed scene path for the affliction overlay.</param>
    public sealed record AfflictionAssetProfile(
        string? OverlayScenePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static AfflictionAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional enchantment icon texture path.
    /// </summary>
    /// <param name="IconPath">Enchantment icon image path.</param>
    public sealed record EnchantmentAssetProfile(
        string? IconPath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static EnchantmentAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Optional act-level background, map layer, rest site, and treasure chest Spine resource paths.
    /// </summary>
    /// <param name="BackgroundScenePath">Main act background scene.</param>
    /// <param name="RestSiteBackgroundPath">Rest site background scene.</param>
    /// <param name="MapTopBgPath">Top layer of the act map background image.</param>
    /// <param name="MapMidBgPath">Middle layer of the act map background image.</param>
    /// <param name="MapBotBgPath">Bottom layer of the act map background image.</param>
    /// <param name="ChestSpineResourcePath">Treasure room chest Spine data resource path.</param>
    public sealed record ActAssetProfile(
        string? BackgroundScenePath = null,
        string? RestSiteBackgroundPath = null,
        string? MapTopBgPath = null,
        string? MapMidBgPath = null,
        string? MapBotBgPath = null,
        string? ChestSpineResourcePath = null)
    {
        /// <summary>
        ///     Default empty profile (no custom paths).
        /// </summary>
        public static ActAssetProfile Empty { get; } = new();
    }

    /// <summary>
    ///     Factory methods that build vanilla-style default asset paths from pool/card/relic entry names.
    /// </summary>
    public static class ContentAssetProfiles
    {
        /// <summary>
        ///     Builds default portrait and overlay paths for a card in <paramref name="poolEntry" /> /
        ///     <paramref name="cardEntry" />.
        /// </summary>
        public static CardAssetProfile Card(string poolEntry, string cardEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(cardEntry);

            var normalizedPool = Normalize(poolEntry);
            var normalizedCard = Normalize(cardEntry);
            return new(
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/{normalizedCard}.png"),
                ImageHelper.GetImagePath($"packed/card_portraits/{normalizedPool}/beta/{normalizedCard}.png"),
                OverlayScenePath: SceneHelper.GetScenePath($"cards/overlays/{normalizedCard}"));
        }

        /// <summary>
        ///     Builds default relic icon paths for <paramref name="relicEntry" />.
        /// </summary>
        public static RelicAssetProfile Relic(string relicEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relicEntry);

            var normalized = Normalize(relicEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/relic_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/relic_outline_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"relics/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default power icon paths for <paramref name="powerEntry" />.
        /// </summary>
        public static PowerAssetProfile Power(string powerEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(powerEntry);

            var normalized = Normalize(powerEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"powers/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default orb icon and visuals scene paths for <paramref name="orbEntry" />.
        /// </summary>
        public static OrbAssetProfile Orb(string orbEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orbEntry);

            var normalized = Normalize(orbEntry);
            return new(
                ImageHelper.GetImagePath($"orbs/{normalized}.png"),
                SceneHelper.GetScenePath($"orbs/orb_visuals/{normalized}"));
        }

        /// <summary>
        ///     Builds default potion image paths for <paramref name="potionEntry" />.
        /// </summary>
        public static PotionAssetProfile Potion(string potionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(potionEntry);

            var normalized = Normalize(potionEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/potion_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/potion_outline_atlas.sprites/{normalized}.tres"));
        }

        /// <summary>
        ///     Builds default affliction overlay scene path for <paramref name="afflictionEntry" />.
        /// </summary>
        public static AfflictionAssetProfile Affliction(string afflictionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(afflictionEntry);

            var normalized = Normalize(afflictionEntry);
            return new(
                SceneHelper.GetScenePath($"cards/overlays/afflictions/{normalized}"));
        }

        /// <summary>
        ///     Builds default enchantment icon path for <paramref name="enchantmentEntry" />.
        /// </summary>
        public static EnchantmentAssetProfile Enchantment(string enchantmentEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(enchantmentEntry);

            var normalized = Normalize(enchantmentEntry);
            return new(
                ImageHelper.GetImagePath($"enchantments/{normalized}.png"));
        }

        /// <summary>
        ///     Builds default act background, map layers, rest site, and chest Spine paths for <paramref name="actEntry" />.
        /// </summary>
        public static ActAssetProfile Act(string actEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actEntry);

            var normalized = Normalize(actEntry);
            return new(
                SceneHelper.GetScenePath($"backgrounds/{normalized}/{normalized}_background"),
                SceneHelper.GetScenePath($"rest_site/{normalized}_rest_site"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_top_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_middle_{normalized}.png"),
                ImageHelper.GetImagePath($"packed/map/map_bgs/{normalized}/map_bottom_{normalized}.png"),
                $"res://animations/backgrounds/treasure_room/chest_room_act_{normalized}_skel_data.tres");
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
