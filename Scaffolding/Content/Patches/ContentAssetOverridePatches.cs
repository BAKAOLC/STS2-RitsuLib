using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters;
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

            var texture = ResourceLoader.Load<Texture2D>(path);
            if (texture == null)
            {
                LogLoadFailure(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
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
        internal static bool TryUseDirectMaterialOverride<TOverrides>(
            object instance,
            ref Material __result,
            Func<TOverrides, Material?> selector)
            where TOverrides : class
        {
            if (instance is not TOverrides overrides)
                return true;

            var material = selector(overrides);
            if (material == null)
                return true;

            __result = material;
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

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseExternalPathOverride(
            object instance,
            ref string __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path) || !AssetPathDiagnostics.Exists(path, instance, memberName))
                return true;

            __result = path;
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseExternalPackedScenePathOverride(
            object instance,
            ref PackedScene __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path) || !AssetPathDiagnostics.Exists(path, instance, memberName))
                return true;

            __result = PreloadManager.Cache.GetScene(path);
            return false;
        }

        // ReSharper disable once InconsistentNaming
        internal static bool TryUseExternalCompressedTexturePathAsTexture2DOverride(
            object instance,
            ref Texture2D __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path) || !AssetPathDiagnostics.Exists(path, instance, memberName))
                return true;

            __result = ResourceLoader.Load<CompressedTexture2D>(path);
            return false;
        }

        internal static string[] CollectExternalExistingPaths(
            object instance,
            params (string? Path, string MemberName)[] candidates)
        {
            return AssetPathDiagnostics.CollectExistingPaths(instance, candidates);
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

        private static void LogLoadFailure(object instance, string memberName, string path, string expectedType)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Assets] Resource exists but failed to load as {expectedType} for {DescribeOwner(instance)}.{memberName}: '{path}'. Falling back to the base asset.");
        }

        private static string DescribeOwner(object owner)
        {
            try
            {
                if (owner is AbstractModel model && !string.IsNullOrWhiteSpace(model.Id.Entry))
                    return $"{owner.GetType().Name}<{model.Id.Entry}>";
            }
            catch
            {
                // Ignore model identity lookup failures and fall back to the CLR type name.
            }

            return owner.GetType().Name;
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

            var texture = ResourceLoader.Load<Texture2D>(path);
            if (texture == null)
            {
                LogLoadFailure(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
            return false;
        }
    }

    /// <summary>
    ///     Optional card art paths consumed by content asset Harmony patches on <see cref="CardModel" />.
    ///     由 content asset Harmony patches on <c>CardModel</c> 使用的可选 card art 路径。
    /// </summary>
    public interface IModCardAssetOverrides
    {
        /// <summary>
        ///     Path bundle; individual properties usually mirror these fields unless overridden.
        ///     路径包；individual properties usually mirror these fields unless overridden。
        /// </summary>
        CardAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override for main portrait image path.
        ///     main portrait image 路径覆盖。
        /// </summary>
        string? CustomPortraitPath { get; }

        /// <summary>
        ///     Override for beta/alternate portrait path.
        ///     beta/alternate portrait 路径覆盖。
        /// </summary>
        string? CustomBetaPortraitPath { get; }

        /// <summary>
        ///     Override for card frame texture path.
        ///     card frame texture 路径覆盖。
        /// </summary>
        string? CustomFramePath { get; }

        /// <summary>
        ///     Override for portrait border texture path.
        ///     portrait border texture 路径覆盖。
        /// </summary>
        string? CustomPortraitBorderPath { get; }

        /// <summary>
        ///     Override for small energy icon texture path.
        ///     small energy icon texture 路径覆盖。
        /// </summary>
        string? CustomEnergyIconPath { get; }

        /// <summary>
        ///     Override for frame <see cref="Material" /> resource path.
        ///     frame <c>Material</c> resource 路径覆盖。
        /// </summary>
        string? CustomFrameMaterialPath { get; }

        /// <summary>
        ///     Override for built-in overlay packed scene path.
        ///     built-in overlay packed scene 路径覆盖。
        /// </summary>
        string? CustomOverlayScenePath { get; }

        /// <summary>
        ///     Override for banner texture path.
        ///     banner texture 路径覆盖。
        /// </summary>
        string? CustomBannerTexturePath { get; }

        /// <summary>
        ///     Override for banner material path.
        ///     banner material 路径覆盖。
        /// </summary>
        string? CustomBannerMaterialPath { get; }
    }

    /// <summary>
    ///     Optional direct frame <see cref="Material" /> override for cards.
    ///     可选 direct frame <c>材质</c> override 用于 卡牌s.
    ///     This bypasses resource-path loading and is checked before
    ///     This bypasses 资源-路径 加载ing 和 is checked 之前
    ///     <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" />.
    /// </summary>
    public interface IModCardFrameMaterialOverride
    {
        /// <summary>
        ///     Direct frame material override.
        ///     直接 frame material 覆盖。
        ///     Return <c>null</c> to continue with other override layers.
        ///     返回 <c>null</c> 以continue with other override layers。
        /// </summary>
        Material? CustomFrameMaterial => null;
    }

    /// <summary>
    ///     Optional direct banner <see cref="Material" /> override for cards.
    ///     可选 direct banner <c>材质</c> override 用于 卡牌s.
    ///     This bypasses resource-path loading and is checked before
    ///     This bypasses 资源-路径 加载ing 和 is checked 之前
    ///     <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" />.
    /// </summary>
    public interface IModCardBannerMaterialOverride
    {
        /// <summary>
        ///     Direct banner material override.
        ///     直接 banner material 覆盖。
        ///     Return <c>null</c> to fall back to frame material semantics.
        ///     返回 <c>null</c> 以fall back to frame material semantics。
        /// </summary>
        Material? CustomBannerMaterial => null;
    }

    /// <summary>
    ///     Implement this interface on a <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> to directly supply
    ///     Implement this interface on a <c>MegaCrit.Sts2.Core.Models.CardPool模型</c> to directly supply
    ///     a <see cref="Material" /> for card frames in the pool.
    ///     一个 <c>Material</c> for card frames in the pool。
    ///     When <see cref="PoolFrameMaterial" /> is non-null, <c>CardFrameMaterialPath</c> is ignored entirely.
    ///     当 <c>PoolFrame材质</c> is non-null, <c>CardFrame材质路径</c> is ignored entirely.
    /// </summary>
    public interface IModCardPoolFrameMaterial
    {
        /// <summary>
        ///     The material to use for card frames in this pool.
        ///     该 material to use for card frames in this pool。
        ///     Return <c>null</c> to fall back to the path-based default.
        ///     返回 <c>null</c> 以fall back to the path-based default。
        /// </summary>
        Material? PoolFrameMaterial { get; }
    }

    /// <summary>
    ///     Optional relic icon paths for Harmony patches on <see cref="RelicModel" />.
    ///     用于 Harmony patches on <c>RelicModel</c> 的可选 relic icon 路径。
    /// </summary>
    public interface IModRelicAssetOverrides
    {
        /// <summary>
        ///     Path bundle for relic presentation assets.
        ///     用于 relic presentation assets 的路径包。
        /// </summary>
        RelicAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Primary relic icon path override.
        ///     Primary 遗物 图标 路径 override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Outline icon path override.
        ///     Outline 图标 路径 override.
        /// </summary>
        string? CustomIconOutlinePath { get; }

        /// <summary>
        ///     Large relic art path override.
        ///     Large 遗物 art 路径 override.
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional power icon paths for Harmony patches on <see cref="PowerModel" />.
    ///     用于 Harmony patches on <c>PowerModel</c> 的可选 power icon 路径。
    /// </summary>
    public interface IModPowerAssetOverrides
    {
        /// <summary>
        ///     Path bundle for power icons.
        ///     用于 power icons 的路径包。
        /// </summary>
        PowerAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Standard icon path override.
        ///     Standard 图标 路径 override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Large icon path override.
        ///     Large 图标 路径 override.
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional orb icon and visuals scene paths for Harmony patches on <see cref="OrbModel" />.
    ///     用于 Harmony patches on <c>OrbModel</c> 的可选 orb icon and visuals scene 路径。
    /// </summary>
    public interface IModOrbAssetOverrides
    {
        /// <summary>
        ///     Path bundle for orb HUD and combat visuals.
        ///     用于 orb HUD and combat visuals 的路径包。
        /// </summary>
        OrbAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Orb icon texture path override.
        ///     充能球 图标 纹理 路径 override.
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Orb combat visuals scene path override.
        ///     充能球 combat visuals 场景 路径 override.
        /// </summary>
        string? CustomVisualsScenePath { get; }
    }

    /// <summary>
    ///     Default act asset override surface; concrete mods typically use <see cref="ModActTemplate" /> instead of
    ///     Default 章节 资源 override surface; concrete mods typically 使用 <c>ModActTemplate</c> instead of
    ///     implementing this directly.
    ///     中文说明：implementing this directly.
    /// </summary>
    public interface IModActAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        ///     路径包；默认为空。
        /// </summary>
        ActAssetProfile AssetProfile => ActAssetProfile.Empty;

        /// <summary>
        ///     Main act background scene path override.
        ///     Main 章节 背景 场景 路径 override.
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Rest site background scene path override.
        ///     Rest site 背景 场景 路径 override.
        /// </summary>
        string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;

        /// <summary>
        ///     Map top-layer background image path override.
        ///     Map top-layer 背景 image 路径 override.
        /// </summary>
        string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;

        /// <summary>
        ///     Map middle-layer background image path override.
        ///     Map middle-layer 背景 image 路径 override.
        /// </summary>
        string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;

        /// <summary>
        ///     Map bottom-layer background image path override.
        ///     Map bottom-layer 背景 image 路径 override.
        /// </summary>
        string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;

        /// <summary>
        ///     Treasure chest Spine resource path override.
        ///     Treasure chest Spine 资源 路径 override.
        /// </summary>
        string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;

        /// <summary>
        ///     Optional <c>res://</c> directory for combat background parallax layers (same <c>_bg_</c> / <c>_fg_</c> naming as
        ///     可选 <c>res://</c> directory 用于 combat 背景 parallax layers (same <c>_bg_</c> / <c>_fg_</c> naming as
        ///     vanilla). When set, <see cref="ActModel.GenerateBackgroundAssets" /> scans this folder instead of
        ///     原版). 当 设置, <c>ActModel.GenerateBackgroundAssets</c> scans this folder instead of
        ///     <c>scenes/backgrounds/&lt;act&gt;/layers</c>.
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;
    }

    /// <summary>
    ///     Optional event layout, portrait, background, and VFX scene paths; use <see cref="ModEventTemplate" /> or implement
    ///     可选 事件 layout, 肖像, 背景, 和 VFX 场景 路径; 使用 <c>ModEventTemplate</c> 或 implement
    ///     on a mod <see cref="EventModel" />.
    ///     on a mod <c>EventModel</c>.
    /// </summary>
    public interface IModEventAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        ///     路径包；<c>Custom*</c> properties mirror these fields unless overridden。
        /// </summary>
        EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene for <c>EventModel.CreateScene</c> (full layout root).
        ///     Override packed 场景 用于 <c>EventModel.创建场景</c> (full layout root).
        /// </summary>
        string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <summary>
        ///     Override texture path for <c>EventModel.CreateInitialPortrait</c>.
        ///     <c>EventModel.CreateInitialPortrait</c> 的纹理路径覆盖。
        /// </summary>
        string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateBackgroundScene</c>.
        ///     <c>EventModel.CreateBackgroundScene</c> 的 PackedScene 路径覆盖。
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateVfx</c> / <c>HasVfx</c>.
        ///     <c>EventModel.CreateVfx</c> / <c>HasVfx</c> 的 PackedScene 路径覆盖。
        /// </summary>
        string? CustomVfxScenePath => AssetProfile.VfxScenePath;
    }

    /// <summary>
    ///     Extends <see cref="IModEventAssetOverrides" /> with ancient map and run-history icon paths; use
    ///     <see cref="ModAncientEventTemplate" /> or implement on a mod <see cref="AncientEventModel" />.
    ///     为 <c>IModEventAssetOverrides</c> 增加远古事件地图节点和运行历史图标路径；可使用
    ///     <c>ModAncientEventTemplate</c>，或在 mod 的 <c>AncientEventModel</c> 上直接实现。
    /// </summary>
    public interface IModAncientEventAssetOverrides : IModEventAssetOverrides
    {
        /// <summary>
        ///     Ancient-only presentation paths (map node + run history).
        ///     仅远古事件使用的表现资源路径（地图节点 + 运行历史）。
        /// </summary>
        AncientEventPresentationAssetProfile AncientPresentationAssetProfile =>
            AncientEventPresentationAssetProfile.Empty;

        /// <summary>
        ///     Override for <c>AncientEventModel.MapIcon</c>.
        ///     <c>AncientEventModel.MapIcon</c> 的覆盖路径。
        /// </summary>
        string? CustomMapIconPath => AncientPresentationAssetProfile?.MapIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.MapIconOutline</c>.
        ///     <c>AncientEventModel.MapIconOutline</c> 的覆盖路径。
        /// </summary>
        string? CustomMapIconOutlinePath => AncientPresentationAssetProfile?.MapIconOutlinePath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIcon</c>.
        ///     <c>AncientEventModel.RunHistoryIcon</c> 的覆盖路径。
        /// </summary>
        string? CustomRunHistoryIconPath => AncientPresentationAssetProfile?.RunHistoryIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIconOutline</c>.
        ///     <c>AncientEventModel.RunHistoryIconOutline</c> 的覆盖路径。
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AncientPresentationAssetProfile?.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     Optional epoch timeline portrait paths; use <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" /> or
    ///     可选 epoch timeline 肖像 路径; 使用 <c>STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate</c> or
    ///     implement on a mod <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel" />.
    ///     implement on a mod <c>MegaCrit.Sts2.Core.Timeline.Epoch模型</c>.
    /// </summary>
    public interface IModEpochAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        ///     路径包；<c>Custom*</c> properties mirror these fields unless overridden。
        /// </summary>
        EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <summary>
        ///     Override for <c>EpochModel.PackedPortraitPath</c> (atlas sprite entry).
        ///     Override 用于 <c>Epoch模型.Packed肖像路径</c> (atlas sprite entry).
        /// </summary>
        string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <summary>
        ///     Override for <c>EpochModel.BigPortraitPath</c> (large portrait texture).
        ///     Override 用于 <c>Epoch模型.Big肖像路径</c> (large 肖像 纹理).
        /// </summary>
        string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }

    /// <summary>
    ///     Patches <see cref="EpochModel" /> portrait path getters for <see cref="IModEpochAssetOverrides" />.
    ///     为 <c>IModEpochAssetOverrides</c> 补丁 <c>EpochModel</c> portrait path getters。
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
                new(typeof(EpochModel), "PackedPortraitPath", MethodType.Getter),
                new(typeof(EpochModel), "BigPortraitPath", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches string overrides for packed atlas vs large portrait paths.
        ///     Dispatches string overrides 用于 packed atlas vs large 肖像 路径.
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
    ///     为 <c>IModCardAssetOverrides</c> 补丁 <c>CardModel</c> portrait path getters。
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
                new(typeof(CardModel), "PortraitPath", MethodType.Getter),
                new(typeof(CardModel), "BetaPortraitPath", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to portrait or beta portrait override based on the patched getter.
        ///     Dispatches to 肖像 或 beta 肖像 override based on the patched getter.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_PortraitPath" => TryCardPortraitPath(__instance, ref __result),
                "get_BetaPortraitPath" => TryCardBetaPortraitPath(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryCardPortraitPath(CardModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitPath(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath));
        }

        private static bool TryCardBetaPortraitPath(CardModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBetaPortraitPath(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomBetaPortraitPath,
                nameof(IModCardAssetOverrides.CustomBetaPortraitPath));
        }
    }

    /// <summary>
    ///     Patches portrait availability flags so custom paths from <see cref="IModCardAssetOverrides" /> are honored.
    ///     补丁 portrait availability flags so custom paths from <c>IModCardAssetOverrides</c> are honored。
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
                new(typeof(CardModel), "HasPortrait", MethodType.Getter),
                new(typeof(CardModel), "HasBetaPortrait", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Sets boolean availability from whether the corresponding custom portrait path exists on disk.
        ///     设置 boolean availability 从 whether the corresponding 自定义 肖像 路径 exists on disk.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModCardAssetOverrides overrides)
                return true;

            return __originalMethod.Name switch
            {
                "get_HasPortrait" => TryHasPortrait(__instance, overrides, ref __result),
                "get_HasBetaPortrait" => TryHasBetaPortrait(__instance, overrides, ref __result),
                _ => true,
            };
        }

        private static bool TryHasPortrait(CardModel instance, IModCardAssetOverrides overrides, ref bool result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitExists(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                instance, overrides.CustomPortraitPath, nameof(IModCardAssetOverrides.CustomPortraitPath), ref result);
        }

        private static bool TryHasBetaPortrait(CardModel instance, IModCardAssetOverrides overrides, ref bool result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBetaPortraitExists(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseExistenceOverride(
                instance, overrides.CustomBetaPortraitPath, nameof(IModCardAssetOverrides.CustomBetaPortraitPath),
                ref result);
        }
    }

    /// <summary>
    ///     Patches card frame, portrait border, and energy icon texture getters for mod path overrides.
    ///     为 mod path overrides 补丁 card frame, portrait border, and energy icon texture getters。
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
                new(typeof(CardModel), "Frame", MethodType.Getter),
                new(typeof(CardModel), "PortraitBorder", MethodType.Getter),
                new(typeof(CardModel), "EnergyIcon", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads textures from the matching <see cref="IModCardAssetOverrides" /> path when present.
        ///     加载 textures from the matching <c>IModCardAssetOverrides</c> path when present。
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Frame" => TryCardFrameTexture(__instance, ref __result),
                "get_PortraitBorder" => TryCardPortraitBorderTexture(__instance, ref __result),
                "get_EnergyIcon" => TryCardEnergyIconTexture(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryCardFrameTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardFrameTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomFramePath, nameof(IModCardAssetOverrides.CustomFramePath));
        }

        private static bool TryCardPortraitBorderTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardPortraitBorderTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomPortraitBorderPath,
                nameof(IModCardAssetOverrides.CustomPortraitBorderPath));
        }

        private static bool TryCardEnergyIconTexture(CardModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardEnergyIconTexture(instance, ref result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                instance, ref result, o => o.CustomEnergyIconPath, nameof(IModCardAssetOverrides.CustomEnergyIconPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CardModel" /> frame material resolution for custom <c>.tres</c> paths.
    ///     为 custom <c>.tres</c> paths 补丁 <c>CardModel</c> frame material resolution。
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
                new(typeof(CardModel), "FrameMaterial", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads <see cref="Material" /> from <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" /> when valid.
        ///     加载 <c>Material</c> from <c>IModCardAssetOverrides.CustomFrameMaterialPath</c> when valid。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
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
    ///     Patches pool-level frame 材质 so <c>IModCardPoolFrame材质.PoolFrame材质</c> can replace 路径
    ///     lookup.
    ///     中文说明：lookup.
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
                new(typeof(CardPoolModel), "FrameMaterial", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns the pool’s inline material when the pool implements <see cref="IModCardPoolFrameMaterial" />.
        ///     返回 the pool’s inline material when the pool implements <c>IModCardPoolFrameMaterial</c>。
        /// </summary>
        public static bool Prefix(CardPoolModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
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
    ///     Patches <see cref="CardModel.AllPortraitPaths" /> so custom portrait/beta paths participate in preload lists.
    ///     补丁 <c>CardModel.AllPortraitPaths</c> so custom portrait/beta paths participate in preload lists。
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
                new(typeof(CardModel), "AllPortraitPaths", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Replaces the enumerable with verified custom portrait paths when the card implements overrides.
        ///     Replaces the enumerable 带有 verified 自定义 肖像 路径 当 the 卡牌 implements overrides.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
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
    ///     为 cards implementing <c>IModCardAssetOverrides</c> 补丁 built-in overlay scene path。
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
                new(typeof(CardModel), "OverlayPath", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModCardAssetOverrides.CustomOverlayScenePath</c>。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
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
    ///     补丁 <c>CardModel.HasBuiltInOverlay</c> using existence checks on custom overlay scene paths。
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
                new(typeof(CardModel), "HasBuiltInOverlay", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Sets <c>true</c> when <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> resolves to an existing
        ///     设置 <c>true</c> 当 <c>IModCardAssetOverrides.CustomOverlayScenePath</c> 解析 to an existing
        ///     resource.
        ///     资源.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
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
    ///     补丁 <c>CardModel.CreateOverlay</c> to instantiate mod overlay scenes when configured。
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
        ///     Instantiates <c>IModCardAssetOverrides.CustomOverlayScenePath</c> 当 the packed 场景 exists.
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
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
    ///     Patches <c>RelicModel.图标路径</c> 和 packed atlas 图标/outline 路径 getters (used 通过 原版
    ///     <c>Icon</c> / <c>IconOutline</c> loaders) for mod-character per–relic-id paths (owner match) first, then
    ///     <see cref="IModRelicAssetOverrides" />.
    /// </summary>
    public class RelicIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_relic_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Owned-relic character overrides first, then mod relic custom icon and packed atlas paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "IconPath", MethodType.Getter),
                new(typeof(RelicModel), "PackedIconPath", null, true, MethodType.Getter),
                new(typeof(RelicModel), "PackedIconOutlinePath", null, true, MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.TryGetVanillaRelicVisualOverrideForOwnedRelic" /> when
        ///     Supplies <c>IModCharacterAssetOverrides.TryGet原版遗物VisualOverrideForOwned遗物</c> 当
        ///     applicable, then <see cref="IModRelicAssetOverrides" /> custom paths.
        ///     applicable, then <c>IModRelicAssetOverrides</c> 自定义 路径.
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(MethodBase __originalMethod, RelicModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_IconPath" or "get_PackedIconPath" => TryRelicMainIconPath(__instance, ref __result),
                "get_PackedIconOutlinePath" => TryRelicPackedIconOutlinePath(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryRelicMainIconPath(RelicModel instance, ref string result)
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

        private static bool TryRelicPackedIconOutlinePath(RelicModel instance, ref string result)
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

    /// <summary>
    ///     Patches relic icon texture getters (main, outline, big): mod-character owned-relic overrides first, then
    ///     Patches 遗物 图标 纹理 getters (main, outline, big): mod-character owned-遗物 overrides first, then
    ///     <see cref="IModRelicAssetOverrides" />.
    /// </summary>
    public class RelicTexturePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_relic_texture";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Owned-relic character overrides first, then mod relic icon textures";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RelicModel), "Icon", MethodType.Getter),
                new(typeof(RelicModel), "IconOutline", MethodType.Getter),
                new(typeof(RelicModel), "BigIcon", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches texture loading to mod-character overrides first, then mod relic overrides.
        ///     Dispatches 纹理 加载ing to mod-character overrides first, then mod 遗物 overrides.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, RelicModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => TryRelicIconTexture(__instance, ref __result),
                "get_IconOutline" => TryRelicIconOutlineTexture(__instance, ref __result),
                "get_BigIcon" => TryRelicBigIconTexture(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryRelicIconTexture(RelicModel instance, ref Texture2D result)
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

        private static bool TryRelicIconOutlineTexture(RelicModel instance, ref Texture2D result)
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

        private static bool TryRelicBigIconTexture(RelicModel instance, ref Texture2D result)
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

    /// <summary>
    ///     Patches <see cref="PowerModel.IconPath" /> and <see cref="PowerModel.PackedIconPath" /> (used by vanilla
    ///     Patches <c>PowerModel.图标路径</c> 和 <c>PowerModel.Packed图标路径</c> (used 通过 原版
    ///     <c>Icon</c> loader) for <see cref="IModPowerAssetOverrides" />.
    /// </summary>
    public class PowerIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_power_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod powers to override icon and packed atlas icon paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PowerModel), "IconPath", MethodType.Getter),
                new(typeof(PowerModel), "PackedIconPath", null, true, MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomIconPath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModPowerAssetOverrides.CustomIconPath</c>。
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(MethodBase __originalMethod, PowerModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_IconPath" or "get_PackedIconPath" => TryPowerIconPath(__instance, ref __result),
                _ => true,
            };
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
    ///     为 mod path overrides 补丁 power standard and big icon textures。
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
                new(typeof(PowerModel), "Icon", MethodType.Getter),
                new(typeof(PowerModel), "BigIcon", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to <see cref="IModPowerAssetOverrides.CustomIconPath" /> or
        ///     中文说明：Dispatches to <c>IModPowerAssetOverrides.CustomIconPath</c> or
        ///     Dispatches to <c>IModPowerAssetOverrides.CustomIconPath</c> or
        ///     中文说明：Dispatches to <c>IModPowerAssetOverrides.CustomIconPath</c> or
        ///     <see cref="IModPowerAssetOverrides.CustomBigIconPath" />.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, PowerModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Icon" => TryPowerIconTexture(__instance, ref __result),
                "get_BigIcon" => TryPowerBigIconTexture(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryPowerIconTexture(PowerModel instance, ref Texture2D result)
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

        private static bool TryPowerBigIconTexture(PowerModel instance, ref Texture2D result)
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

    /// <summary>
    ///     Patches orb HUD icon (<see cref="CompressedTexture2D" />) for <see cref="IModOrbAssetOverrides" />.
    ///     为 <c>IModOrbAssetOverrides</c> 补丁 orb HUD icon (<c>CompressedTexture2D</c>)。
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
                new(typeof(OrbModel), "Icon", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads compressed icon texture from <see cref="IModOrbAssetOverrides.CustomIconPath" /> when valid.
        ///     加载 compressed icon texture from <c>IModOrbAssetOverrides.CustomIconPath</c> when valid。
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref CompressedTexture2D __result)
            // ReSharper restore InconsistentNaming
        {
            if (ExternalAssetOverrideRegistry.TryGetOrbIconTexture(__instance, out var externalTexture))
            {
                __result = externalTexture;
                return false;
            }

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetOrbIconPath(__instance, out var externalPath) &&
                AssetPathDiagnostics.Exists(externalPath, __instance, "ExternalAssetOverrideRegistry.OrbIconPath"))
            {
                __result = ResourceLoader.Load<CompressedTexture2D>(externalPath);
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModOrbAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches orb visuals scene path for combat presentation overrides.
    ///     为 combat presentation overrides 补丁 orb visuals scene path。
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
                new(typeof(OrbModel), "SpritePath", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModOrbAssetOverrides.CustomVisualsScenePath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModOrbAssetOverrides.CustomVisualsScenePath</c>。
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetOrbVisualsScenePath(__instance, out var externalPath) &&
                AssetPathDiagnostics.Exists(externalPath, __instance,
                    "ExternalAssetOverrideRegistry.OrbVisualsScenePath"))
            {
                __result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomVisualsScenePath,
                nameof(IModOrbAssetOverrides.CustomVisualsScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="OrbModel.AssetPaths" /> so custom icon and visuals paths appear in preload enumeration.
    ///     补丁 <c>OrbModel.AssetPaths</c> so custom icon and visuals paths appear in preload enumeration。
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
                new(typeof(OrbModel), "AssetPaths", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Collects existing paths from <see cref="IModOrbAssetOverrides" /> for icon and visuals scenes.
        ///     Collects existing 路径 从 <c>IModOrbAssetOverrides</c> 用于 图标 和 visuals 场景s.
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModOrbAssetOverrides overrides)
                return !TryBuildOrbAssetPathsFromExternal(__instance, out __result);

            var paths = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (overrides.CustomIconPath, nameof(IModOrbAssetOverrides.CustomIconPath)),
                (overrides.CustomVisualsScenePath, nameof(IModOrbAssetOverrides.CustomVisualsScenePath)));
            if (TryBuildOrbAssetPathsFromExternal(__instance, out var externalPaths))
                paths = paths.Concat(externalPaths).Distinct(StringComparer.Ordinal).ToArray();
            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }

        private static bool TryBuildOrbAssetPathsFromExternal(OrbModel instance, out IEnumerable<string> paths)
        {
            var collected = new List<string>(2);
            if (ExternalAssetOverrideRegistry.TryGetOrbIconPath(instance, out var iconPath) &&
                AssetPathDiagnostics.Exists(iconPath, instance, "ExternalAssetOverrideRegistry.OrbIconPath"))
                collected.Add(iconPath);
            if (ExternalAssetOverrideRegistry.TryGetOrbVisualsScenePath(instance, out var visualsPath) &&
                AssetPathDiagnostics.Exists(visualsPath, instance, "ExternalAssetOverrideRegistry.OrbVisualsScenePath"))
                collected.Add(visualsPath);

            paths = collected;
            return collected.Count > 0;
        }
    }

    /// <summary>
    ///     Patches potion image and outline path getters (including packed atlas path getters used by vanilla
    ///     Patches potion image 和 outline 路径 getters (including packed atlas 路径 getters used 通过 原版
    ///     <c>Image</c> / preload) for <see cref="IModPotionAssetOverrides" />.
    /// </summary>
    public class PotionImagePathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_potion_image_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod potions to override image and packed atlas paths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "ImagePath", MethodType.Getter),
                new(typeof(PotionModel), "OutlinePath", MethodType.Getter),
                new(typeof(PotionModel), "PackedImagePath", null, true, MethodType.Getter),
                new(typeof(PotionModel), "PackedOutlinePath", null, true, MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to <see cref="IModPotionAssetOverrides.CustomImagePath" /> or
        ///     中文说明：Dispatches to <c>IModPotionAssetOverrides.CustomImagePath</c> or
        ///     Dispatches to <c>IModPotionAssetOverrides.CustomImagePath</c> or
        ///     中文说明：Dispatches to <c>IModPotionAssetOverrides.CustomImagePath</c> or
        ///     <see cref="IModPotionAssetOverrides.CustomOutlinePath" />.
        /// </summary>
        [HarmonyPriority(410)]
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_ImagePath" => TryPotionImagePath(__instance, ref __result),
                "get_OutlinePath" => TryPotionOutlinePath(__instance, ref __result),
                "get_PackedImagePath" => TryPotionImagePath(__instance, ref __result),
                "get_PackedOutlinePath" => TryPotionOutlinePath(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryPotionImagePath(PotionModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryPotionImagePath(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPotionImagePath(instance, out var externalPath))
            {
                result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomImagePath, nameof(IModPotionAssetOverrides.CustomImagePath));
        }

        private static bool TryPotionOutlinePath(PotionModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryPotionOutlinePath(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPotionOutlinePath(instance, out var externalPath))
            {
                result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomOutlinePath, nameof(IModPotionAssetOverrides.CustomOutlinePath));
        }
    }

    /// <summary>
    ///     Patches potion image and outline textures for mod path overrides.
    ///     为 mod path overrides 补丁 potion image and outline textures。
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
                new(typeof(PotionModel), "Image", MethodType.Getter),
                new(typeof(PotionModel), "Outline", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads textures from the matching <see cref="IModPotionAssetOverrides" /> path property.
        ///     加载 textures from the matching <c>IModPotionAssetOverrides</c> path property。
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, PotionModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_Image" => TryPotionImageTexture(__instance, ref __result),
                "get_Outline" => TryPotionOutlineTexture(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryPotionImageTexture(PotionModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryPotionImageTexture(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPotionImageTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomImagePath, nameof(IModPotionAssetOverrides.CustomImagePath));
        }

        private static bool TryPotionOutlineTexture(PotionModel instance, ref Texture2D result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryPotionOutlineTexture(instance, ref result))
                return false;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetPotionOutlineTexture(instance, out var externalTexture))
            {
                result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomOutlinePath, nameof(IModPotionAssetOverrides.CustomOutlinePath));
        }
    }

    /// <summary>
    ///     Patches run-summary banner texture for cards implementing <see cref="IModCardAssetOverrides" />.
    ///     为 cards implementing <c>IModCardAssetOverrides</c> 补丁 run-summary banner texture。
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
            return [new(typeof(CardModel), "BannerTexture", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads banner texture from <see cref="IModCardAssetOverrides.CustomBannerTexturePath" /> when valid.
        ///     加载 banner texture from <c>IModCardAssetOverrides.CustomBannerTexturePath</c> when valid。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBannerTexture(__instance, ref __result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerTexturePath,
                nameof(IModCardAssetOverrides.CustomBannerTexturePath));
        }
    }

    /// <summary>
    ///     Patches banner <see cref="Material" /> resolution for mod cards.
    ///     为 mod cards 补丁 banner <c>Material</c> resolution。
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
            return [new(typeof(CardModel), "BannerMaterial", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads material from <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> when valid.
        ///     加载 material from <c>IModCardAssetOverrides.CustomBannerMaterialPath</c> when valid。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is IModCardBannerMaterialOverride bannerOverride)
            {
                var directBannerMaterial = bannerOverride.CustomBannerMaterial;
                if (directBannerMaterial != null)
                {
                    __result = directBannerMaterial;
                    return false;
                }
            }

            if (ExternalCardMaterialOverrideRegistry.TryGetBannerMaterial(__instance,
                    out var externalBannerMaterial))
            {
                __result = externalBannerMaterial;
                return false;
            }

            if (!ModCharacterOwnedVisualOverrideHelper.TryCardBannerMaterial(__instance, ref __result))
                return false;

            return ContentAssetOverridePatchHelper.TryUseMaterialOverride<IModCardAssetOverrides>(
                __instance, ref __result, o => o.CustomBannerMaterialPath,
                nameof(IModCardAssetOverrides.CustomBannerMaterialPath));
        }
    }

    /// <summary>
    ///     Patches act main background scene path for <see cref="IModActAssetOverrides" />.
    ///     为 <c>IModActAssetOverrides</c> 补丁 act main background scene path。
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
            return [new(typeof(ActModel), "BackgroundScenePath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModActAssetOverrides.CustomBackgroundScenePath</c>。
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetActBackgroundScenePath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.ActBackgroundScenePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModActAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches rest-site background scene path for mod acts.
    ///     为 mod acts 补丁 rest-site background scene path。
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
            return [new(typeof(ActModel), "RestSiteBackgroundPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomRestSiteBackgroundPath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModActAssetOverrides.CustomRestSiteBackgroundPath</c>。
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetActRestSiteBackgroundPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.ActRestSiteBackgroundPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomRestSiteBackgroundPath,
                nameof(IModActAssetOverrides.CustomRestSiteBackgroundPath));
        }
    }

    /// <summary>
    ///     Patches act map layer background image paths (top/mid/bottom) for mod acts.
    ///     为 mod acts 补丁 act map layer background image paths (top/mid/bottom)。
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
                new(typeof(ActModel), "MapTopBgPath", MethodType.Getter),
                new(typeof(ActModel), "MapMidBgPath", MethodType.Getter),
                new(typeof(ActModel), "MapBotBgPath", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches to the matching <see cref="IModActAssetOverrides" /> map layer path property.
        ///     Dispatches to the matching <c>IModActAssetOverrides</c> map layer 路径 property.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, ActModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_MapTopBgPath" => TryActMapTopBgPath(__instance, ref __result),
                "get_MapMidBgPath" => TryActMapMidBgPath(__instance, ref __result),
                "get_MapBotBgPath" => TryActMapBotBgPath(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryActMapTopBgPath(ActModel instance, ref string result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetActMapTopBgPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.ActMapTopBgPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                instance,
                ref result,
                o => o.CustomMapTopBgPath,
                nameof(IModActAssetOverrides.CustomMapTopBgPath));
        }

        private static bool TryActMapMidBgPath(ActModel instance, ref string result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetActMapMidBgPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.ActMapMidBgPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                instance,
                ref result,
                o => o.CustomMapMidBgPath,
                nameof(IModActAssetOverrides.CustomMapMidBgPath));
        }

        private static bool TryActMapBotBgPath(ActModel instance, ref string result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetActMapBotBgPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.ActMapBotBgPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModActAssetOverrides>(
                instance,
                ref result,
                o => o.CustomMapBotBgPath,
                nameof(IModActAssetOverrides.CustomMapBotBgPath));
        }
    }

    /// <summary>
    ///     Patches <c>EventModel.BackgroundScenePath</c> so preloads and <see cref="EventModel.CreateBackgroundScene" /> use
    ///     <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> instead of the synthetic
    ///     <c>events/background_scenes/&lt;id&gt;.tscn</c> path (which mod packs usually do not ship).
    ///     修补 <c>EventModel.BackgroundScenePath</c>，让预加载和 <c>EventModel.CreateBackgroundScene</c> 使用
    ///     <c>IModEventAssetOverrides.CustomBackgroundScenePath</c>，而不是通常不会随 mod 打包的合成路径
    ///     <c>events/background_scenes/&lt;id&gt;.tscn</c>。
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
            return [new(typeof(EventModel), "BackgroundScenePath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        ///     当资源存在时，提供 <c>IModEventAssetOverrides.CustomBackgroundScenePath</c>。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetEventBackgroundScenePath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.EventBackgroundScenePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModEventAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateScene" /> for <see cref="IModEventAssetOverrides" />.
    ///     为 <c>IModEventAssetOverrides</c> 补丁 <c>EventModel.CreateScene</c>。
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
        ///     当 the resource exists 时提供 <c>IModEventAssetOverrides.CustomLayoutScenePath</c>。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPackedScenePathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetEventLayoutScenePath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.EventLayoutScenePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUsePackedSceneCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomLayoutScenePath,
                nameof(IModEventAssetOverrides.CustomLayoutScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateInitialPortrait" /> for <see cref="IModEventAssetOverrides" />.
    ///     为 <c>IModEventAssetOverrides</c> 补丁 <c>EventModel.CreateInitialPortrait</c>。
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
        ///     加载 portrait from <c>IModEventAssetOverrides.CustomInitialPortraitPath</c> when valid。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetEventInitialPortraitTexture(__instance, out var externalTexture))
            {
                __result = externalTexture;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseTexture2DFromCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomInitialPortraitPath,
                nameof(IModEventAssetOverrides.CustomInitialPortraitPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateBackgroundScene" /> for <see cref="IModEventAssetOverrides" />.
    ///     为 <c>IModEventAssetOverrides</c> 补丁 <c>EventModel.CreateBackgroundScene</c>。
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
        ///     当资源存在时，提供 <c>IModEventAssetOverrides.CustomBackgroundScenePath</c>。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is IModAncientEventAssetOverrides
                {
                    AncientPresentationAssetProfile: { StageProcedural: not null },
                })
                return true;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetEventBackgroundScene(__instance, out var externalScene))
            {
                __result = externalScene;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUsePackedSceneCacheOverride<IModEventAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBackgroundScenePath,
                nameof(IModEventAssetOverrides.CustomBackgroundScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.HasVfx" /> for mod VFX scene overrides.
    ///     为 mod VFX scene overrides 补丁 <c>EventModel.HasVfx</c>。
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
            return [new(typeof(EventModel), "HasVfx", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns true when <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> resolves to an existing resource.
        ///     返回 true when <c>IModEventAssetOverrides.CustomVfxScenePath</c> resolves to an existing resource。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (ExternalAssetOverrideRegistry.TryGetEventVfxScene(__instance, out var externalVfxScene))
            {
                __result = externalVfxScene != null;
                return false;
            }

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
    ///     为 <c>IModEventAssetOverrides</c> 补丁 <c>EventModel.CreateVfx</c>。
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
        ///     Instantiates <c>IModEventAssetOverrides.自定义Vfx场景路径</c> 当 the packed 场景 exists.
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Node2D __result)
            // ReSharper restore InconsistentNaming
        {
            if (ExternalAssetOverrideRegistry.TryGetEventVfxScene(__instance, out var externalVfxScene))
            {
                __result = externalVfxScene.Instantiate<Node2D>();
                return false;
            }

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
    ///     将自定义事件资源路径追加到 <c>EventModel.GetAssetPaths</c>，用于预加载。
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
        ///     将已解析的覆盖资源路径追加到原版枚举结果之后。
        /// </summary>
        public static void Postfix(EventModel __instance, IRunState runState, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            _ = runState;

            var paths = __result;
            var proceduralAncientStage =
                (__instance as IModAncientEventAssetOverrides)?.AncientPresentationAssetProfile?.StageProcedural;
            var suppressAncientBackgroundScene = __instance.LayoutType == EventLayoutType.Ancient &&
                                                 proceduralAncientStage != null;

            if (suppressAncientBackgroundScene)
            {
                var entry = __instance.Id.Entry.ToLowerInvariant();
                var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                paths = RemovePath(paths, vanillaBg);

                if (__instance is IModEventAssetOverrides proceduralEventOverrides)
                    paths = RemovePath(paths, proceduralEventOverrides.CustomBackgroundScenePath);

                if (ExternalAssetOverrideRegistry.TryGetEventBackgroundScenePath(__instance,
                        out var proceduralExternalBackgroundPath))
                    paths = RemovePath(paths, proceduralExternalBackgroundPath);
            }

            if (!suppressAncientBackgroundScene
                && __instance is IModEventAssetOverrides evo
                && __instance.LayoutType == EventLayoutType.Ancient
                && !string.IsNullOrWhiteSpace(evo.CustomBackgroundScenePath)
                && AssetPathDiagnostics.Exists(evo.CustomBackgroundScenePath, __instance,
                    nameof(IModEventAssetOverrides.CustomBackgroundScenePath)))
            {
                var entry = __instance.Id.Entry.ToLowerInvariant();
                var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                paths = paths.Where(p => p != vanillaBg);
            }

            if (!suppressAncientBackgroundScene
                && ExternalAssetOverrideRegistry.TryGetEventBackgroundScenePath(__instance,
                    out var externalBackgroundPath) &&
                AssetPathDiagnostics.Exists(externalBackgroundPath, __instance,
                    "ExternalAssetOverrideRegistry.EventBackgroundScenePath"))
            {
                var entry = __instance.Id.Entry.ToLowerInvariant();
                var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                paths = paths.Where(p => p != vanillaBg);
            }

            var externalMerged = CollectExternalEventAssetPaths(__instance, suppressAncientBackgroundScene);

            if (__instance is not IModEventAssetOverrides eventOverrides)
            {
                __result = externalMerged.Length == 0 ? paths : paths.Concat(externalMerged).Distinct().ToArray();
                return;
            }

            var merged = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (eventOverrides.CustomLayoutScenePath, nameof(IModEventAssetOverrides.CustomLayoutScenePath)),
                (eventOverrides.CustomInitialPortraitPath, nameof(IModEventAssetOverrides.CustomInitialPortraitPath)),
                (suppressAncientBackgroundScene ? null : eventOverrides.CustomBackgroundScenePath,
                    nameof(IModEventAssetOverrides.CustomBackgroundScenePath)),
                (eventOverrides.CustomVfxScenePath, nameof(IModEventAssetOverrides.CustomVfxScenePath)));
            if (externalMerged.Length > 0)
                merged = merged.Concat(externalMerged).Distinct().ToArray();

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

            var proceduralStageAssetPaths =
                CollectExistingProceduralStageAssetPaths(__instance, proceduralAncientStage);
            if (proceduralStageAssetPaths.Length > 0)
                merged = [.. merged, .. proceduralStageAssetPaths];

            if (merged.Length == 0)
            {
                __result = paths;
                return;
            }

            __result = paths.Concat(merged).Distinct();
        }

        private static string[] CollectExternalEventAssetPaths(EventModel instance, bool suppressBackgroundScene)
        {
            return ContentAssetOverridePatchHelper.CollectExternalExistingPaths(
                instance,
                (ExternalAssetOverrideRegistry.TryGetEventLayoutScenePath(instance, out var extLayout)
                    ? extLayout
                    : null, "ExternalAssetOverrideRegistry.EventLayoutScenePath"),
                (!suppressBackgroundScene &&
                 ExternalAssetOverrideRegistry.TryGetEventBackgroundScenePath(instance, out var extBackground)
                    ? extBackground
                    : null, "ExternalAssetOverrideRegistry.EventBackgroundScenePath"));
        }

        private static string[] CollectExistingProceduralStageAssetPaths(
            EventModel instance,
            AncientEventStageProceduralVisualSet? stage)
        {
            var paths = AncientEventStageProceduralAssetPaths.Collect(stage);
            if (paths.Length == 0)
                return [];

            return paths
                .Where(path => AssetPathDiagnostics.Exists(
                    path,
                    instance,
                    nameof(AncientEventPresentationAssetProfile.StageProcedural)))
                .ToArray();
        }

        private static IEnumerable<string> RemovePath(IEnumerable<string> paths, string? pathToRemove)
        {
            if (string.IsNullOrWhiteSpace(pathToRemove))
                return paths;

            return paths.Where(path => !string.Equals(path, pathToRemove, StringComparison.Ordinal));
        }
    }

    /// <summary>
    ///     Patches ancient map icon textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <c>IModAncientEventAssetOverrides</c> 补丁 ancient map icon textures。
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
                new(typeof(AncientEventModel), "MapIcon", MethodType.Getter),
                new(typeof(AncientEventModel), "MapIconOutline", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches compressed texture loading to the matching ancient override path.
        ///     Dispatches compressed 纹理 加载ing to the matching ancient override 路径.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, AncientEventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_MapIcon" => TryAncientMapIcon(__instance, ref __result),
                "get_MapIconOutline" => TryAncientMapIconOutline(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryAncientMapIcon(AncientEventModel instance, ref Texture2D result)
        {
            // ReSharper disable once InvertIf
            if (!ContentAssetOverridePatchHelper.TryUseExternalCompressedTexturePathAsTexture2DOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetAncientMapIconPath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.AncientMapIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                IModAncientEventAssetOverrides>(
                instance,
                ref result,
                o => o.CustomMapIconPath,
                nameof(IModAncientEventAssetOverrides.CustomMapIconPath));
        }

        private static bool TryAncientMapIconOutline(AncientEventModel instance, ref Texture2D result)
        {
            // ReSharper disable once InvertIf
            if (!ContentAssetOverridePatchHelper.TryUseExternalCompressedTexturePathAsTexture2DOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetAncientMapIconOutlinePath(instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AncientMapIconOutlinePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                IModAncientEventAssetOverrides>(
                instance,
                ref result,
                o => o.CustomMapIconOutlinePath,
                nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath));
        }
    }

    /// <summary>
    ///     Patches ancient run-history icon textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <c>IModAncientEventAssetOverrides</c> 补丁 ancient run-history icon textures。
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
                new(typeof(AncientEventModel), "RunHistoryIcon", MethodType.Getter),
                new(typeof(AncientEventModel), "RunHistoryIconOutline", MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Dispatches compressed texture loading to the matching ancient override path.
        ///     Dispatches compressed 纹理 加载ing to the matching ancient override 路径.
        /// </summary>
        public static bool Prefix(MethodBase __originalMethod, AncientEventModel __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
        {
            return __originalMethod.Name switch
            {
                "get_RunHistoryIcon" => TryAncientRunHistoryIcon(__instance, ref __result),
                "get_RunHistoryIconOutline" => TryAncientRunHistoryIconOutline(__instance, ref __result),
                _ => true,
            };
        }

        private static bool TryAncientRunHistoryIcon(AncientEventModel instance, ref Texture2D result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalCompressedTexturePathAsTexture2DOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetAncientRunHistoryIconPath(instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AncientRunHistoryIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                IModAncientEventAssetOverrides>(
                instance,
                ref result,
                o => o.CustomRunHistoryIconPath,
                nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconPath));
        }

        private static bool TryAncientRunHistoryIconOutline(AncientEventModel instance, ref Texture2D result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalCompressedTexturePathAsTexture2DOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetAncientRunHistoryIconOutlinePath(instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AncientRunHistoryIconOutlinePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureAsTexture2DOverride<
                IModAncientEventAssetOverrides>(
                instance,
                ref result,
                o => o.CustomRunHistoryIconOutlinePath,
                nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconOutlinePath));
        }
    }

    /// <summary>
    ///     Merges custom map node asset paths into <see cref="AncientEventModel.MapNodeAssetPaths" />.
    ///     Merges 自定义 map node 资源 路径 into <c>AncientEventModel.MapNodeResourcePaths</c>.
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
            return [new(typeof(AncientEventModel), "MapNodeAssetPaths", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends resolved custom map icon paths after the vanilla pair.
        ///     Appends resolved 自定义 map 图标 路径 之后 the 原版 pair.
        /// </summary>
        public static void Postfix(AncientEventModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            var mapIconPath =
                ExternalAssetOverrideRegistry.TryGetAncientMapIconPath(__instance, out var externalMapIconPath)
                    ? externalMapIconPath
                    : (__instance as IModAncientEventAssetOverrides)?.CustomMapIconPath;
            var mapIconOutlinePath = ExternalAssetOverrideRegistry.TryGetAncientMapIconOutlinePath(__instance,
                out var externalMapIconOutlinePath)
                ? externalMapIconOutlinePath
                : (__instance as IModAncientEventAssetOverrides)?.CustomMapIconOutlinePath;
            if (mapIconPath == null && mapIconOutlinePath == null)
                return;

            var entry = __instance.Id.Entry.ToLowerInvariant();
            var vanillaMain = ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{entry}.png");
            var vanillaOutline = ImageHelper.GetImagePath($"packed/map/ancients/ancient_node_{entry}_outline.png");

            var extra = AssetPathDiagnostics.CollectExistingPaths(
                __instance,
                (mapIconPath, nameof(IModAncientEventAssetOverrides.CustomMapIconPath)),
                (mapIconOutlinePath, nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath)));
            if (extra.Length == 0)
                return;

            __result = __result.Where(p => p != vanillaMain && p != vanillaOutline).Concat(extra);
        }
    }

    /// <summary>
    ///     Optional affliction overlay scene path for patches on <see cref="AfflictionModel" />.
    ///     用于 patches on <c>AfflictionModel</c> 的可选 affliction overlay scene 路径。
    /// </summary>
    public interface IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        ///     路径包；默认为空。
        /// </summary>
        AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <summary>
        ///     Overlay packed scene path override.
        ///     Overlay packed 场景 路径 override.
        /// </summary>
        string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel" /> overlay scene path for <see cref="IModAfflictionAssetOverrides" />.
    ///     为 <c>IModAfflictionAssetOverrides</c> 补丁 <c>AfflictionModel</c> overlay scene path。
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
            return [new(typeof(AfflictionModel), "OverlayPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModAfflictionAssetOverrides.CustomOverlayScenePath</c>。
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                __instance, ref __result, o => o.CustomOverlayScenePath,
                nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel.HasOverlay" /> from custom overlay path existence.
    ///     补丁 <c>AfflictionModel.HasOverlay</c> from custom overlay path existence。
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
            return [new(typeof(AfflictionModel), "HasOverlay", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Resolves the custom overlay path then sets boolean availability from resource existence.
        ///     解析 the custom overlay path then sets boolean availability from resource existence。
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene))
            {
                __result = externalScene != null;
                return false;
            }

            var externalOverlayPath = string.Empty;
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref externalOverlayPath,
                    () => ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayPath"))
            {
                __result = true;
                return false;
            }

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
    ///     补丁 <c>AfflictionModel.CreateOverlay</c> to instantiate mod overlay scenes when configured。
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
        ///     Instantiates <c>IModAfflictionAssetOverrides.CustomOverlayScenePath</c> 当 the packed 场景 exists.
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene))
            {
                __result = externalScene.Instantiate<Control>();
                return false;
            }

            var externalOverlayPath = string.Empty;
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref externalOverlayPath,
                    () => ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayPath"))
            {
                __result = ResourceLoader.Load<PackedScene>(externalOverlayPath).Instantiate<Control>();
                return false;
            }

            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            if (!AssetPathDiagnostics.Exists(path, __instance,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }
    }

    /// <summary>
    ///     Optional enchantment icon path for patches on <see cref="EnchantmentModel" />.
    ///     用于 patches on <c>EnchantmentModel</c> 的可选 enchantment icon 路径。
    /// </summary>
    public interface IModEnchantmentAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        ///     路径包；默认为空。
        /// </summary>
        EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

        /// <summary>
        ///     Intended icon path override.
        ///     Intended 图标 路径 override.
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     Patches <see cref="EnchantmentModel" /> intended icon path for <see cref="IModEnchantmentAssetOverrides" />.
    ///     为 <c>IModEnchantmentAssetOverrides</c> 补丁 <c>EnchantmentModel</c> intended icon path。
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
            return [new(typeof(EnchantmentModel), "IntendedIconPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEnchantmentAssetOverrides.CustomIconPath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModEnchantmentAssetOverrides.CustomIconPath</c>。
        /// </summary>
        public static bool Prefix(EnchantmentModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetEnchantmentIconPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.EnchantmentIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEnchantmentAssetOverrides>(
                __instance, ref __result, o => o.CustomIconPath,
                nameof(IModEnchantmentAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="PowerModel.ResolvedBigIconPath" /> so preload lists include mod big-icon paths.
    ///     补丁 <c>PowerModel.ResolvedBigIconPath</c> so preload lists include mod big-icon paths。
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
            return [new(typeof(PowerModel), "ResolvedBigIconPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomBigIconPath" /> when the resource exists.
        ///     当 the resource exists 时提供 <c>IModPowerAssetOverrides.CustomBigIconPath</c>。
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
    ///     Implement on a <c>CardPool模型</c> subclass to supply a custom image path 用于 the
    ///     small energy icon rendered inside rich-text card descriptions
    ///     small energy 图标 rendered inside rich-text 卡牌 descriptions
    ///     (e.g. <c>[img]…/winefox_energy_icon.png[/img]</c>).
    ///     (e.g. <c>[img]…/winefox_energy_图标.png[/img]</c>).
    ///     <para />
    ///     The default game path pattern is:
    ///     The default game 路径 pattern is:
    ///     <c>res://images/packed/sprite_fonts/{EnergyColorName}_energy_icon.png</c>.
    ///     Use this interface only when you need a different path.
    ///     使用 this interface only 当 you need a different 路径.
    /// </summary>
    public interface IModTextEnergyIconPool
    {
        /// <summary>
        ///     Custom image path for the small energy icon embedded in rich-text card descriptions.
        ///     custom image path 用于 the small energy 图标 embedded in rich-text 卡牌 descriptions.
        /// </summary>
        string? TextEnergyIconPath { get; }
    }
}
