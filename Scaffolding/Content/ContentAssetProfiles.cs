using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Scaffolding.Content
{
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
        public static CardAssetProfile Empty { get; } = new();
    }

    public sealed record RelicAssetProfile(
        string? IconPath = null,
        string? IconOutlinePath = null,
        string? BigIconPath = null)
    {
        public static RelicAssetProfile Empty { get; } = new();
    }

    public sealed record PowerAssetProfile(
        string? IconPath = null,
        string? BigIconPath = null)
    {
        public static PowerAssetProfile Empty { get; } = new();
    }

    public sealed record OrbAssetProfile(
        string? IconPath = null,
        string? VisualsScenePath = null)
    {
        public static OrbAssetProfile Empty { get; } = new();
    }

    public sealed record PotionAssetProfile(
        string? ImagePath = null,
        string? OutlinePath = null)
    {
        public static PotionAssetProfile Empty { get; } = new();
    }

    public static class ContentAssetProfiles
    {
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

        public static RelicAssetProfile Relic(string relicEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relicEntry);

            var normalized = Normalize(relicEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/relic_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/relic_outline_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"relics/{normalized}.png"));
        }

        public static PowerAssetProfile Power(string powerEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(powerEntry);

            var normalized = Normalize(powerEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"powers/{normalized}.png"));
        }

        public static OrbAssetProfile Orb(string orbEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orbEntry);

            var normalized = Normalize(orbEntry);
            return new(
                ImageHelper.GetImagePath($"orbs/{normalized}.png"),
                SceneHelper.GetScenePath($"orbs/orb_visuals/{normalized}"));
        }

        public static PotionAssetProfile Potion(string potionEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(potionEntry);

            var normalized = Normalize(potionEntry);
            return new(
                ImageHelper.GetImagePath($"atlases/potion_atlas.sprites/{normalized}.tres"),
                ImageHelper.GetImagePath($"atlases/potion_outline_atlas.sprites/{normalized}.tres"));
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
