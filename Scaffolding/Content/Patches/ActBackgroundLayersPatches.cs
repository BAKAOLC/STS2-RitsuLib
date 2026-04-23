using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Patches <see cref="ActModel.GenerateBackgroundAssets" /> so mod acts can use a custom <c>res://</c> layers folder.
    /// </summary>
    public class ActGenerateBackgroundAssetsPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_act_generate_background_assets";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod acts to build BackgroundAssets from a custom layers directory";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     When <see cref="IModActAssetOverrides.CustomBackgroundLayersDirectoryPath" /> is set and valid, builds layers
        ///     from that directory; main scene uses <see cref="ActModel.BackgroundScenePath" /> (including existing path patches).
        /// </summary>
        public static bool Prefix(ActModel __instance, Rng rng, ref BackgroundAssets __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModActAssetOverrides overrides)
                return true;

            var dir = overrides.CustomBackgroundLayersDirectoryPath;
            if (string.IsNullOrWhiteSpace(dir))
                return true;

            var normalized = dir.TrimEnd('/');
            using (var open = DirAccess.Open(normalized))
            {
                if (open == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[Assets] Mod act '{__instance.Id.Entry}' CustomBackgroundLayersDirectoryPath does not open: '{normalized}'. " +
                        "Falling back to vanilla layers path.");
                    return true;
                }
            }

            try
            {
                __result = ActBackgroundLayersFactory.CreateFromCustomLayersDirectory(
                    normalized,
                    __instance.BackgroundScenePath,
                    rng);
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Assets] Mod act '{__instance.Id.Entry}' custom background layers failed ({ex.GetType().Name}: {ex.Message}). " +
                    "Falling back to vanilla layers path.");
                return true;
            }
        }
    }

    /// <summary>
    ///     Appends all <c>.tscn</c> paths under the custom layers directory to <see cref="ActModel.AssetPaths" /> for preload.
    /// </summary>
    public class ActAssetPathsBackgroundLayersPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_act_asset_paths_background_layers";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Include custom act layer scenes in ActModel.AssetPaths";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ActModel), "AssetPaths", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Concatenates every layer scene path under the configured directory.
        /// </summary>
        public static void Postfix(ActModel __instance, ref IEnumerable<string> __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModActAssetOverrides overrides)
                return;

            var dir = overrides.CustomBackgroundLayersDirectoryPath;
            if (string.IsNullOrWhiteSpace(dir))
                return;

            var normalized = dir.TrimEnd('/');
            using var da = DirAccess.Open(normalized);
            if (da == null)
                return;

            var extras = new List<string>();
            da.ListDirBegin();
            for (var n = da.GetNext(); n != ""; n = da.GetNext())
            {
                if (da.CurrentIsDir())
                    continue;
                if (n.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                    extras.Add(normalized + "/" + n);
            }

            if (extras.Count == 0)
                return;

            __result = __result.Concat(extras);
        }
    }
}
