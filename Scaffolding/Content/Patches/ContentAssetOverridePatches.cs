using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static class ContentAssetOverridePatchHelper
    {
        // ReSharper disable once InconsistentNaming
        internal static bool TryUseStringOverride<TOverrides>(
            object instance,
            ref string __result,
            Func<TOverrides, string?> selector,
            string memberName,
            bool requireExistingResource = true)
            where TOverrides : class
        {
            if (instance is not TOverrides overrides)
                return true;

            var value = selector(overrides);
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (requireExistingResource && !AssetPathDiagnostics.Exists(value, instance, memberName))
                return true;

            __result = value;
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseTextureOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseCompressedTextureOverride<TOverrides>(
            object instance,
            ref CompressedTexture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = ResourceLoader.Load<CompressedTexture2D>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseMaterialOverride<TOverrides>(
            object instance,
            ref Material __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = ResourceLoader.Load<Material>(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUsePortraitPathList(object instance, IModCardAssetOverrides overrides,
            ref IEnumerable<string> __result)
        {
            var paths = AssetPathDiagnostics.CollectExistingPaths(
                instance,
                (overrides.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath)),
                (overrides.CustomBetaPortraitPath, nameof(IModCardAssetOverrides.CustomBetaPortraitPath)));

            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseExistenceOverride(object instance, string? path, string memberName,
            ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            __result = AssetPathDiagnostics.Exists(path, instance, memberName);
            return false;
        }

        private static bool TryGetPath<TOverrides>(
            object instance,
            Func<TOverrides, string?> selector,
            string memberName,
            out string path)
            where TOverrides : class
        {
            path = string.Empty;

            if (instance is not TOverrides overrides)
                return false;

            var candidate = selector(overrides);
            if (string.IsNullOrWhiteSpace(candidate) || !AssetPathDiagnostics.Exists(candidate, instance, memberName))
                return false;

            path = candidate;
            return true;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUsePackedSceneCacheOverride<TOverrides>(
            object instance,
            ref PackedScene __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = PreloadManager.Cache.GetScene(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseTexture2DFromCacheOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = PreloadManager.Cache.GetTexture2D(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseCompressedTextureAsTexture2DOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetPath(instance, selector, memberName, out var path))
                return true;

            __result = ResourceLoader.Load<CompressedTexture2D>(path);
            return false;
        }
    }

    /// <summary>
    ///     Optional card art paths consumed by content asset Harmony patches on <see cref="CardModel" />.
    /// </summary>
    public interface IModCardAssetOverrides
    {
        /// <summary>
        ///     Path bundle; individual properties usually mirror these fields unless overridden.
        /// </summary>
        CardAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override for main portrait image path.
        /// </summary>
        string? CustomPortraitPath { get; }

        /// <summary>
        ///     Override for beta/alternate portrait path.
        /// </summary>
        string? CustomBetaPortraitPath { get; }

        /// <summary>
        ///     Override for card frame texture path.
        /// </summary>
        string? CustomFramePath { get; }

        /// <summary>
        ///     Override for portrait border texture path.
        /// </summary>
        string? CustomPortraitBorderPath { get; }

        /// <summary>
        ///     Override for small energy icon texture path.
        /// </summary>
        string? CustomEnergyIconPath { get; }

        /// <summary>
        ///     Override for frame <see cref="Material" /> resource path.
        /// </summary>
        string? CustomFrameMaterialPath { get; }

        /// <summary>
        ///     Override for built-in overlay packed scene path.
        /// </summary>
        string? CustomOverlayScenePath { get; }

        /// <summary>
        ///     Override for banner texture path.
        /// </summary>
        string? CustomBannerTexturePath { get; }

        /// <summary>
        ///     Override for banner material path.
        /// </summary>
        string? CustomBannerMaterialPath { get; }
    }

    /// <summary>
    ///     Implement this interface on a <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> to directly supply
    ///     a <see cref="Material" /> for card frames in the pool.
    ///     When <see cref="PoolFrameMaterial" /> is non-null, <c>CardFrameMaterialPath</c> is ignored entirely.
    /// </summary>
    public interface IModCardPoolFrameMaterial
    {
        /// <summary>
        ///     The material to use for card frames in this pool.
        ///     Return <c>null</c> to fall back to the path-based default.
        /// </summary>
        Material? PoolFrameMaterial { get; }
    }

    /// <summary>
    ///     Optional relic icon paths for Harmony patches on <see cref="RelicModel" />.
    /// </summary>
    public interface IModRelicAssetOverrides
    {
        /// <summary>
        ///     Path bundle for relic presentation assets.
        /// </summary>
        RelicAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Primary relic icon path override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Outline icon path override.
        /// </summary>
        string? CustomIconOutlinePath { get; }

        /// <summary>
        ///     Large relic art path override.
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional power icon paths for Harmony patches on <see cref="PowerModel" />.
    /// </summary>
    public interface IModPowerAssetOverrides
    {
        /// <summary>
        ///     Path bundle for power icons.
        /// </summary>
        PowerAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Standard icon path override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Large icon path override.
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional orb icon and visuals scene paths for Harmony patches on <see cref="OrbModel" />.
    /// </summary>
    public interface IModOrbAssetOverrides
    {
        /// <summary>
        ///     Path bundle for orb HUD and combat visuals.
        /// </summary>
        OrbAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Orb icon texture path override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Orb combat visuals scene path override.
        /// </summary>
        string? CustomVisualsScenePath { get; }
    }

    /// <summary>
    ///     Default act asset override surface; concrete mods typically use <see cref="ModActTemplate" /> instead of
    ///     implementing this directly.
    /// </summary>
    public interface IModActAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        /// </summary>
        ActAssetProfile AssetProfile => ActAssetProfile.Empty;

        /// <summary>
        ///     Main act background scene path override.
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Rest site background scene path override.
        /// </summary>
        string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;

        /// <summary>
        ///     Map top-layer background image path override.
        /// </summary>
        string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;

        /// <summary>
        ///     Map middle-layer background image path override.
        /// </summary>
        string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;

        /// <summary>
        ///     Map bottom-layer background image path override.
        /// </summary>
        string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;

        /// <summary>
        ///     Treasure chest Spine resource path override.
        /// </summary>
        string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;

        /// <summary>
        ///     Optional <c>res://</c> directory for combat background parallax layers (same <c>_bg_</c> / <c>_fg_</c> naming as
        ///     vanilla). When set, <see cref="ActModel.GenerateBackgroundAssets" /> scans this folder instead of
        ///     <c>scenes/backgrounds/&lt;act&gt;/layers</c>.
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;
    }

    /// <summary>
    ///     Optional event layout, portrait, background, and VFX scene paths; use <see cref="ModEventTemplate" /> or implement
    ///     on a mod <see cref="EventModel" />.
    /// </summary>
    public interface IModEventAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        /// </summary>
        EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene for <c>EventModel.CreateScene</c> (full layout root).
        /// </summary>
        string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <summary>
        ///     Override texture path for <c>EventModel.CreateInitialPortrait</c>.
        /// </summary>
        string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateBackgroundScene</c>.
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateVfx</c> / <c>HasVfx</c>.
        /// </summary>
        string? CustomVfxScenePath => AssetProfile.VfxScenePath;
    }

    /// <summary>
    ///     Extends <see cref="IModEventAssetOverrides" /> with ancient map and run-history icon paths; use
    ///     <see cref="ModAncientEventTemplate" /> or implement on a mod <see cref="AncientEventModel" />.
    /// </summary>
    public interface IModAncientEventAssetOverrides : IModEventAssetOverrides
    {
        /// <summary>
        ///     Ancient-only presentation paths (map node + run history).
        /// </summary>
        AncientEventPresentationAssetProfile AncientPresentationAssetProfile =>
            AncientEventPresentationAssetProfile.Empty;

        /// <summary>
        ///     Override for <c>AncientEventModel.MapIcon</c>.
        /// </summary>
        string? CustomMapIconPath => AncientPresentationAssetProfile.MapIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.MapIconOutline</c>.
        /// </summary>
        string? CustomMapIconOutlinePath => AncientPresentationAssetProfile.MapIconOutlinePath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIcon</c>.
        /// </summary>
        string? CustomRunHistoryIconPath => AncientPresentationAssetProfile.RunHistoryIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIconOutline</c>.
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AncientPresentationAssetProfile.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     Optional epoch timeline portrait paths; use <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" /> or
    ///     implement on a mod <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel" />.
    /// </summary>
    public interface IModEpochAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        /// </summary>
        EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <summary>
        ///     Override for <c>EpochModel.PackedPortraitPath</c> (atlas sprite entry).
        /// </summary>
        string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <summary>
        ///     Override for <c>EpochModel.BigPortraitPath</c> (large portrait texture).
        /// </summary>
        string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }

    /// <summary>
    ///     Patches <see cref="EpochModel" /> portrait path getters for <see cref="IModEpochAssetOverrides" />.
    /// </summary>
    public class EpochPortraitPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_epoch_portrait_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod epochs to override PackedPortraitPath and BigPortraitPath";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EpochModel), "get_PackedPortraitPath"),
                new(typeof(EpochModel), "get_BigPortraitPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches string overrides for packed atlas vs large portrait paths.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, EpochModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_PackedPortraitPath" => ContentAssetOverridePatchHelper
                    .TryUseStringOverride<IModEpochAssetOverrides>(
                        __instance,
                        ref __result,
                        o => o.CustomPackedPortraitPath,
                        nameof(IModEpochAssetOverrides.CustomPackedPortraitPath)),
                "get_BigPortraitPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModEpochAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomBigPortraitPath,
                    nameof(IModEpochAssetOverrides.CustomBigPortraitPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel" /> portrait path getters for <see cref="IModCardAssetOverrides" />.
    /// </summary>
    public class CardPortraitPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_portrait_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override CardModel portrait paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_PortraitPath"),
                new(typeof(CardModel), "get_BetaPortraitPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to portrait or beta portrait override based on the patched getter.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_PortraitPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomPortraitPath,
                    nameof(IModCardAssetOverrides.CustomPortraitPath)),
                "get_BetaPortraitPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomBetaPortraitPath,
                    nameof(IModCardAssetOverrides.CustomBetaPortraitPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches portrait availability flags so custom paths from <see cref="IModCardAssetOverrides" /> are honored.
    /// </summary>
    public class CardPortraitAvailabilityPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_portrait_availability";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override CardModel portrait availability checks";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_HasPortrait"),
                new(typeof(CardModel), "get_HasBetaPortrait"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Sets boolean availability from whether the corresponding custom portrait path exists on disk.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            return __originalMethod.Name switch
            {
                "get_HasPortrait" => ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                    __instance, overrides.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath),
                    ref __result),
                "get_HasBetaPortrait" => ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                    __instance, overrides.CustomBetaPortraitPath, nameof(IModCardAssetOverrides.CustomBetaPortraitPath),
                    ref __result),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches card frame, portrait border, and energy icon texture getters for mod path overrides.
    /// </summary>
    public class CardTextureOverridePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod cards to override card frame, portrait border, and energy icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
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
        /// <summary>
        ///     Loads textures from the matching <see cref="IModCardAssetOverrides" /> path when present.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Frame" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(__instance,
                    ref __result, o => o.CustomFramePath, nameof(IModCardAssetOverrides.CustomFramePath)),
                "get_PortraitBorder" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomPortraitBorderPath,
                    nameof(IModCardAssetOverrides.CustomPortraitBorderPath)),
                "get_EnergyIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                    __instance, ref __result, o => o.CustomEnergyIconPath,
                    nameof(IModCardAssetOverrides.CustomEnergyIconPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel" /> frame material resolution for custom <c>.tres</c> paths.
    /// </summary>
    public class CardFrameMaterialPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_frame_material";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override card frame materials";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_FrameMaterial"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads <see cref="Material" /> from <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" /> when valid.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
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
    /// </summary>
    public class CardPoolFrameMaterialPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_pool_frame_material";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod card pools to directly supply a Material for card frames";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPoolModel), "get_FrameMaterial"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns the pool’s inline material when the pool implements <see cref="IModCardPoolFrameMaterial" />.
        /// </summary>
        public static bool Prefix(CardPoolModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardPoolFrameMaterial pool)
                return true;

            var material = pool.PoolFrameMaterial;
            if (material == null)
                return true;

            __result = material;
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel.AllPortraitPaths" /> so custom portrait/beta paths participate in preload lists.
    /// </summary>
    public class CardAllPortraitPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_all_portrait_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to advertise custom portrait assets for preloading";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_AllPortraitPaths"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Replaces the enumerable with verified custom portrait paths when the card implements overrides.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            return __instance is not IModCardAssetOverrides overrides ||
                   ContentAssetOverridePatchHelper.TryUsePortraitPathList(__instance, overrides, ref __result);
        }
    }

    /// <summary>
    ///     Patches built-in overlay scene path for cards implementing <see cref="IModCardAssetOverrides" />.
    /// </summary>
    public class CardOverlayPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_overlay_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override overlay scene paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_OverlayPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomOverlayScenePath,
                nameof(IModCardAssetOverrides.CustomOverlayScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel.HasBuiltInOverlay" /> using existence checks on custom overlay scene paths.
    /// </summary>
    public class CardOverlayAvailabilityPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_overlay_availability";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to advertise overlay availability from custom scene paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), "get_HasBuiltInOverlay"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Sets <c>true</c> when <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> resolves to an existing
        ///     resource.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
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
    /// </summary>
    public class CardOverlayCreatePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_create_overlay";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to instantiate overlays from custom scene paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.CreateOverlay)),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Instantiates <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> when the packed scene exists.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="RelicModel.IconPath" /> for <see cref="IModRelicAssetOverrides" />.
    /// </summary>
    public class RelicIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_relic_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod relics to override icon path assets";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "get_IconPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModRelicAssetOverrides.CustomIconPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(RelicModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModRelicAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModRelicAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches relic icon texture getters (main, outline, big) for mod path overrides.
    /// </summary>
    public class RelicTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_relic_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod relics to override icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
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
        /// <summary>
        ///     Dispatches texture loading to the matching <see cref="IModRelicAssetOverrides" /> path property.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, RelicModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(__instance,
                    ref __result, o => o.CustomIconPath, nameof(IModRelicAssetOverrides.CustomIconPath)),
                "get_IconOutline" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(
                    __instance, ref __result, o => o.CustomIconOutlinePath,
                    nameof(IModRelicAssetOverrides.CustomIconOutlinePath)),
                "get_BigIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRelicAssetOverrides>(
                    __instance, ref __result, o => o.CustomBigIconPath,
                    nameof(IModRelicAssetOverrides.CustomBigIconPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches <see cref="PowerModel.IconPath" /> for <see cref="IModPowerAssetOverrides" />.
    /// </summary>
    public class PowerIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_power_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod powers to override icon path assets";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "get_IconPath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomIconPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(PowerModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModPowerAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches power standard and big icon textures for mod path overrides.
    /// </summary>
    public class PowerTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_power_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod powers to override icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "get_Icon"),
                new(typeof(PowerModel), "get_BigIcon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to <see cref="IModPowerAssetOverrides.CustomIconPath" /> or
        ///     <see cref="IModPowerAssetOverrides.CustomBigIconPath" />.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, PowerModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(__instance,
                    ref __result, o => o.CustomIconPath, nameof(IModPowerAssetOverrides.CustomIconPath)),
                "get_BigIcon" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPowerAssetOverrides>(
                    __instance, ref __result, o => o.CustomBigIconPath,
                    nameof(IModPowerAssetOverrides.CustomBigIconPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches orb HUD icon (<see cref="CompressedTexture2D" />) for <see cref="IModOrbAssetOverrides" />.
    /// </summary>
    public class OrbIconPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_orb_icon";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod orbs to override icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_Icon"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads compressed icon texture from <see cref="IModOrbAssetOverrides.CustomIconPath" /> when valid.
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref CompressedTexture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseCompressedTextureOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModOrbAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches orb visuals scene path for combat presentation overrides.
    /// </summary>
    public class OrbSpritePathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_orb_sprite_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod orbs to override visuals scene paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_SpritePath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModOrbAssetOverrides.CustomVisualsScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomVisualsScenePath,
                nameof(IModOrbAssetOverrides.CustomVisualsScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="OrbModel.AssetPaths" /> so custom icon and visuals paths appear in preload enumeration.
    /// </summary>
    public class OrbAssetPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_orb_asset_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod orbs to advertise custom asset paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "get_AssetPaths"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Collects existing paths from <see cref="IModOrbAssetOverrides" /> for icon and visuals scenes.
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModOrbAssetOverrides overrides)
                return true;

            var paths = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (overrides.CustomIconPath, nameof(IModOrbAssetOverrides.CustomIconPath)),
                (overrides.CustomVisualsScenePath, nameof(IModOrbAssetOverrides.CustomVisualsScenePath)));
            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }
    }

    /// <summary>
    ///     Patches potion image and outline path getters for <see cref="IModPotionAssetOverrides" />.
    /// </summary>
    public class PotionImagePathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_potion_image_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod potions to override image paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "get_ImagePath"),
                new(typeof(PotionModel), "get_OutlinePath"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to <see cref="IModPotionAssetOverrides.CustomImagePath" /> or
        ///     <see cref="IModPotionAssetOverrides.CustomOutlinePath" />.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_ImagePath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomImagePath,
                    nameof(IModPotionAssetOverrides.CustomImagePath)),
                "get_OutlinePath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomOutlinePath,
                    nameof(IModPotionAssetOverrides.CustomOutlinePath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches potion image and outline textures for mod path overrides.
    /// </summary>
    public class PotionTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_potion_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod potions to override image textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "get_Image"),
                new(typeof(PotionModel), "get_Outline"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads textures from the matching <see cref="IModPotionAssetOverrides" /> path property.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Image" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomImagePath,
                    nameof(IModPotionAssetOverrides.CustomImagePath)),
                "get_Outline" => ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                    __instance, ref __result, o => o.CustomOutlinePath,
                    nameof(IModPotionAssetOverrides.CustomOutlinePath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches run-summary banner texture for cards implementing <see cref="IModCardAssetOverrides" />.
    /// </summary>
    public class CardBannerTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_banner_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override BannerTexture";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_BannerTexture")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads banner texture from <see cref="IModCardAssetOverrides.CustomBannerTexturePath" /> when valid.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerTexturePath,
                nameof(IModCardAssetOverrides.CustomBannerTexturePath));
        }
    }

    /// <summary>
    ///     Patches banner <see cref="Material" /> resolution for mod cards.
    /// </summary>
    public class CardBannerMaterialPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_card_banner_material";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod cards to override BannerMaterial";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "get_BannerMaterial")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads material from <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> when valid.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerMaterialPath,
                nameof(IModCardAssetOverrides.CustomBannerMaterialPath));
        }
    }

    /// <summary>
    ///     Patches act main background scene path for <see cref="IModActAssetOverrides" />.
    /// </summary>
    public class ActBackgroundScenePathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_act_background_scene_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod acts to override background scene path";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "get_BackgroundScenePath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModActAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches rest-site background scene path for mod acts.
    /// </summary>
    public class ActRestSiteBackgroundPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_act_rest_site_background_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod acts to override rest site background path";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "get_RestSiteBackgroundPath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomRestSiteBackgroundPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomRestSiteBackgroundPath,
                nameof(IModActAssetOverrides.CustomRestSiteBackgroundPath));
        }
    }

    /// <summary>
    ///     Patches act map layer background image paths (top/mid/bottom) for mod acts.
    /// </summary>
    public class ActMapBackgroundPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_act_map_background_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod acts to override map background paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
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
        /// <summary>
        ///     Dispatches to the matching <see cref="IModActAssetOverrides" /> map layer path property.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_MapTopBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapTopBgPath,
                    nameof(IModActAssetOverrides.CustomMapTopBgPath)),
                "get_MapMidBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapMidBgPath,
                    nameof(IModActAssetOverrides.CustomMapMidBgPath)),
                "get_MapBotBgPath" => ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapBotBgPath,
                    nameof(IModActAssetOverrides.CustomMapBotBgPath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches <c>EventModel.BackgroundScenePath</c> so preloads and <see cref="EventModel.CreateBackgroundScene" /> use
    ///     <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> instead of the synthetic
    ///     <c>events/background_scenes/&lt;id&gt;.tscn</c> path (which mod packs usually do not ship).
    /// </summary>
    public class EventBackgroundScenePathGetterPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_background_scene_path_getter";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Route EventModel.BackgroundScenePath to mod CustomBackgroundScenePath when the resource exists";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), "get_BackgroundScenePath")];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModEventAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateScene" /> for <see cref="IModEventAssetOverrides" />.
    /// </summary>
    public class EventLayoutScenePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_layout_scene";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod events to override layout packed scene";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateScene))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomLayoutScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUsePackedSceneCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomLayoutScenePath,
                nameof(IModEventAssetOverrides.CustomLayoutScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateInitialPortrait" /> for <see cref="IModEventAssetOverrides" />.
    /// </summary>
    public class EventInitialPortraitPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_initial_portrait";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod events to override initial portrait texture";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateInitialPortrait))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads portrait from <see cref="IModEventAssetOverrides.CustomInitialPortraitPath" /> when valid.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseTexture2DFromCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomInitialPortraitPath,
                nameof(IModEventAssetOverrides.CustomInitialPortraitPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateBackgroundScene" /> for <see cref="IModEventAssetOverrides" />.
    /// </summary>
    public class EventBackgroundScenePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_background_scene";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod events to override background packed scene";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUsePackedSceneCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModEventAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.HasVfx" /> for mod VFX scene overrides.
    /// </summary>
    public class EventHasVfxPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_has_vfx";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod events to advertise custom VFX scene availability";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), "get_HasVfx")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns true when <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> resolves to an existing resource.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModEventAssetOverrides overrides)
                return true;

            var path = overrides.CustomVfxScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!AssetPathDiagnostics.Exists(path, __instance, nameof(IModEventAssetOverrides.CustomVfxScenePath)))
                return true;

            __result = true;
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateVfx" /> for <see cref="IModEventAssetOverrides" />.
    /// </summary>
    public class EventCreateVfxPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_create_vfx";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod events to instantiate custom VFX scenes";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateVfx))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Instantiates <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> when the packed scene exists.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Node2D __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModEventAssetOverrides overrides)
                return true;

            var path = overrides.CustomVfxScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, __instance, nameof(IModEventAssetOverrides.CustomVfxScenePath)))
                return true;

            __result = PreloadManager.Cache.GetScene(path).Instantiate<Node2D>();
            return false;
        }
    }

    /// <summary>
    ///     Appends custom event asset paths to <see cref="EventModel.GetAssetPaths" /> for preloading.
    /// </summary>
    public class EventGetAssetPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_event_get_asset_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Merge mod event custom paths into GetAssetPaths preload lists";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.GetAssetPaths))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Concatenates resolved override paths after the vanilla enumeration.
        /// </summary>
        public static void Postfix(EventModel __instance, IRunState runState, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            _ = runState;

            var paths = __result;

            if (__instance is IModEventAssetOverrides evo
                && __instance.LayoutType == EventLayoutType.Ancient
                && !string.IsNullOrWhiteSpace(evo.CustomBackgroundScenePath)
                && AssetPathDiagnostics.Exists(evo.CustomBackgroundScenePath, __instance,
                    nameof(IModEventAssetOverrides.CustomBackgroundScenePath)))
            {
                var entry = __instance.Id.Entry.ToLowerInvariant();
                var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                paths = paths.Where(p => p != vanillaBg);
            }

            if (__instance is not IModEventAssetOverrides eventOverrides)
            {
                __result = paths;
                return;
            }

            var merged = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (eventOverrides.CustomLayoutScenePath, nameof(IModEventAssetOverrides.CustomLayoutScenePath)),
                (eventOverrides.CustomInitialPortraitPath, nameof(IModEventAssetOverrides.CustomInitialPortraitPath)),
                (eventOverrides.CustomBackgroundScenePath, nameof(IModEventAssetOverrides.CustomBackgroundScenePath)),
                (eventOverrides.CustomVfxScenePath, nameof(IModEventAssetOverrides.CustomVfxScenePath)));

            if (__instance is IModAncientEventAssetOverrides ancientOverrides)
            {
                var ancientMerged = AssetPathDiagnostics.CollectExistingPaths(
                    __instance,
                    (ancientOverrides.CustomMapIconPath, nameof(IModAncientEventAssetOverrides.CustomMapIconPath)),
                    (ancientOverrides.CustomMapIconOutlinePath,
                        nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath)),
                    (ancientOverrides.CustomRunHistoryIconPath,
                        nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconPath)),
                    (ancientOverrides.CustomRunHistoryIconOutlinePath,
                        nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconOutlinePath)));
                if (ancientMerged.Length > 0)
                    merged = [.. merged, .. ancientMerged];
            }

            if (merged.Length == 0)
            {
                __result = paths;
                return;
            }

            __result = paths.Concat(merged);
        }
    }

    /// <summary>
    ///     Patches ancient map icon textures for <see cref="IModAncientEventAssetOverrides" />.
    /// </summary>
    public class AncientMapIconTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_ancient_map_icon_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod ancients to override map node icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientEventModel), "get_MapIcon"),
                new(typeof(AncientEventModel), "get_MapIconOutline"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches compressed texture loading to the matching ancient override path.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, AncientEventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_MapIcon" => ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                    IModAncientEventAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapIconPath,
                    nameof(IModAncientEventAssetOverrides.CustomMapIconPath)),
                "get_MapIconOutline" => ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                    IModAncientEventAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomMapIconOutlinePath,
                    nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Patches ancient run-history icon textures for <see cref="IModAncientEventAssetOverrides" />.
    /// </summary>
    public class AncientRunHistoryIconTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_ancient_run_history_icon_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod ancients to override run history icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientEventModel), "get_RunHistoryIcon"),
                new(typeof(AncientEventModel), "get_RunHistoryIconOutline"),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches compressed texture loading to the matching ancient override path.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, AncientEventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_RunHistoryIcon" => ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                    IModAncientEventAssetOverrides>(
                    __instance,
                    ref __result,
                    o => o.CustomRunHistoryIconPath,
                    nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconPath)),
                "get_RunHistoryIconOutline" => ContentAssetOverridePatchHelper
                    .TryUseCompressedTextureAsTexture2DOverride<
                        IModAncientEventAssetOverrides>(
                        __instance,
                        ref __result,
                        o => o.CustomRunHistoryIconOutlinePath,
                        nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconOutlinePath)),
                _ => true,
            };
        }
    }

    /// <summary>
    ///     Merges custom map node asset paths into <see cref="AncientEventModel.MapNodeAssetPaths" />.
    /// </summary>
    public class AncientMapNodeAssetPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_ancient_map_node_asset_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod ancients to include custom paths in MapNodeAssetPaths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "get_MapNodeAssetPaths")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends resolved custom map icon paths after the vanilla pair.
        /// </summary>
        public static void Postfix(AncientEventModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModAncientEventAssetOverrides overrides)
                return;

            var entry = __instance.Id.Entry.ToLowerInvariant();
            var vanillaMain = ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{entry}.png");
            var vanillaOutline = ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{entry}_outline.png");

            var extra = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (overrides.CustomMapIconPath, nameof(IModAncientEventAssetOverrides.CustomMapIconPath)),
                (overrides.CustomMapIconOutlinePath, nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath)));
            if (extra.Length == 0)
                return;

            __result = __result.Where(p => p != vanillaMain && p != vanillaOutline).Concat(extra);
        }
    }

    /// <summary>
    ///     Optional affliction overlay scene path for patches on <see cref="AfflictionModel" />.
    /// </summary>
    public interface IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        /// </summary>
        AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <summary>
        ///     Overlay packed scene path override.
        /// </summary>
        string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel" /> overlay scene path for <see cref="IModAfflictionAssetOverrides" />.
    /// </summary>
    public class AfflictionOverlayPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_affliction_overlay_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod afflictions to override OverlayPath";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "get_OverlayPath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                __instance, ref __result, o => o.CustomOverlayScenePath,
                nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel.HasOverlay" /> from custom overlay path existence.
    /// </summary>
    public class AfflictionHasOverlayPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_affliction_has_overlay";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod afflictions to advertise overlay availability";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "get_HasOverlay")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Resolves the custom overlay path then sets boolean availability from resource existence.
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            var path = string.Empty;
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                       __instance,
                       ref path,
                       o => o.CustomOverlayScenePath,
                       nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)) ||
                   ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                       __instance,
                       path,
                       nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath),
                       ref __result);
        }
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel.CreateOverlay" /> to instantiate mod overlay scenes when configured.
    /// </summary>
    public class AfflictionCreateOverlayPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_affliction_create_overlay";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod afflictions to instantiate overlays from custom scene paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), nameof(AfflictionModel.CreateOverlay))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Instantiates <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" /> when the packed scene exists.
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
        {
            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            if (!ResourceLoader.Exists(path))
                return true;

            __result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }
    }

    /// <summary>
    ///     Optional enchantment icon path for patches on <see cref="EnchantmentModel" />.
    /// </summary>
    public interface IModEnchantmentAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        /// </summary>
        EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

        /// <summary>
        ///     Intended icon path override.
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     Patches <see cref="EnchantmentModel" /> intended icon path for <see cref="IModEnchantmentAssetOverrides" />.
    /// </summary>
    public class EnchantmentIntendedIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_enchantment_intended_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod enchantments to override IntendedIconPath";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EnchantmentModel), "get_IntendedIconPath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEnchantmentAssetOverrides.CustomIconPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EnchantmentModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEnchantmentAssetOverrides>(
                __instance, ref __result, o => o.CustomIconPath,
                nameof(IModEnchantmentAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="PowerModel.ResolvedBigIconPath" /> so preload lists include mod big-icon paths.
    /// </summary>
    public class PowerResolvedBigIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_power_resolved_big_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod powers to override ResolvedBigIconPath for preloading";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), "get_ResolvedBigIconPath")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomBigIconPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(PowerModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                __instance, ref __result, o => o.CustomBigIconPath,
                nameof(IModPowerAssetOverrides.CustomBigIconPath));
        }
    }

    /// <summary>
    ///     Implement on a <see cref="CardPoolModel" /> subclass to supply a custom image path for the
    ///     small energy icon rendered inside rich-text card descriptions
    ///     (e.g. <c>[img]…/winefox_energy_icon.png[/img]</c>).
    ///     <para />
    ///     The default game path pattern is:
    ///     <c>res://images/packed/sprite_fonts/{EnergyColorName}_energy_icon.png</c>.
    ///     Use this interface only when you need a different path.
    /// </summary>
    public interface IModTextEnergyIconPool
    {
        /// <summary>
        ///     Custom image path for the small energy icon embedded in rich-text card descriptions.
        /// </summary>
        string? TextEnergyIconPath { get; }
    }
}
