using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Vanilla run-history rows call <see cref="ImageHelper.GetRoomIconPath" /> /
    ///     <see cref="ImageHelper.GetRoomIconOutlinePath" />
    ///     directly, bypassing <see cref="AncientEventModel.RunHistoryIcon" />. This prefix returns mod
    ///     <see cref="IModAncientEventAssetOverrides" /> paths at resolve time so the first load uses the correct textures
    ///     (no post-load replacement on <c>NMapPointHistoryEntry</c>).
    /// </summary>
    public class ImageHelperAncientModRunHistoryIconPathPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "image_helper_ancient_mod_run_history_icon_path";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Route Ancient+Event run-history icon paths through IModAncientEventAssetOverrides when resources exist";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath)),
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Supplies mod run-history texture paths before vanilla builds <c>ui/run_history/&lt;entry&gt;.png</c> paths.
        /// </summary>
        public static bool Prefix(
            MethodBase __originalMethod,
            MapPointType mapPointType,
            RoomType roomType,
            ModelId? modelId,
            ref string? __result)
        {
            if (mapPointType != MapPointType.Ancient || roomType != RoomType.Event || modelId is null)
                return true;

            var ancient = ModelDb.GetByIdOrNull<AncientEventModel>(modelId);
            if (ancient is not IModAncientEventAssetOverrides overrides)
                return true;

            var path = __originalMethod.Name switch
            {
                nameof(ImageHelper.GetRoomIconPath) => overrides.CustomRunHistoryIconPath,
                nameof(ImageHelper.GetRoomIconOutlinePath) => overrides.CustomRunHistoryIconOutlinePath,
                _ => null,
            };

            var memberLabel = __originalMethod.Name == nameof(ImageHelper.GetRoomIconPath)
                ? nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconPath)
                : nameof(IModAncientEventAssetOverrides.CustomRunHistoryIconOutlinePath);

            if (string.IsNullOrWhiteSpace(path) || !AssetPathDiagnostics.Exists(path, ancient, memberLabel))
                return true;

            __result = path;
            return false;
        }
    }
}
