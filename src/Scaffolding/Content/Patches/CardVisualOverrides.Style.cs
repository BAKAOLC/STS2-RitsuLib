using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static partial class CardVisualOverrides
    {
        private static void ApplyStyle(CardVisualState state, CardModel model, bool ancient)
        {
            state.SetVisible("%PortraitBorder", !ancient);
            state.SetVisible("%Portrait", !ancient);
            state.SetVisible("%Frame", !ancient);
            state.SetVisible("%AncientPortrait", ancient);
            state.SetVisible("%AncientBorderGlassOverlay", ancient);
            state.SetVisible("%AncientBorder", ancient);
            state.SetVisible("%AncientTextBg", ancient);
            state.SetVisible("%AncientBanner", ancient);
            state.SetVisible("%TitleBanner", !ancient);
            ApplyPortraitMask(state, ancient);

            if (ancient)
            {
                state.SetTexture("%AncientBorder", model.AncientBorder);
                state.SetTexture("%AncientTextBg", ResourceLoader.Load<Texture2D>(AncientTextBgPath(model.Type)));
                state.SetTexture("%AncientPortrait", model.Portrait);
                state.SetTexture("%AncientBanner",
                    ResourceLoader.Load<Texture2D>(
                        ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/ancient_banner.tres")));
                state.SetMaterial("%TitleBanner", null);
                return;
            }

            state.SetTexture("%Portrait", model.Portrait);
            state.SetTexture("%PortraitBorder", ResourceLoader.Load<Texture2D>(StandardPortraitBorderPath(model.Type)));
            state.SetTexture("%Frame", ResourceLoader.Load<Texture2D>(StandardFramePath(model.Type)));
            state.SetMaterial("%Frame", model.FrameMaterial, () => model.FrameMaterial);
            state.SetTexture("%TitleBanner",
                ResourceLoader.Load<Texture2D>(
                    ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/card_banner.tres")));
            var bannerMaterial = PreloadManager.Cache.GetMaterial(StandardBannerMaterialPath(model.Rarity));
            state.SetMaterial("%PortraitBorder", bannerMaterial);
            state.SetMaterial("%TitleBanner", bannerMaterial);
            state.SetMaterial("%TypePlaque", bannerMaterial, () => model.BannerMaterial);
        }

        private static void ApplyPortraitMask(CardVisualState state, bool ancient)
        {
            if (state.Card.Visibility != ModelVisibility.Visible)
            {
                state.SetMaterial("%PortraitCanvasGroup", PreloadManager.Cache.GetMaterial(ancient
                    ? "res://scenes/cards/card_canvas_group_mask_blur_material.tres"
                    : "res://scenes/cards/card_canvas_group_blur_material.tres"));
                var blur = PreloadManager.Cache.GetMaterial("res://scenes/cards/card_portrait_blur_material.tres");
                state.SetMaterial("%Portrait", blur);
                state.SetMaterial("%AncientPortrait", blur);
                return;
            }

            state.SetMaterial("%PortraitCanvasGroup", ancient
                ? PreloadManager.Cache.GetMaterial("res://scenes/cards/card_canvas_group_mask_material.tres")
                : null);
            state.SetMaterial("%Portrait", null);
            state.SetMaterial("%AncientPortrait", null);
        }

        private static string StandardFramePath(CardType type)
        {
            var normalizedType = Normalize(StandardFrameCardType(type));
            return ImageHelper.GetImagePath($"atlases/ui_atlas.sprites/card/card_frame_{normalizedType}_s.tres");
        }

        private static string StandardPortraitBorderPath(CardType type)
        {
            var normalizedType = Normalize(StandardPortraitBorderCardType(type));
            return ImageHelper.GetImagePath(
                $"atlases/ui_atlas.sprites/card/card_portrait_border_{normalizedType}_s.tres");
        }

        private static string AncientTextBgPath(CardType type)
        {
            var normalizedType = Normalize(AncientTextBgCardType(type));
            return ImageHelper.GetImagePath(
                $"atlases/compressed_atlas.sprites/ancient_text_bg_{normalizedType}.png.tres");
        }

        private static string StandardBannerMaterialPath(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Uncommon => "res://materials/cards/banners/card_banner_uncommon_mat.tres",
                CardRarity.Rare => "res://materials/cards/banners/card_banner_rare_mat.tres",
                CardRarity.Curse => "res://materials/cards/banners/card_banner_curse_mat.tres",
                CardRarity.Status => "res://materials/cards/banners/card_banner_status_mat.tres",
                CardRarity.Event => "res://materials/cards/banners/card_banner_event_mat.tres",
                CardRarity.Quest => "res://materials/cards/banners/card_banner_quest_mat.tres",
                _ => "res://materials/cards/banners/card_banner_common_mat.tres",
            };
        }

        private static CardType StandardFrameCardType(CardType type)
        {
            return type switch
            {
                CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest => type,
                _ => CardType.Skill,
            };
        }

        private static CardType StandardPortraitBorderCardType(CardType type)
        {
            return type switch
            {
                CardType.Attack or CardType.Skill or CardType.Power => type,
                _ => CardType.Skill,
            };
        }

        private static CardType AncientTextBgCardType(CardType type)
        {
            return type switch
            {
                CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest => type,
                _ => CardType.Skill,
            };
        }

        private static string Normalize(CardType type)
        {
            return type.ToString().ToLowerInvariant();
        }
    }
}
