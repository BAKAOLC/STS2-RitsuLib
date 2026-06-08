using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal class EpochPortraitPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_epoch_portrait_path";
        public static string Description => "Allow mod epochs to override packed and large portrait paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EpochModel), "PackedPortraitPath", MethodType.Getter),
            ];
        }

        public static bool Prefix(EpochModel __instance, ref string __result)
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEpochAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomPackedPortraitPath,
                nameof(IModEpochAssetOverrides.CustomPackedPortraitPath));
        }
    }

    internal class EpochBigPortraitPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_epoch_big_portrait_path";
        public static string Description => "Allow mod epochs to override large portrait paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
#if STS2_AT_LEAST_0_106_0
                new(typeof(EpochModel), "ResolvedPortraitPath", MethodType.Getter),
#else
                new(typeof(EpochModel), "BigPortraitPath", MethodType.Getter),
#endif
            ];
        }

        public static bool Prefix(EpochModel __instance, ref string __result)
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEpochAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBigPortraitPath,
                nameof(IModEpochAssetOverrides.CustomBigPortraitPath));
        }
    }

#if STS2_AT_LEAST_0_106_0
    /// <summary>
    ///     Allows mod epoch art overrides to control the placeholder label.
    /// </summary>
    internal class EpochArtPlaceholderPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_epoch_art_placeholder";
        public static string Description => "Allow mod epochs to suppress the timeline placeholder label";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EpochModel), "IsArtPlaceholder", MethodType.Getter)];
        }

        public static bool Prefix(EpochModel __instance, ref bool __result)
        {
            if (__instance is IModEpochAssetOverrides overrides &&
                !string.IsNullOrWhiteSpace(overrides.CustomBigPortraitPath) &&
                AssetPathDiagnostics.Exists(
                    overrides.CustomBigPortraitPath,
                    __instance,
                    nameof(IModEpochAssetOverrides.CustomBigPortraitPath)))
            {
                __result = false;
                return false;
            }

            if (!IsCharacterUnlockEpochTemplate(__instance.GetType()))
                return true;

            __result = false;
            return false;
        }

        private static bool IsCharacterUnlockEpochTemplate(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() ==
                    typeof(CharacterUnlockEpochTemplate<>))
                    return true;

            return false;
        }
    }
#endif

    /// <summary>
    ///     Patches <see cref="CardModel" /> portrait path getters for <see cref="IModCardAssetOverrides" />.
    ///     为 <see cref="IModCardAssetOverrides" /> 修补<see cref="CardModel" /> portrait 路径 getter。
    /// </summary>
    internal class CardPortraitPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_path";
        public static string Description => "Allow mod cards to override CardModel portrait paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "PortraitPath", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref string __result)
        {
            return TryCardPortraitPath(__instance, ref __result);
        }

        internal static bool TryCardPortraitPath(CardModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitPath(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath));
        }

        internal static bool TryCardBetaPortraitPath(CardModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBetaPortraitPath(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomBetaPortraitPath,
                nameof(IModCardAssetOverrides.CustomBetaPortraitPath));
        }
    }

    internal class CardBetaPortraitPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_beta_portrait_path";
        public static string Description => "Allow mod cards to override CardModel beta portrait paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "BetaPortraitPath", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref string __result)
        {
            return CardPortraitPathPatch.TryCardBetaPortraitPath(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches portrait availability flags so custom paths from <see cref="IModCardAssetOverrides" /> are honored.
    ///     修补肖像可用性标志，使来自 <see cref="IModCardAssetOverrides" /> 的自定义路径生效。
    /// </summary>
    internal class CardPortraitAvailabilityPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_availability";
        public static string Description => "Allow mod cards to override CardModel portrait availability checks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "HasPortrait", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref bool __result)
        {
            return __instance is not IModCardAssetOverrides overrides ||
                   TryHasPortrait(__instance, overrides, ref __result);
        }

        internal static bool TryHasPortrait(CardModel instance, IModCardAssetOverrides overrides, ref bool result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitExists(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                instance, overrides.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath), ref result);
        }

        internal static bool TryHasBetaPortrait(CardModel instance, IModCardAssetOverrides overrides, ref bool result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBetaPortraitExists(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                instance, overrides.CustomBetaPortraitPath, nameof(IModCardAssetOverrides.CustomBetaPortraitPath),
                ref result);
        }
    }

    internal class CardBetaPortraitAvailabilityPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_beta_portrait_availability";
        public static string Description => "Allow mod cards to override CardModel beta portrait availability checks";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "HasBetaPortrait", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref bool __result)
        {
            return __instance is not IModCardAssetOverrides overrides ||
                   CardPortraitAvailabilityPatch.TryHasBetaPortrait(__instance, overrides, ref __result);
        }
    }

    /// <summary>
    ///     Patches card frame, portrait border, and energy icon texture getters for mod path overrides.
    ///     为 mod 路径覆盖修补卡牌框、肖像边框和能量图标纹理 getter。
    /// </summary>
    internal class CardTextureOverridePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_texture";

        public static string Description =>
            "Allow mod cards to override card frame, portrait border, and energy icon textures";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "Frame", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            return TryCardFrameTexture(__instance, ref __result);
        }

        internal static bool TryCardFrameTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardFrameTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomFramePath, nameof(IModCardAssetOverrides.CustomFramePath));
        }

        internal static bool TryCardPortraitBorderTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitBorderTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomPortraitBorderPath,
                nameof(IModCardAssetOverrides.CustomPortraitBorderPath));
        }

        internal static bool TryCardEnergyIconTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardEnergyIconTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomEnergyIconPath, nameof(IModCardAssetOverrides.CustomEnergyIconPath));
        }
    }

    internal class CardPortraitBorderTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_border_texture";
        public static string Description => "Allow mod cards to override card portrait border textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "PortraitBorder", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            return CardTextureOverridePatch.TryCardPortraitBorderTexture(__instance, ref __result);
        }
    }

    internal class CardEnergyIconTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_energy_icon_texture";
        public static string Description => "Allow mod cards to override card energy icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "EnergyIcon", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            return CardTextureOverridePatch.TryCardEnergyIconTexture(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel" /> frame material resolution for custom <c>.tres</c> paths.
    ///     修补 <see cref="CardModel" /> 边框材质解析，以支持自定义 <c>.tres</c> 路径。
    /// </summary>
    internal class CardFrameMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_frame_material";
        public static string Description => "Allow mod cards to override card frame materials";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "FrameMaterial", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref Material __result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseDirectMaterialOverride<IModCardFrameMaterialOverride>(
                    __instance, ref __result, static o => o.CustomFrameMaterial))
                return false;

            if (ExternalCardMaterialOverrideRegistry.TryGetFrameMaterial(__instance, out var externalFrameMaterial))
            {
                __result = externalFrameMaterial;
                return false;
            }

            if (!ModCharacterOwnedVisualOverrideHelper.TryCardFrameMaterial(__instance, ref __result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomFrameMaterialPath,
                nameof(IModCardAssetOverrides.CustomFrameMaterialPath));
        }
    }

    /// <summary>
    ///     Patches pool-level frame material so <see cref="IModCardPoolFrameMaterial.PoolFrameMaterial" /> can replace path
    ///     lookup.
    ///     修补池级边框材质，使 <see cref="IModCardPoolFrameMaterial.PoolFrameMaterial" /> 可以替换路径
    ///     查找。
    /// </summary>
    internal class CardPoolFrameMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_pool_frame_material";
        public static string Description => "Allow mod card pools to directly supply a Material for card frames";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPoolModel), "FrameMaterial", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardPoolModel __instance, ref Material __result)
        {
            if (__instance is not IModCardPoolFrameMaterial pool)
            {
                if (!ExternalCardMaterialOverrideRegistry.TryGetPoolFrameMaterial(__instance, out var externalMaterial))
                    return true;

                __result = externalMaterial;
                return false;
            }

            var material = pool.PoolFrameMaterial;
            if (material != null)
            {
                __result = material;
                return false;
            }

            if (!ExternalCardMaterialOverrideRegistry.TryGetPoolFrameMaterial(__instance,
                    out var externalFrameMaterial))
                return true;

            __result = externalFrameMaterial;
            return false;
        }
    }

    /// <summary>
    ///     Applies custom portrait <see cref="Material" /> overrides after <see cref="NCard" /> reloads vanilla visuals.
    ///     在 <see cref="NCard" /> 重载原版视觉后应用自定义卡图 <see cref="Material" /> 覆盖。
    /// </summary>
    internal class CardPortraitMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_portrait_material";
        public static string Description => "Allow mod cards to override the NCard portrait material";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCard), "Reload")];
        }

        public static void Postfix(NCard __instance)
        {
            var model = __instance.Model;
            if (model == null || __instance.Visibility != ModelVisibility.Visible)
                return;

            if (!TryGetPortraitMaterial(model, out var material))
                return;

            var portrait = GetPortraitNode(__instance, model);
            if (portrait == null)
                return;

            portrait.Material = material;
        }

        private static TextureRect? GetPortraitNode(NCard card, CardModel model)
        {
            var path = model.Rarity == CardRarity.Ancient ? "%AncientPortrait" : "%Portrait";
            return card.GetNodeOrNull<TextureRect>(path);
        }

        private static bool TryGetPortraitMaterial(CardModel card, out Material material)
        {
            material = null!;
            if (!ContentAssetOverridePatchHelper.TryUseDirectMaterialOverride<IModCardPortraitMaterialOverride>(
                    card, ref material, static o => o.CustomPortraitMaterial))
                return true;

            if (ExternalCardMaterialOverrideRegistry.TryGetPortraitMaterial(card, out material))
                return true;

            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitMaterial(card, ref material))
                return true;

            return !ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                card,
                ref material,
                static o => o.CustomPortraitMaterialPath,
                nameof(IModCardAssetOverrides.CustomPortraitMaterialPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel.AllPortraitPaths" /> so custom portrait/beta paths participate in preload lists.
    ///     修补<see cref="CardModel.AllPortraitPaths" />，使自定义 portrait/beta 路径 participate in 预加载 列表。
    /// </summary>
    internal class CardAllPortraitPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_all_portrait_paths";
        public static string Description => "Allow mod cards to advertise custom portrait assets for preloading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "AllPortraitPaths", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref IEnumerable<string> __result)
        {
            var ownedCharacterPaths = ModCharacterOwnedVisualOverrideHelper.GetExistingCardPortraitPaths(__instance);
            if (ownedCharacterPaths.Length <= 0)
                return __instance is not IModCardAssetOverrides overrides
                       || ContentAssetOverridePatchHelper.TryUsePortraitPathList(__instance, overrides, ref __result);
            __result = ownedCharacterPaths;
            return false;
        }
    }

    /// <summary>
    ///     Patches built-in overlay scene path for cards implementing <see cref="IModCardAssetOverrides" />.
    ///     为实现 <see cref="IModCardAssetOverrides" /> 的卡牌修补内置覆盖层场景路径。
    /// </summary>
    internal class CardOverlayPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_overlay_path";
        public static string Description => "Allow mod cards to override overlay scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "OverlayPath", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref string __result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardOverlayPath(__instance, ref __result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomOverlayScenePath,
                nameof(IModCardAssetOverrides.CustomOverlayScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel.HasBuiltInOverlay" /> using existence checks on custom overlay scene paths.
    ///     使用自定义覆盖层场景路径的存在性检查来修补 <see cref="CardModel.HasBuiltInOverlay" />。
    /// </summary>
    internal class CardOverlayAvailabilityPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_overlay_availability";
        public static string Description => "Allow mod cards to advertise overlay availability from custom scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "HasBuiltInOverlay", MethodType.Getter),
            ];
        }

        public static bool Prefix(CardModel __instance, ref bool __result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardOverlayExists(__instance, ref __result))
                return false;

            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                __instance,
                overrides.CustomOverlayScenePath,
                nameof(IModCardAssetOverrides.CustomOverlayScenePath),
                ref __result);
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel.CreateOverlay" /> to instantiate mod overlay scenes when configured.
    ///     修补 <see cref="CardModel.CreateOverlay" />，在配置后实例化 mod 覆盖层场景。
    /// </summary>
    internal class CardOverlayCreatePatch : IPatchMethod
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

        public static bool Prefix(CardModel __instance, ref Control __result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardCreateOverlay(__instance, ref __result))
                return false;

            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            var path = overrides.CustomOverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, __instance, nameof(IModCardAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="RelicModel.IconPath" /> and packed atlas icon/outline path getters (used by vanilla
    ///     <c>Icon</c> / <c>IconOutline</c> loaders) for mod-character per–relic-id paths (owner match) first, then
    ///     <see cref="IModRelicAssetOverrides" />.
    ///     修补 <see cref="RelicModel.IconPath" /> 和 packed atlas 图标/轮廓路径 getter（原版
    ///     <c>Icon</c> / <c>IconOutline</c> 加载器使用）：优先使用 mod 角色按遗物 id 的路径（所有者匹配），然后使用
    ///     <see cref="IModRelicAssetOverrides" />。
    /// </summary>
    internal class RelicIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_icon_path";

        public static string Description =>
            "Owned-relic character overrides first, then mod relic custom icon and packed atlas paths";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "IconPath", MethodType.Getter),
                new(typeof(RelicModel), "PackedIconPath", null, true, MethodType.Getter),
            ];
        }

        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.TryGetVanillaRelicVisualOverrideForOwnedRelic" /> when
        ///     applicable, then <see cref="IModRelicAssetOverrides" /> custom paths.
        ///     当条件满足时提供 <see cref="IModCharacterAssetOverrides.TryGetVanillaRelicVisualOverrideForOwnedRelic" />
        ///     applicable, then <see cref="IModRelicAssetOverrides" /> 自定义 路径。
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(RelicModel __instance, ref string __result)
        {
            return TryRelicMainIconPath(__instance, ref __result);
        }

        internal static bool TryRelicMainIconPath(RelicModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryRelicIconPath(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetRelicIconPath(instance, out var externalPath))
            {
                result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModRelicAssetOverrides>(
                instance,
                ref result,
                o => o.CustomIconPath,
                nameof(IModRelicAssetOverrides.CustomIconPath));
        }

        internal static bool TryRelicPackedIconOutlinePath(RelicModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryRelicIconOutlinePath(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetRelicIconOutlinePath(instance, out var externalPath))
            {
                result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModRelicAssetOverrides>(
                instance,
                ref result,
                o => o.CustomIconOutlinePath,
                nameof(IModRelicAssetOverrides.CustomIconOutlinePath));
        }
    }

    internal class RelicPackedIconOutlinePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_packed_icon_outline_path";
        public static string Description => "Allow mod relics to override packed atlas icon outline paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), "PackedIconOutlinePath", null, true, MethodType.Getter)];
        }

        [HarmonyPriority(410)]
        public static bool Prefix(RelicModel __instance, ref string __result)
        {
            return RelicIconPathPatch.TryRelicPackedIconOutlinePath(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches relic icon texture getters (main, outline, big): mod-character owned-relic overrides first, then
    ///     <see cref="IModRelicAssetOverrides" />.
    ///     修补遗物图标纹理 getter（主图、轮廓、大图）：优先使用 mod 角色拥有的遗物覆盖，然后使用
    ///     <see cref="IModRelicAssetOverrides" />。
    /// </summary>
    internal class RelicTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_texture";

        public static string Description =>
            "Owned-relic character overrides first, then mod relic icon textures";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "Icon", MethodType.Getter),
            ];
        }

        public static bool Prefix(RelicModel __instance, ref Texture2D __result)
        {
            return TryRelicIconTexture(__instance, ref __result);
        }

        internal static bool TryRelicIconTexture(RelicModel instance, ref Texture2D result)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!ModCharacterOwnedVisualOverrideHelper.TryRelicIconTexture(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetRelicIconTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(instance,
                ref result, o => o.CustomIconPath, nameof(IModRelicAssetOverrides.CustomIconPath));
        }

        internal static bool TryRelicIconOutlineTexture(RelicModel instance, ref Texture2D result)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!ModCharacterOwnedVisualOverrideHelper.TryRelicIconOutlineTexture(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetRelicIconOutlineTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(instance,
                ref result, o => o.CustomIconOutlinePath,
                nameof(IModRelicAssetOverrides.CustomIconOutlinePath));
        }

        internal static bool TryRelicBigIconTexture(RelicModel instance, ref Texture2D result)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!ModCharacterOwnedVisualOverrideHelper.TryRelicBigIconTexture(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetRelicBigIconTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(instance,
                ref result, o => o.CustomBigIconPath, nameof(IModRelicAssetOverrides.CustomBigIconPath));
        }
    }

    internal class RelicIconOutlineTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_icon_outline_texture";
        public static string Description => "Allow mod relics to override icon outline textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), "IconOutline", MethodType.Getter)];
        }

        public static bool Prefix(RelicModel __instance, ref Texture2D __result)
        {
            return RelicTexturePatch.TryRelicIconOutlineTexture(__instance, ref __result);
        }
    }

    internal class RelicBigIconTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_relic_big_icon_texture";
        public static string Description => "Allow mod relics to override big icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RelicModel), "BigIcon", MethodType.Getter)];
        }

        public static bool Prefix(RelicModel __instance, ref Texture2D __result)
        {
            return RelicTexturePatch.TryRelicBigIconTexture(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches <see cref="PowerModel.IconPath" /> and <see cref="PowerModel.PackedIconPath" /> (used by vanilla
    ///     <c>Icon</c> loader) for <see cref="IModPowerAssetOverrides" />.
    ///     为 <see cref="IModPowerAssetOverrides" /> 修补 <see cref="PowerModel.IconPath" /> 和
    ///     <see cref="PowerModel.PackedIconPath" />（原版
    ///     <c>Icon</c> 加载器使用）。
    /// </summary>
    internal class PowerIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_icon_path";
        public static string Description => "Allow mod powers to override icon and packed atlas icon paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "IconPath", MethodType.Getter),
                new(typeof(PowerModel), "PackedIconPath", null, true, MethodType.Getter),
            ];
        }

        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomIconPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModPowerAssetOverrides.CustomIconPath" />。
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(PowerModel __instance, ref string __result)
        {
            return TryPowerIconPath(__instance, ref __result);
        }

        private static bool TryPowerIconPath(PowerModel instance, ref string result)
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPowerIconPath(instance, out var externalPath))
            {
                result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                instance,
                ref result,
                o => o.CustomIconPath,
                nameof(IModPowerAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches power standard and big icon textures for mod path overrides.
    ///     为 mod 路径覆盖修补能力标准图标和大图标纹理。
    /// </summary>
    internal class PowerTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_texture";
        public static string Description => "Allow mod powers to override icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "Icon", MethodType.Getter),
            ];
        }

        public static bool Prefix(PowerModel __instance, ref Texture2D __result)
        {
            return TryPowerIconTexture(__instance, ref __result);
        }

        internal static bool TryPowerIconTexture(PowerModel instance, ref Texture2D result)
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPowerIconTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(instance,
                ref result, o => o.CustomIconPath, nameof(IModPowerAssetOverrides.CustomIconPath));
        }

        internal static bool TryPowerBigIconTexture(PowerModel instance, ref Texture2D result)
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPowerBigIconTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(
                instance, ref result, o => o.CustomBigIconPath,
                nameof(IModPowerAssetOverrides.CustomBigIconPath));
        }
    }

    internal class PowerBigIconTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_big_icon_texture";
        public static string Description => "Allow mod powers to override big icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), "BigIcon", MethodType.Getter)];
        }

        public static bool Prefix(PowerModel __instance, ref Texture2D __result)
        {
            return PowerTexturePatch.TryPowerBigIconTexture(__instance, ref __result);
        }
    }
}
