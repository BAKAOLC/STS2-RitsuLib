using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static class ContentAssetOverridePatchHelper
    {
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

        internal static bool TryUseTextureOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            if (!GodotResourcePath.TryLoad<Texture2D>(path, out var texture))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
            return false;
        }

        internal static bool TryUseCompressedTextureOverride<TOverrides>(
            object instance,
            ref CompressedTexture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            if (!GodotResourcePath.TryLoad<CompressedTexture2D>(path, out var texture))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(CompressedTexture2D));
                return true;
            }

            __result = texture;
            return false;
        }

        internal static bool TryUseMaterialOverride<TOverrides>(
            object instance,
            ref Material __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            if (!GodotResourcePath.TryLoad<Material>(path, out var material))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(Material));
                return true;
            }

            __result = material;
            return false;
        }

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

        internal static bool TryUseExistenceOverride(object instance, string? path, string memberName,
            ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            __result = AssetPathDiagnostics.Exists(path, instance, memberName);
            return false;
        }

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

        internal static bool TryUseExternalPackedScenePathOverride(
            object instance,
            ref PackedScene __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path))
                return true;

            var scene = ResolveScene(path);
            if (scene == null)
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(PackedScene));
                return true;
            }

            if (!IsPackedSceneOverrideAvailable(instance, scene, memberName, $"path '{path}'"))
                return true;

            __result = scene;
            return false;
        }

        internal static bool TryUseExternalCompressedTexturePathAsTexture2DOverride(
            object instance,
            ref Texture2D __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!GodotResourcePath.TryLoad<Texture2D>(path, out var texture))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
            return false;
        }

        internal static string[] CollectExternalExistingPaths(
            object instance,
            params (string? Path, string MemberName)[] candidates)
        {
            return AssetPathDiagnostics.CollectExistingPaths(instance, candidates);
        }

        private static bool TryGetDefinedPath<TOverrides>(
            object instance,
            Func<TOverrides, string?> selector,
            out string path)
            where TOverrides : class
        {
            path = string.Empty;

            if (instance is not TOverrides overrides)
                return false;

            var candidate = selector(overrides);
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            path = candidate;
            return true;
        }

        /// <summary>
        ///     Resolves a <see cref="PackedScene" /> for an already-defined path, preferring the preload cache and
        ///     falling back to <see cref="ResourceLoader" /> for paths that were never preloaded. Iterates the same
        ///     candidate set as <see cref="GodotResourcePath.TryLoad{T}" /> so <c>uid://</c> / remapped inputs resolve.
        ///     为已定义的路径解析 <see cref="PackedScene" />，优先使用预加载缓存，未预加载的路径回退到
        ///     <see cref="ResourceLoader" />。与 <see cref="GodotResourcePath.TryLoad{T}" /> 使用相同的候选集合，
        ///     以便 <c>uid://</c> / 重映射输入也能解析。
        /// </summary>
        internal static PackedScene? ResolveScene(string definedPath)
        {
            foreach (var candidate in GodotResourcePath.EnumerateCandidatePaths(definedPath))
            {
                if (!ResourceLoader.Exists(candidate))
                    continue;

                if (TryGetCachedResource<PackedScene>(candidate, out var cached))
                    return cached;

                if (ResourceLoader.Load(candidate) is PackedScene scene)
                    return scene;
            }

            return null;
        }

        /// <summary>
        ///     Resolves a <see cref="Texture2D" /> for an already-defined path, preferring the preload cache and
        ///     falling back to <see cref="ResourceLoader" /> for paths that were never preloaded.
        ///     为已定义的路径解析 <see cref="Texture2D" />，优先使用预加载缓存，未预加载的路径回退到
        ///     <see cref="ResourceLoader" />。
        /// </summary>
        internal static Texture2D? ResolveTexture2D(string definedPath)
        {
            foreach (var candidate in GodotResourcePath.EnumerateCandidatePaths(definedPath))
            {
                if (!ResourceLoader.Exists(candidate))
                    continue;

                if (TryGetCachedResource<Texture2D>(candidate, out var cached))
                    return cached;

                if (ResourceLoader.Load(candidate) is Texture2D texture)
                    return texture;
            }

            return null;
        }

        private static bool TryGetCachedResource<TResource>(string path, out TResource resource)
            where TResource : Resource
        {
            resource = null!;

            try
            {
                var cached = typeof(TResource) == typeof(PackedScene)
                    ? PreloadManager.Cache.GetScene(path)
                    : typeof(TResource) == typeof(Texture2D)
                        ? PreloadManager.Cache.GetTexture2D(path)
                        : ResourceLoader.Load(path);

                if (cached is not TResource typed)
                    return false;

                resource = typed;
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Warns about a <em>defined</em> override path that could not be used: either it does not resolve at all
        ///     (logged as a missing path by <see cref="AssetPathDiagnostics.Exists" />) or it resolves but cannot be
        ///     loaded as the expected type. Callers must only reach this after confirming the author defined a path,
        ///     so an undefined override never produces a warning.
        ///     对一个<em>已定义</em>但无法使用的覆盖路径发出警告：要么完全无法解析（由
        ///     <see cref="AssetPathDiagnostics.Exists" /> 记为缺失），要么能解析但无法按期望类型加载。调用方必须在
        ///     确认作者已定义路径之后才会到达此处，因此未定义的覆盖永远不会产生警告。
        /// </summary>
        internal static void WarnOverrideUnavailable(object instance, string memberName, string path,
            string expectedType)
        {
            if (AssetPathDiagnostics.Exists(path, instance, memberName))
                LogLoadFailure(instance, memberName, path, expectedType);
        }

        internal static void LogLoadFailure(object instance, string memberName, string path, string expectedType)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Assets] Resource exists but failed to load as {expectedType} for {DescribeOwner(instance)}.{memberName}: '{path}'. Falling back to the base asset.");
        }

        internal static bool IsPackedSceneOverrideAvailable(object instance, PackedScene? scene, string memberName,
            string source)
        {
            if (scene != null && GodotObject.IsInstanceValid(scene))
                return true;

            RitsuLibFramework.Logger.Warn(
                $"[Assets] PackedScene override is invalid for {DescribeOwner(instance)}.{memberName} from {source}. Ignoring the override.");
            return false;
        }

        internal static bool IsPackedScenePathOverrideAvailable(object instance, string path, string memberName)
        {
            var scene = ResolveScene(path);
            // ReSharper disable once InvertIf
            if (scene == null)
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(PackedScene));
                return false;
            }

            return IsPackedSceneOverrideAvailable(instance, scene, memberName, $"path '{path}'");
        }

        internal static bool TryInstantiatePackedSceneOverride(object instance, PackedScene? scene, string memberName,
            string source, out Control result)
        {
            return TryInstantiatePackedSceneOverride<Control>(instance, scene, memberName, source, out result);
        }

        internal static bool TryInstantiatePackedSceneOverride<TNode>(object instance, PackedScene? scene,
            string memberName, string source, out TNode result)
            where TNode : Node
        {
            result = null!;
            if (!IsPackedSceneOverrideAvailable(instance, scene, memberName, source))
                return false;

            try
            {
                var node = scene!.Instantiate();
                if (node is TNode typed)
                {
                    result = typed;
                    return true;
                }

                if (node != null && GodotObject.IsInstanceValid(node))
                    node.QueueFree();

                RitsuLibFramework.Logger.Warn(
                    $"[Assets] PackedScene override for {DescribeOwner(instance)}.{memberName} from {source} instantiated '{node?.GetType().FullName ?? "null"}' instead of {typeof(TNode).Name}. Falling back to the base asset.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Failed to instantiate PackedScene override for {DescribeOwner(instance)}.{memberName} from {source}: {ex.Message}. Falling back to the base asset.");
            }

            return false;
        }

        internal static bool TryInstantiatePackedScenePathOverride(object instance, string path, string memberName,
            out Control result)
        {
            return TryInstantiatePackedScenePathOverride<Control>(instance, path, memberName, out result);
        }

        internal static bool TryInstantiatePackedScenePathOverride<TNode>(object instance, string path,
            string memberName, out TNode result)
            where TNode : Node
        {
            result = null!;

            var scene = ResolveScene(path);
            // ReSharper disable once InvertIf
            if (scene == null)
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(PackedScene));
                return false;
            }

            return TryInstantiatePackedSceneOverride(instance, scene, memberName, $"path '{path}'", out result);
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

        internal static bool TryUsePackedSceneCacheOverride<TOverrides>(
            object instance,
            ref PackedScene __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            var scene = ResolveScene(path);
            if (scene == null)
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(PackedScene));
                return true;
            }

            if (!IsPackedSceneOverrideAvailable(instance, scene, memberName, $"path '{path}'"))
                return true;

            __result = scene;
            return false;
        }

        internal static bool TryUseTexture2DFromCacheOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            var texture = ResolveTexture2D(path);
            if (texture == null)
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
            return false;
        }

        internal static bool TryUseCompressedTextureAsTexture2DOverride<TOverrides>(
            object instance,
            ref Texture2D __result,
            Func<TOverrides, string?> selector,
            string memberName)
            where TOverrides : class
        {
            if (!TryGetDefinedPath(instance, selector, out var path))
                return true;

            if (!GodotResourcePath.TryLoad<Texture2D>(path, out var texture))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(Texture2D));
                return true;
            }

            __result = texture;
            return false;
        }
    }

    /// <summary>
    ///     Optional card art paths and materials consumed by content asset Harmony patches.
    ///     由 content asset Harmony 补丁使用的可选卡牌美术路径和材质。
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
        ///     Override for card portrait <see cref="Material" /> resource path.
        ///     卡图 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomPortraitMaterialPath => AssetProfile.PortraitMaterialPath;

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
        ///     Override for ancient card border texture path.
        ///     Ancient 卡牌边框纹理路径覆盖。
        /// </summary>
        string? CustomAncientBorderPath => AssetProfile.AncientBorderPath;

        /// <summary>
        ///     Override for ancient card text background texture path.
        ///     Ancient 卡牌文本背景纹理路径覆盖。
        /// </summary>
        string? CustomAncientTextBgPath => AssetProfile.AncientTextBgPath;

        /// <summary>
        ///     Override for ancient card title banner texture path.
        ///     Ancient 卡牌卡名横幅纹理路径覆盖。
        /// </summary>
        string? CustomAncientBannerPath => AssetProfile.AncientBannerPath;

        /// <summary>
        ///     Optional visual layout override. Defaults to the base-game rarity-driven card layout.
        ///     可选视觉布局覆盖。默认使用原版按稀有度驱动的卡牌布局。
        /// </summary>
        CardVisualStyle CustomVisualStyle => AssetProfile.VisualStyle;

        /// <summary>
        ///     Override for frame <see cref="Material" /> resource path.
        ///     边框 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomFrameMaterialPath { get; }

        /// <summary>
        ///     Override for portrait border <see cref="Material" /> resource path.
        ///     肖像边框 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomPortraitBorderMaterialPath => AssetProfile.PortraitBorderMaterialPath;

        /// <summary>
        ///     Override for energy icon <see cref="Material" /> resource path.
        ///     能量图标 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomEnergyIconMaterialPath => AssetProfile.EnergyIconMaterialPath;

        /// <summary>
        ///     Override for ancient card border <see cref="Material" /> resource path.
        ///     Ancient 卡牌边框 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomAncientBorderMaterialPath => AssetProfile.AncientBorderMaterialPath;

        /// <summary>
        ///     Override for ancient card text background <see cref="Material" /> resource path.
        ///     Ancient 卡牌文本背景 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomAncientTextBgMaterialPath => AssetProfile.AncientTextBgMaterialPath;

        /// <summary>
        ///     Override for ancient card title banner <see cref="Material" /> resource path.
        ///     Ancient 卡牌卡名横幅 <see cref="Material" /> 资源路径覆盖。
        /// </summary>
        string? CustomAncientBannerMaterialPath => AssetProfile.AncientBannerMaterialPath;

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
    ///     Optional direct portrait <see cref="Material" /> override for cards.
    ///     This is applied to the portrait TextureRect after <see cref="NCard" /> reloads its vanilla visuals.
    ///     用于卡牌的可选直接卡图 <see cref="Material" /> 覆盖。
    ///     会在 <see cref="NCard" /> 重载原版视觉后应用到卡图 TextureRect。
    /// </summary>
    public interface IModCardPortraitMaterialOverride
    {
        /// <summary>
        ///     Direct portrait material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的卡图材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomPortraitMaterial => null;
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
    ///     Optional direct portrait border <see cref="Material" /> override for cards.
    ///     用于卡牌的可选直接肖像边框 <see cref="Material" /> 覆盖。
    /// </summary>
    public interface IModCardPortraitBorderMaterialOverride
    {
        /// <summary>
        ///     Direct portrait border material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的肖像边框材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomPortraitBorderMaterial => null;
    }

    /// <summary>
    ///     Optional direct energy icon <see cref="Material" /> override for cards.
    ///     用于卡牌的可选直接能量图标 <see cref="Material" /> 覆盖。
    /// </summary>
    public interface IModCardEnergyIconMaterialOverride
    {
        /// <summary>
        ///     Direct energy icon material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的能量图标材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomEnergyIconMaterial => null;
    }

    /// <summary>
    ///     Optional direct ancient card border <see cref="Material" /> override for cards.
    ///     用于 ancient 卡牌的可选直接边框 <see cref="Material" /> 覆盖。
    /// </summary>
    public interface IModCardAncientBorderMaterialOverride
    {
        /// <summary>
        ///     Direct ancient card border material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的 ancient 卡牌边框材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomAncientBorderMaterial => null;
    }

    /// <summary>
    ///     Optional direct ancient card text background <see cref="Material" /> override for cards.
    ///     用于 ancient 卡牌的可选直接文本背景 <see cref="Material" /> 覆盖。
    /// </summary>
    public interface IModCardAncientTextBgMaterialOverride
    {
        /// <summary>
        ///     Direct ancient card text background material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的 ancient 卡牌文本背景材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomAncientTextBgMaterial => null;
    }

    /// <summary>
    ///     Optional direct ancient card title banner <see cref="Material" /> override for cards.
    ///     用于 ancient 卡牌的可选直接卡名横幅 <see cref="Material" /> 覆盖。
    /// </summary>
    public interface IModCardAncientBannerMaterialOverride
    {
        /// <summary>
        ///     Direct ancient card title banner material override.
        ///     Return <c>null</c> to continue with other override layers.
        ///     直接的 ancient 卡牌卡名横幅材质覆盖。
        ///     返回 <c>null</c> 以继续使用其它覆盖层。
        /// </summary>
        Material? CustomAncientBannerMaterial => null;
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
    ///     Optional asset profile for card-pool-level presentation overrides.
    ///     卡池级表现覆盖的可选 asset profile。
    /// </summary>
    public interface IModCardPoolAssetOverrides
    {
        /// <summary>
        ///     Card-pool-level asset profile. Return <see cref="CardPoolAssetProfile.Empty" /> when unused.
        ///     卡池级 asset profile。未使用时返回 <see cref="CardPoolAssetProfile.Empty" />。
        /// </summary>
        CardPoolAssetProfile AssetProfile { get; }
    }

    /// <summary>
    ///     Optional deck-view style override for a card pool.
    ///     卡池的可选牌组查看界面样式覆盖。
    /// </summary>
    public interface IModCardPoolDeckViewStyle
    {
        /// <summary>
        ///     Deck-view style for this pool, or <c>null</c> to keep vanilla behavior.
        ///     此卡池的牌组查看样式；返回 <c>null</c> 保持原版行为。
        /// </summary>
        CardPoolDeckViewStyle? DeckViewStyle { get; }
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
        ///     Override for the large portrait texture path.
        ///     大型肖像纹理路径覆盖。
        /// </summary>
        string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }

    /// <summary>
    ///     Patches <see cref="EpochModel" /> portrait path getters for <see cref="IModEpochAssetOverrides" />.
    ///     为 <see cref="IModEpochAssetOverrides" /> 修补<see cref="EpochModel" /> portrait 路径 getter。
    /// </summary>
    /// <summary>
    ///     Patches orb HUD icon (<see cref="CompressedTexture2D" />) for <see cref="IModOrbAssetOverrides" />.
    ///     为 <see cref="IModOrbAssetOverrides" /> 修补充能球 HUD 图标 (<see cref="CompressedTexture2D" />)。
    /// </summary>
    internal class OrbIconPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_icon";
        public static string Description => "Allow mod orbs to override icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "Icon", MethodType.Getter),
            ];
        }

        /// <summary>
        ///     Loads compressed icon texture from <see cref="IModOrbAssetOverrides.CustomIconPath" /> when valid.
        ///     有效时从 <see cref="IModOrbAssetOverrides.CustomIconPath" /> 加载compressed 图标 纹理。
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref CompressedTexture2D __result)
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
    internal class OrbSpritePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_sprite_path";
        public static string Description => "Allow mod orbs to override visuals scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "SpritePath", MethodType.Getter),
            ];
        }

        /// <summary>
        ///     Supplies <see cref="IModOrbAssetOverrides.CustomVisualsScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModOrbAssetOverrides.CustomVisualsScenePath" />。
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref string __result)
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
    internal class OrbAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_orb_asset_paths";
        public static string Description => "Allow mod orbs to advertise custom asset paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(OrbModel), "AssetPaths", MethodType.Getter),
            ];
        }

        /// <summary>
        ///     Collects existing paths from <see cref="IModOrbAssetOverrides" /> for icon and visuals scenes.
        ///     从 <see cref="IModOrbAssetOverrides" /> 收集现有的图标和视觉场景路径。
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref IEnumerable<string> __result)
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
    internal class PotionImagePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_image_path";
        public static string Description => "Allow mod potions to override image and packed atlas paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "ImagePath", MethodType.Getter),
                new(typeof(PotionModel), "PackedImagePath", null, true, MethodType.Getter),
            ];
        }

        [HarmonyPriority(410)]
        public static bool Prefix(PotionModel __instance, ref string __result)
        {
            return TryPotionImagePath(__instance, ref __result);
        }

        internal static bool TryPotionImagePath(PotionModel instance, ref string result)
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

        internal static bool TryPotionOutlinePath(PotionModel instance, ref string result)
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
    ///     Patches potion outline path getters for mod path overrides.
    ///     为 mod 路径覆盖修补药水轮廓路径 getter。
    /// </summary>
    internal class PotionOutlinePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_outline_path";
        public static string Description => "Allow mod potions to override outline and packed atlas paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "OutlinePath", MethodType.Getter),
                new(typeof(PotionModel), "PackedOutlinePath", null, true, MethodType.Getter),
            ];
        }

        [HarmonyPriority(410)]
        public static bool Prefix(PotionModel __instance, ref string __result)
        {
            return PotionImagePathPatch.TryPotionOutlinePath(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches potion image and outline textures for mod path overrides.
    ///     为 mod 路径覆盖修补药水图像和轮廓纹理。
    /// </summary>
    internal class PotionTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_texture";
        public static string Description => "Allow mod potions to override image textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(PotionModel), "Image", MethodType.Getter),
            ];
        }

        public static bool Prefix(PotionModel __instance, ref Texture2D __result)
        {
            return TryPotionImageTexture(__instance, ref __result);
        }

        internal static bool TryPotionImageTexture(PotionModel instance, ref Texture2D result)
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

        internal static bool TryPotionOutlineTexture(PotionModel instance, ref Texture2D result)
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
    ///     Patches potion outline texture for mod path overrides.
    ///     为 mod 路径覆盖修补药水轮廓纹理。
    /// </summary>
    internal class PotionOutlineTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_potion_outline_texture";
        public static string Description => "Allow mod potions to override outline textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PotionModel), "Outline", MethodType.Getter)];
        }

        public static bool Prefix(PotionModel __instance, ref Texture2D __result)
        {
            return PotionTexturePatch.TryPotionOutlineTexture(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches run-summary banner texture for cards implementing <see cref="IModCardAssetOverrides" />.
    ///     为实现 <see cref="IModCardAssetOverrides" /> 的卡牌修补跑局摘要横幅纹理。
    /// </summary>
    internal class CardBannerTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_banner_texture";
        public static string Description => "Allow mod cards to override BannerTexture";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "BannerTexture", MethodType.Getter)];
        }

        /// <summary>
        ///     Loads banner texture from <see cref="IModCardAssetOverrides.CustomBannerTexturePath" /> when valid.
        ///     有效时从 <see cref="IModCardAssetOverrides.CustomBannerTexturePath" /> 加载横幅纹理。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Texture2D __result)
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
    internal class CardBannerMaterialPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_banner_material";
        public static string Description => "Allow mod cards to override BannerMaterial";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "BannerMaterial", MethodType.Getter)];
        }

        /// <summary>
        ///     Loads material from <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> when valid.
        ///     有效时从 <see cref="IModCardAssetOverrides.CustomBannerMaterialPath" /> 加载材质。
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
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
    internal class ActBackgroundScenePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_background_scene_path";
        public static string Description => "Allow mod acts to override background scene path";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "BackgroundScenePath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModActAssetOverrides.CustomBackgroundScenePath" />。
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
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
    internal class ActRestSiteBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_rest_site_background_path";
        public static string Description => "Allow mod acts to override rest site background path";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "RestSiteBackgroundPath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModActAssetOverrides.CustomRestSiteBackgroundPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModActAssetOverrides.CustomRestSiteBackgroundPath" />。
        /// </summary>
        public static bool Prefix(ActModel __instance, ref string __result)
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
    internal class ActMapBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_map_background_path";
        public static string Description => "Allow mod acts to override map background paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), "MapTopBgPath", MethodType.Getter),
            ];
        }

        public static bool Prefix(ActModel __instance, ref string __result)
        {
            return TryActMapTopBgPath(__instance, ref __result);
        }

        internal static bool TryActMapTopBgPath(ActModel instance, ref string result)
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

        internal static bool TryActMapMidBgPath(ActModel instance, ref string result)
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

        internal static bool TryActMapBotBgPath(ActModel instance, ref string result)
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
    ///     Patches act middle map background image path for mod acts.
    ///     为 mod 章节修补章节地图中层背景图像路径。
    /// </summary>
    internal class ActMapMidBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_map_mid_background_path";
        public static string Description => "Allow mod acts to override middle map background paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "MapMidBgPath", MethodType.Getter)];
        }

        public static bool Prefix(ActModel __instance, ref string __result)
        {
            return ActMapBackgroundPathPatch.TryActMapMidBgPath(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches act bottom map background image path for mod acts.
    ///     为 mod 章节修补章节地图底层背景图像路径。
    /// </summary>
    internal class ActMapBottomBackgroundPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_act_map_bottom_background_path";
        public static string Description => "Allow mod acts to override bottom map background paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "MapBotBgPath", MethodType.Getter)];
        }

        public static bool Prefix(ActModel __instance, ref string __result)
        {
            return ActMapBackgroundPathPatch.TryActMapBotBgPath(__instance, ref __result);
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
    internal class EventBackgroundScenePathGetterPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_background_scene_path_getter";

        public static string Description =>
            "Route EventModel.BackgroundScenePath to mod CustomBackgroundScenePath when the resource exists";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), "BackgroundScenePath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" />。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref string __result)
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
    internal class EventLayoutScenePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_layout_scene";
        public static string Description => "Allow mod events to override layout packed scene";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateScene))];
        }

        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomLayoutScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomLayoutScenePath" />。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
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
    internal class EventInitialPortraitPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_initial_portrait";
        public static string Description => "Allow mod events to override initial portrait texture";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateInitialPortrait))];
        }

        /// <summary>
        ///     Loads portrait from <see cref="IModEventAssetOverrides.CustomInitialPortraitPath" /> when valid.
        ///     有效时从 <see cref="IModEventAssetOverrides.CustomInitialPortraitPath" /> 加载portrait。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Texture2D __result)
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
    internal class EventBackgroundScenePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_background_scene";
        public static string Description => "Allow mod events to override background packed scene";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))];
        }

        /// <summary>
        ///     Supplies <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModEventAssetOverrides.CustomBackgroundScenePath" />。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref PackedScene __result)
        {
            if (__instance is IModAncientEventAssetOverrides
                {
                    AncientPresentationAssetProfile.StageProcedural: not null,
                })
                return true;

            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetEventBackgroundScene(__instance, out var externalScene,
                    out var externalSceneProviderKey) &&
                ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                    __instance,
                    externalScene,
                    "ExternalAssetOverrideRegistry.EventBackgroundScene",
                    $"provider '{externalSceneProviderKey}'"))
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
    internal class EventHasVfxPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_has_vfx";
        public static string Description => "Allow mod events to advertise custom VFX scene availability";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), "HasVfx", MethodType.Getter)];
        }

        /// <summary>
        ///     Returns true when <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> resolves to an existing resource.
        ///     当 <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> 解析到现有资源时返回 true。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref bool __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetEventVfxScene(__instance, out var externalVfxScene,
                    out var externalVfxSceneProviderKey))
            {
                if (ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                        __instance,
                        externalVfxScene,
                        "ExternalAssetOverrideRegistry.EventVfxScene",
                        $"provider '{externalVfxSceneProviderKey}'"))
                {
                    __result = true;
                    return false;
                }

                if (__instance is not IModEventAssetOverrides)
                {
                    __result = false;
                    return false;
                }
            }

            if (__instance is not IModEventAssetOverrides overrides)
                return true;

            var path = overrides.CustomVfxScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            __result = ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                __instance,
                path,
                nameof(IModEventAssetOverrides.CustomVfxScenePath));
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="EventModel.CreateVfx" /> for <see cref="IModEventAssetOverrides" />.
    ///     为 <see cref="IModEventAssetOverrides" /> 修补<see cref="EventModel.CreateVfx" />。
    /// </summary>
    internal class EventCreateVfxPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_create_vfx";
        public static string Description => "Allow mod events to instantiate custom VFX scenes";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.CreateVfx))];
        }

        /// <summary>
        ///     Instantiates <see cref="IModEventAssetOverrides.CustomVfxScenePath" /> when the packed scene exists.
        ///     当 packed scene 存在时实例化 <see cref="IModEventAssetOverrides.CustomVfxScenePath" />。
        /// </summary>
        public static bool Prefix(EventModel __instance, ref Node2D __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetEventVfxScene(__instance, out var externalVfxScene,
                    out var externalVfxSceneProviderKey) &&
                ContentAssetOverridePatchHelper.TryInstantiatePackedSceneOverride(
                    __instance,
                    externalVfxScene,
                    "ExternalAssetOverrideRegistry.EventVfxScene",
                    $"provider '{externalVfxSceneProviderKey}'",
                    out __result))
                return false;

            if (__instance is not IModEventAssetOverrides overrides)
                return true;

            var path = overrides.CustomVfxScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return !ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                __instance,
                path,
                nameof(IModEventAssetOverrides.CustomVfxScenePath),
                out __result);
        }
    }

    /// <summary>
    ///     Appends custom event asset paths to <see cref="EventModel.GetAssetPaths" /> for preloading.
    ///     将自定义事件资源路径追加到 <see cref="EventModel.GetAssetPaths" />，用于预加载。
    /// </summary>
    internal class EventGetAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_event_get_asset_paths";
        public static string Description => "Merge mod event custom paths into GetAssetPaths preload lists";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EventModel), nameof(EventModel.GetAssetPaths))];
        }

        /// <summary>
        ///     Concatenates resolved override paths after the vanilla enumeration.
        ///     将已解析的覆盖资源路径追加到原版枚举结果之后。
        /// </summary>
        public static void Postfix(EventModel __instance, IRunState runState, ref IEnumerable<string> __result)
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
    internal class AncientMapIconTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_ancient_map_icon_texture";
        public static string Description => "Allow mod ancients to override map node icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientEventModel), "MapIcon", MethodType.Getter),
            ];
        }

        public static bool Prefix(AncientEventModel __instance, ref Texture2D __result)
        {
            return TryAncientMapIcon(__instance, ref __result);
        }

        internal static bool TryAncientMapIcon(AncientEventModel instance, ref Texture2D result)
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

        internal static bool TryAncientMapIconOutline(AncientEventModel instance, ref Texture2D result)
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
    ///     Patches ancient map node icon outline textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <see cref="IModAncientEventAssetOverrides" /> 修补远古事件地图节点图标轮廓纹理。
    /// </summary>
    internal class AncientMapIconOutlineTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_ancient_map_icon_outline_texture";
        public static string Description => "Allow mod ancients to override map node icon outline textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "MapIconOutline", MethodType.Getter)];
        }

        public static bool Prefix(AncientEventModel __instance, ref Texture2D __result)
        {
            return AncientMapIconTexturePatch.TryAncientMapIconOutline(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Patches ancient run-history icon textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <see cref="IModAncientEventAssetOverrides" /> 修补远古事件跑局历史图标纹理。
    /// </summary>
    internal class AncientRunHistoryIconTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_ancient_run_history_icon_texture";
        public static string Description => "Allow mod ancients to override run history icon textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(AncientEventModel), "RunHistoryIcon", MethodType.Getter),
            ];
        }

        public static bool Prefix(AncientEventModel __instance, ref Texture2D __result)
        {
            return TryAncientRunHistoryIcon(__instance, ref __result);
        }

        internal static bool TryAncientRunHistoryIcon(AncientEventModel instance, ref Texture2D result)
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

        internal static bool TryAncientRunHistoryIconOutline(AncientEventModel instance, ref Texture2D result)
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
    ///     Patches ancient run-history icon outline textures for <see cref="IModAncientEventAssetOverrides" />.
    ///     为 <see cref="IModAncientEventAssetOverrides" /> 修补远古事件跑局历史图标轮廓纹理。
    /// </summary>
    internal class AncientRunHistoryIconOutlineTexturePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_ancient_run_history_icon_outline_texture";
        public static string Description => "Allow mod ancients to override run history icon outline textures";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "RunHistoryIconOutline", MethodType.Getter)];
        }

        public static bool Prefix(AncientEventModel __instance, ref Texture2D __result)
        {
            return AncientRunHistoryIconTexturePatch.TryAncientRunHistoryIconOutline(__instance, ref __result);
        }
    }

    /// <summary>
    ///     Merges custom map node asset paths into <see cref="AncientEventModel.MapNodeAssetPaths" />.
    ///     将自定义地图节点资源路径合并到 <see cref="AncientEventModel.MapNodeAssetPaths" />。
    /// </summary>
    internal class AncientMapNodeAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_ancient_map_node_asset_paths";
        public static string Description => "Allow mod ancients to include custom paths in MapNodeAssetPaths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AncientEventModel), "MapNodeAssetPaths", MethodType.Getter)];
        }

        /// <summary>
        ///     Appends resolved custom map icon paths after the vanilla pair.
        ///     在原版路径对之后追加已解析的自定义地图图标路径。
        /// </summary>
        public static void Postfix(AncientEventModel __instance, ref IEnumerable<string> __result)
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
    internal class AfflictionOverlayPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_overlay_path";
        public static string Description => "Allow mod afflictions to override OverlayPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "OverlayPath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" />。
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref string __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var externalPath,
                    out var externalPathProviderKey) &&
                ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    externalPath,
                    $"ExternalAssetOverrideRegistry.AfflictionOverlayPath[{externalPathProviderKey}]"))
            {
                __result = externalPath;
                return false;
            }

            if (__instance is not IModAfflictionAssetOverrides overrides)
                return true;

            var path = overrides.CustomOverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    path,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = path;
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel.HasOverlay" /> from custom overlay path existence.
    ///     根据自定义 overlay 路径 existence修补<see cref="AfflictionModel.HasOverlay" />。
    /// </summary>
    internal class AfflictionHasOverlayPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_has_overlay";
        public static string Description => "Allow mod afflictions to advertise overlay availability";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), "HasOverlay", MethodType.Getter)];
        }

        /// <summary>
        ///     Resolves the custom overlay path then sets boolean availability from resource existence.
        ///     解析自定义覆盖层路径，然后根据资源是否存在来设置布尔可用性。
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref bool __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene,
                    out var externalSceneProviderKey))
            {
                __result = ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                    __instance,
                    externalScene,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayScene",
                    $"provider '{externalSceneProviderKey}'");
                return false;
            }

            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var externalOverlayPath,
                    out var externalPathProviderKey))
            {
                __result = ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    externalOverlayPath,
                    $"ExternalAssetOverrideRegistry.AfflictionOverlayPath[{externalPathProviderKey}]");
                return false;
            }

            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                __instance,
                path,
                nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath));
            return false;
        }
    }

    /// <summary>
    ///     Patches <see cref="AfflictionModel.CreateOverlay" /> to instantiate mod overlay scenes when configured.
    ///     修补 <see cref="AfflictionModel.CreateOverlay" />，在配置后实例化 mod 覆盖层场景。
    /// </summary>
    internal class AfflictionCreateOverlayPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_affliction_create_overlay";
        public static string Description => "Allow mod afflictions to instantiate overlays from custom scene paths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AfflictionModel), nameof(AfflictionModel.CreateOverlay))];
        }

        /// <summary>
        ///     Instantiates <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" /> when the packed scene exists.
        ///     当 packed scene 存在时实例化 <see cref="IModAfflictionAssetOverrides.CustomOverlayScenePath" />。
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref Control __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene,
                    out var externalSceneProviderKey))
                return !ContentAssetOverridePatchHelper.TryInstantiatePackedSceneOverride(
                    __instance,
                    externalScene,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayScene",
                    $"provider '{externalSceneProviderKey}'",
                    out __result);

            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var externalOverlayPath,
                    out var externalPathProviderKey))
                return !ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                    __instance,
                    externalOverlayPath,
                    $"ExternalAssetOverrideRegistry.AfflictionOverlayPath[{externalPathProviderKey}]",
                    out __result);

            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            return !ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                __instance,
                path,
                nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath),
                out __result);
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
    internal class EnchantmentIntendedIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_enchantment_intended_icon_path";
        public static string Description => "Allow mod enchantments to override IntendedIconPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EnchantmentModel), "IntendedIconPath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModEnchantmentAssetOverrides.CustomIconPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModEnchantmentAssetOverrides.CustomIconPath" />。
        /// </summary>
        public static bool Prefix(EnchantmentModel __instance, ref string __result)
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
    internal class PowerResolvedBigIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_power_resolved_big_icon_path";
        public static string Description => "Allow mod powers to override ResolvedBigIconPath for preloading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PowerModel), "ResolvedBigIconPath", MethodType.Getter)];
        }

        /// <summary>
        ///     Supplies <see cref="IModPowerAssetOverrides.CustomBigIconPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModPowerAssetOverrides.CustomBigIconPath" />。
        /// </summary>
        public static bool Prefix(PowerModel __instance, ref string __result)
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
