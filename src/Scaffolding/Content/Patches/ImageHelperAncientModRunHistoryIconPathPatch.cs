using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes direct <see cref="ImageHelper.GetRoomIconPath" /> and
    ///         <see cref="ImageHelper.GetRoomIconOutlinePath" /> calls honor Ancient-event run-history icon overrides.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使直接调用 <see cref="ImageHelper.GetRoomIconPath" /> 和
    ///         <see cref="ImageHelper.GetRoomIconOutlinePath" /> 时识别先古事件的游戏历史图标覆盖。
    ///     </para>
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class ImageHelperAncientModRunHistoryIconPathPatch : IPatchMethod
    {
        public static string PatchId => "image_helper_ancient_mod_run_history_icon_path";

        public static string Description =>
            "Route Ancient+Event run-history icon paths through IModAncientEventAssetOverrides when resources exist";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath)),
                new(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath)),
            ];
        }

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
            if (ancient == null)
                return true;

            var path = __originalMethod.Name switch
            {
                nameof(ImageHelper.GetRoomIconPath) => ResolveMainIconPath(ancient),
                nameof(ImageHelper.GetRoomIconOutlinePath) => ResolveOutlineIconPath(ancient),
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

        private static string? ResolveMainIconPath(AncientEventModel ancient)
        {
            if (ExternalAssetOverrideRegistry.TryGetAncientRunHistoryIconPath(ancient, out var externalPath) &&
                AssetPathDiagnostics.Exists(
                    externalPath,
                    ancient,
                    "ExternalAssetOverrideRegistry.AncientRunHistoryIconPath"))
                return externalPath;

            return (ancient as IModAncientEventAssetOverrides)?.CustomRunHistoryIconPath;
        }

        private static string? ResolveOutlineIconPath(AncientEventModel ancient)
        {
            if (ExternalAssetOverrideRegistry.TryGetAncientRunHistoryIconOutlinePath(
                    ancient,
                    out var externalPath) &&
                AssetPathDiagnostics.Exists(
                    externalPath,
                    ancient,
                    "ExternalAssetOverrideRegistry.AncientRunHistoryIconOutlinePath"))
                return externalPath;

            return (ancient as IModAncientEventAssetOverrides)?.CustomRunHistoryIconOutlinePath;
        }
    }
}
