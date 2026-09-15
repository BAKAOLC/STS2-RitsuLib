using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static partial class CardVisualOverrides
    {
        internal static void Apply(CardVisualState state, CardModel model)
        {
            var ancient = model.Rarity == CardRarity.Ancient;
            if (TryResolveStyle(model, out var style))
            {
                ancient = style == CardVisualStyle.Ancient;
                ApplyStyle(state, model, ancient);
            }

            string portraitPath = null!;
            if (!CardPortraitPathPatch.TryCardPortraitPath(model, ref portraitPath) &&
                GodotResourcePath.TryLoad<Texture2D>(portraitPath, out var portrait))
            {
                state.SetTexture("%Portrait", portrait);
                state.SetTexture("%AncientPortrait", portrait);
            }

            ApplyTexture(state, model, "%Frame", static overrides => overrides.CustomFramePath,
                nameof(IModCardAssetOverrides.CustomFramePath),
                ModCharacterOwnedVisualOverrideHelper.TryCardFrameTexture);
            ApplyTexture(state, model, "%PortraitBorder", static overrides => overrides.CustomPortraitBorderPath,
                nameof(IModCardAssetOverrides.CustomPortraitBorderPath),
                ModCharacterOwnedVisualOverrideHelper.TryCardPortraitBorderTexture);
            ApplyTexture(state, model, "%EnergyIcon", static overrides => overrides.CustomEnergyIconPath,
                nameof(IModCardAssetOverrides.CustomEnergyIconPath),
                ModCharacterOwnedVisualOverrideHelper.TryCardEnergyIconTexture, () => model.EnergyIcon);
            ApplyTexture(state, model, "%AncientBorder", static overrides => overrides.CustomAncientBorderPath,
                nameof(IModCardAssetOverrides.CustomAncientBorderPath),
                ModCharacterOwnedVisualOverrideHelper.TryCardAncientBorderTexture);
            ApplyTexture(state, model, "%AncientTextBg", static overrides => overrides.CustomAncientTextBgPath,
                nameof(IModCardAssetOverrides.CustomAncientTextBgPath),
                ModCharacterOwnedVisualOverrideHelper.TryCardAncientTextBgTexture);
            ApplyTexture(state, model, "%AncientBanner", static overrides => overrides.CustomAncientBannerPath,
                nameof(IModCardAssetOverrides.CustomAncientBannerPath),
                ModCharacterOwnedVisualOverrideHelper.TryCardAncientBannerTexture);
            ApplyTexture(state, model, "%TitleBanner", static overrides => overrides.CustomBannerTexturePath,
                nameof(IModCardAssetOverrides.CustomBannerTexturePath),
                ModCharacterOwnedVisualOverrideHelper.TryCardBannerTexture);

            if (state.Card.Visibility != ModelVisibility.Visible)
                return;

            Material material = null!;
            if (!CardFrameMaterialPatch.Prefix(model, ref material))
                state.SetMaterial("%Frame", material, () => model.FrameMaterial);
            if (!CardBannerMaterialPatch.Prefix(model, ref material))
            {
                state.SetMaterial("%TitleBanner", material);
                state.SetMaterial("%PortraitBorder", material);
                state.SetMaterial("%TypePlaque", material, () => model.BannerMaterial);
            }

            if (TryMaterial<IModCardPortraitMaterialOverride>(
                    model, static overrides => overrides.CustomPortraitMaterial,
                    nameof(IModCardPortraitMaterialOverride.CustomPortraitMaterial),
                    static overrides => overrides.CustomPortraitMaterialPath,
                    nameof(IModCardAssetOverrides.CustomPortraitMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardPortraitMaterial,
                    ExternalCardMaterialOverrideRegistry.TryGetPortraitMaterial, out material))
                state.SetMaterial(ancient ? "%AncientPortrait" : "%Portrait", material);

            if (TryMaterial<IModCardPortraitBorderMaterialOverride>(
                    model, static overrides => overrides.CustomPortraitBorderMaterial,
                    nameof(IModCardPortraitBorderMaterialOverride.CustomPortraitBorderMaterial),
                    static overrides => overrides.CustomPortraitBorderMaterialPath,
                    nameof(IModCardAssetOverrides.CustomPortraitBorderMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardPortraitBorderMaterial, null, out material))
                state.SetMaterial("%PortraitBorder", material);

            if (TryMaterial<IModCardEnergyIconMaterialOverride>(
                    model, static overrides => overrides.CustomEnergyIconMaterial,
                    nameof(IModCardEnergyIconMaterialOverride.CustomEnergyIconMaterial),
                    static overrides => overrides.CustomEnergyIconMaterialPath,
                    nameof(IModCardAssetOverrides.CustomEnergyIconMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardEnergyIconMaterial, null, out material))
                state.SetMaterial("%EnergyIcon", material);

            if (TryMaterial<IModCardAncientBorderMaterialOverride>(
                    model, static overrides => overrides.CustomAncientBorderMaterial,
                    nameof(IModCardAncientBorderMaterialOverride.CustomAncientBorderMaterial),
                    static overrides => overrides.CustomAncientBorderMaterialPath,
                    nameof(IModCardAssetOverrides.CustomAncientBorderMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardAncientBorderMaterial, null, out material))
                state.SetMaterial("%AncientBorder", material);

            if (TryMaterial<IModCardAncientTextBgMaterialOverride>(
                    model, static overrides => overrides.CustomAncientTextBgMaterial,
                    nameof(IModCardAncientTextBgMaterialOverride.CustomAncientTextBgMaterial),
                    static overrides => overrides.CustomAncientTextBgMaterialPath,
                    nameof(IModCardAssetOverrides.CustomAncientTextBgMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardAncientTextBgMaterial, null, out material))
                state.SetMaterial("%AncientTextBg", material);

            if (TryMaterial<IModCardAncientBannerMaterialOverride>(
                    model, static overrides => overrides.CustomAncientBannerMaterial,
                    nameof(IModCardAncientBannerMaterialOverride.CustomAncientBannerMaterial),
                    static overrides => overrides.CustomAncientBannerMaterialPath,
                    nameof(IModCardAssetOverrides.CustomAncientBannerMaterialPath),
                    ModCharacterOwnedVisualOverrideHelper.TryCardAncientBannerMaterial, null, out material))
                state.SetMaterial("%AncientBanner", material);
        }

        private static bool TryResolveStyle(CardModel model, out CardVisualStyle style)
        {
            if (ModCharacterOwnedVisualOverrideHelper.TryCardVisualStyle(model, out style))
                return true;
            if (model is IModCardAssetOverrides { CustomVisualStyle: not CardVisualStyle.Default } overrides)
            {
                style = overrides.CustomVisualStyle;
                return true;
            }

            style = CardVisualStyle.Default;
            return false;
        }

        private static void ApplyTexture(CardVisualState state, CardModel model, string nodePath,
            Func<IModCardAssetOverrides, string?> selector, string memberName, OwnedTexture owned,
            Func<Texture2D?>? restore = null)
        {
            Texture2D texture = null!;
            if (!owned(model, ref texture) ||
                !ContentAssetOverridePatchHelper.TryUseTextureOverride(model, ref texture, selector, memberName))
                state.SetTexture(nodePath, texture, restore);
        }

        private static bool TryMaterial<TOverrides>(CardModel model,
            Func<TOverrides, Material?> direct, string directName,
            Func<IModCardAssetOverrides, string?> path, string pathName,
            OwnedMaterial owned, ExternalMaterial? external, out Material material) where TOverrides : class
        {
            material = null!;
            return !ContentAssetOverridePatchHelper.TryUseDirectMaterialOverride(model, ref material, direct,
                       directName) ||
                   external != null && external(model, out material) ||
                   !owned(model, ref material) ||
                   !ContentAssetOverridePatchHelper.TryUseMaterialOverride(model, ref material, path, pathName);
        }

        private delegate bool OwnedTexture(CardModel model, ref Texture2D texture);

        private delegate bool OwnedMaterial(CardModel model, ref Material material);

        private delegate bool ExternalMaterial(CardModel model, out Material material);
    }
}
