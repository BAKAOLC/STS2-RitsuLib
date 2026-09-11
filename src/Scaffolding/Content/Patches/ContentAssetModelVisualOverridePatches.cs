using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
#if !STS2_AT_LEAST_0_108_0
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.Utils;
#endif

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

#if STS2_AT_LEAST_0_106_0 && !STS2_AT_LEAST_0_108_0
    /// <summary>
    ///     <para xml:lang="en">Allows mod epoch artwork to suppress the timeline placeholder label.</para>
    ///     <para xml:lang="zh-CN">允许模组时代美术资源隐藏时间线占位标签。</para>
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
    ///     <para xml:lang="en">
    ///         Applies character-owned and <see cref="IModCardAssetOverrides" /> portrait-path overrides to
    ///         <see cref="CardModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将角色所属和 <see cref="IModCardAssetOverrides" /> 卡图路径覆盖应用到 <see cref="CardModel" />。
    ///     </para>
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
    ///     <para xml:lang="en">Makes card portrait availability checks honor custom portrait paths.</para>
    ///     <para xml:lang="zh-CN">使卡图可用性检查识别自定义卡图路径。</para>
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
            if (__instance is IModCardAssetOverrides overrides)
                return TryHasPortrait(__instance, overrides, ref __result);

            return ModCharacterOwnedVisualOverrideHelper.TryCardPortraitExists(__instance, ref __result);
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
            if (__instance is IModCardAssetOverrides overrides)
                return CardPortraitAvailabilityPatch.TryHasBetaPortrait(__instance, overrides, ref __result);

            return ModCharacterOwnedVisualOverrideHelper.TryCardBetaPortraitExists(__instance, ref __result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies mod path overrides to card frame, portrait-border, energy-icon, and Ancient-card textures.
    ///     </para>
    ///     <para xml:lang="zh-CN">将模组路径覆盖应用到卡牌边框、卡图边框、能量图标和先古卡牌纹理。</para>
    /// </summary>
    internal class CardTextureOverridePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_texture";

        public static string Description =>
            "Allow mod cards to override card frame, portrait border, energy icon, and ancient textures";

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

#if STS2_AT_LEAST_0_105_0
        internal static bool TryCardAncientBorderTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardAncientBorderTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomAncientBorderPath,
                nameof(IModCardAssetOverrides.CustomAncientBorderPath));
        }
#endif

        internal static bool TryCardAncientTextBgTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardAncientTextBgTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomAncientTextBgPath,
                nameof(IModCardAssetOverrides.CustomAncientTextBgPath));
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

#if STS2_AT_LEAST_0_105_0
    internal class CardAncientBorderTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_ancient_border_texture";
        public static string Description => "Allow mod cards to override ancient card border textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "AncientBorder", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            return CardTextureOverridePatch.TryCardAncientBorderTexture(__instance, ref __result);
        }
    }
#endif

    internal class CardAncientTextBgTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_ancient_text_bg_texture";
        public static string Description => "Allow mod cards to override ancient card text background textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "AncientTextBg", MethodType.Getter)];
        }

        public static bool Prefix(CardModel __instance, ref Texture2D __result)
        {
            return CardTextureOverridePatch.TryCardAncientTextBgTexture(__instance, ref __result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies direct, external, character-owned, and path-based frame-material overrides to
    ///         <see cref="CardModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将直接、外部、角色所属和基于路径的边框材质覆盖应用到 <see cref="CardModel" />。
    ///     </para>
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
                    __instance,
                    ref __result,
                    static overrides => overrides.CustomFrameMaterial,
                    nameof(IModCardFrameMaterialOverride.CustomFrameMaterial)))
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
    ///     <para xml:lang="en">
    ///         Allows a card pool to supply its frame <see cref="Material" /> directly or through an external override.
    ///     </para>
    ///     <para xml:lang="zh-CN">允许卡池直接提供边框 <see cref="Material" />，或使用外部覆盖。</para>
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
            if (!ContentAssetOverridePatchHelper.TryUseDirectMaterialOverride<IModCardPoolFrameMaterial>(
                    __instance,
                    ref __result,
                    static overrides => overrides.PoolFrameMaterial,
                    nameof(IModCardPoolFrameMaterial.PoolFrameMaterial)))
                return false;

            if (!ExternalCardMaterialOverrideRegistry.TryGetPoolFrameMaterial(__instance,
                    out var externalFrameMaterial))
                return true;

            __result = externalFrameMaterial;
            return false;
        }
    }


    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds custom regular and beta portrait paths to <see cref="CardModel.AllPortraitPaths" /> for preloading.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将自定义普通和测试版卡图路径添加到 <see cref="CardModel.AllPortraitPaths" />，以供预加载。
    ///     </para>
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
            if (ModCharacterOwnedVisualOverrideHelper.TryGetExistingCardPortraitPaths(
                    __instance,
                    out var ownedPortraitPath,
                    out var ownedBetaPortraitPath))
            {
                if (ownedBetaPortraitPath == null)
                {
                    string betaPath = null!;
                    if (!CardPortraitPathPatch.TryCardBetaPortraitPath(__instance, ref betaPath))
                        ownedBetaPortraitPath = betaPath;
                }

                __result = ownedBetaPortraitPath == null
                    ? [ownedPortraitPath ?? __instance.PortraitPath]
                    : [ownedPortraitPath ?? __instance.PortraitPath, ownedBetaPortraitPath];
                return false;
            }

            return __instance is not IModCardAssetOverrides overrides
                   || ContentAssetOverridePatchHelper.TryUsePortraitPathList(__instance, overrides, ref __result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom built-in overlay scene paths to cards.</para>
    ///     <para xml:lang="zh-CN">将自定义内置覆盖层场景路径应用到卡牌。</para>
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

            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            var path = overrides.CustomOverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    path,
                    nameof(IModCardAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = path;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="CardModel.HasBuiltInOverlay" /> honor available custom overlay scenes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <see cref="CardModel.HasBuiltInOverlay" /> 识别可用的自定义覆盖层场景。
    ///     </para>
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

            var path = overrides.CustomOverlayScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    path,
                    nameof(IModCardAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = true;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="CardModel.CreateOverlay" /> instantiate a configured custom overlay scene.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <see cref="CardModel.CreateOverlay" /> 实例化已配置的自定义覆盖层场景。
    ///     </para>
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
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return !ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                __instance,
                path,
                nameof(IModCardAssetOverrides.CustomOverlayScenePath),
                out __result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies relic icon-path overrides in character-owned, external-registry, then relic-interface order.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按角色所属覆盖、外部注册表、遗物接口的顺序应用遗物图标路径覆盖。
    ///     </para>
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
        ///     <para xml:lang="en">Applies the first available relic icon-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的遗物图标路径覆盖。</para>
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

            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetRelicIconPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.RelicIconPath"))
                return false;

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

            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetRelicIconOutlinePath(instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.RelicIconOutlinePath"))
                return false;

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
    ///     <para xml:lang="en">
    ///         Applies main, outline, and large relic icon overrides in character-owned, external-registry, then
    ///         relic-interface order.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按角色所属覆盖、外部注册表、遗物接口的顺序应用遗物主图标、轮廓图标和大图标覆盖。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Applies external-registry and <see cref="IModPowerAssetOverrides" /> path overrides to
    ///         <see cref="PowerModel.IconPath" /> and <see cref="PowerModel.PackedIconPath" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将外部注册表和 <see cref="IModPowerAssetOverrides" /> 路径覆盖应用到
    ///         <see cref="PowerModel.IconPath" /> 与 <see cref="PowerModel.PackedIconPath" />。
    ///     </para>
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
        ///     <para xml:lang="en">Applies the first available power icon-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的能力图标路径覆盖。</para>
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(PowerModel __instance, ref string __result)
        {
            return TryPowerIconPath(__instance, ref __result);
        }

        private static bool TryPowerIconPath(PowerModel instance, ref string result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetPowerIconPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.PowerIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                instance,
                ref result,
                o => o.CustomIconPath,
                nameof(IModPowerAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies external-registry and mod-path overrides to power icon textures.</para>
    ///     <para xml:lang="zh-CN">将外部注册表和模组路径覆盖应用到能力图标纹理。</para>
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
