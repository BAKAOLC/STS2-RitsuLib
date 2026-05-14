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
    ///     由 <see cref="CardModel" /> 上的 content asset Harmony 补丁使用的可选卡牌美术路径。
    /// </summary>
    public interface IModCardAssetOverrides
    {
        /// <summary>
        ///     Path bundle; individual properties usually mirror these fields unless overridden.
        ///     路径包；除非被重写，各个属性通常会映射这些字段。
        /// </summary>
        CardAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override for main portrait image path.
        ///     主肖像图像路径覆盖。
        /// </summary>
        string? CustomPortraitPath { get; }

        /// <summary>
        ///     Override for beta/alternate portrait path.
        ///     beta/备用肖像路径覆盖。
        /// </summary>
        string? CustomBetaPortraitPath { get; }

        /// <summary>
        ///     Override for card frame texture path.
        ///     卡牌边框纹理路径覆盖。
        /// </summary>
        string? CustomFramePath { get; }

        /// <summary>
        ///     Override for portrait border texture path.
        ///     肖像边框纹理路径覆盖。
        /// </summary>
        string? CustomPortraitBorderPath { get; }

        /// <summary>
        ///     Override for small energy icon texture path.
        ///     小型能量图标纹理路径覆盖。
        /// </summary>
        string? CustomEnergyIconPath { get; }

        /// <summary>
        ///     Override for frame <see cref="Material" /> resource path.
        ///     边框 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomFrameMaterialPath { get; }

        /// <summary>
        ///     Override for built-in overlay packed scene path.
        ///     内置覆盖层 packed scene路径覆盖。
        /// </summary>
        string? CustomOverlayScenePath { get; }

        /// <summary>
        ///     Override for banner texture path.
        ///     横幅纹理路径覆盖。
        /// </summary>
        string? CustomBannerTexturePath { get; }

        /// <summary>
        ///     Override for banner material path.
        ///     横幅材质路径覆盖。
        /// </summary>
        string? CustomBannerMaterialPath { get; }
    }

    /// <summary>
    ///     Optional direct frame <see cref="Material" /> override for cards.
    ///     This bypasses resource-path loading and is checked before
    ///     <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" />.
    ///     用于卡牌的可选直接frame <see cref="Material" /> 覆盖。
    ///     这会绕过资源路径加载，并优先于
    ///     <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" /> 检查。
    /// </summary>
    public interface IModCardFrameMaterialOverride
    {
        /// <summary>
        ///     Direct frame material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的边框材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomFrameMaterial => null;
    }

    /// <summary>
    ///     Optional direct banner <see cref="Material" /> override for cards.
    ///     This bypasses resource-path loading and is checked before
    ///     <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" />.
    ///     用于卡牌的可选直接banner <see cref="Material" /> 覆盖。
    ///     这会绕过资源路径加载，并优先于
    ///     <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> 检查。
    /// </summary>
    public interface IModCardBannerMaterialOverride
    {
        /// <summary>
        ///     Direct banner material override.
        ///     Return <c>null</c> to fall back to frame material semantics.
        ///     直接的横幅材质覆盖。
        ///     返回 <c>null</c> 以回退到边框材质语义。
        /// </summary>
        Material? CustomBannerMaterial => null;
    }

    /// <summary>
    ///     Implement this interface on a <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> to directly supply
    ///     a <see cref="Material" /> for card frames in the pool.
    ///     When <see cref="PoolFrameMaterial" /> is non-null, <c>CardFrameMaterialPath</c> is ignored entirely.
    ///     在 <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> 上实现此接口，以直接提供
    ///     牌池中卡牌边框使用的 <see cref="Material" />。
    ///     当 <see cref="PoolFrameMaterial" /> 非 null 时，<c>CardFrameMaterialPath</c> 会被完全忽略。
    /// </summary>
    public interface IModCardPoolFrameMaterial
    {
        /// <summary>
        ///     The material to use for card frames in this pool.
        ///     Return <c>null</c> to fall back to the path-based default.
        ///     此牌池中卡牌边框使用的材质。
        ///     返回 <c>null</c> 以回退到基于路径的默认值。
        /// </summary>
        Material? PoolFrameMaterial { get; }
    }

    /// <summary>
    ///     Optional relic icon paths for Harmony patches on <see cref="RelicModel" />.
    ///     用于 <see cref="RelicModel" /> 的 Harmony 补丁的可选遗物 图标路径。
    /// </summary>
    public interface IModRelicAssetOverrides
    {
        /// <summary>
        ///     Path bundle for relic presentation assets.
        ///     遗物表现资源的路径包。
        /// </summary>
        RelicAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Primary relic icon path override.
        ///     主 遗物 图标路径覆盖。
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Outline icon path override.
        ///     轮廓 图标路径覆盖。
        /// </summary>
        string? CustomIconOutlinePath { get; }

        /// <summary>
        ///     Large relic art path override.
        ///     大型 遗物 art路径覆盖。
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional power icon paths for Harmony patches on <see cref="PowerModel" />.
    ///     用于 <see cref="PowerModel" /> 的 Harmony 补丁的可选能力 图标路径。
    /// </summary>
    public interface IModPowerAssetOverrides
    {
        /// <summary>
        ///     Path bundle for power icons.
        ///     能力图标的路径包。
        /// </summary>
        PowerAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Standard icon path override.
        ///     标准 图标路径覆盖。
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Large icon path override.
        ///     大型 图标路径覆盖。
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     Optional orb icon and visuals scene paths for Harmony patches on <see cref="OrbModel" />.
    ///     用于 <see cref="OrbModel" /> 上 Harmony 补丁的可选充能球图标和视觉场景路径。
    /// </summary>
    public interface IModOrbAssetOverrides
    {
        /// <summary>
        ///     Path bundle for orb HUD and combat visuals.
        ///     充能球 HUD 和战斗视觉的路径包。
        /// </summary>
        OrbAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Orb icon texture path override.
        ///     充能球 图标 纹理路径覆盖。
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Orb combat visuals scene path override.
        ///     充能球 combat 视觉场景路径覆盖。
        /// </summary>
        string? CustomVisualsScenePath { get; }
    }

    /// <summary>
    ///     Default act asset override surface; concrete mods typically use <see cref="ModActTemplate" /> instead of
    ///     implementing this directly.
    ///     默认章节资源覆盖接口；具体 mod 通常使用 <see cref="ModActTemplate" />，而不是
    ///     直接实现此接口。
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
        ///     Main 章节 背景场景路径覆盖。
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Rest site background scene path override.
        ///     休息处 背景场景路径覆盖。
        /// </summary>
        string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;

        /// <summary>
        ///     Map top-layer background image path override.
        ///     地图顶层背景图像路径覆盖。
        /// </summary>
        string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;

        /// <summary>
        ///     Map middle-layer background image path override.
        ///     地图中层背景图像路径覆盖。
        /// </summary>
        string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;

        /// <summary>
        ///     Map bottom-layer background image path override.
        ///     地图底层背景图像路径覆盖。
        /// </summary>
        string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;

        /// <summary>
        ///     Treasure chest Spine resource path override.
        ///     宝箱 Spine 资源路径覆盖。
        /// </summary>
        string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;

        /// <summary>
        ///     Optional <c>res://</c> directory for combat background parallax layers (same <c>_bg_</c> / <c>_fg_</c> naming as
        ///     vanilla). When set, <see cref="ActModel.GenerateBackgroundAssets" /> scans this folder instead of
        ///     <c>scenes/backgrounds/&lt;act&gt;/layers</c>.
        ///     战斗背景视差图层的可选 <c>res://</c> 目录（命名方式与原版相同，使用 <c>_bg_</c> / <c>_fg_</c>）。
        ///     设置后，<see cref="ActModel.GenerateBackgroundAssets" /> 会扫描此文件夹，而不是
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;
    }

    /// <summary>
    ///     Optional event layout, portrait, background, and VFX scene paths; use <see cref="ModEventTemplate" /> or implement
    ///     on a mod <see cref="EventModel" />.
    ///     on a mod <c>EventModel</c>.
    ///     可选事件布局、肖像、背景和 VFX 场景路径；使用 <see cref="ModEventTemplate" />，或在 mod
    ///     <see cref="EventModel" /> 上实现。
    ///     在 mod <c>EventModel</c> 上实现。
    /// </summary>
    public interface IModEventAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        ///     路径包；除非被覆盖，否则 <c>Custom*</c> 属性会映射这些字段。
        /// </summary>
        EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene for <c>EventModel.CreateScene</c> (full layout root).
        ///     <c>EventModel.CreateScene</c> 的 packed scene 覆盖（完整布局根节点）。
        /// </summary>
        string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <summary>
        ///     Override texture path for <c>EventModel.CreateInitialPortrait</c>.
        ///     <c>EventModel.CreateInitialPortrait</c> 的纹理路径覆盖。
        /// </summary>
        string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateBackgroundScene</c>.
        ///     <c>EventModel.CreateBackgroundScene</c> 的 packed scene 路径覆盖。
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Override packed scene path for <c>EventModel.CreateVfx</c> / <c>HasVfx</c>.
        ///     <c>EventModel.CreateVfx</c> / <c>HasVfx</c> 的 packed scene 路径覆盖。
        /// </summary>
        string? CustomVfxScenePath => AssetProfile.VfxScenePath;
    }

    /// <summary>
    ///     Extends <see cref="IModEventAssetOverrides" /> with ancient map and run-history icon paths; use
    ///     <see cref="ModAncientEventTemplate" /> or implement on a mod <see cref="AncientEventModel" />.
    ///     扩展 <see cref="IModEventAssetOverrides" />，增加远古地图和跑局历史图标路径；使用
    ///     <see cref="ModAncientEventTemplate" />，或在 mod <see cref="AncientEventModel" />.
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
        ///     <c>AncientEventModel.MapIcon</c> 的覆盖。
        /// </summary>
        string? CustomMapIconPath => AncientPresentationAssetProfile?.MapIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.MapIconOutline</c>.
        ///     <c>AncientEventModel.MapIconOutline</c> 的覆盖。
        /// </summary>
        string? CustomMapIconOutlinePath => AncientPresentationAssetProfile?.MapIconOutlinePath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIcon</c>.
        ///     <c>AncientEventModel.RunHistoryIcon</c> 的覆盖。
        /// </summary>
        string? CustomRunHistoryIconPath => AncientPresentationAssetProfile?.RunHistoryIconPath;

        /// <summary>
        ///     Override for <c>AncientEventModel.RunHistoryIconOutline</c>.
        ///     <c>AncientEventModel.RunHistoryIconOutline</c> 的覆盖。
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AncientPresentationAssetProfile?.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     Optional epoch timeline portrait paths; use <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" /> or
    ///     implement on a mod <see cref="MegaCrit.Sts2.Core.Timeline.EpochModel" />.
    ///     可选纪元时间线肖像路径；使用 <see cref="STS2RitsuLib.Timeline.Scaffolding.ModEpochTemplate" /> or
    /// </summary>
    public interface IModEpochAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        ///     路径包；除非被覆盖，否则 <c>Custom*</c> 属性会映射这些字段。
        /// </summary>
        EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <summary>
        ///     Override for <c>EpochModel.PackedPortraitPath</c> (atlas sprite entry).
        ///     <c>EpochModel.PackedPortraitPath</c> 的覆盖（图集 sprite 条目）。
        /// </summary>
        string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <summary>
        ///     Override for <c>EpochModel.BigPortraitPath</c> (large portrait texture).
        ///     <c>EpochModel.BigPortraitPath</c> 的覆盖（大型肖像纹理）。
        /// </summary>
        string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }

    /// <summary>
    ///     Patches <see cref="EpochModel" /> portrait path getters for <see cref="IModEpochAssetOverrides" />.
    ///     为 <see cref="IModEpochAssetOverrides" /> 修补<see cref="EpochModel" /> portrait 路径 getter。
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
        ///     按 packed atlas 与大型肖像路径分派字符串覆盖。
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
    ///     为 <see cref="IModCardAssetOverrides" /> 修补<see cref="CardModel" /> portrait 路径 getter。
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
        ///     根据被修补的 getter 分派到肖像或 beta 肖像覆盖。
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
    ///     修补肖像可用性标志，使来自 <see cref="IModCardAssetOverrides" /> 的自定义路径生效。
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
        ///     根据对应自定义肖像路径是否存在于磁盘上来设置布尔可用性。
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
    ///     为 mod 路径覆盖修补卡牌框、肖像边框和能量图标纹理 getter。
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
        ///     存在时从匹配的 <see cref="IModCardAssetOverrides" /> 路径加载纹理。
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
    ///     修补 <see cref="CardModel" /> 边框材质解析，以支持自定义 <c>.tres</c> 路径。
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
        ///     有效时从 <see cref="IModCardAssetOverrides.CustomFrameMaterialPath" /> 加载<see cref="Material" />。
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
    ///     lookup.
    ///     修补池级边框材质，使 <see cref="IModCardPoolFrameMaterial.PoolFrameMaterial" /> 可以替换路径
    ///     查找。
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
        ///     当池实现 <see cref="IModCardPoolFrameMaterial" /> 时，返回池的内联材质。
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
    ///     修补<see cref="CardModel.AllPortraitPaths" />，使自定义 portrait/beta 路径 participate in 预加载 列表。
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
        ///     当卡牌实现覆盖时，用已验证的自定义肖像路径替换可枚举集合。
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
    ///     为实现 <see cref="IModCardAssetOverrides" /> 的卡牌修补内置覆盖层场景路径。
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
        ///     当资源存在时提供 <see cref="IModCardAssetOverrides.CustomOverlayScenePath" />。
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
    ///     使用自定义覆盖层场景路径的存在性检查来修补 <see cref="CardModel.HasBuiltInOverlay" />。
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
        ///     resource.
        ///     当 <see cref="IModCardAssetOverrides.CustomOverlayScenePath" /> 解析到现有
        ///     资源时设置为 <c>true</c>。
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
    ///     修补 <see cref="CardModel.CreateOverlay" />，在配置后实例化 mod 覆盖层场景。
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
        ///     当 packed scene 存在时实例化 <see cref="IModCardAssetOverrides.CustomOverlayScenePath" />。
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
    ///     <c>Icon</c> / <c>IconOutline</c> loaders) for mod-character per–relic-id paths (owner match) first, then
    ///     <see cref="IModRelicAssetOverrides" />.
    ///     修补 <see cref="RelicModel.IconPath" /> 和 packed atlas 图标/轮廓路径 getter（原版
    ///     <c>Icon</c> / <c>IconOutline</c> 加载器使用）：优先使用 mod 角色按遗物 id 的路径（所有者匹配），然后使用
    ///     <see cref="IModRelicAssetOverrides" />。
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
        ///     applicable, then <see cref="IModRelicAssetOverrides" /> custom paths.
        ///     当条件满足时提供 <see cref="IModCharacterAssetOverrides.TryGetVanillaRelicVisualOverrideForOwnedRelic" />
        ///     applicable, then <see cref="IModRelicAssetOverrides" /> 自定义 路径。
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
    ///     <see cref="IModRelicAssetOverrides" />.
    ///     修补遗物图标纹理 getter（主图、轮廓、大图）：优先使用 mod 角色拥有的遗物覆盖，然后使用
    ///     <see cref="IModRelicAssetOverrides" />。
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
        ///     优先将纹理加载分派到 mod 角色覆盖，然后使用 mod 遗物覆盖。
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
    ///     <c>Icon</c> loader) for <see cref="IModPowerAssetOverrides" />.
    ///     为 <see cref="IModPowerAssetOverrides" /> 修补 <see cref="PowerModel.IconPath" /> 和
    ///     <see cref="PowerModel.PackedIconPath" />（原版
    ///     <c>Icon</c> 加载器使用）。
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
        ///     当资源存在时提供 <see cref="IModPowerAssetOverrides.CustomIconPath" />。
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
    ///     为 mod 路径覆盖修补能力标准图标和大图标纹理。
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
        ///     <see cref="IModPowerAssetOverrides.CustomBigIconPath" />.
        ///     分派到 <see cref="IModPowerAssetOverrides.CustomIconPath" /> 或
        ///     <see cref="IModPowerAssetOverrides.CustomBigIconPath" />。
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
    ///     为 <see cref="IModOrbAssetOverrides" /> 修补充能球 HUD 图标 (<see cref="CompressedTexture2D" />)。
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
        ///     有效时从 <see cref="IModOrbAssetOverrides.CustomIconPath" /> 加载compressed 图标 纹理。
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
    ///     为战斗表现覆盖修补充能球视觉场景路径。
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
        ///     当资源存在时提供 <see cref="IModOrbAssetOverrides.CustomVisualsScenePath" />。
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
    ///     修补 <see cref="OrbModel.AssetPaths" />，使自定义图标和视觉路径出现在预加载枚举中。
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
        ///     从 <see cref="IModOrbAssetOverrides" /> 收集现有的图标和视觉场景路径。
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
    ///     <c>Image</c> / preload) for <see cref="IModPotionAssetOverrides" />.
    ///     为 <see cref="IModPotionAssetOverrides" /> 修补药水图像和轮廓路径 getter（包括原版
    ///     <c>Image</c> / 预加载使用的 packed atlas 路径 getter）。
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
        ///     <see cref="IModPotionAssetOverrides.CustomOutlinePath" />.
        ///     分派到 <see cref="IModPotionAssetOverrides.CustomImagePath" /> 或
        ///     <see cref="IModPotionAssetOverrides.CustomOutlinePath" />。
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
    ///     为 mod 路径覆盖修补药水图像和轮廓纹理。
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
        ///     从匹配的 <see cref="IModPotionAssetOverrides" /> 路径属性加载纹理。
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
    ///     为实现 <see cref="IModCardAssetOverrides" /> 的卡牌修补跑局摘要横幅纹理。
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
        ///     有效时从 <see cref="IModCardAssetOverrides.CustomBannerTexturePath" /> 加载横幅纹理。
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
    ///     为 mod 卡牌修补横幅 <see cref="Material" /> 解析。
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
        ///     有效时从 <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> 加载材质。
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
    ///     为 <see cref="IModActAssetOverrides" /> 修补章节 主背景场景 路径。
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
        ///     当资源存在时提供 <see cref="IModActAssetOverrides.CustomBackgroundScenePath" />。
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
    ///     为 mod 章节修补休息处背景场景路径。
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
        ///     当资源存在时提供 <see cref="IModActAssetOverrides.CustomRestSiteBackgroundPath" />。
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
    ///     为 mod 章节修补章节地图图层背景图像路径（top/mid/bottom）。
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
        ///     分派到匹配的 <see cref="IModActAssetOverrides" /> map layer 路径属性。
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
    ///     <c>events/background_scenes/&lt;id&gt;.tscn</c>。
    ///     修补 <c>EventModel.BackgroundScenePath</c>，使预加载和 <see cref="EventModel.CreateBackgroundScene" /> 使用
    ///     <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" />，而不是合成的
    ///     <c>events/background_scenes/&lt;id&gt;.tscn</c> 路径（mod 包通常不会提供该路径）。
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
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" />。
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
    ///     为 <see cref="IModEventAssetOverrides" /> 修补<see cref="EventModel.CreateScene" />。
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
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomLayoutScenePath" />。
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
    ///     为 <see cref="IModEventAssetOverrides" /> 修补<see cref="EventModel.CreateInitialPortrait" />。
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
        ///     有效时从 <see cref="IModEventAssetOverrides.CustomInitialPortraitPath" /> 加载portrait。
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
    ///     为 <see cref="IModEventAssetOverrides" /> 修补<see cref="EventModel.CreateBackgroundScene" />。
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
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" />。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is IModAncientEventAssetOverrides
                {
                    AncientPresentationAssetProfile.StageProcedural: not null,
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
    ///     为 mod VFX 场景覆盖修补 <see cref="EventModel.HasVfx" />。
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
        ///     当 <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> 解析到现有资源时返回 true。
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
    ///     为 <see cref="IModEventAssetOverrides" /> 修补<see cref="EventModel.CreateVfx" />。
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
        ///     当 packed scene 存在时实例化 <see cref="IModEventAssetOverrides.CustomVfxScenePath" />。
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
    ///     将自定义事件资源路径追加到 <see cref="EventModel.GetAssetPaths" />，用于预加载。
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

            switch (suppressAncientBackgroundScene)
            {
                case true:
                {
                    var entry = __instance.Id.Entry.ToLowerInvariant();
                    var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                    paths = RemovePath(paths, vanillaBg);

                    if (__instance is IModEventAssetOverrides proceduralEventOverrides)
                        paths = RemovePath(paths, proceduralEventOverrides.CustomBackgroundScenePath);

                    if (ExternalAssetOverrideRegistry.TryGetEventBackgroundScenePath(__instance,
                            out var proceduralExternalBackgroundPath))
                        paths = RemovePath(paths, proceduralExternalBackgroundPath);
                    break;
                }
                case false
                    when __instance is IModEventAssetOverrides evo
                         && __instance.LayoutType == EventLayoutType.Ancient
                         && !string.IsNullOrWhiteSpace(evo.CustomBackgroundScenePath)
                         && AssetPathDiagnostics.Exists(evo.CustomBackgroundScenePath, __instance,
                             nameof(IModEventAssetOverrides.CustomBackgroundScenePath)):
                {
                    var entry = __instance.Id.Entry.ToLowerInvariant();
                    var vanillaBg = SceneHelper.GetScenePath($"events/background_scenes/{entry}");
                    paths = paths.Where(p => p != vanillaBg);
                    break;
                }
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
            return string.IsNullOrWhiteSpace(pathToRemove)
                ? paths
                : paths.Where(path => !string.Equals(path, pathToRemove, StringComparison.Ordinal));
        }
    }

    /// <summary>
    ///     Patches ancient map icon textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <see cref="IModAncientEventAssetOverrides" /> 修补远古事件地图图标纹理。
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
        ///     将压缩纹理加载分派到匹配的远古事件覆盖路径。
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
    ///     为 <see cref="IModAncientEventAssetOverrides" /> 修补远古事件跑局历史图标纹理。
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
        ///     将压缩纹理加载分派到匹配的远古事件覆盖路径。
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
    ///     将自定义地图节点资源路径合并到 <see cref="AncientEventModel.MapNodeAssetPaths" />。
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
        ///     在原版路径对之后追加已解析的自定义地图图标路径。
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
    ///     用于 <see cref="AfflictionModel" /> 补丁的可选苦痛 overlay 场景路径。
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
        ///     Overlay packed 场景路径覆盖。
        /// </summary>
        string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel" /> overlay scene path for <see cref="IModAfflictionAssetOverrides" />.
    ///     为 <see cref="IModAfflictionAssetOverrides" /> 修补<see cref="AfflictionModel" /> overlay 场景 路径。
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
        ///     当资源存在时提供 <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" />。
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
    ///     根据自定义 overlay 路径 existence修补<see cref="AfflictionModel.HasOverlay" />。
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
        ///     解析自定义覆盖层路径，然后根据资源是否存在来设置布尔可用性。
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
    ///     修补 <see cref="AfflictionModel.CreateOverlay" />，在配置后实例化 mod 覆盖层场景。
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
        ///     当 packed scene 存在时实例化 <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" />。
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
    ///     用于 <see cref="EnchantmentModel" /> 补丁的可选附魔 图标路径。
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
        ///     Intended 图标路径覆盖。
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     Patches <see cref="EnchantmentModel" /> intended icon path for <see cref="IModEnchantmentAssetOverrides" />.
    ///     为 <see cref="IModEnchantmentAssetOverrides" /> 修补<see cref="EnchantmentModel" /> intended 图标 路径。
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
        ///     当资源存在时提供 <see cref="IModEnchantmentAssetOverrides.CustomIconPath" />。
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
    ///     修补<see cref="PowerModel.ResolvedBigIconPath" />，使预加载 列表 include mod big-图标 路径。
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
        ///     当资源存在时提供 <see cref="IModPowerAssetOverrides.CustomBigIconPath" />。
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
    ///     在 <see cref="CardPoolModel" /> 子类上实现此接口，以提供自定义图像路径，用于
    ///     富文本卡牌描述中渲染的小型能量图标
    ///     （例如 <c>[img]…/winefox_energy_icon.png[/img]</c>）。
    ///     <para />
    ///     游戏默认路径模式为：
    ///     仅在需要不同路径时使用此接口。
    /// </summary>
    public interface IModTextEnergyIconPool
    {
        /// <summary>
        ///     Custom image path for the small energy icon embedded in rich-text card descriptions.
        ///     嵌入富文本卡牌描述的小型能量图标的自定义图像路径。
        /// </summary>
        string? TextEnergyIconPath { get; }
    }
}
