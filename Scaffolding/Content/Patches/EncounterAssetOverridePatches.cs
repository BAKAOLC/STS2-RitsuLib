using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
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
    ///     Optional encounter presentation and preload paths; use <see cref="ModEncounterTemplate" /> or implement on a mod
    ///     <see cref="EncounterModel" />.
    /// </summary>
    public interface IModEncounterAssetOverrides
    {
        /// <summary>
        ///     Path bundle; <c>Custom*</c> properties mirror these fields unless overridden.
        /// </summary>
        EncounterAssetProfile AssetProfile => EncounterAssetProfile.Empty;

        /// <summary>
        ///     Override packed scene for <c>EncounterModel.CreateScene</c>.
        /// </summary>
        string? CustomEncounterScenePath => AssetProfile.EncounterScenePath;

        /// <summary>
        ///     Override main combat background scene when building <see cref="BackgroundAssets" /> for this encounter.
        /// </summary>
        string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <summary>
        ///     Override layers directory (<c>_bg_</c> / <c>_fg_</c>); when null, vanilla per-id folder is used with custom main
        ///     scene if set.
        /// </summary>
        string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;

        /// <summary>
        ///     Override <c>EncounterModel.BossNodePath</c> (Spine <c>.tres</c> or base path used for map node art).
        /// </summary>
        string? CustomBossNodePath => AssetProfile.BossNodeSpinePath;

        /// <summary>
        ///     Extra paths merged into <c>GetAssetPaths</c> for preloading.
        /// </summary>
        IEnumerable<string>? CustomExtraAssetPaths => AssetProfile.ExtraAssetPaths;

        /// <summary>
        ///     When non-null and non-empty after filtering to existing resources, replaces <c>MapNodeAssetPaths</c>.
        /// </summary>
        IEnumerable<string>? CustomMapNodeAssetPaths => AssetProfile.MapNodeAssetPaths;

        /// <summary>
        ///     When set and the resource exists, overrides <see cref="ImageHelper.GetRoomIconPath" /> for this encounter id.
        /// </summary>
        string? CustomRunHistoryIconPath => AssetProfile.RunHistoryIconPath;

        /// <summary>
        ///     When set and the resource exists, overrides <see cref="ImageHelper.GetRoomIconOutlinePath" /> for this encounter
        ///     id.
        /// </summary>
        string? CustomRunHistoryIconOutlinePath => AssetProfile.RunHistoryIconOutlinePath;
    }

    /// <summary>
    ///     Patches <see cref="EncounterModel.CreateScene" /> for mod encounter scene path overrides.
    /// </summary>
    public class EncounterCreateScenePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_encounter_create_scene";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod encounters to override CreateScene packed scene path";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), nameof(EncounterModel.CreateScene))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Instantiates <see cref="IModEncounterAssetOverrides.CustomEncounterScenePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref Control __result)
            // ReSharper restore InconsistentNaming
        {
            string? path;
            if (ExternalAssetOverrideRegistry.TryGetEncounterScenePath(__instance, out var externalPath))
                path = externalPath;
            else if (__instance is IModEncounterAssetOverrides overrides)
                path = overrides.CustomEncounterScenePath;
            else
                return true;

            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, __instance,
                    nameof(IModEncounterAssetOverrides.CustomEncounterScenePath)))
                return true;

            __result = PreloadManager.Cache.GetScene(path).Instantiate<Control>();
            return false;
        }
    }

    /// <summary>
    ///     Patches <c>EncounterModel.CreateBackgroundAssetsForCustom</c> to honor mod background scene and/or layers
    ///     directory.
    /// </summary>
    public class EncounterCreateBackgroundAssetsForCustomPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_encounter_create_background_assets_custom";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod encounters to customize BackgroundAssets (path-based or programmatic via ModEncounterTemplate)";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EncounterModel), "CreateBackgroundAssetsForCustom", [typeof(Rng)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Path-based <see cref="ActBackgroundLayersFactory" /> when overrides supply paths; otherwise
        ///     <see cref="ModEncounterTemplate" /> programmatic slot from
        ///     <see cref="EncounterGetBackgroundAssetsProgrammaticPrepPatch" />.
        /// </summary>
        public static bool Prefix(EncounterModel __instance, Rng rng, ref BackgroundAssets __result)
            // ReSharper restore InconsistentNaming
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
    ///     Patches <see cref="EncounterModel.BossNodePath" /> for mod map node spine overrides.
    /// </summary>
    public class EncounterBossNodePathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_encounter_boss_node_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod encounters to override BossNodePath";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "BossNodePath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModEncounterAssetOverrides.CustomBossNodePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            // ReSharper disable once InvertIf
            if (ExternalAssetOverrideRegistry.TryGetEncounterBossNodePath(__instance, out var externalPath) &&
                AssetPathDiagnostics.Exists(externalPath, __instance,
                    "ExternalAssetOverrideRegistry.EncounterBossNodePath"))
            {
                __result = externalPath;
                return false;
            }

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModEncounterAssetOverrides>(
                __instance,
                ref __result,
                o => o.CustomBossNodePath,
                nameof(IModEncounterAssetOverrides.CustomBossNodePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="EncounterModel.MapNodeAssetPaths" /> when a mod supplies an explicit path list.
    /// </summary>
    public class EncounterMapNodeAssetPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_encounter_map_node_asset_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod encounters to override MapNodeAssetPaths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "MapNodeAssetPaths", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Replaces enumeration with existing resources from
        ///     <see cref="IModEncounterAssetOverrides.CustomMapNodeAssetPaths" />.
        /// </summary>
        public static bool Prefix(EncounterModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            var hasExternal =
                ExternalAssetOverrideRegistry.TryGetEncounterMapNodeAssetPaths(__instance, out var externalRaw);
            var raw = hasExternal
                ? externalRaw
                : (__instance as IModEncounterAssetOverrides)?.CustomMapNodeAssetPaths;
            if (raw == null)
                return true;

            var candidates = raw.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (candidates.Length == 0)
                return true;

            var pathTuples = candidates
                .Select(p => ((string?)p, nameof(IModEncounterAssetOverrides.CustomMapNodeAssetPaths)))
                .ToArray();
            var paths = AssetPathDiagnostics.CollectExistingPaths(__instance, pathTuples);
            if (paths.Length == 0)
                return true;

            __result = paths;
            return false;
        }
    }

    /// <summary>
    ///     Merges mod encounter paths into <see cref="EncounterModel.GetAssetPaths" /> for preloading.
    /// </summary>
    public class EncounterGetAssetPathsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_encounter_get_asset_paths";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Merge mod encounter scene, extras, and layer scenes into GetAssetPaths; omit synthetic encounters/<modId> preload when using borrowed or factory scenes";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), nameof(EncounterModel.GetAssetPaths))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Appends encounter scene override, extra paths, and all <c>.tscn</c> under the configured layers directory.
        /// </summary>
        public static void Postfix(EncounterModel __instance, IRunState runState, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
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

            var syntheticEncounterScene =
                SceneHelper.GetScenePath($"encounters/{__instance.Id.Entry.ToLowerInvariant()}");
            var customScene = externalSceneOk ? externalScenePath : overrides?.CustomEncounterScenePath;
            var customSceneOk = !string.IsNullOrWhiteSpace(customScene) && ResourceLoader.Exists(customScene);
            var factoryOnly =
                (__instance as IModEncounterCombatSceneFactory)?.SuppliesEncounterCombatSceneFromFactory == true;
            if ((customSceneOk && !ResPathEquals(syntheticEncounterScene, customScene!)) || factoryOnly)
                __result = __result.Where(p => !ResPathEquals(p, syntheticEncounterScene)).ToList();

            var extras = new List<string>();

            var scenePath = externalSceneOk ? externalScenePath : overrides?.CustomEncounterScenePath;
            if (!string.IsNullOrWhiteSpace(scenePath) &&
                AssetPathDiagnostics.Exists(scenePath, __instance,
                    nameof(IModEncounterAssetOverrides.CustomEncounterScenePath)))
                extras.Add(scenePath);

            var more = overrides?.CustomExtraAssetPaths;
            if (more != null)
                extras.AddRange(more.Where(p => !string.IsNullOrWhiteSpace(p)).Where(p =>
                    AssetPathDiagnostics.Exists(p, __instance,
                        nameof(IModEncounterAssetOverrides.CustomExtraAssetPaths))));

            var layersDir = externalLayersOk ? externalLayersDirectory : overrides?.CustomBackgroundLayersDirectoryPath;
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

            var backgroundPath = externalBackgroundOk ? externalBackgroundPath : overrides?.CustomBackgroundScenePath;
            if (!string.IsNullOrWhiteSpace(backgroundPath) &&
                AssetPathDiagnostics.Exists(backgroundPath, __instance,
                    nameof(IModEncounterAssetOverrides.CustomBackgroundScenePath)))
                extras.Add(backgroundPath);

            if (extras.Count == 0)
                return;

            __result = __result.Concat(extras);
        }

        private static bool ResPathEquals(string a, string b)
        {
            return string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
    }
}
