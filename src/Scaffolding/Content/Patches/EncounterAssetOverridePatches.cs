using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional encounter presentation and preload paths. Mods may use
    ///         <see cref="ModEncounterTemplate" /> or implement this interface on an <see cref="EncounterModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义可选的遭遇表现和预加载路径。模组可以使用 <see cref="ModEncounterTemplate" />，
    ///         或在 <see cref="EncounterModel" /> 上实现此接口。
    ///     </para>
    /// </summary>
    public interface IModEncounterAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the encounter asset profile.</para>
        ///     <para xml:lang="zh-CN">获取遭遇资源配置。</para>
        /// </summary>
        EncounterAssetProfile AssetProfile => EncounterAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the encounter combat <see cref="PackedScene" /> path override.</para>
        ///     <para xml:lang="zh-CN">获取遭遇战斗 <see cref="PackedScene" /> 路径覆盖。</para>
        /// </summary>
        string? CustomEncounterScenePath => AssetProfile.EncounterScenePath;

        /// <summary>
        ///     <para xml:lang="en">Gets the main combat-background scene-path override.</para>
        ///     <para xml:lang="zh-CN">获取主战斗背景场景路径覆盖。</para>
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the combat-background layer directory override. A missing value retains the base game's
        ///         per-encounter directory.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取战斗背景图层目录覆盖。未设置时保留原版游戏按遭遇划分的目录。
        ///     </para>
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the boss map-node path override.</para>
        ///     <para xml:lang="zh-CN">获取首领地图节点路径覆盖。</para>
        /// </summary>
        string? CustomBossNodePath => AssetProfile.BossNodeSpinePath;

        /// <summary>
        ///     <para xml:lang="en">Gets additional asset paths to include in preload enumeration.</para>
        ///     <para xml:lang="zh-CN">获取要加入预加载枚举的额外资源路径。</para>
        /// </summary>
        IEnumerable<string>? CustomExtraAssetPaths => AssetProfile.ExtraAssetPaths;

        /// <summary>
        ///     <para xml:lang="en">Gets a replacement map-node asset-path enumeration.</para>
        ///     <para xml:lang="zh-CN">获取替换用的地图节点资源路径枚举。</para>
        /// </summary>
        IEnumerable<string>? CustomMapNodeAssetPaths => AssetProfile.MapNodeAssetPaths;

        /// <summary>
        ///     <para xml:lang="en">Gets the run-history icon-path override.</para>
        ///     <para xml:lang="zh-CN">获取游戏历史图标路径覆盖。</para>
        /// </summary>
        string? CustomRunHistoryIconPath => AssetProfile.RunHistoryIconPath;

        /// <summary>
        ///     <para xml:lang="en">Gets the run-history outline-icon path override.</para>
        ///     <para xml:lang="zh-CN">获取游戏历史轮廓图标路径覆盖。</para>
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AssetProfile.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     <para xml:lang="en">Makes <see cref="EncounterModel.CreateScene" /> instantiate custom encounter scenes.</para>
    ///     <para xml:lang="zh-CN">使 <see cref="EncounterModel.CreateScene" /> 实例化自定义遭遇场景。</para>
    /// </summary>
    internal class EncounterCreateScenePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_encounter_create_scene";
        public static string Description => "Allow mod encounters to override CreateScene packed scene path";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), nameof(EncounterModel.CreateScene))];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries the registered scene path and then the model-provided path; if neither can be instantiated,
        ///         runs the base-game implementation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         依次尝试已注册场景路径和模型提供的路径；均无法实例化时运行原版游戏实现。
        ///     </para>
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref Control __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetEncounterScenePath(__instance, out var externalPath) &&
                ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                    __instance,
                    externalPath,
                    "ExternalAssetOverrideRegistry.EncounterScenePath",
                    out __result))
                return false;

            var path = (__instance as IModEncounterAssetOverrides)?.CustomEncounterScenePath;
            return string.IsNullOrWhiteSpace(path) ||
                   !ContentAssetOverridePatchHelper.TryInstantiatePackedScenePathOverride(
                       __instance,
                       path,
                       nameof(IModEncounterAssetOverrides.CustomEncounterScenePath),
                       out __result);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <c>EncounterModel.CreateBackgroundAssetsForCustom</c> honor custom background scenes, layer
    ///         directories, and programmatic backgrounds.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <c>EncounterModel.CreateBackgroundAssetsForCustom</c> 识别自定义背景场景、图层目录和程序化背景。
    ///     </para>
    /// </summary>
    internal class EncounterCreateBackgroundAssetsForCustomPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_encounter_create_background_assets_custom";

        public static string Description =>
            "Allow mod encounters to customize BackgroundAssets (path-based or programmatic via ModEncounterTemplate)";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EncounterModel), "CreateBackgroundAssetsForCustom", [typeof(Rng)]),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries path-based overrides first, then a prepared programmatic background, and finally the base-game
        ///         implementation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         依次尝试基于路径的覆盖、预先准备的程序化背景和原版游戏实现。
        ///     </para>
        /// </summary>
        public static bool Prefix(EncounterModel __instance, Rng rng, ref BackgroundAssets __result)
        {
            var overrides = __instance as IModEncounterAssetOverrides;
            var hasExternalLayers = ExternalAssetOverrideRegistry.TryGetEncounterBackgroundLayersDirectory(__instance,
                out var externalLayersDirectory);
            var hasExternalBackground = ExternalAssetOverrideRegistry.TryGetEncounterBackgroundScenePath(__instance,
                out var externalBackgroundPath);

            if (overrides != null || hasExternalLayers || hasExternalBackground)
            {
                var customLayers = hasExternalLayers
                    ? externalLayersDirectory
                    : overrides?.CustomBackgroundLayersDirectoryPath;
                var customMain = hasExternalBackground ? externalBackgroundPath : overrides?.CustomBackgroundScenePath;
                if (!string.IsNullOrWhiteSpace(customLayers) || !string.IsNullOrWhiteSpace(customMain))
                {
                    var id = __instance.Id.Entry.ToLowerInvariant();
                    var layersDir = string.IsNullOrWhiteSpace(customLayers)
                        ? $"res://scenes/backgrounds/{id}/layers"
                        : customLayers.TrimEnd('/');
                    var mainBg = string.IsNullOrWhiteSpace(customMain)
                        ? SceneHelper.GetScenePath($"backgrounds/{id}/{id}_background")
                        : customMain;

                    try
                    {
                        __result = ActBackgroundLayersFactory.CreateFromCustomLayersDirectory(layersDir, mainBg, rng);
                        if (__instance is ModEncounterTemplate pathTemplate)
                            pathTemplate.AbandonProgrammaticCombatBackgroundSlot();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Assets] Mod encounter '{__instance.Id.Entry}' custom BackgroundAssets failed ({ex.GetType().Name}: {ex.Message}). " +
                            "Trying programmatic or vanilla encounter background.");
                    }
                }
            }

            if (__instance is not ModEncounterTemplate template) return true;
            var slot = template.ConsumeProgrammaticCombatBackgroundSlot();
            if (slot != null)
            {
                __result = slot;
                return false;
            }

            if (template.UsesProgrammaticCombatBackground)
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Mod encounter '{__instance.Id.Entry}' has UseProgrammaticCombatBackground but " +
                    "BuildProgrammaticCombatBackground returned null; using vanilla per-encounter background layout.");

            return true;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Applies custom boss map-node paths to <see cref="EncounterModel.BossNodePath" />.</para>
    ///     <para xml:lang="zh-CN">将自定义首领地图节点路径应用到 <see cref="EncounterModel.BossNodePath" />。</para>
    /// </summary>
    internal class EncounterBossNodePathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_encounter_boss_node_path";
        public static string Description => "Allow mod encounters to override BossNodePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "BossNodePath", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the first available boss map-node path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的首领地图节点路径覆盖。</para>
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref string __result)
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetEncounterBossNodePath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.EncounterBossNodePath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEncounterAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBossNodePath,
                nameof(IModEncounterAssetOverrides.CustomBossNodePath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Replaces <see cref="EncounterModel.MapNodeAssetPaths" /> when a custom path enumeration contains
    ///         available resources.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         自定义路径枚举包含可用资源时，替换 <see cref="EncounterModel.MapNodeAssetPaths" />。
    ///     </para>
    /// </summary>
    internal class EncounterMapNodeAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_encounter_map_node_asset_paths";
        public static string Description => "Allow mod encounters to override MapNodeAssetPaths";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "MapNodeAssetPaths", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies available paths from the registered enumeration, then the model-provided enumeration;
        ///         if neither contains an available resource, retains the base-game enumeration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         依次应用已注册枚举和模型提供枚举中的可用路径；两者均不含可用资源时保留原版游戏枚举。
        ///     </para>
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref IEnumerable<string> __result)
        {
            if (ExternalAssetOverrideRegistry.TryGetEncounterMapNodeAssetPaths(__instance, out var externalRaw) &&
                TryCollectExistingPaths(
                    externalRaw,
                    "ExternalAssetOverrideRegistry.EncounterMapNodeAssetPaths",
                    out __result))
                return false;

            return !TryCollectExistingPaths(
                (__instance as IModEncounterAssetOverrides)?.CustomMapNodeAssetPaths,
                nameof(IModEncounterAssetOverrides.CustomMapNodeAssetPaths),
                out __result);

            bool TryCollectExistingPaths(
                IEnumerable<string>? raw,
                string memberLabel,
                out IEnumerable<string> paths)
            {
                paths = [];
                if (raw == null)
                    return false;

                var pathTuples = raw
                    .Where(static path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => ((string?)path, memberLabel))
                    .ToArray();
                if (pathTuples.Length == 0)
                    return false;

                var existing = AssetPathDiagnostics.CollectExistingPaths(__instance, pathTuples);
                if (existing.Length == 0)
                    return false;

                paths = existing;
                return true;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Merges custom encounter paths into <see cref="EncounterModel.GetAssetPaths" /> for preloading.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将自定义遭遇路径合并到 <see cref="EncounterModel.GetAssetPaths" />，以供预加载。
    ///     </para>
    /// </summary>
    internal class EncounterGetAssetPathsPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_encounter_get_asset_paths";

        public static string Description =>
            "Merge mod encounter scene, extras, and layer scenes into GetAssetPaths; omit synthetic encounters/<modId> preload when using borrowed or factory scenes";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), nameof(EncounterModel.GetAssetPaths))];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds the encounter scene, extra paths, background scene, and layer-directory scenes to the preload
        ///         enumeration.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将遭遇场景、额外路径、背景场景和图层目录中的场景添加到预加载枚举。
        ///     </para>
        /// </summary>
        public static void Postfix(EncounterModel __instance, IRunState runState, ref IEnumerable<string> __result)
        {
            _ = runState;
            var overrides = __instance as IModEncounterAssetOverrides;
            var externalSceneOk =
                ExternalAssetOverrideRegistry.TryGetEncounterScenePath(__instance, out var externalScenePath)
                && ResourceLoader.Exists(externalScenePath);
            var externalLayersOk = ExternalAssetOverrideRegistry.TryGetEncounterBackgroundLayersDirectory(__instance,
                out var externalLayersDirectory);
            var externalBackgroundOk = ExternalAssetOverrideRegistry.TryGetEncounterBackgroundScenePath(__instance,
                out var externalBackgroundPath) && ResourceLoader.Exists(externalBackgroundPath);
            if (overrides == null &&
                !externalSceneOk &&
                !externalLayersOk &&
                !externalBackgroundOk)
                return;

            var extras = CollectEncounterExtraAssetPaths(__instance, overrides,
                externalSceneOk ? externalScenePath : null,
                externalLayersOk ? externalLayersDirectory : null,
                externalBackgroundOk ? externalBackgroundPath : null);

            var syntheticEncounterScene =
                SceneHelper.GetScenePath($"encounters/{__instance.Id.Entry.ToLowerInvariant()}");
            var customScene = externalSceneOk ? externalScenePath : overrides?.CustomEncounterScenePath;
            var customSceneOk = !string.IsNullOrWhiteSpace(customScene) && ResourceLoader.Exists(customScene);
            var factoryOnly =
                (__instance as IModEncounterCombatSceneFactory)?.SuppliesEncounterCombatSceneFromFactory == true;
            if ((customSceneOk && !ResPathEquals(syntheticEncounterScene, customScene!)) || factoryOnly)
                __result = [.. __result.Where(p => !ResPathEquals(p, syntheticEncounterScene))];

            if (extras.Count == 0)
                return;

            __result = __result.Concat(extras);
        }

        private static bool ResPathEquals(string a, string b)
        {
            return string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> CollectEncounterExtraAssetPaths(
            EncounterModel instance,
            IModEncounterAssetOverrides? overrides,
            string? externalScenePath,
            string? externalLayersDirectory,
            string? externalBackgroundPath)
        {
            var extras = new List<string>();

            var scenePath = externalScenePath ?? overrides?.CustomEncounterScenePath;
            if (!string.IsNullOrWhiteSpace(scenePath) &&
                AssetPathDiagnostics.Exists(scenePath, instance,
                    nameof(IModEncounterAssetOverrides.CustomEncounterScenePath)))
                extras.Add(scenePath);

            var more = overrides?.CustomExtraAssetPaths;
            if (more != null)
                extras.AddRange(more.Where(p => !string.IsNullOrWhiteSpace(p)).Where(p =>
                    AssetPathDiagnostics.Exists(p, instance,
                        nameof(IModEncounterAssetOverrides.CustomExtraAssetPaths))));

            var layersDir = externalLayersDirectory ?? overrides?.CustomBackgroundLayersDirectoryPath;
            if (!string.IsNullOrWhiteSpace(layersDir))
            {
                var normalized = layersDir.TrimEnd('/');
                using var da = DirAccess.Open(normalized);
                if (da != null)
                {
                    da.ListDirBegin();
                    for (var n = da.GetNext(); n != ""; n = da.GetNext())
                    {
                        if (da.CurrentIsDir())
                            continue;
                        if (n.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                            extras.Add(normalized + "/" + n);
                    }
                }
            }

            var backgroundPath = externalBackgroundPath ?? overrides?.CustomBackgroundScenePath;
            if (!string.IsNullOrWhiteSpace(backgroundPath) &&
                AssetPathDiagnostics.Exists(backgroundPath, instance,
                    nameof(IModEncounterAssetOverrides.CustomBackgroundScenePath)))
                extras.Add(backgroundPath);

            return extras;
        }
    }
}
