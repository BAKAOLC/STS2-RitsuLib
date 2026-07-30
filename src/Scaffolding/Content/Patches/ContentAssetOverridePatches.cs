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
            Func<TOverrides, Material?> selector,
            string memberName)
            where TOverrides : class
        {
            if (instance is not TOverrides overrides)
                return true;

            Material? material;
            try
            {
                material = selector(overrides);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Direct material override failed for {DescribeOwner(instance)}.{memberName}: {ex.Message}. Falling back.");
                return true;
            }

            if (material == null)
                return true;

            if (!GodotObject.IsInstanceValid(material))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Direct material override is invalid for {DescribeOwner(instance)}.{memberName}. Falling back.");
                return true;
            }

            __result = material;
            return false;
        }

        internal static bool TryUsePortraitPathList(CardModel instance, IModCardAssetOverrides overrides,
            ref IEnumerable<string> __result)
        {
            var portraitPath = GetExistingPath(
                overrides.CustomPortraitPath,
                nameof(IModCardAssetOverrides.CustomPortraitPath));
            var betaPortraitPath = GetExistingPath(
                overrides.CustomBetaPortraitPath,
                nameof(IModCardAssetOverrides.CustomBetaPortraitPath));
            if (portraitPath == null && betaPortraitPath == null)
                return true;

            __result = betaPortraitPath == null
                ? [portraitPath ?? instance.PortraitPath]
                : [portraitPath ?? instance.PortraitPath, betaPortraitPath];
            return false;

            string? GetExistingPath(string? path, string memberName)
            {
                return !string.IsNullOrWhiteSpace(path) &&
                       AssetPathDiagnostics.Exists(path, instance, memberName)
                    ? path
                    : null;
            }
        }

        internal static bool TryUseExistenceOverride(object instance, string? path, string memberName,
            ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!AssetPathDiagnostics.Exists(path, instance, memberName))
                return true;

            __result = true;
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

        internal static bool TryUseExternalCompressedTexturePathOverride(
            object instance,
            ref CompressedTexture2D __result,
            Func<string?> externalPathFactory,
            string memberName)
        {
            var path = externalPathFactory();
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!GodotResourcePath.TryLoad<CompressedTexture2D>(path, out var texture))
            {
                WarnOverrideUnavailable(instance, memberName, path, nameof(CompressedTexture2D));
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
        ///     <para xml:lang="en">
        ///         Resolves a <see cref="PackedScene" /> from a defined path, preferring the preload cache and falling
        ///         back to <see cref="ResourceLoader" />. Candidate enumeration supports <c>uid://</c> and remapped paths.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从已定义路径解析 <see cref="PackedScene" />，优先使用预加载缓存，并回退到
        ///         <see cref="ResourceLoader" />。候选路径枚举支持 <c>uid://</c> 和重映射路径。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Resolves a <see cref="Texture2D" /> from a defined path, preferring the preload cache and falling
        ///         back to <see cref="ResourceLoader" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从已定义路径解析 <see cref="Texture2D" />，优先使用预加载缓存，并回退到
        ///         <see cref="ResourceLoader" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Reports a defined override path that is missing or cannot be loaded as the expected resource type.
        ///         Undefined overrides do not produce warnings.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         报告已定义但不存在或无法按预期资源类型加载的覆盖路径。未定义的覆盖不会产生警告。
        ///     </para>
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
    ///     <para xml:lang="en">Defines optional card artwork paths and materials used by RitsuLib patches.</para>
    ///     <para xml:lang="zh-CN">定义由 RitsuLib 补丁使用的可选卡牌美术资源路径和材质。</para>
    /// </summary>
    public interface IModCardAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the card asset profile used by the default property implementations.</para>
        ///     <para xml:lang="zh-CN">获取默认属性实现使用的卡牌资源配置。</para>
        /// </summary>
        CardAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the main card portrait path override.</para>
        ///     <para xml:lang="zh-CN">获取主要卡图路径覆盖。</para>
        /// </summary>
        string? CustomPortraitPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the beta or alternate card portrait path override.</para>
        ///     <para xml:lang="zh-CN">获取测试版或备用卡图路径覆盖。</para>
        /// </summary>
        string? CustomBetaPortraitPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the card portrait <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取卡图 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomPortraitMaterialPath => AssetProfile.PortraitMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the card frame texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取卡牌边框纹理路径覆盖。</para>
        /// </summary>
        string? CustomFramePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the portrait-border texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取卡图边框纹理路径覆盖。</para>
        /// </summary>
        string? CustomPortraitBorderPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the small energy-icon texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取小型能量图标纹理路径覆盖。</para>
        /// </summary>
        string? CustomEnergyIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-card border texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取先古卡牌边框纹理路径覆盖。</para>
        /// </summary>
        string? CustomAncientBorderPath => AssetProfile.AncientBorderPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-card text-background texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取先古卡牌文本背景纹理路径覆盖。</para>
        /// </summary>
        string? CustomAncientTextBgPath => AssetProfile.AncientTextBgPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-card title-banner texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取先古卡牌标题横幅纹理路径覆盖。</para>
        /// </summary>
        string? CustomAncientBannerPath => AssetProfile.AncientBannerPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional visual-layout override.</para>
        ///     <para xml:lang="zh-CN">获取可选的视觉布局覆盖。</para>
        /// </summary>
        CardVisualStyle CustomVisualStyle => AssetProfile.VisualStyle;

        /// <summary>
        ///     <para xml:lang="en">Gets the frame <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取边框 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomFrameMaterialPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the portrait-border <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取卡图边框 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomPortraitBorderMaterialPath => AssetProfile.PortraitBorderMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the energy-icon <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取能量图标 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomEnergyIconMaterialPath => AssetProfile.EnergyIconMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-card border <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取先古卡牌边框 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomAncientBorderMaterialPath => AssetProfile.AncientBorderMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the Ancient-card text-background <see cref="Material" /> resource-path override.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取先古卡牌文本背景 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomAncientTextBgMaterialPath => AssetProfile.AncientTextBgMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-card title-banner <see cref="Material" /> resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取先古卡牌标题横幅 <see cref="Material" /> 资源路径覆盖。</para>
        /// </summary>
        string? CustomAncientBannerMaterialPath => AssetProfile.AncientBannerMaterialPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the built-in overlay <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取内置覆盖层 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomOverlayScenePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the title-banner texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取标题横幅纹理路径覆盖。</para>
        /// </summary>
        string? CustomBannerTexturePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the title-banner material-path override.</para>
        ///     <para xml:lang="zh-CN">获取标题横幅材质路径覆盖。</para>
        /// </summary>
        string? CustomBannerMaterialPath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct portrait <see cref="Material" /> override for cards.</para>
    ///     <para xml:lang="zh-CN">为卡牌定义可选的直接卡图 <see cref="Material" /> 覆盖。</para>
    /// </summary>
    public interface IModCardPortraitMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct portrait material, or <see langword="null" /> to continue with later override layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接卡图材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomPortraitMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct frame <see cref="Material" /> override for cards.</para>
    ///     <para xml:lang="zh-CN">为卡牌定义可选的直接边框 <see cref="Material" /> 覆盖。</para>
    /// </summary>
    public interface IModCardFrameMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct frame material, or <see langword="null" /> to continue with later override layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接边框材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomFrameMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct portrait-border material override for cards.</para>
    ///     <para xml:lang="zh-CN">为卡牌定义可选的直接卡图边框材质覆盖。</para>
    /// </summary>
    public interface IModCardPortraitBorderMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct portrait-border material, or <see langword="null" /> to continue with later layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接卡图边框材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomPortraitBorderMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct energy-icon material override for cards.</para>
    ///     <para xml:lang="zh-CN">为卡牌定义可选的直接能量图标材质覆盖。</para>
    /// </summary>
    public interface IModCardEnergyIconMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct energy-icon material, or <see langword="null" /> to continue with later layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接能量图标材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomEnergyIconMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct Ancient-card border material override.</para>
    ///     <para xml:lang="zh-CN">定义可选的直接先古卡牌边框材质覆盖。</para>
    /// </summary>
    public interface IModCardAncientBorderMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct Ancient-card border material, or <see langword="null" /> to continue with later layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接先古卡牌边框材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomAncientBorderMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct Ancient-card text-background material override.</para>
    ///     <para xml:lang="zh-CN">定义可选的直接先古卡牌文本背景材质覆盖。</para>
    /// </summary>
    public interface IModCardAncientTextBgMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct Ancient-card text-background material, or <see langword="null" /> to continue.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接先古卡牌文本背景材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomAncientTextBgMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct Ancient-card title-banner material override.</para>
    ///     <para xml:lang="zh-CN">定义可选的直接先古卡牌标题横幅材质覆盖。</para>
    /// </summary>
    public interface IModCardAncientBannerMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct Ancient-card title-banner material, or <see langword="null" /> to continue.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接先古卡牌标题横幅材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomAncientBannerMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional direct title-banner <see cref="Material" /> override for cards.</para>
    ///     <para xml:lang="zh-CN">为卡牌定义可选的直接标题横幅 <see cref="Material" /> 覆盖。</para>
    /// </summary>
    public interface IModCardBannerMaterialOverride
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the direct title-banner material, or <see langword="null" /> to continue with later layers.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取直接标题横幅材质；返回 <see langword="null" /> 时继续处理后续覆盖层。</para>
        /// </summary>
        Material? CustomBannerMaterial => null;
    }

    /// <summary>
    ///     <para xml:lang="en">Allows a card pool to supply its card-frame <see cref="Material" /> directly.</para>
    ///     <para xml:lang="zh-CN">允许卡池直接提供其卡牌边框 <see cref="Material" />。</para>
    /// </summary>
    public interface IModCardPoolFrameMaterial
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the card-frame material, or <see langword="null" /> to use the path-based default.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取卡牌边框材质；返回 <see langword="null" /> 时使用基于路径的默认值。</para>
        /// </summary>
        Material? PoolFrameMaterial { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional card-pool presentation overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的卡池表现覆盖。</para>
    /// </summary>
    public interface IModCardPoolAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the card-pool asset profile.</para>
        ///     <para xml:lang="zh-CN">获取卡池资源配置。</para>
        /// </summary>
        CardPoolAssetProfile AssetProfile { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional deck-view style override for a card pool.</para>
    ///     <para xml:lang="zh-CN">定义卡池的可选牌组查看界面样式覆盖。</para>
    /// </summary>
    public interface IModCardPoolDeckViewStyle
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deck-view style, or <see langword="null" /> to retain base-game behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取牌组查看界面样式；返回 <see langword="null" /> 时保留原版行为。</para>
        /// </summary>
        CardPoolDeckViewStyle? DeckViewStyle { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional relic icon-path overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的遗物图标路径覆盖。</para>
    /// </summary>
    public interface IModRelicAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the relic asset profile.</para>
        ///     <para xml:lang="zh-CN">获取遗物资源配置。</para>
        /// </summary>
        RelicAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the primary relic icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取遗物主图标路径覆盖。</para>
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the relic outline-icon path override.</para>
        ///     <para xml:lang="zh-CN">获取遗物轮廓图标路径覆盖。</para>
        /// </summary>
        string? CustomIconOutlinePath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the large relic artwork path override.</para>
        ///     <para xml:lang="zh-CN">获取遗物大图路径覆盖。</para>
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional power icon-path overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的能力图标路径覆盖。</para>
    /// </summary>
    public interface IModPowerAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the power asset profile.</para>
        ///     <para xml:lang="zh-CN">获取能力资源配置。</para>
        /// </summary>
        PowerAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the standard icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取标准图标路径覆盖。</para>
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the large icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取大图标路径覆盖。</para>
        /// </summary>
        string? CustomBigIconPath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional orb icon and combat-visual scene-path overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的充能球图标和战斗视觉场景路径覆盖。</para>
    /// </summary>
    public interface IModOrbAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the orb asset profile.</para>
        ///     <para xml:lang="zh-CN">获取充能球资源配置。</para>
        /// </summary>
        OrbAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the orb icon texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取充能球图标纹理路径覆盖。</para>
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the orb combat-visual scene-path override.</para>
        ///     <para xml:lang="zh-CN">获取充能球战斗视觉场景路径覆盖。</para>
        /// </summary>
        string? CustomVisualsScenePath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional act asset overrides. Most mods can use <see cref="ModActTemplate" /> instead of
    ///         implementing this interface directly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的章节资源覆盖。大多数模组可以使用 <see cref="ModActTemplate" />，无需直接实现此接口。
    ///     </para>
    /// </summary>
    public interface IModActAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the act asset profile.</para>
        ///     <para xml:lang="zh-CN">获取章节资源配置。</para>
        /// </summary>
        ActAssetProfile AssetProfile => ActAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the main act background scene-path override.</para>
        ///     <para xml:lang="zh-CN">获取章节主背景场景路径覆盖。</para>
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     <para xml:lang="en">Gets the rest-site background scene-path override.</para>
        ///     <para xml:lang="zh-CN">获取休息处背景场景路径覆盖。</para>
        /// </summary>
        string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the map's top-layer background image-path override.</para>
        ///     <para xml:lang="zh-CN">获取地图顶层背景图像路径覆盖。</para>
        /// </summary>
        string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the map's middle-layer background image-path override.</para>
        ///     <para xml:lang="zh-CN">获取地图中层背景图像路径覆盖。</para>
        /// </summary>
        string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the map's bottom-layer background image-path override.</para>
        ///     <para xml:lang="zh-CN">获取地图底层背景图像路径覆盖。</para>
        /// </summary>
        string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the treasure-chest Spine resource-path override.</para>
        ///     <para xml:lang="zh-CN">获取宝箱 Spine 资源路径覆盖。</para>
        /// </summary>
        string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional <c>res://</c> directory containing combat-background parallax layers named with
        ///         the base game's <c>_bg_</c> and <c>_fg_</c> conventions.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取可选的 <c>res://</c> 战斗背景视差图层目录，其中的文件采用原版游戏的 <c>_bg_</c> 和
        ///         <c>_fg_</c> 命名约定。
        ///     </para>
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional event layout, portrait, background, and VFX asset overrides. Mods may use
    ///         <see cref="ModEventTemplate" /> or implement this interface on an <see cref="EventModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的事件布局、立绘、背景和 VFX 资源覆盖。模组可以使用 <see cref="ModEventTemplate" />，
    ///         或在 <see cref="EventModel" /> 上实现此接口。
    ///     </para>
    /// </summary>
    public interface IModEventAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the event asset profile.</para>
        ///     <para xml:lang="zh-CN">获取事件资源配置。</para>
        /// </summary>
        EventAssetProfile AssetProfile => EventAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the full event-layout <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取完整事件布局 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomLayoutScenePath => AssetProfile.LayoutScenePath;

        /// <summary>
        ///     <para xml:lang="en">Gets the initial portrait texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取初始立绘纹理路径覆盖。</para>
        /// </summary>
        string? CustomInitialPortraitPath => AssetProfile.InitialPortraitPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the background <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取背景 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     <para xml:lang="en">Gets the VFX <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取 VFX <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomVfxScenePath => AssetProfile.VfxScenePath;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Extends event asset overrides with Ancient map-node, run-history, and procedural-stage presentation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         扩展事件资源覆盖，增加先古地图节点、游戏历史和程序化舞台表现。
    ///     </para>
    /// </summary>
    public interface IModAncientEventAssetOverrides : IModEventAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the Ancient-event presentation profile.</para>
        ///     <para xml:lang="zh-CN">获取先古事件表现资源配置。</para>
        /// </summary>
        AncientEventPresentationAssetProfile AncientPresentationAssetProfile =>
            AncientEventPresentationAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the map-node icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取地图节点图标路径覆盖。</para>
        /// </summary>
        string? CustomMapIconPath => AncientPresentationAssetProfile?.MapIconPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the map-node outline-icon path override.</para>
        ///     <para xml:lang="zh-CN">获取地图节点轮廓图标路径覆盖。</para>
        /// </summary>
        string? CustomMapIconOutlinePath => AncientPresentationAssetProfile?.MapIconOutlinePath;

        /// <summary>
        ///     <para xml:lang="en">Gets the run-history icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取游戏历史图标路径覆盖。</para>
        /// </summary>
        string? CustomRunHistoryIconPath => AncientPresentationAssetProfile?.RunHistoryIconPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the run-history outline-icon path override.</para>
        ///     <para xml:lang="zh-CN">获取游戏历史轮廓图标路径覆盖。</para>
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AncientPresentationAssetProfile?.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     <para xml:lang="en">Defines optional epoch timeline portrait-path overrides.</para>
    ///     <para xml:lang="zh-CN">定义可选的时代时间线肖像路径覆盖。</para>
    /// </summary>
    public interface IModEpochAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the epoch asset profile.</para>
        ///     <para xml:lang="zh-CN">获取时代资源配置。</para>
        /// </summary>
        EpochAssetProfile AssetProfile => EpochAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the packed timeline-portrait path override.</para>
        ///     <para xml:lang="zh-CN">获取打包时间线肖像路径覆盖。</para>
        /// </summary>
        string? CustomPackedPortraitPath => AssetProfile.PackedPortraitPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the large portrait texture-path override.</para>
        ///     <para xml:lang="zh-CN">获取大型肖像纹理路径覆盖。</para>
        /// </summary>
        string? CustomBigPortraitPath => AssetProfile.BigPortraitPath;
    }

    /// <summary>
    ///     <para xml:lang="en">Applies external and interface overrides to the orb HUD icon.</para>
    ///     <para xml:lang="zh-CN">将外部和接口覆盖应用到充能球 HUD 图标。</para>
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
        ///     <para xml:lang="en">Applies the first available orb HUD icon override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的充能球 HUD 图标覆盖。</para>
        /// </summary>
        public static bool Prefix(OrbModel __instance, ref CompressedTexture2D __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetOrbIconTexture(__instance, out var externalTexture))
            {
                __result = externalTexture;
                return false;
            }

            if (!ContentAssetOverridePatchHelper.TryUseExternalCompressedTexturePathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetOrbIconPath(__instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.OrbIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseCompressedTextureOverride<IModOrbAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModOrbAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies external and interface overrides to the orb combat-visual scene path.</para>
    ///     <para xml:lang="zh-CN">将外部和接口覆盖应用到充能球战斗视觉场景路径。</para>
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
        ///     <para xml:lang="en">Applies the first available orb combat-visual scene-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的充能球战斗视觉场景路径覆盖。</para>
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
    ///     <para xml:lang="en">Adds custom orb icon and combat-visual paths to preload enumeration.</para>
    ///     <para xml:lang="zh-CN">将自定义充能球图标和战斗视觉路径添加到预加载枚举。</para>
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
        ///     <para xml:lang="en">Builds the effective custom orb asset-path enumeration.</para>
        ///     <para xml:lang="zh-CN">构建有效的自定义充能球资源路径枚举。</para>
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
                paths = [.. paths.Concat(externalPaths).Distinct(StringComparer.Ordinal)];
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
    ///     <para xml:lang="en">
    ///         Applies character-owned, external-registry, and potion-interface image-path overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">按角色所属、外部注册表和药水接口的顺序应用药水图像路径覆盖。</para>
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

            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetPotionImagePath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.PotionImagePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomImagePath, nameof(IModPotionAssetOverrides.CustomImagePath));
        }

        internal static bool TryPotionOutlinePath(PotionModel instance, ref string result)
        {
            if (!ModCharacterOwnedVisualOverrideHelper.TryPotionOutlinePath(instance, ref result))
                return false;

            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    instance,
                    ref result,
                    () => ExternalAssetOverrideRegistry.TryGetPotionOutlinePath(instance, out var path) ? path : null,
                    "ExternalAssetOverrideRegistry.PotionOutlinePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPotionAssetOverrides>(
                instance, ref result, o => o.CustomOutlinePath, nameof(IModPotionAssetOverrides.CustomOutlinePath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom potion outline-path overrides.</para>
    ///     <para xml:lang="zh-CN">应用自定义药水轮廓路径覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom potion image and outline texture overrides.</para>
    ///     <para xml:lang="zh-CN">应用自定义药水图像和轮廓纹理覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom potion outline texture overrides.</para>
    ///     <para xml:lang="zh-CN">应用自定义药水轮廓纹理覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom card title-banner texture overrides.</para>
    ///     <para xml:lang="zh-CN">应用自定义卡牌标题横幅纹理覆盖。</para>
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
        ///     <para xml:lang="en">Applies the first available title-banner texture override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的标题横幅纹理覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom card title-banner <see cref="Material" /> overrides.</para>
    ///     <para xml:lang="zh-CN">应用自定义卡牌标题横幅 <see cref="Material" /> 覆盖。</para>
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
        ///     <para xml:lang="en">Applies the first available title-banner material override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的标题横幅材质覆盖。</para>
        /// </summary>
        public static bool Prefix(CardModel __instance, ref Material __result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseDirectMaterialOverride<IModCardBannerMaterialOverride>(
                    __instance,
                    ref __result,
                    static overrides => overrides.CustomBannerMaterial,
                    nameof(IModCardBannerMaterialOverride.CustomBannerMaterial)))
                return false;

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
    ///     <para xml:lang="en">Applies external and interface overrides to an act's main background scene path.</para>
    ///     <para xml:lang="zh-CN">将外部和接口覆盖应用到章节主背景场景路径。</para>
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
        ///     <para xml:lang="en">Applies the first available main background scene-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的主背景场景路径覆盖。</para>
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
    ///     <para xml:lang="en">Applies external and interface overrides to an act's rest-site background path.</para>
    ///     <para xml:lang="zh-CN">将外部和接口覆盖应用到章节休息处背景路径。</para>
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
        ///     <para xml:lang="en">Applies the first available rest-site background-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的休息处背景路径覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom overrides to an act map's top, middle, and bottom background layers.</para>
    ///     <para xml:lang="zh-CN">将自定义覆盖应用到章节地图的顶层、中层和底层背景。</para>
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
    ///     <para xml:lang="en">Applies custom overrides to an act map's middle background layer.</para>
    ///     <para xml:lang="zh-CN">将自定义覆盖应用到章节地图的中层背景。</para>
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
    ///     <para xml:lang="en">Applies custom overrides to an act map's bottom background layer.</para>
    ///     <para xml:lang="zh-CN">将自定义覆盖应用到章节地图的底层背景。</para>
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
    ///     <para xml:lang="en">
    ///         Applies a custom event background scene path before the base game's synthesized path is used.
    ///     </para>
    ///     <para xml:lang="zh-CN">在使用原版游戏合成的路径之前，应用自定义事件背景场景路径。</para>
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
        ///     <para xml:lang="en">Applies the first available event background scene-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的事件背景场景路径覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom event layout scenes to <see cref="EventModel.CreateScene" />.</para>
    ///     <para xml:lang="zh-CN">将自定义事件布局场景应用到 <see cref="EventModel.CreateScene" />。</para>
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
        ///     <para xml:lang="en">Applies the first available event layout-scene override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的事件布局场景覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom event portraits to <see cref="EventModel.CreateInitialPortrait" />.</para>
    ///     <para xml:lang="zh-CN">将自定义事件立绘应用到 <see cref="EventModel.CreateInitialPortrait" />。</para>
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
        ///     <para xml:lang="en">Applies the first available initial-portrait override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的初始立绘覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom event background scenes to <see cref="EventModel.CreateBackgroundScene" />.</para>
    ///     <para xml:lang="zh-CN">将自定义事件背景场景应用到 <see cref="EventModel.CreateBackgroundScene" />。</para>
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
        ///     <para xml:lang="en">Applies the first available event background-scene override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的事件背景场景覆盖。</para>
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
    ///     <para xml:lang="en">Makes <see cref="EventModel.HasVfx" /> honor custom VFX scenes.</para>
    ///     <para xml:lang="zh-CN">使 <see cref="EventModel.HasVfx" /> 识别自定义 VFX 场景。</para>
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
        ///     <para xml:lang="en">Reports VFX availability from the first valid custom scene.</para>
        ///     <para xml:lang="zh-CN">根据首个有效的自定义场景报告 VFX 可用性。</para>
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
            }

            if (__instance is not IModEventAssetOverrides overrides)
                return true;

            var path = overrides.CustomVfxScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (!ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    path,
                    nameof(IModEventAssetOverrides.CustomVfxScenePath)))
                return true;

            __result = true;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Makes <see cref="EventModel.CreateVfx" /> instantiate custom VFX scenes.</para>
    ///     <para xml:lang="zh-CN">使 <see cref="EventModel.CreateVfx" /> 实例化自定义 VFX 场景。</para>
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
        ///     <para xml:lang="en">Instantiates the first configured custom VFX scene.</para>
        ///     <para xml:lang="zh-CN">实例化首个已配置的自定义 VFX 场景。</para>
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
    ///     <para xml:lang="en">Merges custom event asset paths into <see cref="EventModel.GetAssetPaths" /> for preloading.</para>
    ///     <para xml:lang="zh-CN">将自定义事件资源路径合并到 <see cref="EventModel.GetAssetPaths" />，以供预加载。</para>
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
        ///     <para xml:lang="en">Merges available override paths with the base-game enumeration.</para>
        ///     <para xml:lang="zh-CN">将可用的覆盖路径与原版游戏枚举结果合并。</para>
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
                __result = externalMerged.Length == 0 ? paths : [.. paths.Concat(externalMerged).Distinct()];
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
                merged = [.. merged.Concat(externalMerged).Distinct()];

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

            return
            [
                .. paths
                    .Where(path => AssetPathDiagnostics.Exists(
                        path,
                        instance,
                        nameof(AncientEventPresentationAssetProfile.StageProcedural))),
            ];
        }

        private static IEnumerable<string> RemovePath(IEnumerable<string> paths, string? pathToRemove)
        {
            return string.IsNullOrWhiteSpace(pathToRemove)
                ? paths
                : paths.Where(path => !string.Equals(path, pathToRemove, StringComparison.Ordinal));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom Ancient-event map-node icon textures.</para>
    ///     <para xml:lang="zh-CN">应用自定义先古事件地图节点图标纹理。</para>
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
    ///     <para xml:lang="en">Applies custom Ancient-event map-node outline-icon textures.</para>
    ///     <para xml:lang="zh-CN">应用自定义先古事件地图节点轮廓图标纹理。</para>
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
    ///     <para xml:lang="en">Applies custom Ancient-event run-history icon textures.</para>
    ///     <para xml:lang="zh-CN">应用自定义先古事件游戏历史图标纹理。</para>
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
    ///     <para xml:lang="en">Applies custom Ancient-event run-history outline-icon textures.</para>
    ///     <para xml:lang="zh-CN">应用自定义先古事件游戏历史轮廓图标纹理。</para>
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
    ///     <para xml:lang="en">
    ///         Replaces corresponding base-game paths in <see cref="AncientEventModel.MapNodeAssetPaths" /> with
    ///         available custom map-node icon paths.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用可用的自定义地图节点图标路径替换 <see cref="AncientEventModel.MapNodeAssetPaths" /> 中的对应原版路径。
    ///     </para>
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
        ///     <para xml:lang="en">Replaces available base-game map-icon paths with their custom counterparts.</para>
        ///     <para xml:lang="zh-CN">使用自定义路径替换对应的可用原版地图图标路径。</para>
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

            var customMainExists = !string.IsNullOrWhiteSpace(mapIconPath) &&
                                   AssetPathDiagnostics.Exists(
                                       mapIconPath,
                                       __instance,
                                       nameof(IModAncientEventAssetOverrides.CustomMapIconPath));
            var customOutlineExists = !string.IsNullOrWhiteSpace(mapIconOutlinePath) &&
                                      AssetPathDiagnostics.Exists(
                                          mapIconOutlinePath,
                                          __instance,
                                          nameof(IModAncientEventAssetOverrides.CustomMapIconOutlinePath));
            if (!customMainExists && !customOutlineExists)
                return;

            var retained = __result.Where(path =>
                (!customMainExists || path != vanillaMain) &&
                (!customOutlineExists || path != vanillaOutline));
            var replacements = new List<string>(2);
            if (customMainExists)
                replacements.Add(mapIconPath!);
            if (customOutlineExists)
                replacements.Add(mapIconOutlinePath!);
            __result = retained.Concat(replacements);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Defines an optional affliction overlay scene-path override.</para>
    ///     <para xml:lang="zh-CN">定义可选的侵蚀覆盖层场景路径覆盖。</para>
    /// </summary>
    public interface IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the affliction asset profile.</para>
        ///     <para xml:lang="zh-CN">获取侵蚀资源配置。</para>
        /// </summary>
        AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the overlay <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取覆盖层 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom overlay scene paths to <see cref="AfflictionModel" />.</para>
    ///     <para xml:lang="zh-CN">将自定义覆盖层场景路径应用到 <see cref="AfflictionModel" />。</para>
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
        ///     <para xml:lang="en">Applies the first available affliction overlay path.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的侵蚀覆盖层路径。</para>
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
    ///     <para xml:lang="en">Makes <see cref="AfflictionModel.HasOverlay" /> honor custom overlay scenes.</para>
    ///     <para xml:lang="zh-CN">使 <see cref="AfflictionModel.HasOverlay" /> 识别自定义覆盖层场景。</para>
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
        ///     <para xml:lang="en">Reports overlay availability from the first valid custom scene.</para>
        ///     <para xml:lang="zh-CN">根据首个有效的自定义场景报告覆盖层可用性。</para>
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref bool __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene,
                    out var externalSceneProviderKey) &&
                ContentAssetOverridePatchHelper.IsPackedSceneOverrideAvailable(
                    __instance,
                    externalScene,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayScene",
                    $"provider '{externalSceneProviderKey}'"))
            {
                __result = true;
                return false;
            }

            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var externalOverlayPath,
                    out var externalPathProviderKey) &&
                ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    externalOverlayPath,
                    $"ExternalAssetOverrideRegistry.AfflictionOverlayPath[{externalPathProviderKey}]"))
            {
                __result = true;
                return false;
            }

            var path = string.Empty;
            if (ContentAssetOverridePatchHelper.TryUseStringOverride<IModAfflictionAssetOverrides>(
                    __instance,
                    ref path,
                    o => o.CustomOverlayScenePath,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            if (!ContentAssetOverridePatchHelper.IsPackedScenePathOverrideAvailable(
                    __instance,
                    path,
                    nameof(IModAfflictionAssetOverrides.CustomOverlayScenePath)))
                return true;

            __result = true;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Makes <see cref="AfflictionModel.CreateOverlay" /> instantiate custom overlay scenes.</para>
    ///     <para xml:lang="zh-CN">使 <see cref="AfflictionModel.CreateOverlay" /> 实例化自定义覆盖层场景。</para>
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
        ///     <para xml:lang="en">
        ///         Tries the registered scene, registered path, and model-provided path in order; if none can be
        ///         instantiated, runs the base-game implementation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         依次尝试已注册场景、已注册路径和模型提供的路径；均无法实例化时运行原版游戏实现。
        ///     </para>
        /// </summary>
        public static bool Prefix(AfflictionModel __instance, ref Control __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayScene(__instance, out var externalScene,
                    out var externalSceneProviderKey) &&
                ContentAssetOverridePatchHelper.TryInstantiatePackedSceneOverride(
                    __instance,
                    externalScene,
                    "ExternalAssetOverrideRegistry.AfflictionOverlayScene",
                    $"provider '{externalSceneProviderKey}'",
                    out __result))
                return false;

            if (ExternalAssetOverrideRegistry.TryGetAfflictionOverlayPath(__instance, out var externalOverlayPath,
                    out var externalPathProviderKey) &&
                ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                    __instance,
                    externalOverlayPath,
                    $"ExternalAssetOverrideRegistry.AfflictionOverlayPath[{externalPathProviderKey}]",
                    out __result))
                return false;

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
    ///     <para xml:lang="en">Defines an optional enchantment icon-path override.</para>
    ///     <para xml:lang="zh-CN">定义可选的附魔图标路径覆盖。</para>
    /// </summary>
    public interface IModEnchantmentAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the enchantment asset profile.</para>
        ///     <para xml:lang="zh-CN">获取附魔资源配置。</para>
        /// </summary>
        EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the intended icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取预期图标路径覆盖。</para>
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom intended icon paths to <see cref="EnchantmentModel" />.</para>
    ///     <para xml:lang="zh-CN">将自定义预期图标路径应用到 <see cref="EnchantmentModel" />。</para>
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
        ///     <para xml:lang="en">Applies the first available enchantment icon-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的附魔图标路径覆盖。</para>
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
    ///     <para xml:lang="en">Applies custom large power-icon paths to preload resolution.</para>
    ///     <para xml:lang="zh-CN">将自定义能力大图标路径应用到预加载解析。</para>
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
        ///     <para xml:lang="en">Applies an available large power-icon path override.</para>
        ///     <para xml:lang="zh-CN">应用可用的能力大图标路径覆盖。</para>
        /// </summary>
        public static bool Prefix(PowerModel __instance, ref string __result)
        {
            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModPowerAssetOverrides>(
                __instance, ref __result, o => o.CustomBigIconPath,
                nameof(IModPowerAssetOverrides.CustomBigIconPath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Allows a <see cref="CardPoolModel" /> to supply the small energy-icon image embedded in rich-text card
    ///         descriptions. Use this only when the base-game
    ///         <c>res://images/packed/sprite_fonts/{EnergyColorName}_energy_icon.png</c> pattern is unsuitable.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         允许 <see cref="CardPoolModel" /> 提供嵌入富文本卡牌描述的小型能量图标。
    ///         仅当原版游戏的 <c>res://images/packed/sprite_fonts/{EnergyColorName}_energy_icon.png</c>
    ///         路径模式不适用时使用此接口。
    ///     </para>
    /// </summary>
    public interface IModTextEnergyIconPool
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the custom rich-text energy-icon image path.</para>
        ///     <para xml:lang="zh-CN">获取自定义富文本能量图标的图像路径。</para>
        /// </summary>
        string? TextEnergyIconPath { get; }
    }
}
